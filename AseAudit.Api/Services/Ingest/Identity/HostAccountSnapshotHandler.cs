using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services.Ingest.Identity;

/// <summary>
/// 處理 <see cref="HostAccountSnapshotPayload.Script"/>：帳號快照寫入 Identification_AM_Account。
/// </summary>
public sealed class HostAccountSnapshotHandler : ISnapshotHandler
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIdentificationAmAccountRepository _accountRepo;

    public HostAccountSnapshotHandler(IIdentificationAmAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public string ScriptName => HostAccountSnapshotPayload.Script;

    public async Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (!upload.Success)
            return 0;

        // Collector 的 HostAccountSnapshotConverter 輸出完整 HostAccountSnapshotPayload
        // (ScriptName/HostId/Hostname/Payload)，envelope.Payload 即為該物件。
        // 以完整型別反序列化後，envelope 欄位覆寫 HostId/Hostname 作為可信來源。
        var wire = upload.Payload.Deserialize<HostAccountSnapshotPayload>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(HostAccountSnapshotPayload)}.");

        var payload = new HostAccountSnapshotPayload
        {
            HostId   = string.IsNullOrEmpty(upload.HostId) ? wire.HostId : upload.HostId,
            Hostname = string.IsNullOrEmpty(upload.HostName) ? wire.Hostname : upload.HostName,
            Payload  = wire.Payload,
        };

        var entities = HostAccountSnapshotMapper.ToEntities(payload).ToList();
        return await _accountRepo.AddRangeAsync(entities, cancellationToken);
    }
}
