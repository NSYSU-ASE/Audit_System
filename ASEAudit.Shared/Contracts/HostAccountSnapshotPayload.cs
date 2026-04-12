namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [HostAccountSnapshot] Collector 傳送至 API 的 Payload 格式。
/// 對應 PowerShell 腳本 HostAccountSnapshot.Content 的 JSON 輸出。
/// 放入 AuditSnapshotUpload.Payload，ScriptName = "HostAccountSnapshot"。
/// </summary>
public sealed class HostAccountSnapshotPayload
{
    /// <summary>主機識別碼 ($env:COMPUTERNAME)。</summary>
    public string HostId { get; init; } = string.Empty;

    /// <summary>主機名稱 ($env:COMPUTERNAME)。</summary>
    public string Hostname { get; init; } = string.Empty;

    /// <summary>
    /// 本機所有使用者帳號清單 (Get-LocalUser)。
    /// 用於檢查是否有帳號未設定密碼要求或仍啟用。
    /// </summary>
    public List<LocalUserEntry> LoginRequirement { get; init; } = [];

    /// <summary>
    /// 內建預設帳號狀態：Administrator、Guest、DefaultAccount。
    /// 用於稽核預設帳號是否已停用。
    /// </summary>
    public List<LocalUserEntry> DefaultAccounts { get; init; } = [];
}

/// <summary>本機使用者帳號基本資訊 (Get-LocalUser 子集)。</summary>
public sealed class LocalUserEntry
{
    /// <summary>帳號名稱。</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>登入是否要求密碼。</summary>
    public bool PasswordRequired { get; init; }

    /// <summary>帳號是否啟用。</summary>
    public bool Enabled { get; init; }
}
