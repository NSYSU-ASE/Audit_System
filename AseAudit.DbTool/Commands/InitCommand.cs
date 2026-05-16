using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using Spectre.Console;

namespace AseAudit.DbTool.Commands;

public sealed class InitCommand
{
    private readonly ISqlServerConnector _conn;
    private readonly SqlScriptRunner _scriptRunner;

    public InitCommand(ISqlServerConnector conn, SqlScriptRunner scriptRunner)
    {
        _conn = conn;
        _scriptRunner = scriptRunner;
    }

    public int Execute(
        string masterConnectionString,
        string auditDbConnectionString,
        string databaseName,
        ManifestFile manifest,
        string scriptRootAbsPath)
    {
        if (!_conn.DatabaseExists(masterConnectionString, databaseName))
        {
            AnsiConsole.Status().Start($"建立資料庫 {databaseName}...", _ =>
            {
                _conn.CreateDatabase(masterConnectionString, databaseName);
            });
            AnsiConsole.MarkupLine($"[green]✓[/] 已建立資料庫 [yellow]{databaseName}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[grey]·[/] 資料庫 [yellow]{databaseName}[/] 已存在，僅建表");
        }

        var ordered = manifest.Tables.OrderBy(t => t.LoadOrder).ToList();
        AnsiConsole.MarkupLine($"建立資料表（共 {ordered.Count} 張）...");

        foreach (var t in ordered)
        {
            var scriptPath = Path.Combine(scriptRootAbsPath, t.CreateScript);
            try
            {
                _scriptRunner.RunFile(scriptPath, auditDbConnectionString);
                AnsiConsole.MarkupLine($"  [green]✓[/] {t.Name}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [red]✗ {t.Name} 失敗：{Markup.Escape(ex.Message)}[/]");
                return ExitCodes.GeneralError;
            }
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[green]✓ 初始化完成。[/]");
        return ExitCodes.Success;
    }
}
