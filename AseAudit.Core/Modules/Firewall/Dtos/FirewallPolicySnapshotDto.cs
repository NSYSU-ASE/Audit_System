using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// 防火牆策略設定快照
    /// </summary>
    public sealed class FirewallPolicySnapshotDto
    {
        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 是否採用預設拒絕（default deny）策略
        /// </summary>
        public bool DefaultDenyEnabled { get; set; }

        /// <summary>
        /// 是否啟用未信任網路存取控制
        /// </summary>
        public bool UntrustedNetworkAccessControlEnabled { get; set; }

        /// <summary>
        /// 是否明確要求存取核可
        /// </summary>
        public bool ExplicitAccessApprovalRequired { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        public string? Remark { get; set; }
    }
}