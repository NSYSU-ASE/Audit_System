using System.Text.Json;
using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using Spectre.Console;

namespace AseAudit.DbTool.Commands;

public sealed class BackupCommand
{
    private readonly BcpRunner _bcp;
    private readonly ISqlServerConnector _conn;

    public BackupCommand(BcpRunner bcp, ISqlServerConnector conn)
    {
        _bcp = bcp;
        _conn = conn;
    }

    public int Execute(
        IReadOnlyList<TableEntry> tablesToBackup,
        string targetFolderPath,
        string databaseName,
        string auditDbConnectionString,
        string serverInstance)
    {
        if (tablesToBackup.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]未選擇任何表，取消備份[/]");
            return ExitCodes.UserCancelled;
        }

        var rowCounts = new Dictionary<string, int>();

        foreach (var t in tablesToBackup)
        {
            var datPath = Path.Combine(targetFolderPath, $"{t.Name}.dat");
            var result = _bcp.ExportTable(databaseName, t.Name, datPath, serverInstance);

            if (result.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]✗ {t.Name} 備份失敗（bcp exit={result.ExitCode}）[/]");
                AnsiConsole.WriteLine(result.StdErr);
                return ExitCodes.GeneralError;
            }

            var count = _conn.GetTableRowCount(auditDbConnectionString, t.Name);
            rowCounts[t.Name] = count;
            AnsiConsole.MarkupLine($"  [green]✓[/] {t.Name} ({count} 列)");
        }

        var manifestJson = JsonSerializer.Serialize(new
        {
            createdAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            database = databaseName,
            tables = tablesToBackup.Select(t => t.Name).ToArray(),
            rowCounts
        }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(targetFolderPath, "manifest.json"), manifestJson);

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine($"[green]✓ 備份完成：[/] {Markup.Escape(targetFolderPath)}");
        return ExitCodes.Success;
    }
}
