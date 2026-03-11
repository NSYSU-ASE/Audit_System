using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// 各廠棟網域 / 網段對照表
    /// </summary>
    public sealed class DomainTableRecordDto
    {
        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 網段名稱 / 區域名稱
        /// </summary>
        public string SegmentName { get; set; } = "";

        /// <summary>
        /// 網段，例如 10.10.1.0/24
        /// </summary>
        public string NetworkCidr { get; set; } = "";
    }
}
