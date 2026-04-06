namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class SessionControlStatusDto
    {
        // SR2.5 會期鎖 / 自動登出
        public bool FeatureAvailable { get; set; }   // 這台設備是否具備這類功能
        public bool HasScreenSaverLock { get; set; }
        public bool HasAutoLogout { get; set; }
        public int? IdleTimeoutMinutes { get; set; } // null = 未設定
    }
}
