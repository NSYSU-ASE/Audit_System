using System.Text.Json;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 腳本輸出 → JSON 字串轉換器介面。
/// 每個有自訂轉換需求的腳本對應一個實作。
/// </summary>
public interface IScriptJsonConverter
{
    /// <summary>將腳本原始輸出轉換為格式化 JSON 字串。</summary>
    string ToJson(string rawOutput, JsonSerializerOptions options);
}
