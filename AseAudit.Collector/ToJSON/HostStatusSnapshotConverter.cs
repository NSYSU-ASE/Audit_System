using System.Text.Json;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 將 HostStatusSnapshot 的巢狀 JSON 輸出攤平為單層結構，
/// 僅保留最內層欄位名稱，以符合資料庫扁平化儲存需求。
///
/// 攤平規則：
///   純值屬性   → 直接保留（如 HostId, Hostname）
///   巢狀物件   → 子屬性提升至頂層（如 AnonymousAccess.RestrictAnonymousSAM → RestrictAnonymousSAM）
///   陣列屬性   → 序列化為 JSON 字串存入單一欄位（如 DefaultAccounts → "[{...}, ...]"）
/// </summary>
public class HostStatusSnapshotConverter : IScriptJsonConverter
{
    public string ToJson(string rawOutput, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.Parse(rawOutput);
        var flat = new Dictionary<string, object?>();

        Flatten(doc.RootElement, flat);

        return JsonSerializer.Serialize(flat, options);
    }

    private static void Flatten(JsonElement element, Dictionary<string, object?> result)
    {
        foreach (var prop in element.EnumerateObject())
        {
            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    // 巢狀物件 → 遞迴提升子屬性
                    Flatten(prop.Value, result);
                    break;

                case JsonValueKind.Array:
                    // 陣列 → 序列化為 JSON 字串
                    result[prop.Name] = prop.Value.GetRawText();
                    break;

                case JsonValueKind.String:
                    result[prop.Name] = prop.Value.GetString();
                    break;

                case JsonValueKind.Number:
                    result[prop.Name] = prop.Value.GetRawText();
                    break;

                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[prop.Name] = prop.Value.GetBoolean();
                    break;

                case JsonValueKind.Null:
                    result[prop.Name] = null;
                    break;

                default:
                    result[prop.Name] = prop.Value.GetRawText();
                    break;
            }
        }
    }
}
