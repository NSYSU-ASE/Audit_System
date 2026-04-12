namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Firewall] 人對人通訊限制快照 — 驗證 SR 5.3 Restrict General Person-to-Person Communication。
///
/// 涵蓋驗證項目：
///   SR 5.3      — 控制系統是否能防止接收來自外部的一般人對人通訊
///                 （電子郵件、社交媒體、即時通訊等）
///   SR 5.3 RE(1) — 控制系統是否完全禁止發送和接收一般個人對個人訊息
///
/// 驗證策略：
///   1. 檢查常見通訊軟體（郵件客戶端、IM、社交媒體）是否安裝或正在執行
///   2. 檢查防火牆是否封鎖常見通訊協定埠（SMTP、POP3、IMAP、常見 IM 埠）
///   3. 檢查 SMTP 服務是否存在
///   4. 檢查 Outlook/Mail 等郵件應用程式設定
///
/// 輸出：JSON 物件
///   - CommSoftwareInstalled:  已安裝的通訊相關軟體清單
///   - CommProcessesRunning:   正在執行的通訊相關處理程序
///   - BlockedCommPorts:       防火牆對通訊協定埠的封鎖狀態
///   - SmtpServiceStatus:      SMTP 服務狀態
///   - OutboundCommRules:      出站通訊相關防火牆規則
/// </summary>
public static class PersonToPersonCommSnapshot
{
    public const string Content = @"
# ── SR 5.3：定義常見通訊軟體關鍵字 ──
$commKeywords = @(
    'Outlook', 'Thunderbird', 'Mail',
    'Teams', 'Slack', 'Skype', 'Zoom', 'Discord', 'Telegram', 'WeChat', 'LINE',
    'WhatsApp', 'Messenger', 'Signal',
    'Chrome Remote Desktop', 'AnyDesk', 'TeamViewer'
)

# ── SR 5.3：檢查已安裝的通訊相關軟體 ──
$regPaths = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*'
)
$installedComm = foreach ($path in $regPaths) {
    Get-ItemProperty -Path $path -ErrorAction SilentlyContinue |
        Where-Object { $_.DisplayName } |
        Where-Object {
            $name = $_.DisplayName
            $commKeywords | Where-Object { $name -like ""*$_*"" }
        } |
        Select-Object DisplayName, Publisher, InstallDate
}
$installedComm = @($installedComm | Sort-Object DisplayName -Unique)

# ── SR 5.3：檢查正在執行的通訊相關處理程序 ──
$commProcessNames = @(
    'OUTLOOK', 'thunderbird', 'Mail',
    'Teams', 'slack', 'Skype', 'Zoom', 'Discord', 'Telegram', 'WeChat', 'LINE',
    'WhatsApp', 'Signal', 'AnyDesk', 'TeamViewer'
)
$runningComm = Get-Process -ErrorAction SilentlyContinue |
    Where-Object {
        $proc = $_.ProcessName
        $commProcessNames | Where-Object { $proc -like ""*$_*"" }
    } |
    ForEach-Object {
        @{
            ProcessName = $_.ProcessName
            Id          = $_.Id
            Path        = $_.Path
        }
    }

# ── SR 5.3 / RE(1)：檢查防火牆對通訊協定埠的封鎖狀態 ──
$commPorts = @(
    @{ Port = 25;   Protocol = 'SMTP';    Description = '電子郵件發送' },
    @{ Port = 110;  Protocol = 'POP3';    Description = '電子郵件接收' },
    @{ Port = 143;  Protocol = 'IMAP';    Description = '電子郵件接收' },
    @{ Port = 465;  Protocol = 'SMTPS';   Description = '加密郵件發送' },
    @{ Port = 587;  Protocol = 'SMTP-SUB'; Description = '郵件提交' },
    @{ Port = 993;  Protocol = 'IMAPS';   Description = '加密郵件接收' },
    @{ Port = 995;  Protocol = 'POP3S';   Description = '加密郵件接收' },
    @{ Port = 5222; Protocol = 'XMPP';    Description = '即時通訊' },
    @{ Port = 5228; Protocol = 'Google-Push'; Description = 'Google 推播服務' },
    @{ Port = 6667; Protocol = 'IRC';     Description = 'IRC 聊天' }
)

$portBlockStatus = foreach ($cp in $commPorts) {
    $portStr = $cp.Port.ToString()
    # 檢查是否有出站封鎖規則涵蓋此埠
    $blockRules = Get-NetFirewallRule -Direction Outbound -Action Block -Enabled True -ErrorAction SilentlyContinue |
        ForEach-Object {
            $pf = $_ | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
            if ($pf.LocalPort -contains $portStr -or $pf.RemotePort -contains $portStr) { $_ }
        }
    @{
        Port        = $cp.Port
        Protocol    = $cp.Protocol
        Description = $cp.Description
        IsBlocked   = ($null -ne $blockRules -and @($blockRules).Count -gt 0)
        BlockRules  = @($blockRules | ForEach-Object { $_.DisplayName })
    }
}

# ── SR 5.3：SMTP 服務狀態 ──
$smtpService = @{}
try {
    $smtp = Get-Service -Name 'SMTPSVC' -ErrorAction SilentlyContinue
    if ($smtp) {
        $smtpService = @{
            Exists    = $true
            Status    = $smtp.Status.ToString()
            StartType = $smtp.StartType.ToString()
        }
    } else {
        $smtpService = @{ Exists = $false }
    }
} catch {
    $smtpService = @{ Exists = $false }
}

# ── SR 5.3 RE(1)：出站通訊相關防火牆規則彙總 ──
$outboundRules = Get-NetFirewallRule -Direction Outbound -Enabled True -ErrorAction SilentlyContinue |
    ForEach-Object {
        $rule = $_
        $appFilter = $rule | Get-NetFirewallApplicationFilter -ErrorAction SilentlyContinue
        $portFilter = $rule | Get-NetFirewallPortFilter -ErrorAction SilentlyContinue
        if ($appFilter.Program -and $appFilter.Program -ne 'Any') {
            $progName = $appFilter.Program
            $isComm = $commKeywords | Where-Object { $progName -like ""*$_*"" }
            if ($isComm) {
                @{
                    RuleName   = $rule.DisplayName
                    Action     = $rule.Action.ToString()
                    Program    = $appFilter.Program
                    LocalPort  = $portFilter.LocalPort
                    RemotePort = $portFilter.RemotePort
                }
            }
        }
    }

@{
    CommSoftwareInstalled = @($installedComm)
    CommProcessesRunning  = @($runningComm)
    BlockedCommPorts      = @($portBlockStatus)
    SmtpServiceStatus     = $smtpService
    OutboundCommRules     = @($outboundRules)
} | ConvertTo-Json -Depth 4
";
}
