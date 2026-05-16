using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace AseAudit.DbTool.Services;

public sealed class SqlServerConnector : ISqlServerConnector
{
    private static readonly Regex SafeIdentifier = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    public bool CanConnect(string connectionString)
    {
        try
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            return true;
        }
        catch (SqlException)
        {
            return false;
        }
    }

    public bool DatabaseExists(string masterConnectionString, string databaseName)
    {
        Validate(databaseName);
        using var conn = new SqlConnection(masterConnectionString);
        conn.Open();
        using var cmd = new SqlCommand(
            "SELECT 1 FROM sys.databases WHERE name = @name", conn);
        cmd.Parameters.AddWithValue("@name", databaseName);
        return cmd.ExecuteScalar() is not null;
    }

    public bool AnyTableExists(string auditDbConnectionString, IReadOnlyCollection<string> tableNames)
    {
        if (tableNames.Count == 0) return false;
        foreach (var t in tableNames) Validate(t);

        using var conn = new SqlConnection(auditDbConnectionString);
        conn.Open();
        var inClause = string.Join(", ", tableNames.Select((_, i) => $"@t{i}"));
        using var cmd = new SqlCommand(
            $"SELECT TOP 1 1 FROM sys.tables WHERE name IN ({inClause})", conn);
        int idx = 0;
        foreach (var t in tableNames) cmd.Parameters.AddWithValue($"@t{idx++}", t);
        return cmd.ExecuteScalar() is not null;
    }

    public void CreateDatabase(string masterConnectionString, string databaseName)
    {
        Validate(databaseName);
        using var conn = new SqlConnection(masterConnectionString);
        conn.Open();
        using var cmd = new SqlCommand($"CREATE DATABASE [{databaseName}]", conn);
        cmd.ExecuteNonQuery();
    }

    public void ExecuteNonQuery(string connectionString, string sql)
    {
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 120 };
        cmd.ExecuteNonQuery();
    }

    public int GetTableRowCount(string connectionString, string tableName)
    {
        Validate(tableName);
        using var conn = new SqlConnection(connectionString);
        conn.Open();
        using var cmd = new SqlCommand($"SELECT COUNT(*) FROM [dbo].[{tableName}]", conn);
        return (int)cmd.ExecuteScalar();
    }

    private static void Validate(string identifier)
    {
        if (!SafeIdentifier.IsMatch(identifier))
            throw new ArgumentException($"識別字含不合法字元：{identifier}", nameof(identifier));
    }
}
