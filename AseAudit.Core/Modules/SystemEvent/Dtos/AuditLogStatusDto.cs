using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class AuditLogStatusDto
    {
        // SR2.8 可稽核事件
        public bool AlarmFileEnabled { get; set; }
        public bool AlarmFileIsRecordingNormally { get; set; }

        public bool WindowsEventEnabled { get; set; }
        public bool WindowsEventIsRecordingNormally { get; set; }
    }
}