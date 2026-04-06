using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ManualReview.Dtos
{
    public sealed class ReviewDocumentQueryDto
    {
        public string DeviceId { get; set; } = "";
        public string? Keyword { get; set; }
    }
}