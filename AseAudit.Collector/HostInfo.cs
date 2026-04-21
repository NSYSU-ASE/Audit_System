namespace AseAudit.Collector;

/// <summary>
/// 主機識別資訊，由 <see cref="Script_lib.HostInfoSnapshot"/> 蒐集後，
/// 傳入各 <see cref="ToJSON.IScriptJsonConverter"/> 用於組裝 Contract Payload。
/// </summary>
public sealed record HostInfo(string HostId, string Hostname)
{
    public static HostInfo FromEnvironment() =>
        new(Environment.MachineName, Environment.MachineName);
}
