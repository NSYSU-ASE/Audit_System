using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using Spectre.Console;

namespace AseAudit.DbTool.Commands;

public sealed class ResetCommand
{
    private readonly ISqlServerConnector _conn;
    private readonly BcpRunner _bcp;
    private readonly SqlScriptRunner _scriptRunner;
    private readonly BackupArchive _archive;
    private readonly BackupCommand _backup;

    public ResetCommand(
        ISqlServerConnector conn,
        BcpRunner bcp,
        SqlScriptRunner scriptRunner,
        BackupArchive archive,
        BackupCommand backup)
    {
        _conn = conn;
        _bcp = bcp;
        _scriptRunner = scriptRunner;
        _archive = archive;
        _backup = backup;
    }

    public int Execute(
        ManifestFile manifest,
        string scriptRootAbsPath,
        string auditDbConnectionString,
        string serverInstance,
        int autoBackupRetention)
    {
        AnsiConsole.MarkupLine("[red]⚠ 此操作將清空資料庫內所有 manifest 管理的表，無法復原[/]");
        AnsiConsole.MarkupLine($"請輸入資料庫名稱 [yellow]{manifest.Database}[/] 確認：");
        var typed = AnsiConsole.Ask<string>(">");

        if (!string.Equals(typed, manifest.Database, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine("[grey]輸入不符，已取消[/]");
            return ExitCodes.UserCancelled;
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[blue]→[/] 自動備份所有 backupable 表至 _autoBackup...");
        var autoFolder = _archive.CreateAutoBackupFolder(DateTime.Now);
        var backupTables = manifest.Tables.Where(t => t.Backupable).OrderBy(t => t.LoadOrder).ToList();
        var backupExit = _backup.Execute(backupTables, autoFolder, manifest.Database, auditDbConnectionString, serverInstance);
        if (backupExit != ExitCodes.Success)
        {
            AnsiConsole.MarkupLine("[red]自動備份失敗，中止 Reset[/]");
            return backupExit;
        }
        _archive.RotateAutoBackups(autoBackupRetention);

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[blue]→[/] 依 loadOrder 反向 DROP 所有 manifest 表...");
        foreach (var t in manifest.Tables.OrderByDescending(t => t.LoadOrder))
        {
            try
            {
                _conn.ExecuteNonQuery(auditDbConnectionString,
                    $"IF OBJECT_ID(N'[dbo].[{t.Name}]', N'U') IS NOT NULL DROP TABLE [dbo].[{t.Name}]");
                AnsiConsole.MarkupLine($"  [green]✓[/] DROP {t.Name}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]✗ DROP {t.Name} 失敗：{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine($"[grey]_autoBackup 已保留於 {Markup.Escape(autoFolder)}[/]");
                return ExitCodes.GeneralError;
            }
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[blue]→[/] 依 loadOrder 正向 CREATE...");
        foreach (var t in manifest.Tables.OrderBy(t => t.LoadOrder))
        {
            var path = Path.Combine(scriptRootAbsPath, t.CreateScript);
            try
            {
                _scriptRunner.RunFile(path, auditDbConnectionString);
                AnsiConsole.MarkupLine($"  [green]✓[/] CREATE {t.Name}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]✗ CREATE {t.Name} 失敗：{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.GeneralError;
            }
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[green]✓ 重建完成，所有表為空[/]");
        AnsiConsole.MarkupLine($"[grey]💾 _autoBackup：{Markup.Escape(autoFolder)}[/]");
        return ExitCodes.Success;
    }
}
