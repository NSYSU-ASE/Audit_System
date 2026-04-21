namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [Identity] 帳號授權快照 — 針對 SR 2.1 授權執行的延伸驗證。
///
/// 涵蓋驗證項目：
///   SR 2.1 #3 — 確認無未授權帳號存在（列出所有本機帳號供白名單比對）
///   SR 2.1 #5 — 職責分離（SoD）：偵測同時屬於多個高權限群組的帳號
///   SR 2.1 #6 — 最小特權原則：列出每個帳號的有效群組成員資格
///   RE 2  #2 — 確認角色權限邊界清楚，無過度授權
///   RE 2  #3 — 確認使用者透過角色指派權限，而非直接 ACL 賦予
///
/// 輸出：JSON 物件
///   - AllLocalAccounts: 所有本機帳號基本資訊（供白名單比對）
///   - HighPrivilegeGroups: 高權限群組列表
///   - SoDViolations: 同時屬於 ≥2 個高權限群組的帳號（職責分離違規）
///   - UserGroupMemberships: 每個使用者的完整群組成員資格（最小特權審查）
///   - DirectAclAssignments: 具有直接資料夾 ACL 指派的使用者（繞過角色機制）
/// </summary>
public static class AccountAuthorizationSnapshot
{
    public const string Content = @"
# ── SR 2.1 #3：列出所有本機帳號（供白名單比對，偵測未授權帳號） ──
$allAccounts = Get-LocalUser | Select-Object Name, Enabled, LastLogon, PasswordRequired, UserMayChangePassword

# ── 定義高權限群組清單（Administrators、Remote Desktop Users 等） ──
$highPrivGroups = @('Administrators', 'Remote Desktop Users', 'Backup Operators',
                    'Power Users', 'Hyper-V Administrators')

# ── SR 2.1 #5 / RE 2 #2：職責分離（SoD）檢查 ──
#    找出同時屬於 2 個以上高權限群組的帳號
$userHighPriv = @{}
foreach ($gName in $highPrivGroups) {
    $members = Get-LocalGroupMember -Group $gName -ErrorAction SilentlyContinue |
               Select-Object -ExpandProperty Name
    foreach ($m in $members) {
        if (-not $userHighPriv.ContainsKey($m)) { $userHighPriv[$m] = @() }
        $userHighPriv[$m] += $gName
    }
}
$sodViolations = $userHighPriv.GetEnumerator() |
    Where-Object { $_.Value.Count -ge 2 } |
    ForEach-Object { @{ Account = $_.Key; Groups = $_.Value; GroupCount = $_.Value.Count } }

# ── SR 2.1 #6：每個使用者的完整群組成員資格（最小特權審查） ──
$userMemberships = Get-LocalUser | ForEach-Object {
    $u = $_
    $groups = Get-LocalGroup | Where-Object {
        (Get-LocalGroupMember -Group $_.Name -ErrorAction SilentlyContinue |
         Select-Object -ExpandProperty Name) -contains $u.Name
    } | Select-Object -ExpandProperty Name
    @{ Account = $u.Name; Enabled = $u.Enabled; Groups = @($groups) }
}

# ── RE 2 #3：偵測直接 ACL 指派（繞過角色 / 群組機制） ──
#    檢查關鍵系統目錄是否有非群組的個人帳號直接權限
$criticalPaths = @('C:\Windows\System32', 'C:\Program Files')
$directAcl = foreach ($p in $criticalPaths) {
    if (Test-Path $p) {
        $acl = Get-Acl $p -ErrorAction SilentlyContinue
        $acl.Access | Where-Object {
            $_.IdentityReference -notmatch 'BUILTIN\\|NT AUTHORITY\\|S-1-5-' -and
            $_.IdentityReference -notmatch 'CREATOR OWNER'
        } | ForEach-Object {
            @{ Path = $p; Identity = $_.IdentityReference.ToString()
               Rights = $_.FileSystemRights.ToString(); Type = $_.AccessControlType.ToString() }
        }
    }
}

@{
    AllLocalAccounts     = @($allAccounts)
    HighPrivilegeGroups  = $highPrivGroups
    SoDViolations        = @($sodViolations)
    UserGroupMemberships = @($userMemberships)
    DirectAclAssignments = @($directAcl)
} | ConvertTo-Json -Depth 5
";
}
