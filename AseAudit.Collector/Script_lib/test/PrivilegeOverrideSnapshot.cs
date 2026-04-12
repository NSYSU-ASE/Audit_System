namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [AuditProcess] 權限覆蓋與雙人核准快照 — 驗證監督者覆蓋與核准機制。
///
/// 涵蓋驗證項目：
///   RE 3 #1 — 確認系統存在緊急覆蓋（Override）機制
///              偵測是否存在 Emergency / Bypass / Break-Glass 帳號或群組
///   RE 3 #3 — 確認覆蓋權限限制於特定角色（不對一般使用者開放）
///              驗證高權限帳號僅存在於 Administrators 群組，非一般使用者可取得
///   RE 4 #2 — 確認雙人核准不能由同一帳號完成（防自我核准）
///              分析帳號角色，確認申請人與核准人為不同身份
///
/// 輸出：JSON 物件
///   - EmergencyAccounts: 偵測到的緊急 / Bypass 帳號（名稱含 emergency, break, override 等）
///   - AdminGroupMembers: Administrators 群組成員清單
///   - NonAdminHighPrivUsers: 非 Administrators 群組但擁有高權限的使用者
///   - RunAsAdminProcesses: 目前以管理員權限執行的處理程序（即時覆蓋偵測）
///   - UserRightsAssignment: 本機安全性原則中的使用者權利指派（SeDebugPrivilege 等）
/// </summary>
public static class PrivilegeOverrideSnapshot
{
    public const string Content = @"
# ── RE 3 #1：偵測緊急 / Bypass 帳號 ──
#    比對帳號名稱是否含有 emergency、bypass、breakglass、override 等關鍵字
$emergencyKeywords = @('emergency', 'bypass', 'breakglass', 'break-glass',
                       'override', 'firecall', 'intruder')
$allUsers = Get-LocalUser
$emergencyAccounts = $allUsers | Where-Object {
    $name = $_.Name.ToLower()
    $emergencyKeywords | Where-Object { $name -like ""*$_*"" }
} | Select-Object Name, Enabled, LastLogon, PasswordRequired

# ── RE 3 #3：Administrators 群組成員 ──
$adminMembers = Get-LocalGroupMember -Group 'Administrators' -ErrorAction SilentlyContinue |
    Select-Object Name, ObjectClass, PrincipalSource

# ── 非 Administrators 但擁有高權限的使用者 ──
#    檢查 Remote Desktop Users、Backup Operators 等群組中不在 Admin 群組的帳號
$adminNames = $adminMembers | Select-Object -ExpandProperty Name
$otherPrivGroups = @('Remote Desktop Users', 'Backup Operators',
                     'Power Users', 'Hyper-V Administrators')
$nonAdminHighPriv = foreach ($gName in $otherPrivGroups) {
    $members = Get-LocalGroupMember -Group $gName -ErrorAction SilentlyContinue
    if ($members) {
        $members | Where-Object { $adminNames -notcontains $_.Name } |
        ForEach-Object {
            @{ Account = $_.Name; Group = $gName; ObjectClass = $_.ObjectClass.ToString() }
        }
    }
}

# ── RE 3 #1 補充：目前以提升權限執行的處理程序 ──
#    列出以 SYSTEM 或 Administrator 身份執行的非系統處理程序
$elevatedProcs = Get-WmiObject Win32_Process -ErrorAction SilentlyContinue |
    ForEach-Object {
        $owner = $_.GetOwner()
        if ($owner.ReturnValue -eq 0) {
            @{ ProcessName = $_.Name; PID = $_.ProcessId
               User = ""$($owner.Domain)\$($owner.User)"" }
        }
    } | Where-Object {
        $_.User -match 'Administrator' -and $_.ProcessName -notmatch 'svchost|csrss|wininit|services'
    }

# ── RE 4 #2：使用者權利指派（偵測可自我核准的高權限） ──
#    匯出本機安全性原則中的使用者權利指派
$tempFile = [System.IO.Path]::GetTempFileName()
$seceditDb = [System.IO.Path]::GetTempFileName()
secedit /export /cfg $tempFile /quiet 2>$null
$userRights = @{}
if (Test-Path $tempFile) {
    $inSection = $false
    Get-Content $tempFile | ForEach-Object {
        if ($_ -match '^\[Privilege Rights\]') { $inSection = $true }
        elseif ($_ -match '^\[' -and $inSection) { $inSection = $false }
        elseif ($inSection -and $_ -match '^(Se\w+)\s*=\s*(.+)$') {
            $userRights[$Matches[1]] = ($Matches[2] -split ',').Trim()
        }
    }
    Remove-Item $tempFile -Force -ErrorAction SilentlyContinue
}
Remove-Item $seceditDb -Force -ErrorAction SilentlyContinue

@{
    EmergencyAccounts     = @($emergencyAccounts)
    AdminGroupMembers     = @($adminMembers)
    NonAdminHighPrivUsers = @($nonAdminHighPriv)
    RunAsAdminProcesses   = @($elevatedProcs)
    UserRightsAssignment  = $userRights
} | ConvertTo-Json -Depth 5
";
}
