using AseAudit.Api.Models.Ingest;
using AseAudit.Api.Services.Ingest;

namespace AseAudit.Api.Services;

/// <summary>
/// 依 ScriptName 將 Collector 上傳的 Payload 分派給對應的 <see cref="ISnapshotHandler"/>。
/// 新增 ScriptName 只需新增一個 Handler 實作並於 DI 註冊，不需修改本類別。
/// </summary>
public sealed class DatabaseAuditIngestService : IAuditIngestService
{
    private readonly Dictionary<string, ISnapshotHandler> _handlers;
    private readonly ILogger<DatabaseAuditIngestService> _logger;

    public DatabaseAuditIngestService(
        IEnumerable<ISnapshotHandler> handlers,
        ILogger<DatabaseAuditIngestService> logger)
    {
        _handlers = handlers.ToDictionary(h => h.ScriptName, StringComparer.Ordinal);
        _logger = logger;
    }

    public async Task<AuditIngestResponse> StoreAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (upload is null) throw new ArgumentNullException(nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.HostName))
            throw new ArgumentException("HostName is required.", nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.ScriptName))
            throw new ArgumentException("ScriptName is required.", nameof(upload));

        if (!_handlers.TryGetValue(upload.ScriptName, out var handler))
            throw new ArgumentException(
                $"Unsupported ScriptName: {upload.ScriptName}", nameof(upload));

        var receivedAt = DateTime.UtcNow;
        var ingestId = $"{receivedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}";

        var writtenRows = await handler.HandleAsync(upload, cancellationToken);

        _logger.LogInformation(
            "Ingested {Script} from {Host}: {Rows} row(s) written to DB.",
            upload.ScriptName, upload.HostName, writtenRows);

        return new AuditIngestResponse
        {
            IngestId = ingestId,
            HostName = upload.HostName,
            ScriptName = upload.ScriptName,
            StoredPath = string.Empty,
            ReceivedAt = receivedAt,
            SizeBytes = writtenRows
        };
    }
}
