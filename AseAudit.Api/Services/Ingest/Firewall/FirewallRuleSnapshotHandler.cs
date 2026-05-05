using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services.Ingest.Firewall;

/// <summary>
/// 處理 <see cref="FirewallRuleSnapshotPayload.Script"/>：防火牆規則批次寫入 FireWallRule。
/// 每個 FirewallRuleEntry 展開為一列；空規則清單回傳 0。
/// </summary>
public sealed class FirewallRuleSnapshotHandler : ISnapshotHandler
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IFireWallRuleRepository _repo;

    public FirewallRuleSnapshotHandler(IFireWallRuleRepository repo) => _repo = repo;

    public string ScriptName => FirewallRuleSnapshotPayload.Script;

    public async Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (!upload.Success) return 0;

        var wire = upload.Payload.Deserialize<FirewallRuleSnapshotPayload>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(FirewallRuleSnapshotPayload)}.");

        var payload = new FirewallRuleSnapshotPayload
        {
            HostId   = string.IsNullOrEmpty(upload.HostId)   ? wire.HostId   : upload.HostId,
            Hostname = string.IsNullOrEmpty(upload.HostName) ? wire.Hostname : upload.HostName,
            Payload  = wire.Payload,
        };

        var entities = FirewallRuleSnapshotMapper.ToEntities(payload);
        return await _repo.AddRangeAsync(entities, cancellationToken);
    }
}
