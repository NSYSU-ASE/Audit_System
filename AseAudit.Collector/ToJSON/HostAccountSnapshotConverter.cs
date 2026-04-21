using System.Text.Json;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 將 HostAccountSnapshot 的 PowerShell 原始 JSON 輸出，
/// 以 <see cref="HostAccountSnapshotPayload"/> 為 schema 反序列化並重新輸出，
/// 保證 JSON 結構與 Shared Contract 對齊，後續由 API 寫入
/// 資料表 <see cref="HostAccountSnapshotPayload.TableName"/> (Identification_AM_Account)。
///
/// 輸出保留列表結構 (LoginRequirement / DefaultAccounts)，
/// 由 Ingest 服務逐列展開寫入資料表。
/// </summary>
public class HostAccountSnapshotConverter : IScriptJsonConverter
{
    private static readonly JsonSerializerOptions _deserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public string ToJson(string rawOutput, JsonSerializerOptions options)
    {
        var payload = JsonSerializer.Deserialize<HostAccountSnapshotPayload>(rawOutput, _deserializeOptions)
                      ?? new HostAccountSnapshotPayload();

        // 強制覆寫為常數，確保 Payload JSON 自我描述來源腳本
        payload.ScriptName = HostAccountSnapshotPayload.Script;

        return JsonSerializer.Serialize(payload, options);
    }
}
