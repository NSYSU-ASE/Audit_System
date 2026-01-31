using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.Identity.Dtos
{
    public sealed class EmployeeDirectoryRecordDto
    {
        public string AdAccount { get; set; } = "";
        public bool IsActive { get; set; }
    }
}
