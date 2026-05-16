using AseAudit.DbTool;
using AseAudit.DbTool.Commands;
using AseAudit.DbTool.Manifest;
using AseAudit.DbTool.Services;
using AseAudit.DbTool.Tui;
using Microsoft.Extensions.Configuration;
using Spectre.Console;

try
{
    var config = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .Build();

    var auditDbCs = config.GetConnectionString("AuditDb")
        ?? throw new InvalidOperationException("缺少 ConnectionStrings:AuditDb");
    var masterCs = config.GetConnectionString("Master")
        ?? throw new InvalidOperationException("缺少 ConnectionStrings:Master");
    var dbName = config["DbTool:DatabaseName"] ?? "Ase_Audit";
    var backupRoot = config["DbTool:BackupRootPath"] ?? "db-backups";
    var retention = int.Parse(config["DbTool:AutoBackupRetentionCount"] ?? "5");
    var serverInstance = @"(localdb)\MSSQLLocalDB";

    var manifestPath = Path.Combine(AppContext.BaseDirectory, "schema-manifest.json");
    ManifestFile manifest;
    try
    {
        manifest = new ManifestLoader().Load(manifestPath);
    }
    catch (ManifestException ex)
    {
        AnsiConsole.MarkupLine($"[red]manifest 載入失敗：{Markup.Escape(ex.Message)}[/]");
        return ExitCodes.ConfigInvalid;
    }

    var manifestDir = Path.GetDirectoryName(manifestPath)!;
    var scriptRootAbs = Path.GetFullPath(Path.Combine(manifestDir, manifest.ScriptRoot));

    var connector = new SqlServerConnector();
    var bcp = new BcpRunner();
    if (!bcp.IsBcpAvailable())
    {
        AnsiConsole.MarkupLine("[red]找不到 bcp.exe[/]");
        AnsiConsole.MarkupLine("請安裝 SQL Server Command Line Utilities：");
        AnsiConsole.MarkupLine("https://learn.microsoft.com/sql/tools/sqlcmd-utility");
        return ExitCodes.DependencyMissing;
    }

    var manifestTableNames = manifest.Tables.Select(t => t.Name).ToList();
    var detector = new ModeDetector(connector);
    var mode = detector.Detect(masterCs, auditDbCs, dbName, manifestTableNames);

    if (mode == Mode.NoConnection)
    {
        AnsiConsole.MarkupLine($"[red]無法連到 SQL Server LocalDB[/]");
        AnsiConsole.MarkupLine("請確認 LocalDB 已安裝並可用：sqllocaldb info");
        return ExitCodes.EnvironmentError;
    }

    var scriptRunner = new SqlScriptRunner(connector);
    var archive = new BackupArchive(Path.GetFullPath(backupRoot));
    var initCmd = new InitCommand(connector, scriptRunner);
    var backupCmd = new BackupCommand(bcp, connector);
    var restoreCmd = new RestoreCommand(bcp);
    var resetCmd = new ResetCommand(connector, bcp, scriptRunner, archive, backupCmd);

    var menu = new InteractiveMenu(
        manifest, scriptRootAbs, auditDbCs, masterCs, serverInstance, retention,
        archive, initCmd, backupCmd, resetCmd, restoreCmd);

    return mode == Mode.Deploy ? menu.RunDeployMode() : menu.RunDevMode();
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex);
    return ExitCodes.GeneralError;
}
