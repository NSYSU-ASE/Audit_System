using AseAudit.Core.Entities;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Infrastructure.Mapping;

/// <summary>
/// 將 <see cref="FirewallRuleSnapshotPayload"/> 中每條規則攤平為獨立的
/// <see cref="FireWallRule"/> 實體；回傳清單長度等於 Payload.Rules 筆數。
/// </summary>
public static class FirewallRuleSnapshotMapper
{
    // 對齊 [dbo].[FireWallRule] 欄位上限。Windows UWP 規則的 Name/DisplayName
    // 可能超過欄位長度（例如 @{Pkg?ms-resource://...} 樣板），單筆爆 SqlException
    // 會讓整批 AddRangeAsync 回滾，因此在邊界截斷以保住其他規則寫入。
    private const int RuleNameMax = 256;
    private const int DisplayNameMax = 512;
    private const int ShortFieldMax = 100;

    public static List<FireWallRule> ToEntities(FirewallRuleSnapshotPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        return (payload.Payload.Rules ?? [])
            .Select(rule => new FireWallRule
            {
                HostName      = payload.Hostname,
                MACAddress    = null,
                RuleName      = Truncate(rule.Name, RuleNameMax) ?? string.Empty,
                DisplayName   = Truncate(rule.DisplayName, DisplayNameMax),
                Status        = Truncate(rule.Enabled, ShortFieldMax),
                Profile       = Truncate(rule.Profile, ShortFieldMax),
                Direction     = Truncate(rule.Direction, ShortFieldMax),
                Action        = Truncate(rule.Action, ShortFieldMax),
                Protocol      = Truncate(rule.Protocol, ShortFieldMax),
                Port          = Truncate(rule.LocalPort, ShortFieldMax),
                RemotePort    = Truncate(rule.RemotePort, ShortFieldMax),
                SourceIP      = Truncate(rule.LocalAddress, ShortFieldMax),
                DestinationIP = Truncate(rule.RemoteAddress, ShortFieldMax),
            })
            .ToList();
    }

    private static string? Truncate(string? value, int maxLength)
        => value is { Length: > 0 } && value.Length > maxLength
            ? value[..maxLength]
            : value;
}
