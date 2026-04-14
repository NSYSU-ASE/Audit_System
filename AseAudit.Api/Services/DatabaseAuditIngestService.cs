using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services;

/// <summary>
/// 將 Collector 上傳的 JSON Payload 依 ScriptName 反序列化後，
/// 透過對應 Mapper 轉成 Entity 並交由 Repository 寫入 SQL Server。
/// </summary>
public sealed class DatabaseAuditIngestService : IAuditIngestService
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIdentificationAmAccountRepository _accountRepo;
    private readonly IIdentificationAmRuleRepository _ruleRepo;
    private readonly ILogger<DatabaseAuditIngestService> _logger;

    public DatabaseAuditIngestService(
        IIdentificationAmAccountRepository accountRepo,
        IIdentificationAmRuleRepository ruleRepo,
        ILogger<DatabaseAuditIngestService> logger)
    {
        _accountRepo = accountRepo;
        _ruleRepo = ruleRepo;
        _logger = logger;
    }

    public async Task<AuditIngestResponse> StoreAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (upload is null) throw new ArgumentNullException(nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.HostName))
            throw new ArgumentException("HostName is required.", nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.ScriptName))
            throw new ArgumentException("ScriptName is required.", nameof(upload));

        var receivedAt = DateTime.UtcNow;
        var ingestId = $"{receivedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}";

        int writtenRows = upload.ScriptName switch
        {
            HostAccountSnapshotPayload.Script =>
                await HandleAccountSnapshotAsync(upload, cancellationToken),

            HostAccountRuleSnapshotPayload.Script =>
                await HandleRuleSnapshotAsync(upload, cancellationToken),

            _ => throw new ArgumentException(
                $"Unsupported ScriptName: {upload.ScriptName}", nameof(upload))
        };

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

    private async Task<int> HandleAccountSnapshotAsync(AuditSnapshotUpload upload, CancellationToken ct)
    {
        // upload.Payload 是 envelope 內層 content（不含 ScriptName/Hostname），
        // 直接反序列化成 Content 型別，再以 envelope 欄位組回強型別 Payload。
        var content = upload.Payload.Deserialize<HostAccountSnapshotContent>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(HostAccountSnapshotContent)}.");

        var payload = new HostAccountSnapshotPayload
        {
            HostId   = upload.HostId ?? string.Empty,
            Hostname = upload.HostName,
            Payload  = content,
        };

        var entities = HostAccountSnapshotMapper.ToEntities(payload).ToList();
        return await _accountRepo.AddRangeAsync(entities, ct);
    }

    private async Task<int> HandleRuleSnapshotAsync(AuditSnapshotUpload upload, CancellationToken ct)
    {
        var content = upload.Payload.Deserialize<HostAccountRuleSnapshotContent>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(HostAccountRuleSnapshotContent)}.");

        var payload = new HostAccountRuleSnapshotPayload
        {
            HostId   = upload.HostId ?? string.Empty,
            Hostname = upload.HostName,
            Payload  = content,
        };

        var entity = HostAccountRuleSnapshotMapper.ToEntity(payload);
        return await _ruleRepo.AddAsync(entity, ct);
    }
}
