using System.Text.Json;
using AseAudit.Api.Models.Ingest;
using AseAudit.Infrastructure.Mapping;
using AseAudit.Infrastructure.Repositories;
using ASEAudit.Shared.Contracts;

namespace AseAudit.Api.Services.Ingest.Identity;

/// <summary>
/// 處理 <see cref="HostAccountSnapshotPayload.Script"/>：帳號快照寫入 Identification_AM_Account。
/// </summary>
public sealed class HostAccountSnapshotHandler : ISnapshotHandler
{
    private static readonly JsonSerializerOptions DeserializeOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IIdentificationAmAccountRepository _accountRepo;

    public HostAccountSnapshotHandler(IIdentificationAmAccountRepository accountRepo)
    {
        _accountRepo = accountRepo;
    }

    public string ScriptName => HostAccountSnapshotPayload.Script;

    public async Task<int> HandleAsync(AuditSnapshotUpload upload, CancellationToken cancellationToken)
    {
        // upload.Payload 是 envelope 內層 content（不含 ScriptName/Hostname），
        // 直接反序列化成 Content 型別，再以 envelope 欄位組回強型別 Payload。
        var content = upload.Payload.Deserialize<HostAccountSnapshotContent>(DeserializeOptions)
            ?? throw new ArgumentException(
                $"Failed to deserialize Payload as {nameof(HostAccountSnapshotContent)}.");

        var payload = new HostAccountSnapshotPayload
        {
            HostId   = upload.HostId ?? string.Empty,
            Hostname = upload.HostName,
            Payload  = content,
        };

        var entities = HostAccountSnapshotMapper.ToEntities(payload).ToList();
        return await _accountRepo.AddRangeAsync(entities, cancellationToken);
    }
}
