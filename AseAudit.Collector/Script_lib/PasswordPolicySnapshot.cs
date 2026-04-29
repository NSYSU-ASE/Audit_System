namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 收集本機密碼原則與內建帳號設定（透過 secedit /export 匯出 SECURITYPOLICY）。
/// 腳本僅輸出 Payload 內容；主機識別 (HostId / Hostname) 由 <see cref="HostInfoSnapshot"/> 共同收集，
/// 於 <see cref="ToJSON.PasswordPolicySnapshotConverter"/> 組裝成完整 Contract Payload。
///
/// 輸出 JSON 對應 <c>PasswordPolicySnapshotContent</c>：
/// <code>
/// {
///   "MinimumPasswordLength": 0, "MaximumPasswordAge": 42, "MinimumPasswordAge": 0,
///   "PasswordHistorySize": 0,   "PasswordComplexity": 1,
///   "LockoutBadCount": 0,       "LockoutDuration": 30,    "ResetLockoutCount": 30,
///   "EnableAdminAccount": 1,    "EnableGuestAccount": 0,
///   "NewAdministratorName": "Administrator", "NewGuestName": "Guest"
/// }
/// </code>
///
/// 來源：secedit /export 輸出之 INF 檔 [System Access] 區段。
/// 注意：secedit 需具備系統管理員權限；INF 檔以 UTF-16 LE 寫出，需以 -Encoding Unicode 讀取。
/// </summary>
public static class PasswordPolicySnapshot
{
    public const string Content = @"
# ══════════════════════════════════════════════════════════════
#  PasswordPolicySnapshot — 密碼原則 / 鎖定原則 / 內建帳號收集 (Payload only)
#  使用 secedit 將 SECURITYPOLICY 區段匯出至暫存 INF，再解析 [System Access]。
# ══════════════════════════════════════════════════════════════

try {
    $inf = [System.IO.Path]::Combine($env:TEMP, ""secpol_$([guid]::NewGuid().ToString('N')).inf"")

    & secedit /export /cfg $inf /areas SECURITYPOLICY /quiet | Out-Null

    if (-not (Test-Path -LiteralPath $inf)) {
        throw ""secedit failed to produce $inf (exit=$LASTEXITCODE)""
    }

    # 解析 INF：擷取 [System Access] 區段的 key=value
    $fields    = @{}
    $inSection = $false
    foreach ($line in (Get-Content -LiteralPath $inf -Encoding Unicode)) {
        $trim = $line.Trim()
        if ($trim -eq '[System Access]') { $inSection = $true; continue }
        if ($trim.StartsWith('[')) { $inSection = $false; continue }
        if (-not $inSection) { continue }
        if ($trim -match '^([^=\s]+)\s*=\s*(.*)$') {
            $key   = $matches[1].Trim()
            $value = $matches[2].Trim().Trim('""')
            $fields[$key] = $value
        }
    }

    Remove-Item -LiteralPath $inf -Force -ErrorAction SilentlyContinue

    function _Int([string]$k) {
        if ($fields.ContainsKey($k) -and $fields[$k] -ne '') { return [int]$fields[$k] }
        return $null
    }
    function _Str([string]$k) {
        if ($fields.ContainsKey($k)) { return [string]$fields[$k] }
        return $null
    }

    @{
        MinimumPasswordLength = _Int 'MinimumPasswordLength'
        MaximumPasswordAge    = _Int 'MaximumPasswordAge'
        MinimumPasswordAge    = _Int 'MinimumPasswordAge'
        PasswordHistorySize   = _Int 'PasswordHistorySize'
        PasswordComplexity    = _Int 'PasswordComplexity'
        LockoutBadCount       = _Int 'LockoutBadCount'
        LockoutDuration       = _Int 'LockoutDuration'
        ResetLockoutCount     = _Int 'ResetLockoutCount'
        EnableAdminAccount    = _Int 'EnableAdminAccount'
        EnableGuestAccount    = _Int 'EnableGuestAccount'
        NewAdministratorName  = _Str 'NewAdministratorName'
        NewGuestName          = _Str 'NewGuestName'
    } | ConvertTo-Json -Depth 4
}
catch {
    @{
        Error   = 'Failed to retrieve password policy snapshot'
        Message = $_.Exception.Message
    } | ConvertTo-Json
}
";
}
