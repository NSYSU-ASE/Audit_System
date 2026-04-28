namespace ASEAudit.Shared.Contracts;

/// <summary>
/// [PasswordPolicySnapshot] Collector → API 的 Payload 格式。
/// 對應 PowerShell 腳本 <c>PasswordPolicySnapshot.Content</c> 的 JSON 輸出，
/// 整筆 Payload 攤平後寫入資料表 <c>[dbo].[Identification_PasswordPolicy]</c> 單列。
///
/// 欄位對應 (Payload → Entity <c>IdentificationPasswordPolicy</c>)：
///   Hostname                         → HostName
///   Payload.MinimumPasswordLength    → MinimumPasswordLength
///   Payload.MaximumPasswordAge       → MaximumPasswordAge
///   Payload.MinimumPasswordAge       → MinimumPasswordAge
///   Payload.PasswordHistorySize      → PasswordHistorySize
///   Payload.PasswordComplexity       → PasswordComplexity
///   Payload.LockoutBadCount          → LockoutBadCount
///   Payload.LockoutDuration          → LockoutDuration
///   Payload.ResetLockoutCount        → ResetLockoutCount
///   Payload.EnableAdminAccount       → EnableAdminAccount
///   Payload.EnableGuestAccount       → EnableGuestAccount
///   Payload.NewAdministratorName     → NewAdministratorName
///   Payload.NewGuestName             → NewGuestName
///   (MACAddress 由 Server 端補齊)
/// </summary>
public sealed class PasswordPolicySnapshotPayload : ScriptPayload<PasswordPolicySnapshotContent>
{
    /// <summary>Agent / Server 共用的腳本名稱常數 (單一真相來源)。</summary>
    public const string Script = "PasswordPolicySnapshot";

    /// <summary>此 Payload 對應的資料表名稱，供 Ingest 層路由使用。</summary>
    public const string TableName = "Identification_PasswordPolicy";

    /// <inheritdoc />
    public override string ScriptName { get; set; } = Script;
}

/// <summary>
/// <see cref="PasswordPolicySnapshotPayload"/> 的實際稽核資料內容。
/// 對應 PowerShell 輸出 JSON 中 <c>Payload</c> 物件，欄位來源為
/// secedit /export 之 INF 檔 [System Access] 區段。
///
/// 任一欄位若 secedit 未輸出 (機碼缺失) 則為 null。
/// </summary>
public sealed class PasswordPolicySnapshotContent : PayloadWrapper
{
    // ── 密碼原則 ────────────────────────────────────────────────

    /// <summary>密碼長度下限（字元數）。</summary>
    public int? MinimumPasswordLength { get; init; }

    /// <summary>密碼最長使用期限（天）；0 = 永不過期。</summary>
    public int? MaximumPasswordAge { get; init; }

    /// <summary>密碼最短使用期限（天）。</summary>
    public int? MinimumPasswordAge { get; init; }

    /// <summary>密碼歷程記錄保留筆數。</summary>
    public int? PasswordHistorySize { get; init; }

    /// <summary>密碼複雜度需求；1 = 啟用，0 = 停用。</summary>
    public int? PasswordComplexity { get; init; }

    // ── 帳號鎖定原則 ────────────────────────────────────────────

    /// <summary>帳號鎖定閾值（連續失敗登入次數）；0 = 不鎖定。</summary>
    public int? LockoutBadCount { get; init; }

    /// <summary>鎖定持續時間（分鐘）；-1 = 直到管理員手動解除。</summary>
    public int? LockoutDuration { get; init; }

    /// <summary>失敗計數重設視窗（分鐘）。</summary>
    public int? ResetLockoutCount { get; init; }

    // ── 內建帳號控制 ────────────────────────────────────────────

    /// <summary>是否啟用內建 Administrator 帳號；1 = 啟用，0 = 停用。</summary>
    public int? EnableAdminAccount { get; init; }

    /// <summary>是否啟用內建 Guest 帳號；1 = 啟用，0 = 停用。</summary>
    public int? EnableGuestAccount { get; init; }

    /// <summary>內建 Administrator 帳號重新命名後的名稱。</summary>
    public string? NewAdministratorName { get; init; }

    /// <summary>內建 Guest 帳號重新命名後的名稱。</summary>
    public string? NewGuestName { get; init; }
}
