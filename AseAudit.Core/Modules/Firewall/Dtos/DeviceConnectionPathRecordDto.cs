using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Firewall.Dtos
{
    /// <summary>
    /// 裝置連線路徑紀錄
    /// 用來判斷是否經由跳板主機存取 OT / 控制系統
    /// </summary>
    public sealed class DeviceConnectionPathRecordDto
    {
        /// <summary>
        /// 裝置 ID
        /// </summary>
        public string DeviceId { get; set; } = "";

        /// <summary>
        /// 廠別 / 廠棟代碼
        /// </summary>
        public string SiteId { get; set; } = "";

        /// <summary>
        /// 使用者 / 帳號
        /// </summary>
        public string? UserAccount { get; set; }

        /// <summary>
        /// 是否經由跳板主機
        /// </summary>
        public bool ViaJumpHost { get; set; }

        /// <summary>
        /// 跳板主機名稱
        /// </summary>
        public string? JumpHostName { get; set; }

        /// <summary>
        /// 目標系統
        /// </summary>
        public string? TargetSystem { get; set; }
    }
}
