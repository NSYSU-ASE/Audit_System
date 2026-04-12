using AseAudit.Collector.Script_lib;

namespace AseAudit.Collector.ToJSON;

// ═══════════════════════════════════════════════════════════════
//  JsonConverterRegistry — 腳本名稱 → 自訂 JSON 轉換器的對應表
//
//  用途：
//    Worker 透過此表查詢腳本是否有自訂轉換器。
//    有對應項目 → 使用自訂 IScriptJsonConverter 解析原始輸出。
//    無對應項目 → 回退至通用 JsonDocument.Parse 流程。
//
//  新增轉換器時：
//    ① 在 ToJSON/ 建立實作 IScriptJsonConverter 的新 .cs 檔
//    ② 在下方 _converters 字典加入一行
// ═══════════════════════════════════════════════════════════════

public static class JsonConverterRegistry
{
    private static readonly IReadOnlyDictionary<string, IScriptJsonConverter> _converters =
        new Dictionary<string, IScriptJsonConverter>
        {
            [nameof(EventStatusSnapshot)] = new EventStatusSnapshotConverter(),
            [nameof(HostAccountSnapshot)] = new HostAccountSnapshotConverter(),
            [nameof(HostStatusSnapshot)]  = new HostStatusSnapshotConverter(),

            // ↓ 新增轉換器時在此加一行
        };

    /// <summary>
    /// 查詢指定腳本名稱是否有對應的自訂轉換器。
    /// </summary>
    public static bool TryGet(string scriptName, out IScriptJsonConverter? converter)
        => _converters.TryGetValue(scriptName, out converter);
}
