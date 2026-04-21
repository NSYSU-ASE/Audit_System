using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class ResourceManagementSnapshotDto
    {
        public string DeviceId { get; set; } = "";

        public ResourceMonitoringSnapshotDto? Monitoring { get; set; }
        public EmergencyPowerSnapshotDto? EmergencyPower { get; set; }

        public List<TopologyAssetRecordDto> TopologyAssets { get; set; } = new();
        public List<NetworkSecurityBaselineSnapshotDto> SecurityBaselines { get; set; } = new();
        public List<ComponentInventoryRecordDto> ComponentInventory { get; set; } = new();
    }
}