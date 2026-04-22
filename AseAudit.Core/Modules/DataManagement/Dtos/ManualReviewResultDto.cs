using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class ManualReviewResultDto
    {
        /// <summary>例如 data.3.1 / data.3.5</summary>
        public string ItemKey { get; set; } = "";

        public string DeviceId { get; set; } = "";

        /// <summary>是否已完成人工審查</summary>
        public bool IsReviewed { get; set; }

        /// <summary>是否通過</summary>
        public bool IsPass { get; set; }

        /// <summary>是否部分符合</summary>
        public bool IsPartial { get; set; }

        /// <summary>人工審查意見</summary>
        public string? Comment { get; set; }

        /// <summary>對應的證據檔名，可連到 ManualReview</summary>
        public string? EvidenceFileName { get; set; }
    }
}
