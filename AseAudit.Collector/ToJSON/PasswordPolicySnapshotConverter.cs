using System.Text.Json;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 將 PasswordPolicySnapshot 的 PowerShell 原始輸出 (僅 Payload 內容) 組裝為
/// 完整的 <see cref="PasswordPolicySnapshotPayload"/>：先反序列化為
/// <see cref="PasswordPolicySnapshotContent"/>，再填入 <see cref="HostInfo"/>
/// 取得的 HostId / Hostname，確保 JSON 結構與 Shared Contract 對齊。
///
/// 後續由 API 將其寫入資料表 <see cref="PasswordPolicySnapshotPayload.TableName"/>
/// (Identification_PasswordPolicy) 單列。
/// </summary>
public class PasswordPolicySnapshotConverter : IScriptJsonConverter
{
    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ToJson(string rawOutput, HostInfo hostInfo, JsonSerializerOptions options)
    {
        var content = JsonSerializer.Deserialize<PasswordPolicySnapshotContent>(rawOutput, _deserializeOptions)
                      ?? new PasswordPolicySnapshotContent();

        var payload = new PasswordPolicySnapshotPayload
        {
            HostId = hostInfo.HostId,
            Hostname = hostInfo.Hostname,
            Payload = content,
            ScriptName = PasswordPolicySnapshotPayload.Script
        };

        return JsonSerializer.Serialize(payload, options);
    }
}
