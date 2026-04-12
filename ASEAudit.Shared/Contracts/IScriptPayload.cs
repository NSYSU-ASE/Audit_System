namespace ASEAudit.Shared.Contracts;

/// <summary>
/// 所有 Agent → Server Payload 的共同介面。
/// 保證 Payload JSON 自我描述來源腳本，離開 envelope 後仍可識別。
///
/// 實作者需提供：
///   - <see cref="ScriptName"/>        執行時期可序列化欄位，寫入輸出 JSON
///   - 靜態常數 <c>Script</c>           Agent / Server 雙方引用之字串字面值 (單一真相來源)
///   - 靜態常數 <c>TableName</c>        對應的資料表名稱 (Ingest 層路由用)
///
/// 這兩個常數刻意以 <c>public const string</c> 形式宣告，而非介面成員，
/// 以便在 <c>JsonConverterRegistry</c> / <c>ScriptRegistry</c> 等靜態字典中直接引用。
/// </summary>
public interface IScriptPayload
{
    /// <summary>
    /// 此 Payload 所屬腳本名稱，應等於對應 Payload class 的 <c>Script</c> 常數。
    /// 由 Converter 於輸出 JSON 時強制填入，確保接收端可由 Payload 本體識別來源。
    /// </summary>
    string ScriptName { get; set; }
}
