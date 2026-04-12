using System.Text;
using System.Text.Json;
using AseAudit.Api.Models.Ingest;

namespace AseAudit.Api.Services;

/// <summary>
/// 將 Collector 上傳的 JSON 以檔案形式落地：
///   {IngestRoot}/{HostName}/{ScriptName}/{yyyyMMdd_HHmmssfff}_{guid}.json
/// 同時於 {HostName}/{ScriptName}/latest.json 維護最新一份快照。
/// </summary>
public class FileSystemAuditIngestService : IAuditIngestService
{
    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<FileSystemAuditIngestService> _logger;
    private readonly string _ingestRoot;

    public FileSystemAuditIngestService(IConfiguration configuration, ILogger<FileSystemAuditIngestService> logger)
    {
        _logger = logger;

        var configured = configuration["Ingest:StoragePath"];
        _ingestRoot = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "IngestData")
            : (Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured));

        Directory.CreateDirectory(_ingestRoot);
        _logger.LogInformation("Audit ingest storage root: {Root}", _ingestRoot);
    }

    public async Task<AuditIngestResponse> StoreAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        if (upload is null) throw new ArgumentNullException(nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.HostName))
            throw new ArgumentException("HostName is required.", nameof(upload));
        if (string.IsNullOrWhiteSpace(upload.ScriptName))
            throw new ArgumentException("ScriptName is required.", nameof(upload));

        var safeHost = SanitizeSegment(upload.HostName);
        var safeScript = SanitizeSegment(upload.ScriptName);
        var receivedAt = DateTime.UtcNow;
        var ingestId = $"{receivedAt:yyyyMMdd_HHmmssfff}_{Guid.NewGuid():N}";

        var folder = Path.Combine(_ingestRoot, safeHost, safeScript);
        Directory.CreateDirectory(folder);

        var fileName = $"{ingestId}.json";
        var fullPath = Path.Combine(folder, fileName);

        // 將原始 Payload 與 metadata 一起落地，方便日後追溯。
        var envelope = new
        {
            ingestId,
            hostName = upload.HostName,
            hostId = upload.HostId,
            scriptName = upload.ScriptName,
            collectedAt = upload.CollectedAt,
            receivedAt,
            success = upload.Success,
            errorMessage = upload.ErrorMessage,
            payload = upload.Payload
        };

        var json = JsonSerializer.Serialize(envelope, WriteOptions);
        var bytes = Encoding.UTF8.GetBytes(json);

        await File.WriteAllBytesAsync(fullPath, bytes, cancellationToken);

        // 更新 latest.json (best-effort)
        try
        {
            var latestPath = Path.Combine(folder, "latest.json");
            await File.WriteAllBytesAsync(latestPath, bytes, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update latest.json for {Host}/{Script}", safeHost, safeScript);
        }

        _logger.LogInformation(
            "Stored audit snapshot {Script} from {Host} -> {Path} ({Size} bytes)",
            upload.ScriptName, upload.HostName, fullPath, bytes.LongLength);

        return new AuditIngestResponse
        {
            IngestId = ingestId,
            HostName = upload.HostName,
            ScriptName = upload.ScriptName,
            StoredPath = Path.GetRelativePath(_ingestRoot, fullPath).Replace('\\', '/'),
            ReceivedAt = receivedAt,
            SizeBytes = bytes.LongLength
        };
    }

    /// <summary>
    /// 過濾不安全或不適合作為檔名/資料夾名稱的字元，避免 path traversal。
    /// </summary>
    private static string SanitizeSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            if (ch == '.' || ch == '/' || ch == '\\' || Array.IndexOf(invalid, ch) >= 0)
                builder.Append('_');
            else
                builder.Append(ch);
        }

        var cleaned = builder.ToString().Trim('_', ' ');
        return string.IsNullOrEmpty(cleaned) ? "unknown" : cleaned;
    }
}
