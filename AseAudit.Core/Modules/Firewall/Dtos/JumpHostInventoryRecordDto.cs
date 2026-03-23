using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// 跳板主機清單
    /// </summary>
    public sealed class JumpHostInventoryRecordDto
    {
        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 跳板主機名稱
        /// </summary>
        public string HostName { get; set; } = "";

        /// <summary>
        /// IP 位址
        /// </summary>
        public string IpAddress { get; set; } = "";

        /// <summary>
        /// 是否啟用
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
