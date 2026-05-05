using AseAudit.Core.Entities;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Infrastructure.Mapping;

/// <summary>
/// 將 <see cref="FirewallRuleSnapshotPayload"/> 中每條規則攤平為獨立的
/// <see cref="FireWallRule"/> 實體；回傳清單長度等於 Payload.Rules 筆數。
/// </summary>
public static class FirewallRuleSnapshotMapper
{
    public static List<FireWallRule> ToEntities(FirewallRuleSnapshotPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        return payload.Payload.Rules
            .Select(rule => new FireWallRule
            {
                HostName      = payload.Hostname,
                MACAddress    = null,
                RuleName      = rule.Name,
                DisplayName   = rule.DisplayName,
                Status        = rule.Enabled,
                Profile       = rule.Profile,
                Direction     = rule.Direction,
                Action        = rule.Action,
                Protocol      = rule.Protocol,
                Port          = rule.LocalPort,
                RemotePort    = rule.RemotePort,
                SourceIP      = rule.LocalAddress,
                DestinationIP = rule.RemoteAddress,
            })
            .ToList();
    }
}
