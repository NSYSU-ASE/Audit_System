using AseAudit.Core.Entities;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Infrastructure.Mapping;

/// <summary>
/// 將 <see cref="HostAccountSnapshotPayload"/> 轉換為多筆
/// <see cref="IdentificationAmAccount"/> 實體 (每個 LocalUserEntry 對應一列)。
/// </summary>
public static class HostAccountSnapshotMapper
{
    public static IEnumerable<IdentificationAmAccount> ToEntities(HostAccountSnapshotPayload payload)
    {
        if (payload is null) throw new ArgumentNullException(nameof(payload));

        foreach (var user in payload.Payload.LoginRequirement)
            yield return BuildEntity(payload.Hostname, user);

        foreach (var user in payload.Payload.DefaultAccounts)
            yield return BuildEntity(payload.Hostname, user);
    }

    private static IdentificationAmAccount BuildEntity(string hostname, LocalUserEntry user) => new()
    {
        HostName         = hostname,
        // TODO: MACAddress 暫填 null，待 Host Inventory 機制上線後由 HostName 反查補齊
        MACAddress       = null,
        AccountName      = user.Name,
        Status           = user.Enabled ? "Enabled" : "Disabled",
        PasswordRequired = user.PasswordRequired,
        // CreatedTime 由 DB 預設值 GETDATE() 填入
    };
}
