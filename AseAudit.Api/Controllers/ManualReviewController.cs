using Microsoft.AspNetCore.Mvc;
using AseAudit.Core.Modules.ManualReview.Services;

namespace AseAuditApi.Controllers
{
    [ApiController]
    [Route("api/manual-review")]
    public sealed class ManualReviewController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public ManualReviewController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("{deviceId}")]
        public IActionResult GetByDeviceId(string deviceId)
        {
            var reviewFilesRootPath = Path.Combine(_env.WebRootPath, "review-files");
            var service = new ManualReviewDocumentService(reviewFilesRootPath);

            var result = service.GetByDeviceId(deviceId);

            if (!result.FileExists)
            {
                return NotFound(new
                {
                    message = "找不到對應的 PDF 或圖片檔案。",
                    deviceId
                });
            }

            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var reviewFilesRootPath = Path.Combine(_env.WebRootPath, "review-files");
            var service = new ManualReviewDocumentService(reviewFilesRootPath);

            var results = service.GetAll();
            return Ok(results);
        }
    }
}
