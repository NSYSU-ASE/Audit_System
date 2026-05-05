using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector.Script_lib;

public static class ScriptRegistry
{
    // 新增腳本時在此加一行
    public static readonly IReadOnlyDictionary<string, string> All =
        new Dictionary<string, string>
        {
            [HostAccountSnapshotPayload.Script]     = HostAccountSnapshot.Content,
            [HostAccountRuleSnapshotPayload.Script] = HostAccountRuleSnapshot.Content,
            [PasswordPolicySnapshotPayload.Script]  = PasswordPolicySnapshot.Content,
            [nameof(EventStatusSnapshot)]           = EventStatusSnapshot.Content,
        };
}
