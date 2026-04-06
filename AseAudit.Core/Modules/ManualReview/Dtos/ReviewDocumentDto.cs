using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ManualReview.Dtos
{
    public sealed class ReviewDocumentDto
    {
        public string DeviceId { get; set; } = "";
        public string DeviceName { get; set; } = "";

        public string FileName { get; set; } = "";
        public string FilePath { get; set; } = "";
        public string FileUrl { get; set; } = "";

        public ReviewDocumentContentType ContentType { get; set; }

        public bool FileExists { get; set; }

        public string DisplayTypeText =>
            ContentType == ReviewDocumentContentType.Pdf ? "PDF" :
            ContentType == ReviewDocumentContentType.Image ? "圖片" :
            "未知格式";
    }
}