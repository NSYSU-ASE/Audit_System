namespace ASEAudit.Shared.Contracts;

/// <summary>
/// 所有 Agent → Server Payload 的共同介面。
/// 保證 Payload JSON 自我描述來源腳本，離開 envelope 後仍可識別。
///
/// 實作者應優先繼承 <see cref="ScriptPayload{TContent}"/> 抽象基類（已處理共用欄位與
/// <see cref="Payload"/> 的顯式介面實作），僅需宣告：
///   - 靜態常數 <c>Script</c>        Agent / Server 雙方引用之字串字面值 (單一真相來源)
///   - 靜態常數 <c>TableName</c>     對應的資料表名稱 (Ingest 層路由用)
///   - 覆寫 <see cref="ScriptName"/> 預設值為 <c>Script</c>
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

    /// <summary>主機識別碼 ($env:COMPUTERNAME)。</summary>
    string HostId { get; init; }

    /// <summary>主機名稱 ($env:COMPUTERNAME)。</summary>
    string Hostname { get; init; }

    /// <summary>
    /// 實際稽核資料內容。由各腳本對應的 <see cref="PayloadWrapper"/> 實作承載。
    /// 以非泛型 <see cref="PayloadWrapper"/> 型別暴露，供 Ingest 層以共通介面操作；
    /// 強型別存取請使用具體 Payload class 的 <c>Payload</c> 屬性。
    /// </summary>
    PayloadWrapper Payload { get; }
}

/// <summary>
/// Payload 實際內容的標記介面。每個腳本應提供一個實作類別，
/// 將該腳本蒐集到的所有欄位集中於其中（對應 PowerShell 輸出 JSON 中的 <c>Payload</c> 物件）。
/// </summary>
public interface PayloadWrapper { }

/// <summary>
/// <see cref="IScriptPayload"/> 的抽象基類。集中共用欄位與顯式介面實作，
/// 子類只需提供 <typeparamref name="TContent"/> 與腳本常數。
/// </summary>
/// <typeparam name="TContent">腳本對應的 <see cref="PayloadWrapper"/> 實作類別。</typeparam>
public abstract class ScriptPayload<TContent> : IScriptPayload
    where TContent : PayloadWrapper, new()
{
    /// <inheritdoc />
    public abstract string ScriptName { get; set; }

    /// <inheritdoc />
    public string HostId { get; init; } = string.Empty;

    /// <inheritdoc />
    public string Hostname { get; init; } = string.Empty;

    /// <summary>強型別 Payload 內容，對應 PowerShell 輸出 JSON 的 <c>Payload</c> 物件。</summary>
    public TContent Payload { get; init; } = new();

    /// <summary>以 <see cref="PayloadWrapper"/> 型別暴露 <see cref="Payload"/>，滿足 <see cref="IScriptPayload"/>。</summary>
    PayloadWrapper IScriptPayload.Payload => Payload;
}
