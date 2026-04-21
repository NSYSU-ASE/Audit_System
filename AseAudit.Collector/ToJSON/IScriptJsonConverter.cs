using System.Text.Json;

namespace AseAudit.Collector.ToJSON;

/// <summary>
/// 腳本輸出 → JSON 字串轉換器介面。
/// 每個有自訂轉換需求的腳本對應一個實作。
///
/// 各 payload 腳本僅輸出 Payload 內容；主機識別 (HostId / Hostname) 由共同腳本
/// <see cref="Script_lib.HostInfoSnapshot"/> 收集後以 <see cref="HostInfo"/> 傳入，
/// Converter 負責組裝成完整的 Contract Payload (<see cref="ASEAudit.Shared.Contracts.IScriptPayload"/>)。
/// </summary>
public interface IScriptJsonConverter
{
    /// <summary>將腳本原始輸出 (payload 內容) 與主機資訊組裝為格式化 JSON 字串。</summary>
    string ToJson(string rawOutput, HostInfo hostInfo, JsonSerializerOptions options);
}
