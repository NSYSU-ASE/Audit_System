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
        //
        // 未來系統拓展功能 
        // SR 1.4 驗證現場系統在主要帳號管理失效後人能在本地運作不會因此無法操作
        // SR 2.1

        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(HostAccountSnapshot),   HostAccountSnapshot.Content),                  
            (nameof(PasswordPolicySnapshot), PasswordPolicySnapshot.Content),              
            (nameof(UserGroupSnapshot),      UserGroupSnapshot.Content),                   
        ];
    }

    public static class SoftwareControl
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(InstalledProgramsSnapshot), InstalledProgramsSnapshot.Content),
            (nameof(AntivirusStatusSnapshot),   AntivirusStatusSnapshot.Content),
        ];
    }

    public static class Firewall
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            (nameof(FirewallPolicySnapshot),    FirewallPolicySnapshot.Content),
            (nameof(NetworkInterfaceSnapshot),  NetworkInterfaceSnapshot.Content),
        ];
    }

    public static class SystemEvent
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            // ↓ 新增腳本時在此加一行
        ];
    }

    public static class AuditProcess
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            // ↓ 新增腳本時在此加一行
        ];
    }

    public static class DataManagement
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            // ↓ 新增腳本時在此加一行
        ];
    }

    public static class SourceManagement
    {
        public static readonly IReadOnlyList<(string Name, string Content)> Scripts =
        [
            // ↓ 新增腳本時在此加一行
        ];
    }

    // ── 全域平坦字典（名稱 → 腳本內容，供動態查找） ────────────

    public static readonly IReadOnlyDictionary<string, string> All =
        new Dictionary<string, string>
        {
            // Identity
            [nameof(HostAccountSnapshot)]    = HostAccountSnapshot.Content,
            [nameof(PasswordPolicySnapshot)] = PasswordPolicySnapshot.Content,
            [nameof(UserGroupSnapshot)]      = UserGroupSnapshot.Content,

            // SoftwareControl
            [nameof(InstalledProgramsSnapshot)] = InstalledProgramsSnapshot.Content,
            [nameof(AntivirusStatusSnapshot)]   = AntivirusStatusSnapshot.Content,

            // Firewall
            [nameof(FirewallPolicySnapshot)]   = FirewallPolicySnapshot.Content,
            [nameof(NetworkInterfaceSnapshot)] = NetworkInterfaceSnapshot.Content,

            // ↓ 新增腳本時在此加一行
        };
}
