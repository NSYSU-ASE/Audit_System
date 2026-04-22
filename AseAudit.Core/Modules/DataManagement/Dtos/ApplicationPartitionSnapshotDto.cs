using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.DataManagement.Dtos
{
    public sealed class ApplicationPartitionSnapshotDto
    {
        public string DeviceId { get; set; } = "";

        public bool HasSystemDisk { get; set; }
        public bool HasAppDisk { get; set; }
        public bool HasDataDisk { get; set; }
        public bool HasLogDisk { get; set; }

        public string? SystemDiskName { get; set; }
        public string? AppDiskName { get; set; }
        public string? DataDiskName { get; set; }
        public string? LogDiskName { get; set; }
    }
}
