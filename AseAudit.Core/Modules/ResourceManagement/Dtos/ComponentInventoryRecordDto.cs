using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ResourceManagement.Dtos
{
    public sealed class ComponentInventoryRecordDto
    {
        public string ComponentId { get; set; } = "";
        public string ComponentName { get; set; } = "";
        public string? ComponentType { get; set; }
        public string? Version { get; set; }
        public string? Vendor { get; set; }
        public string? DeviceId { get; set; }
        public bool IsActive { get; set; }
    }
}