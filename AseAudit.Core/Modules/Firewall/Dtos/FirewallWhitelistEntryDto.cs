using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// 防火牆白名單規則
    /// </summary>
    public sealed class FirewallWhitelistEntryDto
    {
        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 來源主機名稱（可空）
        /// </summary>
        public string? SourceHostName { get; set; }

        /// <summary>
        /// 來源 IP
        /// </summary>
        public string SourceIp { get; set; } = "";

        /// <summary>
        /// 目的網段 / 目的主機
        /// </summary>
        public string Destination { get; set; } = "";

        /// <summary>
        /// 通訊埠或服務名稱
        /// </summary>
        public string? PortOrService { get; set; }

        /// <summary>
        /// 是否允許
        /// </summary>
        public bool IsAllowed { get; set; } = true;
    }
}