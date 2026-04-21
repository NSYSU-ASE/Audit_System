using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace AseAudit.Api.Controllers;

/// <summary>
/// 測試用端點:單純接收 Collector (或任意 client) 上傳的 JSON,
/// 將內容印到 Console 並回傳一份摘要,讓你在另一台電腦驗證網路連線/上傳是否成功。
///
/// 對應 Kestrel 的 AgentIngest endpoint (http://0.0.0.0:5001 by default)。
/// </summary>
[ApiController]
[Route("api/test")]
public class TestApiController : ControllerBase
{
    private static readonly JsonSerializerOptions PrettyJson = new() { WriteIndented = true };

    private readonly ILogger<TestApiController> _logger;

    public TestApiController(ILogger<TestApiController> logger)
    {
        _logger = logger;
    }

    /// <summary>健康檢查 - 用瀏覽器或 curl 確認 server 可達。</summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        var msg = $"[TestApi] PING from {HttpContext.Connection.RemoteIpAddress} at {DateTime.Now:HH:mm:ss}";
        Console.WriteLine(msg);
        _logger.LogInformation("{Message}", msg);

        return Ok(new
        {
            status = "ok",
            serverTime = DateTime.Now,
            serverMachine = Environment.MachineName,
            yourIp = HttpContext.Connection.RemoteIpAddress?.ToString()
        });
    }

    /// <summary>
    /// 接收任意 JSON,印出到 Console 並回傳摘要。
    /// 用 [FromBody] JsonElement 讓 client 可以送任何結構。
    /// </summary>
    [HttpPost("upload")]
    [Consumes("application/json")]
    public IActionResult UploadJson([FromBody] JsonElement payload)
    {
        var receivedAt = DateTime.Now;
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var prettyJson = JsonSerializer.Serialize(payload, PrettyJson);
        var sizeBytes = System.Text.Encoding.UTF8.GetByteCount(prettyJson);

        // ─── 印到 Console (黃色,顯眼) ───
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine($"[TestApi] ✔ 收到 JSON 上傳");
        Console.WriteLine($"  時間 : {receivedAt:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  來源 : {remoteIp}");
        Console.WriteLine($"  大小 : {sizeBytes} bytes");
        Console.WriteLine("------------------------- 內容 -------------------------");
        Console.WriteLine(prettyJson);
        Console.WriteLine("════════════════════════════════════════════════════════════");
        Console.WriteLine();
        Console.ForegroundColor = prevColor;

        // 同時寫進 ILogger,在 log 系統也看得到
        _logger.LogInformation(
            "TestApi received JSON upload from {RemoteIp} ({Size} bytes)",
            remoteIp, sizeBytes);

        return Ok(new
        {
            received = true,
            receivedAt,
            fromIp = remoteIp,
            sizeBytes,
            // 把原始 payload 一併回傳,client 可以對照
            echo = payload
        });
    }

    /// <summary>
    /// 接收純文字 (text/plain),便於用最簡單的 curl/Invoke-WebRequest 測試。
    /// </summary>
    [HttpPost("upload-raw")]
    [Consumes("text/plain")]
    public async Task<IActionResult> UploadRaw()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        var remoteIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"[TestApi] ✔ RAW upload from {remoteIp} ({body.Length} chars)");
        Console.WriteLine(body);
        Console.WriteLine();
        Console.ResetColor();

        _logger.LogInformation("TestApi received RAW upload from {RemoteIp} ({Len} chars)",
            remoteIp, body.Length);

        return Ok(new { received = true, length = body.Length, fromIp = remoteIp });
    }
}
