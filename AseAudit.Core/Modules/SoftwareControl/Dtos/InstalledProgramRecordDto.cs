using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Software.Dtos
{
    public sealed class InstalledProgramRecordDto
    {
        public string DisplayName { get; set; } = "";
        public string? Publisher { get; set; }
        public string? DisplayVersion { get; set; }
    }
}