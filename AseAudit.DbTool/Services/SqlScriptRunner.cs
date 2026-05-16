namespace AseAudit.DbTool.Services;

public sealed class SqlScriptRunner
{
    private readonly ISqlServerConnector _conn;

    public SqlScriptRunner(ISqlServerConnector conn) => _conn = conn;

    public void RunFile(string filePath, string connectionString)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"找不到 SQL 腳本：{filePath}");

        var script = File.ReadAllText(filePath);
        foreach (var batch in SplitOnGo(script))
        {
            var trimmed = batch.Trim();
            if (trimmed.Length == 0) continue;
            _conn.ExecuteNonQuery(connectionString, trimmed);
        }
    }

    private static IEnumerable<string> SplitOnGo(string script)
    {
        var lines = script.Split('\n');
        var buf = new System.Text.StringBuilder();
        foreach (var raw in lines)
        {
            var line = raw.TrimEnd('\r');
            if (string.Equals(line.Trim(), "GO", StringComparison.OrdinalIgnoreCase))
            {
                yield return buf.ToString();
                buf.Clear();
            }
            else
            {
                buf.AppendLine(line);
            }
        }
        if (buf.Length > 0) yield return buf.ToString();
    }
}
