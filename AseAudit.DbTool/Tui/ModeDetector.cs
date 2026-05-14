using AseAudit.DbTool.Services;

namespace AseAudit.DbTool.Tui;

public enum Mode
{
    NoConnection,
    Deploy,
    Dev
}

public sealed class ModeDetector
{
    private readonly ISqlServerConnector _conn;

    public ModeDetector(ISqlServerConnector conn) => _conn = conn;

    public Mode Detect(
        string masterConnectionString,
        string auditDbConnectionString,
        string databaseName,
        IReadOnlyCollection<string> manifestTableNames)
    {
        if (!_conn.CanConnect(masterConnectionString))
            return Mode.NoConnection;

        if (!_conn.DatabaseExists(masterConnectionString, databaseName))
            return Mode.Deploy;

        return _conn.AnyTableExists(auditDbConnectionString, manifestTableNames)
            ? Mode.Dev
            : Mode.Deploy;
    }
}
