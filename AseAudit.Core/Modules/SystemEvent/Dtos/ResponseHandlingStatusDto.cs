using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace AseAudit.Core.Modules.SystemEvent.Dtos
{
    public sealed class ResponseHandlingStatusDto
    {
        // SR2.10 對稽核處理不成功之回應
        public bool HasResponseProcedure { get; set; }
        public string? ProcedureDocumentNo { get; set; }
        public bool HasExecutionRecord { get; set; }
        public DateTime? LastHandledAt { get; set; }
    }
}