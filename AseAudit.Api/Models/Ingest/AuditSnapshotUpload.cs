using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace AseAudit.Api.Models.Ingest;

/// <summary>
/// 稽核端 (AseAudit.Collector) 上傳單一腳本快照的請求格式。
/// 對應 Collector 端 ScriptResult 與 JsonConverterRegistry 輸出的內容。
/// </summary>
public class AuditSnapshotUpload
{
    /// <summary>主機名稱 (例如 Environment.MachineName)。</summary>
    [Required]
    [StringLength(128)]
    public string HostName { get; set; } = string.Empty;

    /// <summary>主機唯一識別碼 (可選，例如 AD SID 或 GUID)。</summary>
    [StringLength(128)]
    public string? HostId { get; set; }

    /// <summary>腳本/快照名稱 (例如 "EventStatusSnapshot")。</summary>
    [Required]
    [StringLength(128)]
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>Collector 收集這份資料的時間 (UTC)。</summary>
    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    /// <summary>腳本是否執行成功。</summary>
    public bool Success { get; set; } = true;

    /// <summary>錯誤訊息 (Success=false 時填寫)。</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>實際稽核資料 (任意 JSON 結構)。</summary>
    [Required]
    public JsonElement Payload { get; set; }
}

/// <summary>批次上傳：一次送多份快照。</summary>
public class AuditBatchUpload
{
    [Required]
    [StringLength(128)]
    public string HostName { get; set; } = string.Empty;

    [StringLength(128)]
    public string? HostId { get; set; }

    public DateTime CollectedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [MinLength(1)]
    public List<AuditSnapshotUpload> Snapshots { get; set; } = new();
}
