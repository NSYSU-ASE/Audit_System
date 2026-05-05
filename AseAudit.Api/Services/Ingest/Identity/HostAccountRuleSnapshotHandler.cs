using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services.Ingest.Identity;

/// <summary>
/// 處理 <see cref="HostAccountRuleSnapshotPayload.Script"/>：帳號規則（網域/匿名存取）寫入 Identification_AM_rule。
/// </summary>
public sealed class HostAccountRuleSnapshotHandler : ISnapshotHandler
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIdentificationAmRuleRepository _ruleRepo;

    public HostAccountRuleSnapshotHandler(IIdentificationAmRuleRepository ruleRepo)
    {
        _ruleRepo = ruleRepo;
    }

    public string ScriptName => HostAccountRuleSnapshotPayload.Script;

    public async Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (!upload.Success)
            return 0;

        // Collector 的 HostAccountRuleSnapshotConverter 輸出完整 HostAccountRuleSnapshotPayload
        // (ScriptName/HostId/Hostname/Payload)，envelope.Payload 即為該物件。
        // 以完整型別反序列化後，envelope 欄位覆寫 HostId/Hostname 作為可信來源。
        var wire = upload.Payload.Deserialize<HostAccountRuleSnapshotPayload>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(HostAccountRuleSnapshotPayload)}.");

        var payload = new HostAccountRuleSnapshotPayload
        {
            HostId   = string.IsNullOrEmpty(upload.HostId) ? wire.HostId : upload.HostId,
            Hostname = string.IsNullOrEmpty(upload.HostName) ? wire.Hostname : upload.HostName,
            Payload  = wire.Payload,
        };

        var entity = HostAccountRuleSnapshotMapper.ToEntity(payload);
        return await _ruleRepo.AddAsync(entity, cancellationToken);
    }
}
