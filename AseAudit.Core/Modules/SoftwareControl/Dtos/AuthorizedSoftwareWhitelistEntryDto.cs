using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Software.Dtos
{
    public sealed class AuthorizedSoftwareWhitelistEntryDto
    {
        // 白名單顯示名稱（或關鍵字）
        public string Name { get; set; } = "";

        // 可選：限制廠商
        public string? Publisher { get; set; }

        // 可選：版本規則（先留著，現在不用也沒關係）
        public string? VersionPrefix { get; set; }

        // 比對模式：Exact / Contains（不想做 enum 也可以用 string）
        public string MatchMode { get; set; } = "Contains";
    }
}
