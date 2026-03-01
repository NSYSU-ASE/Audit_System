using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;

namespace AseAudit.Core.Modules.Software.Dtos
{
    public sealed class SoftwareInventorySnapshotDto
    {
        public List<InstalledProgramRecordDto> InstalledPrograms { get; set; } = new();
    }
}
