namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 會話完整性快照 — 驗證 SR 3.8 Session Integrity。
///
/// 涵蓋驗證項目：
///   SR 3.8      — 確認系統具備保護會話完整性的能力，防範中間人攻擊、會話劫持
///   SR 3.8 RE(1) — 會話結束後（含瀏覽器會話）Session ID 是否立即失效
///   SR 3.8 RE(2) — 是否為每個會話產生唯一 Session ID，並將過期 ID 視為無效
///   SR 3.8 RE(3) — Session ID 是否使用普遍接受的隨機來源產生（無法程式化驗證，見下方註釋）
///
/// 註釋：
///   RE(3) 要求 Session ID 使用密碼學安全隨機來源產生。
///   此項屬於應用程式層級的實作細節，無法僅透過 OS 層 PowerShell 腳本驗證，
///   需由應用程式開發團隊提供 Session ID 產生機制的文件佐證。
///   本腳本僅收集 OS 層可觀測的會話管理設定供佐證。
///
/// 輸出：JSON 物件
///   - RdpSessionTimeout:   RDP 會話逾時與閒置斷線設定（登錄檔）
///   - RdpNlaEnabled:       RDP 網路層級驗證（NLA）啟用狀態
///   - ActiveSessions:      目前作用中的登入會話清單
///   - SmbSessionSettings:  SMB 伺服器會話逾時設定
///   - WinRmSessionConfig:  WinRM 會話逾時設定
/// </summary>
public static class SessionIntegritySnapshot
{
    public const string Content = @"
# ── SR 3.8：RDP 會話逾時設定（登錄檔） ──
# RE(1)：確認會話結束後 Session 是否會失效（透過逾時與斷線設定驗證）
$rdpPolicyPath = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows NT\Terminal Services'
$rdpConfigPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Terminal Server\WinStations\RDP-Tcp'

$rdpTimeout = @{
    # 閒置會話限制（分鐘），0 = 無限制
    MaxIdleTime_Policy          = (Get-ItemProperty -Path $rdpPolicyPath -Name 'MaxIdleTime' -ErrorAction SilentlyContinue).MaxIdleTime
    MaxIdleTime_Config          = (Get-ItemProperty -Path $rdpConfigPath -Name 'MaxIdleTime' -ErrorAction SilentlyContinue).MaxIdleTime
    # 中斷連線後保留會話的時間限制
    MaxDisconnectionTime_Policy = (Get-ItemProperty -Path $rdpPolicyPath -Name 'MaxDisconnectionTime' -ErrorAction SilentlyContinue).MaxDisconnectionTime
    MaxDisconnectionTime_Config = (Get-ItemProperty -Path $rdpConfigPath -Name 'MaxDisconnectionTime' -ErrorAction SilentlyContinue).MaxDisconnectionTime
    # 作用中會話時間限制
    MaxConnectionTime_Policy    = (Get-ItemProperty -Path $rdpPolicyPath -Name 'MaxConnectionTime' -ErrorAction SilentlyContinue).MaxConnectionTime
    MaxConnectionTime_Config    = (Get-ItemProperty -Path $rdpConfigPath -Name 'MaxConnectionTime' -ErrorAction SilentlyContinue).MaxConnectionTime
    # 斷線後是否自動刪除會話（1 = 是，RE(1) 關鍵指標）
    fResetBroken_Policy         = (Get-ItemProperty -Path $rdpPolicyPath -Name 'fResetBroken' -ErrorAction SilentlyContinue).fResetBroken
    fResetBroken_Config         = (Get-ItemProperty -Path $rdpConfigPath -Name 'fResetBroken' -ErrorAction SilentlyContinue).fResetBroken
}

# ── SR 3.8：RDP NLA（網路層級驗證）狀態 ──
# NLA 在會話建立前即驗證身份，強化會話完整性
$nlaEnabled = @{
    UserAuthentication_Policy = (Get-ItemProperty -Path $rdpPolicyPath -Name 'UserAuthentication' -ErrorAction SilentlyContinue).UserAuthentication
    UserAuthentication_Config = (Get-ItemProperty -Path $rdpConfigPath -Name 'UserAuthentication' -ErrorAction SilentlyContinue).UserAuthentication
    SecurityLayer_Config      = (Get-ItemProperty -Path $rdpConfigPath -Name 'SecurityLayer' -ErrorAction SilentlyContinue).SecurityLayer
}

# ── SR 3.8 RE(2)：列出目前作用中的登入會話（驗證 Session ID 唯一性） ──
$activeSessions = @()
try {
    $quser = quser 2>$null
    if ($quser) {
        $activeSessions = $quser | Select-Object -Skip 1 | ForEach-Object {
            $line = $_.Trim() -replace '\s{2,}', ','
            $parts = $line -split ','
            @{
                Username    = $parts[0]
                SessionName = if ($parts.Count -ge 4) { $parts[1] } else { '' }
                SessionId   = if ($parts.Count -ge 4) { $parts[2] } else { $parts[1] }
                State       = if ($parts.Count -ge 4) { $parts[3] } else { $parts[2] }
                IdleTime    = if ($parts.Count -ge 5) { $parts[4] } else { '' }
                LogonTime   = if ($parts.Count -ge 6) { $parts[5] } else { '' }
            }
        }
    }
} catch { }

# ── SR 3.8：SMB 伺服器會話逾時設定 ──
$smbConfig = @{}
try {
    $smb = Get-SmbServerConfiguration -ErrorAction SilentlyContinue
    $smbConfig = @{
        AutoDisconnectTimeout  = $smb.AutoDisconnectTimeout    # 閒置自動斷線（分鐘）
        EnableSecuritySignature = $smb.EnableSecuritySignature  # SMB 簽章（完整性保護）
        RequireSecuritySignature = $smb.RequireSecuritySignature # 強制 SMB 簽章
        EncryptData             = $smb.EncryptData              # SMB 加密
    }
} catch { }

# ── SR 3.8：WinRM 會話逾時設定 ──
$winrmConfig = @{}
try {
    $winrm = Get-Item WSMan:\localhost\Shell -ErrorAction SilentlyContinue
    $winrmConfig = @{
        MaxConcurrentUsers = (Get-Item WSMan:\localhost\Shell\MaxConcurrentUsers -ErrorAction SilentlyContinue).Value
        IdleTimeout        = (Get-Item WSMan:\localhost\Shell\IdleTimeout -ErrorAction SilentlyContinue).Value
        MaxShellRunTime    = (Get-Item WSMan:\localhost\Shell\MaxShellRunTime -ErrorAction SilentlyContinue).Value
        MaxProcessesPerShell = (Get-Item WSMan:\localhost\Shell\MaxProcessesPerShell -ErrorAction SilentlyContinue).Value
    }
} catch { }

@{
    RdpSessionTimeout  = $rdpTimeout
    RdpNlaEnabled      = $nlaEnabled
    ActiveSessions     = @($activeSessions)
    SmbSessionSettings = $smbConfig
    WinRmSessionConfig = $winrmConfig
} | ConvertTo-Json -Depth 4
";
}
