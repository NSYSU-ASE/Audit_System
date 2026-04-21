using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class TopologyAssetRecordDto
    {
        public string AssetId { get; set; } = "";
        public string AssetName { get; set; } = "";
        public string? AssetType { get; set; }
        public string? IpAddress { get; set; }
        public string? Zone { get; set; }
        public string? SiteId { get; set; }
        public bool IsManaged { get; set; }
    }
}