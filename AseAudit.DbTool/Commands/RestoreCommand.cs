using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using Spectre.Console;

namespace AseAudit.DbTool.Commands;

public sealed class RestoreCommand
{
    private readonly BcpRunner _bcp;

    public RestoreCommand(BcpRunner bcp) => _bcp = bcp;

    public int Execute(
        string backupFolderPath,
        IReadOnlyList<TableEntry> tablesToRestoreOrdered,
        string databaseName,
        string serverInstance)
    {
        foreach (var t in tablesToRestoreOrdered)
        {
            var datPath = Path.Combine(backupFolderPath, $"{t.Name}.dat");
            if (!File.Exists(datPath))
            {
                AnsiConsole.MarkupLine($"[red]✗ 找不到 {t.Name}.dat[/]");
                return ExitCodes.GeneralError;
            }

            var result = _bcp.ImportTable(databaseName, t.Name, datPath, serverInstance);
            if (result.ExitCode != 0)
            {
                AnsiConsole.MarkupLine($"[red]✗ {t.Name} 還原失敗（bcp exit={result.ExitCode}）[/]");
                AnsiConsole.WriteLine(result.StdErr);
                AnsiConsole.MarkupLine("[yellow]DB 處於部分還原狀態，請評估 Reset 後重試[/]");
                return ExitCodes.GeneralError;
            }
            AnsiConsole.MarkupLine($"  [green]✓[/] {t.Name}");
        }

        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine($"[green]✓ 還原完成（{tablesToRestoreOrdered.Count} 張表）[/]");
        return ExitCodes.Success;
    }
}
