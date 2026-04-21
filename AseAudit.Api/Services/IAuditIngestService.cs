using AseAudit.Api.Models.Ingest;

namespace AseAudit.Api.Services;

/// <summary>
/// 接收 Collector 上傳之稽核 JSON 並負責落地儲存。
/// 後續可換成寫入資料庫的實作 (例如 EF Core / Dapper)。
/// </summary>
public interface IAuditIngestService
{
    Task<AuditIngestResponse> StoreAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken);
}
