namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [HostAccountRuleSnapshot] Collector → API 的 Payload 格式。
/// 對應 PowerShell 腳本 <c>HostAccountRuleSnapshot.Content</c> 的 JSON 輸出，
/// 整筆 Payload 攤平後寫入資料表 <c>[dbo].[Identification_AM_rule]</c> 單列。
///
/// 欄位對應 (Payload → Entity <c>IdentificationAmRule</c>)：
///   Hostname                                  → HostName
///   SystemInfo.Domain                         → UserDomain
///   SystemInfo.DomainRole                     → DomainRole
///   AnonymousAccess.RestrictAnonymousSAM      → RestrictAnonymousSAM (BIT)
///   AnonymousAccess.RestrictAnonymous         → RestrictAnonymous    (BIT)
///   AnonymousAccess.EveryoneIncludesAnonymous → EveryoneIncludesAnonymous (BIT)
///   (MACAddress 由 Server 端補齊)
/// </summary>
public sealed class HostAccountRuleSnapshotPayload : IScriptPayload
{
    /// <summary>Agent / Server 共用的腳本名稱常數 (單一真相來源)。</summary>
    public const string Script = "HostAccountRuleSnapshot";

    /// <summary>此 Payload 對應的資料表名稱，供 Ingest 層路由使用。</summary>
    public const string TableName = "Identification_AM_rule";

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
    /// WMI Win32_ComputerSystem 系統資訊，包含 AD 網域加入狀態。
    /// 若 WMI 查詢失敗則為 null。
    /// </summary>
    public SystemInfoEntry? SystemInfo { get; init; }

    /// <summary>
    /// 登錄檔匿名存取設定 (HKLM:\SYSTEM\CurrentControlSet\Control\Lsa)。
    /// 若登錄機碼不存在則對應欄位為 null。
    /// </summary>
    public AnonymousAccessEntry? AnonymousAccess { get; init; }
}

/// <summary>
/// WMI Win32_ComputerSystem 子集，用於判斷 AD 加入狀態與網域角色。
/// </summary>
public sealed class SystemInfoEntry
{
    /// <summary>電腦名稱。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 所屬網域或工作群組名稱，寫入 UserDomain 欄位。
    /// AD 環境下為 FQDN (如 corp.local)，工作群組下為群組名稱。
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// WMI DomainRole 值，寫入 DomainRole 欄位：
    ///   0 = Standalone Workstation（未加入網域）
    ///   1 = Member Workstation（已加入網域）
    ///   2 = Standalone Server
    ///   3 = Member Server
    ///   4 = Backup Domain Controller
    ///   5 = Primary Domain Controller
    /// </summary>
    public int DomainRole { get; init; }
}

/// <summary>
/// 登錄檔匿名存取管控設定，對應 HKLM:\SYSTEM\CurrentControlSet\Control\Lsa 下各機碼。
/// 值為 null 表示登錄機碼不存在（視同未設定）。
/// </summary>
public sealed class AnonymousAccessEntry
{
    /// <summary>
    /// RestrictAnonymousSAM：限制匿名存取 SAM 帳號資訊。
    ///   1 = 限制（建議值）  0 = 允許
    /// </summary>
    public int? RestrictAnonymousSAM { get; init; }

    /// <summary>
    /// RestrictAnonymous：限制匿名存取共用資源與帳號清單。
    ///   1 = 限制（建議值）  0 = 允許
    /// </summary>
    public int? RestrictAnonymous { get; init; }

    /// <summary>
    /// EveryoneIncludesAnonymous：Everyone 群組是否包含匿名使用者。
    ///   0 = 不包含（建議值）  1 = 包含
    /// </summary>
    public int? EveryoneIncludesAnonymous { get; init; }
}
