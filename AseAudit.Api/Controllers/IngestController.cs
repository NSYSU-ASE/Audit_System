using AseAudit.Api.Models.Ingest;
using AseAudit.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace AseAudit.Api.Controllers;

/// <summary>
/// 接收 AseAudit.Collector 上傳之稽核 JSON 資料的端點。
/// 對應 appsettings.json 中的 "AgentIngest" Kestrel endpoint (預設 :5001)。
/// </summary>
[ApiController]
[Route("api/ingest")]
public class IngestController : ControllerBase
{
    private readonly IAuditIngestService _ingestService;
    private readonly ILogger<IngestController> _logger;
    private readonly string? _expectedApiKey;

    private const string ApiKeyHeader = "X-Audit-ApiKey";

    public IngestController(
        IAuditIngestService ingestService,
        IConfiguration configuration,
        ILogger<IngestController> logger)
    {
        _ingestService = ingestService;
        _logger = logger;
        _expectedApiKey = configuration["Ingest:ApiKey"];
    }

    /// <summary>健康檢查 - Collector 啟動時可先確認 Server 是否可達。</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new
    {
        status = "ok",
        serverTime = DateTime.UtcNow
    });

    /// <summary>上傳單一腳本快照。</summary>
    [HttpPost("snapshot")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuditIngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadSnapshot(
        [FromBody] AuditSnapshotUpload upload,
        CancellationToken cancellationToken)
    {
        if (!IsApiKeyValid()) return Unauthorized(new { error = "Invalid or missing API key." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        try
        {
            var result = await _ingestService.StoreAsync(upload, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ingest snapshot {Script} from {Host}",
                upload.ScriptName, upload.HostName);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { error = "Failed to store snapshot." });
        }
    }

    /// <summary>批次上傳多份腳本快照 (建議 Collector 一次稽核完使用)。</summary>
    [HttpPost("batch")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(AuditBatchIngestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadBatch(
        [FromBody] AuditBatchUpload batch,
        CancellationToken cancellationToken)
    {
        if (!IsApiKeyValid()) return Unauthorized(new { error = "Invalid or missing API key." });
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var response = new AuditBatchIngestResponse
        {
            HostName = batch.HostName,
            ReceivedAt = DateTime.UtcNow,
            TotalCount = batch.Snapshots.Count
        };

        foreach (var snapshot in batch.Snapshots)
        {
            // 若批次層提供 HostName/HostId，未填者沿用之
            if (string.IsNullOrWhiteSpace(snapshot.HostName)) snapshot.HostName = batch.HostName;
            if (string.IsNullOrWhiteSpace(snapshot.HostId)) snapshot.HostId = batch.HostId;
            if (snapshot.CollectedAt == default) snapshot.CollectedAt = batch.CollectedAt;

            try
            {
                var stored = await _ingestService.StoreAsync(snapshot, cancellationToken);
                response.Results.Add(stored);
                response.SuccessCount++;
            }
            catch (Exception ex)
            {
                response.FailedCount++;
                response.Errors.Add($"{snapshot.ScriptName}: {ex.Message}");
                _logger.LogError(ex, "Batch ingest failed for {Script} from {Host}",
                    snapshot.ScriptName, batch.HostName);
            }
        }

        return Ok(response);
    }

    private bool IsApiKeyValid()
    {
        // 未設定 ApiKey 視為開放模式 (dev/local)
        if (string.IsNullOrEmpty(_expectedApiKey)) return true;

        if (!Request.Headers.TryGetValue(ApiKeyHeader, out var provided)) return false;
        return string.Equals(provided.ToString(), _expectedApiKey, StringComparison.Ordinal);
    }
}
