namespace AseAudit.Collector.Script_lib;

/// <summary>
/// [DataManagement] 控制系統備份快照 — 驗證備份機制與備份保護。
///
/// 涵蓋驗證項目：
///   SR 7.3 #1 — 控制系統是否支援關鍵檔案的識別和定位（SL 1）
///   SR 7.3 #2 — 是否能進行使用者層級和系統層級資訊（含系統狀態）的備份
///               收集 Windows Backup 設定、VSS 快照、系統還原點
///   SR 7.3 #3 — 備份過程是否不影響正常工廠運作
///               收集備份排程與執行歷史
///   SR 7.3 #4 — 事後鑑識所需資訊（如稽核來源）是否包含在備份清單中
///               收集備份涵蓋範圍設定
///   SR 7.3 #5 — 備份中若含機密資訊，是否已考慮加密保護
///               收集 BitLocker 與備份加密設定
///   SR 7.3 RE(1) #6 — 是否能驗證備份機制的可靠性（SL 2）
///               收集最近備份的驗證狀態
///   SR 7.3 RE(2) #7 — 是否能基於可設定頻率自動執行備份（SL 3～4）
///               收集自動備份排程設定
///
///   【區域層級 Zone】
///   #8 — 各區域是否依據安全等級對備份要求納入區域安全政策
///   #9 — 區域內所有關鍵資產是否均納入備份計畫
///       → 需搭配架構文件與政策審查
///
///   【元件層級 CR 7.3】
///   #10 — 元件是否能配合系統等級的備份操作（保護元件狀態、使用者和系統級資訊）（SL 1）
///   #11 — 備份過程是否不影響正常元件操作
///   #12 — 備份機制是否包含對備份中敏感資訊（加密密鑰等）的保護（如備份加密）
///   #13 — 加密密鑰是否與備份分開儲存
///   CR 7.3 RE(1) #14 — 元件是否能在恢復前驗證備份資訊的完整性（SL 2～4）
///       → 需個別元件測試
///
/// 輸出：JSON 物件
///   - WindowsBackup: Windows Server Backup 設定與歷史
///   - VSSSnapshots: VSS 磁碟區陰影複製快照清單
///   - SystemRestorePoints: 系統還原點
///   - BitLockerStatus: BitLocker 加密狀態（備份加密指標）
///   - BackupScheduledTasks: 與備份相關的排程任務
///   - WBAdminHistory: wbadmin 備份歷史記錄
/// </summary>
public static class SystemBackupSnapshot
{
    public const string Content = @"
# ── SR 7.3 #1 #2：Windows Server Backup 設定 ──
$wbPolicy = try {
    if (Get-Command Get-WBPolicy -ErrorAction SilentlyContinue) {
        $policy = Get-WBPolicy -ErrorAction SilentlyContinue
        if ($policy) {
            @{
                Configured     = $true
                Schedule       = @($policy.Schedule)
                BackupTargets  = @($policy.BackupTarget | ForEach-Object { $_.Label })
                VolumesToBackup = @($policy.VolumesToBackup | ForEach-Object { $_.MountPath })
                SystemState    = $policy.BMR
            }
        } else { @{ Configured = $false } }
    } else { @{ WBFeatureInstalled = $false } }
} catch { @{ WBFeatureInstalled = $false } }

# ── SR 7.3 #2：VSS 磁碟區陰影複製快照 ──
$vssSnapshots = try {
    $shadows = Get-CimInstance Win32_ShadowCopy -ErrorAction SilentlyContinue |
        Select-Object -First 20 |
        ForEach-Object {
            @{
                ID            = $_.ID
                VolumeName    = $_.VolumeName
                InstallDate   = $_.InstallDate.ToString('o')
                DeviceObject  = $_.DeviceObject
                State         = $_.State
                ClientAccessible = $_.ClientAccessible
            }
        }
    @($shadows)
} catch { @() }

# ── SR 7.3 #2：系統還原點 ──
$restorePoints = try {
    Get-ComputerRestorePoint -ErrorAction SilentlyContinue |
        Select-Object -First 10 |
        ForEach-Object {
            @{
                SequenceNumber = $_.SequenceNumber
                Description    = $_.Description
                RestorePointType = $_.RestorePointType
                CreationTime   = $_.CreationTime
            }
        }
} catch { @() }

# ── SR 7.3 #5：BitLocker 加密狀態 ──
$bitlocker = try {
    if (Get-Command Get-BitLockerVolume -ErrorAction SilentlyContinue) {
        Get-BitLockerVolume -ErrorAction SilentlyContinue |
            ForEach-Object {
                @{
                    MountPoint       = $_.MountPoint
                    VolumeStatus     = $_.VolumeStatus.ToString()
                    ProtectionStatus = $_.ProtectionStatus.ToString()
                    EncryptionMethod = $_.EncryptionMethod.ToString()
                    LockStatus       = $_.LockStatus.ToString()
                    KeyProtector     = @($_.KeyProtector | ForEach-Object { $_.KeyProtectorType.ToString() })
                }
            }
    } else { @{ BitLockerAvailable = $false } }
} catch { @{ BitLockerAvailable = $false } }

# ── SR 7.3 RE(2) #7：與備份相關的排程任務 ──
$backupTasks = try {
    Get-ScheduledTask -ErrorAction SilentlyContinue |
        Where-Object { $_.TaskName -match 'backup|Backup|wbadmin|VSS|shadow' -or $_.TaskPath -match 'Backup' } |
        ForEach-Object {
            $info = $_ | Get-ScheduledTaskInfo -ErrorAction SilentlyContinue
            @{
                TaskName      = $_.TaskName
                TaskPath      = $_.TaskPath
                State         = $_.State.ToString()
                LastRunTime   = if ($info) { $info.LastRunTime.ToString('o') } else { $null }
                NextRunTime   = if ($info) { $info.NextRunTime.ToString('o') } else { $null }
                LastResult    = if ($info) { $info.LastTaskResult } else { $null }
            }
        }
} catch { @() }

# ── SR 7.3 RE(1) #6：wbadmin 備份歷史 ──
$wbHistory = try {
    $history = wbadmin get versions 2>$null | Out-String
    @{ Output = $history.Trim() }
} catch { @{ Output = 'N/A' } }

@{
    WindowsBackup       = $wbPolicy
    VSSSnapshots        = @($vssSnapshots)
    SystemRestorePoints = @($restorePoints)
    BitLockerStatus     = @($bitlocker)
    BackupScheduledTasks = @($backupTasks)
    WBAdminHistory      = $wbHistory
} | ConvertTo-Json -Depth 5
";
}
