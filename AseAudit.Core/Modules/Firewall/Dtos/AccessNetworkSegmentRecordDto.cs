using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// Access 表：設備網段 / 存取清單
    /// </summary>
    public sealed class AccessNetworkSegmentRecordDto
    {
        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 設備 ID 或主機名稱
        /// </summary>
        public string DeviceId { get; set; } = "";

        /// <summary>
        /// 設備 IP
        /// </summary>
        public string DeviceIp { get; set; } = "";

        /// <summary>
        /// 所屬網段
        /// </summary>
        public string NetworkSegment { get; set; } = "";
    }
}
