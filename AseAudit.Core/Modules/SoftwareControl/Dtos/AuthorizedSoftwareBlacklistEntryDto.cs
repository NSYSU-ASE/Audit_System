namespace AseAudit.Core.Modules.Software.Dtos
{
    public class AuthorizedSoftwareBlacklistEntryDto
    {
        // 黑名單顯示名稱（或關鍵字）
        public string Name { get; set; } = "";

        // 可選：限制廠商
        public string? Publisher { get; set; }

        // 可選：版本規則
        public string? VersionPrefix { get; set; }

        // 比對模式：Exact / Contains
        public string MatchMode { get; set; } = "Contains";
    }
}
