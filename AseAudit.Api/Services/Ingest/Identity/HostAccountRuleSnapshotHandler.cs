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
        return await _ruleRepo.AddAsync(entity, cancellationToken);
    }
}
