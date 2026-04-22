using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class RecoveryTargetBaselineDto
    {
        public string DeviceId { get; set; } = "";

        /// <summary>目標 RPO（分鐘）</summary>
        public int? TargetRpoMinutes { get; set; }

        /// <summary>目標 RTO（分鐘）</summary>
        public int? TargetRtoMinutes { get; set; }

        /// <summary>是否允許使用者覆寫系統預設</summary>
        public bool AllowOverride { get; set; } = true;

        public string? Source { get; set; }
    }
}
