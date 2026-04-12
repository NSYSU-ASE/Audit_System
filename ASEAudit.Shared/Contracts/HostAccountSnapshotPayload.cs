namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [HostAccountSnapshot] Collector → API 的 Payload 格式。
/// 對應 PowerShell 腳本 <c>HostAccountSnapshot.Content</c> 的 JSON 輸出，
/// 每筆 <see cref="LocalUserEntry"/> 展開後寫入資料表 <c>[dbo].[Identification_AM_Account]</c>。
///
/// 欄位對應 (Payload → Entity <c>IdentificationAmAccount</c>)：
///   Hostname                 → HostName
///   LocalUserEntry.Name      → AccountName
///   LocalUserEntry.Enabled   → Status (true = Enabled / false = Disabled)
///   LocalUserEntry.PasswordRequired → PasswordRequired
///   (MACAddress 由 Server 端補齊)
/// </summary>
public sealed class HostAccountSnapshotPayload : IScriptPayload
{
    /// <summary>Agent / Server 共用的腳本名稱常數 (單一真相來源)。</summary>
    public const string Script = "HostAccountSnapshot";

    /// <summary>此 Payload 對應的資料表名稱，供 Ingest 層路由使用。</summary>
    public const string TableName = "Identification_AM_Account";

    /// <summary>
    /// 此 Payload 來源腳本名稱，由 Converter 輸出時強制寫入 (等於 <see cref="Script"/>)。
    /// 接收端可直接由 Payload 本體識別來源，無需依賴外層 envelope。
    /// </summary>
    public string ScriptName { get; set; } = Script;

    /// <summary>主機識別碼 ($env:COMPUTERNAME)。</summary>
    public string HostId { get; init; } = string.Empty;

    /// <summary>主機名稱 ($env:COMPUTERNAME)，寫入 HostName 欄位。</summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// 本機一般使用者帳號清單 (Get-LocalUser，排除內建預設帳號)。
    /// 每筆展開為 Identification_AM_Account 一列。
    /// </summary>
    public List<LocalUserEntry> LoginRequirement { get; init; } = [];

    /// <summary>
    /// 內建預設帳號狀態：Administrator、Guest、DefaultAccount。
    /// 每筆展開為 Identification_AM_Account 一列，用於稽核預設帳號是否已停用。
    /// </summary>
    public List<LocalUserEntry> DefaultAccounts { get; init; } = [];
}

/// <summary>
/// 本機使用者帳號基本資訊 (Get-LocalUser 子集)。
/// 每個實例對應資料表 <c>Identification_AM_Account</c> 一列。
/// </summary>
public sealed class LocalUserEntry
{
    /// <summary>帳號名稱，寫入 AccountName 欄位。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>登入是否要求密碼，寫入 PasswordRequired 欄位。</summary>
    public bool PasswordRequired { get; init; }

    /// <summary>帳號是否啟用，轉字串寫入 Status 欄位 ("Enabled" / "Disabled")。</summary>
    public bool Enabled { get; init; }
}
