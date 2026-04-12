namespace AseAudit.Api.Models.Ingest;

/// <summary>單筆 ingest 完成後回傳給 Collector 的結果。</summary>
public class AuditIngestResponse
{
    /// <summary>本次 ingest 的識別碼 (檔名/紀錄 ID)。</summary>
    public string IngestId { get; set; } = string.Empty;

    /// <summary>主機名稱。</summary>
    public string HostName { get; set; } = string.Empty;

    /// <summary>腳本名稱。</summary>
    public string ScriptName { get; set; } = string.Empty;

    /// <summary>實際儲存於伺服器的相對路徑。</summary>
    public string StoredPath { get; set; } = string.Empty;

    /// <summary>伺服器接收時間 (UTC)。</summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>儲存的 JSON Payload 大小 (bytes)。</summary>
    public long SizeBytes { get; set; }
}

/// <summary>批次 ingest 結果。</summary>
public class AuditBatchIngestResponse
{
    public string HostName { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public List<AuditIngestResponse> Results { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}
