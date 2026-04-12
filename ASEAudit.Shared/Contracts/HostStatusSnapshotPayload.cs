namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [HostStatusSnapshot] Collector 傳送至 API 的 Payload 格式。
/// 對應 PowerShell 腳本 HostStatusSnapshot.Content 的 JSON 輸出。
/// 放入 AuditSnapshotUpload.Payload，ScriptName = "HostStatusSnapshot"。
/// </summary>
public sealed class HostStatusSnapshotPayload
{
    /// <summary>主機識別碼 ($env:COMPUTERNAME)。</summary>
    public string HostId { get; init; } = string.Empty;

    /// <summary>主機名稱 ($env:COMPUTERNAME)。</summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// WMI Win32_ComputerSystem 系統資訊，包含 AD 網域加入狀態。
    /// 若 WMI 查詢失敗則為 null。
    /// </summary>
    public SystemInfoEntry? SystemInfo { get; init; }

    /// <summary>
    /// 內建預設帳號狀態：Administrator、Guest、DefaultAccount。
    /// 用於稽核預設帳號是否已停用。
    /// </summary>
    public List<LocalUserEntry> DefaultAccounts { get; init; } = [];

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
    /// 所屬網域或工作群組名稱。
    /// AD 環境下為 FQDN (如 corp.local)，工作群組下為群組名稱。
    /// </summary>
    public string Domain { get; init; } = string.Empty;

    /// <summary>
    /// WMI DomainRole 值：
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
