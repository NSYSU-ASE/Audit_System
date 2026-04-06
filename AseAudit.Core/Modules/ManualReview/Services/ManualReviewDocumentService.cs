using AseAudit.Core.Modules.ManualReview.Dtos;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AseAudit.Core.Modules.ManualReview.Services
{
    public sealed class ManualReviewDocumentService
    {
        private readonly string _reviewFilesRootPath;

        public ManualReviewDocumentService(string reviewFilesRootPath)
        {
            _reviewFilesRootPath = reviewFilesRootPath ?? "";
        }

        public ReviewDocumentDto GetByDeviceId(string deviceId, string? deviceName = null)
        {
            deviceId = (deviceId ?? "").Trim();

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                return new ReviewDocumentDto
                {
                    DeviceId = "",
                    DeviceName = deviceName ?? "",
                    FileExists = false
                };
            }

            if (!Directory.Exists(_reviewFilesRootPath))
            {
                return new ReviewDocumentDto
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName ?? "",
                    FileExists = false
                };
            }

            var supportedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

            var file = Directory
                .GetFiles(_reviewFilesRootPath)
                .FirstOrDefault(x =>
                {
                    var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(x);
                    var ext = Path.GetExtension(x);

                    return string.Equals(fileNameWithoutExtension, deviceId, StringComparison.OrdinalIgnoreCase)
                           && supportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                });

            if (string.IsNullOrWhiteSpace(file))
            {
                return new ReviewDocumentDto
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName ?? "",
                    FileExists = false
                };
            }

            var extension = Path.GetExtension(file);
            var contentType = ResolveContentType(extension);

            return new ReviewDocumentDto
            {
                DeviceId = deviceId,
                DeviceName = deviceName ?? "",
                FileName = Path.GetFileName(file),
                FilePath = file,
                FileUrl = $"/review-files/{Path.GetFileName(file)}",
                ContentType = contentType,
                FileExists = true
            };
        }

        public IReadOnlyList<ReviewDocumentDto> GetAll()
        {
            if (!Directory.Exists(_reviewFilesRootPath))
            {
                return Array.Empty<ReviewDocumentDto>();
            }

            var supportedExtensions = new[] { ".pdf", ".png", ".jpg", ".jpeg", ".webp", ".bmp" };

            var files = Directory
                .GetFiles(_reviewFilesRootPath)
                .Where(x => supportedExtensions.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                .Select(x =>
                {
                    var extension = Path.GetExtension(x);
                    return new ReviewDocumentDto
                    {
                        DeviceId = Path.GetFileNameWithoutExtension(x),
                        DeviceName = Path.GetFileNameWithoutExtension(x),
                        FileName = Path.GetFileName(x),
                        FilePath = x,
                        FileUrl = $"/review-files/{Path.GetFileName(x)}",
                        ContentType = ResolveContentType(extension),
                        FileExists = true
                    };
                })
                .OrderBy(x => x.DeviceId)
                .ToList();

            return files;
        }

        private static ReviewDocumentContentType ResolveContentType(string? extension)
        {
            extension = (extension ?? "").Trim().ToLowerInvariant();

            return extension switch
            {
                ".pdf" => ReviewDocumentContentType.Pdf,
                ".png" => ReviewDocumentContentType.Image,
                ".jpg" => ReviewDocumentContentType.Image,
                ".jpeg" => ReviewDocumentContentType.Image,
                ".webp" => ReviewDocumentContentType.Image,
                ".bmp" => ReviewDocumentContentType.Image,
                _ => ReviewDocumentContentType.Unknown
            };
        }
    }
}