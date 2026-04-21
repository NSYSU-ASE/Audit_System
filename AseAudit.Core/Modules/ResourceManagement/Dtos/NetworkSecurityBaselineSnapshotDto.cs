using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class NetworkSecurityBaselineSnapshotDto
    {
        public string AssetId { get; set; } = "";
        public bool HasSecurityBaseline { get; set; }
        public bool HasZoneAssignment { get; set; }
        public bool HasAssetRegistration { get; set; }
        public bool HasReviewProcess { get; set; }
    }
}