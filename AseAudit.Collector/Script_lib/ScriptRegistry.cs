namespace AseAudit.Collector.Script_lib;

// ═══════════════════════════════════════════════════════════════
//  ScriptRegistry — Script_lib 資料夾的腳本名稱管理中心
//
//  用途：
//    1. 集中列舉所有腳本類別名稱，避免散落各處的字串魔法數字
//    2. 提供模組分組，方便 CollectionService / Worker 按模組批次執行
//    3. All 字典供動態查找（key = 類別名稱, value = 腳本內容）
//
//  新增腳本時：
//    ① 在 Script_lib/ 建立新的 .cs 檔（靜態類別 + Content const）
//    ② 在下方對應的 Module 區段加入 typeof(YourNewScript)
//    ③ 在 All 字典加入一行 [nameof(YourNewScript)] = YourNewScript.Content
// ═══════════════════════════════════════════════════════════════

public static class ScriptRegistry
{
    // ── 模組分組（按稽核模組分類） ────────────────────────────

    public static class Identity
    {   //
        // SR 1.1確認受AD監管 SR1.3 SR1.4確認管理一致性
        //
        // SR 1.5確認密碼是否所有帳號都須設定以及預設長度符合標準 SR 1.7密碼複雜度 SR 1.11 嘗試登入次數
        //
        // SR 2.1 簡單說就是群組功能的驗證，包含列出所有帳號（供白名單比對）、職責分離檢查、最小特權審查、直接 ACL 指派偵測等。
        // SR 1.10 SR 1.12 先以圖片作為分析後續在看如何改善
        //
        // SR 3.8 會話完整性（Session Integrity）：RDP/SMB/WinRM 會話逾時、NLA、Session ID 管理
        //   RE(1) 會話結束後 Session ID 無效  RE(2) 唯一 Session ID  RE(3) 隨機來源（無法程式化驗證）
        //
        // 未來系統拓展功能
        // SR 1.4 驗證現場系統在主要帳號管理失效後人能在本地運作不會因此無法操作
        //

        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(OsVersionSnapshot),          OsVersionSnapshot.Content),
            (nameof(HostAccountSnapshot),         HostAccountSnapshot.Content),
            (nameof(PasswordPolicySnapshot),      PasswordPolicySnapshot.Content),
            (nameof(UserGroupSnapshot),           UserGroupSnapshot.Content),
            (nameof(AccountAuthorizationSnapshot), AccountAuthorizationSnapshot.Content),
            (nameof(SessionIntegritySnapshot),    SessionIntegritySnapshot.Content),
        ];
    }

    public static class SoftwareControl
    {   // 這裡只收集資料所有識別由衷邀進行確認
        // SR 1.2 SR 7.8 識別所有安裝軟體  
        //
        // SR 2.3 SR 2.4 SR 6.2 以確認IPGuard安裝情況驗證這些條文
        //
        // SR 6.2 SR 3.2&RE 確認趨勢軟體防毒以認證條文-還沒加
        // 
        // 未來系統拓展功能 
        // 
        // 
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(InstalledProgramsSnapshot), InstalledProgramsSnapshot.Content),
            (nameof(AntivirusStatusSnapshot),   AntivirusStatusSnapshot.Content),
        ];
    }

    public static class Firewall
    {
        // SR 1.12 SR 1.13 先以防火牆有正常設定為主
        //
        // SR 2.6 對外連線均須透過跳板主機、本地端連線設置有idle timeout、本地詳細連線日誌
        // 缺少部分: 跳板主機timeout、例外規範檔案、使用者端有可操作的手動登出功能、Idle timeout 為可設定參數
        //
        // SR 5.1 網路分段（Network Segmentation）：網路介面/子網路/VLAN/路由表/IP轉發/DNS 配置
        //   RE(1) 實體隔離（需現場稽核）RE(2) 獨立網路服務  RE(3) 關鍵與非關鍵網路隔離
        //
        // SR 5.2 區域邊界保護（Zone Boundary Protection）：防火牆設定檔預設動作/IPsec/日誌/服務狀態
        //   RE(1) 預設拒絕策略  RE(2) 島嶼模式（需架構文件）RE(3) 通道失敗 Fail Close
        //
        // SR 5.3 限制人對人通訊：通訊軟體安裝偵測/通訊埠封鎖/SMTP 服務/出站規則
        //   RE(1) 完全禁止收發個人對個人訊息
        //
        // SR 7.1 DoS 防護：TCP/IP 堆疊強化/連線限制/服務復原/資源配額
        //   RE(1) 通訊負載管理（速率限制）RE(2) 限制 DoS 擴散至其他系統
        //
        // SR 6.2 SR 3.2&RE 確認趨勢軟體防毒以認證條文-還沒加
        //
        // 未來系統拓展功能
        //
        //
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(FirewallPolicySnapshot),       FirewallPolicySnapshot.Content),
            (nameof(NetworkInterfaceSnapshot),      NetworkInterfaceSnapshot.Content),
            (nameof(NetworkSegmentationSnapshot),   NetworkSegmentationSnapshot.Content),
            (nameof(ZoneBoundarySnapshot),          ZoneBoundarySnapshot.Content),
            (nameof(PersonToPersonCommSnapshot),    PersonToPersonCommSnapshot.Content),
            (nameof(DosProtectionSnapshot),         DosProtectionSnapshot.Content),
        ];
    }

    public static class SystemEvent
    {
        // RE 3 #2 — 覆蓋行為稽核日誌
        // RE 4 #3 — 雙人核准稽核紀錄
        //
        // EventStatusSnapshot — 各項稽核記錄是否開啟狀態
        // EventLogSnapshot    — 各類稽核事件日誌內容（含時間戳記、來源、類別、類型、事件ID、事件結果）
        //
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(SecurityEventLogSnapshot), SecurityEventLogSnapshot.Content),
            (nameof(EventStatusSnapshot),      EventStatusSnapshot.Content),
            (nameof(EventLogSnapshot),         EventLogSnapshot.Content),
        ];
    }

    public static class AuditProcess
    {
        // RE 3 #1 — 緊急覆蓋機制偵測
        // RE 3 #3 — 覆蓋權限角色限制
        // RE 4 #2 — 防自我核准驗證
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(PrivilegeOverrideSnapshot), PrivilegeOverrideSnapshot.Content),
        ];
    }

    public static class DataManagement
    {
        // SR 2.1 #4 — 各介面存取控制驗證
        //
        // SR 3.1 通信完整性：TLS/SSL 設定、SChannel 協定、SMB 簽章、憑證、加密套件
        //   RE(1) 加密完整性保護（訊息認證碼、雜湊）
        //   #3 #4 #5 實體連接器/環境因素/無線頻譜分析 → 需現場實體稽核
        //
        // SR 3.5 輸入驗證：DEP、ASLR、Exploit Guard ASR、IIS 請求過濾、ASP.NET 驗證
        //   #3 程式碼解釋器輸入篩選 → 需應用程式層級程式碼審查
        //
        // SR 3.6 確定性輸出：服務故障復原設定、系統當機控制、開機設定
        //
        // SR 3.7 錯誤處理：WER 設定、事件日誌保留策略、IIS 自訂錯誤頁、詳細錯誤顯示控制
        //   CR 3.7 元件層級（#7 #8 #9）→ 需個別元件/應用程式測試
        //
        // SR 3.9 保護稽核資訊：事件日誌 ACL、稽核原則、稽核工具完整性、WEF 轉發設定
        //   RE(1) 一次寫入媒體 → 需實體稽核 WORM 儲存裝置
        //
        // SR 4.2 資訊持久性：分頁檔清除、記憶體轉儲、暫存檔案、Credential Guard、休眠檔
        //   RE(1) 共享記憶體保護  RE(2) 驗證資訊抹除 → 需個別元件測試
        //
        // SR 5.4 應用程式分割：程序帳號隔離、虛擬化偵測、Windows 角色、多網卡、AppLocker
        //
        // SR 7.3 控制系統備份：Windows Backup、VSS 快照、還原點、BitLocker、備份排程
        //   RE(1) 備份驗證  RE(2) 自動備份
        //
        // SR 7.4 控制系統恢復與重建：WinRE、Secure Boot、UAC、SFC/DISM、Windows Update
        //   #7 恢復後全面測試 → 需人工恢復測試程序
        //
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(ListeningPortAccessSnapshot),      ListeningPortAccessSnapshot.Content),
            (nameof(CommunicationIntegritySnapshot),   CommunicationIntegritySnapshot.Content),
            (nameof(InputValidationSnapshot),           InputValidationSnapshot.Content),
            (nameof(DeterministicOutputSnapshot),       DeterministicOutputSnapshot.Content),
            (nameof(ErrorHandlingSnapshot),             ErrorHandlingSnapshot.Content),
            (nameof(AuditInfoProtectionSnapshot),       AuditInfoProtectionSnapshot.Content),
            (nameof(DataPersistenceSnapshot),           DataPersistenceSnapshot.Content),
            (nameof(ApplicationPartitionSnapshot),      ApplicationPartitionSnapshot.Content),
            (nameof(SystemBackupSnapshot),              SystemBackupSnapshot.Content),
            (nameof(SystemRecoverySnapshot),            SystemRecoverySnapshot.Content),
        ];
    }

    public static class SourceManagement
    {
        // RE 1 #1 — 服務帳號權限限縮
        // RE 1 #3 — 排程任務非高權限執行
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(ServiceAccountSnapshot), ServiceAccountSnapshot.Content),
        ];
    }

    // ── 全域平坦字典（名稱 → 腳本內容，供動態查找） ────────────

    public static readonly IReadOnlyDictionary<string, string> All =
        new Dictionary<string, string>
        {
            // Identity
            [nameof(OsVersionSnapshot)]      = OsVersionSnapshot.Content,
            [nameof(HostAccountSnapshot)]    = HostAccountSnapshot.Content,
            [nameof(PasswordPolicySnapshot)] = PasswordPolicySnapshot.Content,
            [nameof(UserGroupSnapshot)]      = UserGroupSnapshot.Content,
            [nameof(AccountAuthorizationSnapshot)] = AccountAuthorizationSnapshot.Content,

            // SoftwareControl
            [nameof(InstalledProgramsSnapshot)] = InstalledProgramsSnapshot.Content,
            [nameof(AntivirusStatusSnapshot)]   = AntivirusStatusSnapshot.Content,

            // Identity (SR 3.8)
            [nameof(SessionIntegritySnapshot)] = SessionIntegritySnapshot.Content,

            // Firewall
            [nameof(FirewallPolicySnapshot)]      = FirewallPolicySnapshot.Content,
            [nameof(NetworkInterfaceSnapshot)]     = NetworkInterfaceSnapshot.Content,
            [nameof(NetworkSegmentationSnapshot)]  = NetworkSegmentationSnapshot.Content,
            [nameof(ZoneBoundarySnapshot)]         = ZoneBoundarySnapshot.Content,
            [nameof(PersonToPersonCommSnapshot)]   = PersonToPersonCommSnapshot.Content,
            [nameof(DosProtectionSnapshot)]        = DosProtectionSnapshot.Content,

            // SystemEvent
            [nameof(SecurityEventLogSnapshot)]  = SecurityEventLogSnapshot.Content,

            // AuditProcess
            [nameof(PrivilegeOverrideSnapshot)] = PrivilegeOverrideSnapshot.Content,

            // DataManagement
            [nameof(ListeningPortAccessSnapshot)]    = ListeningPortAccessSnapshot.Content,
            [nameof(CommunicationIntegritySnapshot)] = CommunicationIntegritySnapshot.Content,
            [nameof(InputValidationSnapshot)]         = InputValidationSnapshot.Content,
            [nameof(DeterministicOutputSnapshot)]     = DeterministicOutputSnapshot.Content,
            [nameof(ErrorHandlingSnapshot)]            = ErrorHandlingSnapshot.Content,
            [nameof(AuditInfoProtectionSnapshot)]     = AuditInfoProtectionSnapshot.Content,
            [nameof(DataPersistenceSnapshot)]          = DataPersistenceSnapshot.Content,
            [nameof(ApplicationPartitionSnapshot)]    = ApplicationPartitionSnapshot.Content,
            [nameof(SystemBackupSnapshot)]             = SystemBackupSnapshot.Content,
            [nameof(SystemRecoverySnapshot)]           = SystemRecoverySnapshot.Content,

            // SourceManagement
            [nameof(ServiceAccountSnapshot)] = ServiceAccountSnapshot.Content,

            // SystemEvent (new)
            [nameof(EventStatusSnapshot)]  = EventStatusSnapshot.Content,
            [nameof(EventLogSnapshot)]     = EventLogSnapshot.Content,

            // ↓ 新增腳本時在此加一行
        };
}
