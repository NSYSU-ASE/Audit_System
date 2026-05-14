namespace AseAudit.DbTool.Services;

public interface ISqlServerConnector
{
    bool CanConnect(string connectionString);
    bool DatabaseExists(string masterConnectionString, string databaseName);
    bool AnyTableExists(string auditDbConnectionString, IReadOnlyCollection<string> tableNames);
    void CreateDatabase(string masterConnectionString, string databaseName);
    void ExecuteNonQuery(string connectionString, string sql);
    int GetTableRowCount(string connectionString, string tableName);
}
