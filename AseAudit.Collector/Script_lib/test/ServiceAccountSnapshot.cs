namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [SourceManagement] 服務帳號與排程任務快照 — 驗證實體授權。
///
/// 涵蓋驗證項目：
///   RE 1 #1 — 確認服務帳號（Service Account）具有獨立且限縮的權限
///              列出所有 Windows 服務及其執行身份（LogOnAs），識別使用高權限帳號的服務
///   RE 1 #3 — 確認自動化流程（腳本、排程）不以高權限帳號執行
///              列出所有排程任務及其執行帳號，標記以 SYSTEM / Administrator 執行者
///
/// 輸出：JSON 物件
///   - Services: 所有 Windows 服務的名稱、狀態、啟動類型、執行帳號
///   - HighPrivilegeServices: 以 LocalSystem 等高權限身份執行的服務（需審查）
///   - ScheduledTasks: 所有排程任務的名稱、狀態、執行帳號、觸發器
///   - HighPrivilegeScheduledTasks: 以高權限帳號執行的排程任務（需審查）
/// </summary>
public static class ServiceAccountSnapshot
{
    public const string Content = @"
# ── RE 1 #1：列出所有 Windows 服務及其執行身份 ──
$services = Get-WmiObject Win32_Service |
    Select-Object Name, DisplayName, State, StartMode, StartName

# 識別以高權限帳號執行的服務（LocalSystem、Administrator 等）
$highPrivSvc = $services | Where-Object {
    $_.StartName -match 'LocalSystem|LocalService|NetworkService' -or
    $_.StartName -match 'Administrator'
}

# ── RE 1 #3：列出所有排程任務及其執行帳號 ──
$tasks = Get-ScheduledTask -ErrorAction SilentlyContinue | ForEach-Object {
    $info = $_ | Get-ScheduledTaskInfo -ErrorAction SilentlyContinue
    @{
        TaskName    = $_.TaskName
        TaskPath    = $_.TaskPath
        State       = $_.State.ToString()
        RunAsUser   = $_.Principal.UserId
        LogonType   = $_.Principal.LogonType.ToString()
        LastRunTime = if ($info) { $info.LastRunTime.ToString('o') } else { $null }
    }
}

# 識別以高權限帳號執行的排程任務
$highPrivTasks = $tasks | Where-Object {
    $_.RunAsUser -match 'SYSTEM|Administrator|Administrators' -or
    $_.LogonType -eq 'ServiceAccount'
}

@{
    Services                  = @($services)
    HighPrivilegeServices     = @($highPrivSvc)
    ScheduledTasks            = @($tasks)
    HighPrivilegeScheduledTasks = @($highPrivTasks)
} | ConvertTo-Json -Depth 4
";
}
