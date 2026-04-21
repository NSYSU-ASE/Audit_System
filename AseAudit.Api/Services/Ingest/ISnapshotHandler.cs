using AseAudit.Api.Models.Ingest;

namespace AseAudit.Api.Services.Ingest;

/// <summary>
/// 單一 ScriptName 的稽核資料處理器：負責反序列化 Payload、轉 Entity、寫入 DB。
/// 每種 ScriptName 對應一個實作，由 <see cref="DatabaseAuditIngestService"/> 依 ScriptName 分派。
/// </summary>
public interface ISnapshotHandler
{
    /// <summary>對應的 ScriptName（與 Payload 常數一致）。</summary>
    string ScriptName { get; }

    /// <summary>處理上傳資料並回傳寫入筆數。</summary>
    Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken);
}
