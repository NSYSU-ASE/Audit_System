namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 通信完整性快照 — 驗證傳輸資訊的完整性保護機制。
///
/// 涵蓋驗證項目：
///   SR 3.1 #1 — 控制系統是否具備保護傳輸資訊完整性的能力
///   SR 3.1 #2 — 是否針對不同網路類型（TCP/IP、串接埠迴路）採用適當的完整性保護機制
///               收集 TLS/SSL 設定、SChannel 協定啟用狀態、SMB 簽章設定
///   SR 3.1 RE(1) #6 — 是否使用加密機制（如訊息認證碼、雜湊）識別通信或資訊的變更
///               收集憑證、加密套件設定
///
///   【無法程式化驗證項目】
///   SR 3.1 #3 — 網路基礎設施設計是否已考量環境因素對通信完整性的影響（微粒、液體、振動、EMI等）
///               → 需現場實體稽核
///   SR 3.1 #4 — 是否使用適當的實體連接器（可封 RJ-45、M12、屏蔽雙絞線、光纖等）
///               → 需現場實體稽核
///   SR 3.1 #5 — 無線網路是否已執行頻譜分析或驗證可行性
///               → 需專用無線分析工具與現場稽核
///
///   【區域層級 Zone — 需搭配架構文件與政策審查】
///   #7 — 各區域是否依據目標安全等級（SL-T）選用對應的通信完整性控制措施
///   #8 — 跨區域管道（Conduit）的通信是否實施完整性保護
///   #9 — 區域間之通信完整性保護是否已納入區域安全政策
///
///   【元件層級 CR 3.1 — 需個別元件測試】
///   #10 — 元件是否具備保護傳輸資訊完整性的能力
///   #11 — 元件是否能面對環境干擾（EMI、振動等）維持通信信號完整性
///   CR 3.1 RE(1) #12 — 元件是否具備在接收時驗證資訊的實時性（通信認證）的能力
///
/// 輸出：JSON 物件
///   - TlsProtocols: SChannel 協定啟用狀態（TLS 1.0/1.1/1.2/1.3、SSL 2.0/3.0）
///   - CipherSuites: 系統啟用的加密套件清單
///   - SmbSigning: SMB 簽章設定（用戶端與伺服器）
///   - WinRmEncryption: WinRM 加密與驗證設定
///   - CertificateStore: 本機憑證存放區中的伺服器憑證摘要
///   - DotNetStrongCrypto: .NET Framework 強加密設定
/// </summary>
public static class CommunicationIntegritySnapshot
{
    public const string Content = @"
# ── SR 3.1 #1 #2：SChannel 協定啟用狀態（TLS/SSL） ──
$protocols = @('SSL 2.0','SSL 3.0','TLS 1.0','TLS 1.1','TLS 1.2','TLS 1.3')
$tlsSettings = foreach ($proto in $protocols) {
    foreach ($side in @('Client','Server')) {
        $regPath = ""HKLM:\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\$proto\$side""
        $enabled = $null
        $disabledByDefault = $null
        if (Test-Path $regPath) {
            $enabled = (Get-ItemProperty -Path $regPath -Name 'Enabled' -ErrorAction SilentlyContinue).Enabled
            $disabledByDefault = (Get-ItemProperty -Path $regPath -Name 'DisabledByDefault' -ErrorAction SilentlyContinue).DisabledByDefault
        }
        @{
            Protocol          = $proto
            Side              = $side
            Enabled           = $enabled
            DisabledByDefault = $disabledByDefault
            RegistryExists    = (Test-Path $regPath)
        }
    }
}

# ── SR 3.1 RE(1) #6：啟用的加密套件 ──
$cipherSuites = try {
    Get-TlsCipherSuite -ErrorAction SilentlyContinue |
        Select-Object -First 30 |
        ForEach-Object {
            @{
                Name       = $_.Name
                Protocols  = $_.Protocols -join ','
                KeyType    = $_.KeyType
                Exchange   = $_.Exchange
            }
        }
} catch { @() }

# ── SR 3.1 #2：SMB 簽章設定（防止中間人攻擊） ──
$smbClient = Get-SmbClientConfiguration -ErrorAction SilentlyContinue |
    Select-Object RequireSecuritySignature, EnableSecuritySignature, EncryptionCiphers
$smbServer = Get-SmbServerConfiguration -ErrorAction SilentlyContinue |
    Select-Object RequireSecuritySignature, EnableSecuritySignature, EncryptData, RejectUnencryptedAccess

$smbSigning = @{
    Client = @{
        RequireSecuritySignature = $smbClient.RequireSecuritySignature
        EnableSecuritySignature  = $smbClient.EnableSecuritySignature
        EncryptionCiphers        = $smbClient.EncryptionCiphers
    }
    Server = @{
        RequireSecuritySignature = $smbServer.RequireSecuritySignature
        EnableSecuritySignature  = $smbServer.EnableSecuritySignature
        EncryptData              = $smbServer.EncryptData
        RejectUnencryptedAccess  = $smbServer.RejectUnencryptedAccess
    }
}

# ── SR 3.1 #2：WinRM 加密設定 ──
$winrmConfig = try {
    $svc = winrm get winrm/config/service 2>$null | Out-String
    $client = winrm get winrm/config/client 2>$null | Out-String
    @{ Service = $svc; Client = $client }
} catch { @{ Service = 'N/A'; Client = 'N/A' } }

# ── SR 3.1 RE(1) #6：本機憑證存放區伺服器憑證 ──
$certs = Get-ChildItem Cert:\LocalMachine\My -ErrorAction SilentlyContinue |
    Select-Object -First 20 |
    ForEach-Object {
        @{
            Subject    = $_.Subject
            Issuer     = $_.Issuer
            NotAfter   = $_.NotAfter.ToString('o')
            NotBefore  = $_.NotBefore.ToString('o')
            Thumbprint = $_.Thumbprint
            HasPrivateKey = $_.HasPrivateKey
            SignatureAlgorithm = $_.SignatureAlgorithm.FriendlyName
        }
    }

# ── .NET Framework 強加密設定 ──
$dotnetCrypto = @()
$netPaths = @(
    'HKLM:\SOFTWARE\Microsoft\.NETFramework\v4.0.30319',
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\.NETFramework\v4.0.30319'
)
foreach ($p in $netPaths) {
    if (Test-Path $p) {
        $dotnetCrypto += @{
            Path = $p
            SchUseStrongCrypto = (Get-ItemProperty -Path $p -Name 'SchUseStrongCrypto' -ErrorAction SilentlyContinue).SchUseStrongCrypto
            SystemDefaultTlsVersions = (Get-ItemProperty -Path $p -Name 'SystemDefaultTlsVersions' -ErrorAction SilentlyContinue).SystemDefaultTlsVersions
        }
    }
}

@{
    TlsProtocols      = @($tlsSettings)
    CipherSuites       = @($cipherSuites)
    SmbSigning         = $smbSigning
    WinRmEncryption    = $winrmConfig
    CertificateStore   = @($certs)
    DotNetStrongCrypto = @($dotnetCrypto)
} | ConvertTo-Json -Depth 5
";
}
