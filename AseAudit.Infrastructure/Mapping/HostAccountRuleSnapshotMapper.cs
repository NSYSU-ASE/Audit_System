using AseAudit.Core.Entities;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Infrastructure.Mapping;

/// <summary>
/// 將 <see cref="HostAccountRuleSnapshotPayload"/> 攤平為單筆
/// <see cref="IdentificationAmRule"/> 實體。
/// </summary>
public static class HostAccountRuleSnapshotMapper
{
    public static IdentificationAmRule ToEntity(HostAccountRuleSnapshotPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        return new IdentificationAmRule
        {
            HostName                  = payload.Hostname,
            // TODO: Required 欄位，待 Host Inventory 補齊；此階段先填空字串
            MACAddress                = string.Empty,
            RestrictAnonymousSAM      = (payload.Payload.AnonymousAccess?.RestrictAnonymousSAM ?? 0) == 1,
            RestrictAnonymous         = (payload.Payload.AnonymousAccess?.RestrictAnonymous ?? 0) == 1,
            EveryoneIncludesAnonymous = (payload.Payload.AnonymousAccess?.EveryoneIncludesAnonymous ?? 0) == 1,
            UserDomain                = payload.Payload.SystemInfo?.Domain ?? string.Empty,
            DomainRole                = payload.Payload.SystemInfo?.DomainRole ?? 0,
            // CreatedTime 由 DB 預設值 GETDATE() 填入
        };
    }
}
