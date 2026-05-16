using System.Text.Json;
using AseAudit.DbTool.Commands;
using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using Spectre.Console;

namespace AseAudit.DbTool.Tui;

public sealed class InteractiveMenu
{
    private readonly ManifestFile _manifest;
    private readonly string _scriptRootAbs;
    private readonly string _auditDbCs;
    private readonly string _serverInstance;
    private readonly BackupArchive _archive;
    private readonly BackupCommand _backupCmd;
    private readonly ResetCommand _resetCmd;
    private readonly RestoreCommand _restoreCmd;
    private readonly InitCommand _initCmd;
    private readonly string _masterCs;
    private readonly int _autoBackupRetention;

    public InteractiveMenu(
        ManifestFile manifest,
        string scriptRootAbs,
        string auditDbCs,
        string masterCs,
        string serverInstance,
        int autoBackupRetention,
        BackupArchive archive,
        InitCommand initCmd,
        BackupCommand backupCmd,
        ResetCommand resetCmd,
        RestoreCommand restoreCmd)
    {
        _manifest = manifest;
        _scriptRootAbs = scriptRootAbs;
        _auditDbCs = auditDbCs;
        _masterCs = masterCs;
        _serverInstance = serverInstance;
        _autoBackupRetention = autoBackupRetention;
        _archive = archive;
        _initCmd = initCmd;
        _backupCmd = backupCmd;
        _resetCmd = resetCmd;
        _restoreCmd = restoreCmd;
    }

    public int RunDeployMode()
    {
        Header("ASE Audit 資料庫初始化工具", "[yellow]偵測到資料庫尚未初始化[/]");
        var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("請選擇")
            .AddChoices("初始化資料庫", "結束"));

        if (choice == "結束") return ExitCodes.UserCancelled;
        return _initCmd.Execute(_masterCs, _auditDbCs, _manifest.Database, _manifest, _scriptRootAbs);
    }

    public int RunDevMode()
    {
        Header("ASE Audit DbTool（開發者模式）",
            $"[grey]連線：{Markup.Escape(_serverInstance)} / {Markup.Escape(_manifest.Database)}[/]");

        while (true)
        {
            var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("請選擇操作")
                .AddChoices("備份資料表", "重建資料庫", "還原備份", "檢視備份歷史", "結束"));

            switch (choice)
            {
                case "備份資料表":
                    InteractiveBackup();
                    break;
                case "重建資料庫":
                    _resetCmd.Execute(_manifest, _scriptRootAbs, _auditDbCs, _serverInstance, _autoBackupRetention);
                    break;
                case "還原備份":
                    InteractiveRestore();
                    break;
                case "檢視備份歷史":
                    ShowHistory();
                    break;
                case "結束":
                    return ExitCodes.Success;
            }
            AnsiConsole.WriteLine();
        }
    }

    private void InteractiveBackup()
    {
        var backupable = _manifest.Tables.Where(t => t.Backupable).OrderBy(t => t.LoadOrder).ToList();
        var picked = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
            .Title("勾選要備份的資料表（[grey]空白鍵勾選、Enter 確認[/]）")
            .NotRequired()
            .PageSize(15)
            .AddChoices(backupable.Select(t => t.Name)));

        if (picked.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]未選擇任何表，取消[/]");
            return;
        }

        var folder = _archive.CreateUserBackupFolder(DateTime.Now);
        var tables = backupable.Where(t => picked.Contains(t.Name)).ToList();
        _backupCmd.Execute(tables, folder, _manifest.Database, _auditDbCs, _serverInstance);
    }

    private void InteractiveRestore()
    {
        var folders = _archive.ListUserBackupFolders();
        if (folders.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]尚無使用者備份，請先執行備份[/]");
            return;
        }

        var pick = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("選擇要還原的備份")
            .PageSize(15)
            .AddChoices(folders.Select(f => Path.GetFileName(f) ?? f)));

        var folder = folders.First(f => Path.GetFileName(f) == pick);
        var manifestJsonPath = Path.Combine(folder, "manifest.json");

        var tableNamesInBackup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (File.Exists(manifestJsonPath))
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestJsonPath));
            foreach (var n in doc.RootElement.GetProperty("tables").EnumerateArray())
                tableNamesInBackup.Add(n.GetString()!);
        }
        else
        {
            foreach (var f in Directory.EnumerateFiles(folder, "*.dat"))
                tableNamesInBackup.Add(Path.GetFileNameWithoutExtension(f));
        }

        var orderedTables = _manifest.Tables
            .Where(t => tableNamesInBackup.Contains(t.Name))
            .OrderBy(t => t.LoadOrder)
            .ToList();

        var picked = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
            .Title("勾選要還原的表")
            .NotRequired()
            .PageSize(15)
            .AddChoices(orderedTables.Select(t => t.Name))
            .UseConverter(name => name));
        var subset = orderedTables.Where(t => picked.Contains(t.Name)).ToList();

        if (subset.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]未選擇任何表，取消[/]");
            return;
        }

        _restoreCmd.Execute(folder, subset, _manifest.Database, _serverInstance);
    }

    private void ShowHistory()
    {
        var table = new Table().AddColumn("時間戳").AddColumn("類型").AddColumn("路徑");
        foreach (var d in _archive.ListAllBackupFolders())
        {
            var name = Path.GetFileName(d) ?? "";
            var type = name.StartsWith("_autoBackup-") ? "auto" : "user";
            table.AddRow(Markup.Escape(name), type, Markup.Escape(d));
        }
        AnsiConsole.Write(table);
    }

    private static void Header(string title, string subtitle)
    {
        AnsiConsole.Write(new Rule($"[yellow]{Markup.Escape(title)}[/]").RuleStyle("grey"));
        AnsiConsole.MarkupLine(subtitle);
        AnsiConsole.WriteLine();
    }
}
