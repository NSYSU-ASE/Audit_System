using System.Text.Json;
using System.Text.RegularExpressions;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 將 <c>auditpol /get /category:*</c> 的原始文字輸出解析為結構化 JSON。
///
/// 輸入格式：
///   第 1–2 行為標題列（跳過）
///   無縮排行   → 稽核類別名稱
///   有縮排行   → "  子政策名稱        設定值"（以 2 個以上空白分隔）
///
/// 輸出結構：{ "類別": { "子政策": "設定值", ... }, ... }
/// </summary>
public partial class EventStatusSnapshotConverter : IScriptJsonConverter
{
    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleSpaces();

    public string ToJson(string rawOutput, HostInfo hostInfo, JsonSerializerOptions options)
    {
        // EventStatusSnapshot 尚未使用 Contract Payload 格式，hostInfo 暫不參與組裝；
        // 主機識別由 SendJson 的 envelope (hostName) 提供。
        _ = hostInfo;
        var parsed = Parse(rawOutput);
        return JsonSerializer.Serialize(parsed, options);
    }

    private static Dictionary<string, Dictionary<string, string>> Parse(string rawOutput)
    {
        var result = new Dictionary<string, Dictionary<string, string>>();
        string currentCategory = "General";

        var lines = rawOutput.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.TrimEnd();
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            if (trimmed.StartsWith("System audit policy") ||
                trimmed.TrimStart().StartsWith("Category/Subcategory") ||
                trimmed.Contains("---"))
                continue;

            if (char.IsWhiteSpace(line[0]))
            {
                // 有縮排 → 子政策行：以 2 個以上空白分割名稱與設定值
                var parts = MultipleSpaces().Split(trimmed.Trim());
                if (parts.Length >= 2)
                    result[currentCategory][parts[0].Trim()] = parts[^1].Trim();
            }
            else
            {
                // 無縮排 → 類別標頭
                currentCategory = trimmed.Trim();
                result.TryAdd(currentCategory, []);
            }
        }

        return result;
    }
}
