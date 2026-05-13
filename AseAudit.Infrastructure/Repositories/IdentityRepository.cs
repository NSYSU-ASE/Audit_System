using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;
using Dapper;
using AseAudit.Core.Entities;
using AseAudit.Core.Modules.Identity.Dtos;

namespace AseAudit.Infrastructure.Repositories;

public class IdentityRepository
{
    private readonly IDbConnection _conn;

    public IdentityRepository(IDbConnection conn)
    {
        _conn = conn;
    }

    /// <summary>
    /// 取得帳號資料
    /// </summary>
    public IEnumerable<IdentificationAmAccount> GetAccounts()
    {
        return _conn.Query<IdentificationAmAccount>(
            "SELECT * FROM Identification_AM_Account");
    }

    /// <summary>
    /// 取得規則資料（密碼政策）
    /// </summary>
    public IEnumerable<IdentificationAmRule> GetRules()
    {
        return _conn.Query<IdentificationAmRule>(
            "SELECT * FROM Identification_AM_rule");
    }

    public TargetDeviceInfo GetTargetDevice(string buildingCode = "K18")
    {
        const string sql = @"
SELECT TOP 1
    a.AreaCode,
    a.AreaName,
    b.BuildingCode,
    b.BuildingName,
    d.DeviceId,
    d.DeviceType,
    d.HostName
FROM dbo.Building b
INNER JOIN dbo.Area a ON b.AreaId = a.AreaId
LEFT JOIN dbo.Device d ON b.BuildingId = d.BuildingId
WHERE b.BuildingCode = @BuildingCode
ORDER BY d.DeviceId;";

        return _conn.QueryFirstOrDefault<TargetDeviceInfo>(sql, new { BuildingCode = buildingCode })
            ?? new TargetDeviceInfo
            {
                AreaCode = "ZONEII",
                AreaName = "ZoneII",
                BuildingCode = buildingCode,
                BuildingName = buildingCode,
                DeviceId = buildingCode,
                DeviceType = "HMI",
                HostName = null
            };
    }

    public TargetDeviceInfo? GetTargetDeviceByDeviceId(string deviceId)
    {
        const string sql = @"
SELECT TOP 1
    a.AreaCode,
    a.AreaName,
    b.BuildingCode,
    b.BuildingName,
    d.DeviceId,
    d.DeviceType,
    d.HostName
FROM dbo.Device d
INNER JOIN dbo.Building b ON d.BuildingId = b.BuildingId
INNER JOIN dbo.Area a ON b.AreaId = a.AreaId
WHERE d.DeviceId = @DeviceId
   OR d.HostName = @DeviceId;";

        return _conn.QueryFirstOrDefault<TargetDeviceInfo>(sql, new { DeviceId = deviceId });
    }

    public TargetDeviceInfo UpsertIdentityDevice(
        string buildingCode,
        string hostName,
        string deviceType = "Windows")
    {
        const string upsertSql = @"
DECLARE @BuildingId INT;

SELECT @BuildingId = BuildingId
FROM dbo.Building
WHERE BuildingCode = @BuildingCode OR BuildingName = @BuildingCode;

IF @BuildingId IS NULL
BEGIN
    THROW 50001, 'Target building not found.', 1;
END;

UPDATE dbo.Device
SET HostName = @HostName,
    DeviceType = COALESCE(NULLIF(DeviceType, N''), @DeviceType),
    BuildingId = @BuildingId
WHERE DeviceId = @DeviceId;

IF @@ROWCOUNT = 0
BEGIN
    BEGIN TRY
        INSERT INTO dbo.Device (DeviceId, HostName, IP, DeviceType, BuildingId, OwnerName)
        VALUES (@DeviceId, @HostName, NULL, @DeviceType, @BuildingId, NULL);
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN (2601, 2627)
        BEGIN
            UPDATE dbo.Device
            SET HostName = @HostName,
                DeviceType = COALESCE(NULLIF(DeviceType, N''), @DeviceType),
                BuildingId = @BuildingId
            WHERE DeviceId = @DeviceId;
        END
        ELSE
        BEGIN
            THROW;
        END
    END CATCH
END;";

        var deviceId = NormalizeDeviceId(hostName);

        _conn.Execute(upsertSql, new
        {
            BuildingCode = buildingCode,
            DeviceId = deviceId,
            HostName = hostName,
            DeviceType = deviceType
        });

        return GetTargetDeviceByDeviceId(deviceId)
            ?? throw new InvalidOperationException($"Unable to create identity device for host {hostName}.");
    }

    public AuditResultRecord UpsertIdentityAuditResult(
        string period,
        TargetDeviceInfo target,
        IEnumerable<IdentityRuleResultDto> rules)
    {
        var ruleList = rules.ToList();
        var identityScore = ruleList
            .Where(x => x.IncludedInScore && x.Score.HasValue)
            .Select(x => x.Score!.Value)
            .ToList();

        var iamScore = identityScore.Count == 0
            ? 0
            : (int)Math.Round(identityScore.Average());

        const string selectSql = @"
SELECT TOP 1
    AuditResultId, DeviceId, AuditPeriod, IAM, SWI, FWL, EVT, AUD, DAT, RES, TotalScore, CreatedAt
FROM dbo.AuditResult
WHERE DeviceId = @DeviceId AND AuditPeriod = @AuditPeriod;";

        const string insertSql = @"
INSERT INTO dbo.AuditResult
(
    DeviceId, AuditPeriod, IAM, SWI, FWL, EVT, AUD, DAT, RES
)
OUTPUT
    INSERTED.AuditResultId,
    INSERTED.DeviceId,
    INSERTED.AuditPeriod,
    INSERTED.IAM,
    INSERTED.SWI,
    INSERTED.FWL,
    INSERTED.EVT,
    INSERTED.AUD,
    INSERTED.DAT,
    INSERTED.RES,
    INSERTED.TotalScore,
    INSERTED.CreatedAt
VALUES
(
    @DeviceId, @AuditPeriod, @IAM, 0, 0, 0, 0, 0, 0
);";

        const string updateSql = @"
UPDATE dbo.AuditResult
SET IAM = @IAM
OUTPUT
    INSERTED.AuditResultId,
    INSERTED.DeviceId,
    INSERTED.AuditPeriod,
    INSERTED.IAM,
    INSERTED.SWI,
    INSERTED.FWL,
    INSERTED.EVT,
    INSERTED.AUD,
    INSERTED.DAT,
    INSERTED.RES,
    INSERTED.TotalScore,
    INSERTED.CreatedAt
WHERE AuditResultId = @AuditResultId;";

        const string deleteFindingSql = @"
DELETE FROM dbo.AuditFinding
WHERE AuditResultId = @AuditResultId
  AND FRCode = N'IAM';";

        const string insertFindingSql = @"
INSERT INTO dbo.AuditFinding (AuditResultId, FRCode, Reason)
VALUES (@AuditResultId, N'IAM', @Reason);";

        if (_conn.State != ConnectionState.Open)
        {
            _conn.Open();
        }

        using var tx = _conn.BeginTransaction();
        try
        {
            var existing = _conn.QueryFirstOrDefault<AuditResultRecord>(
                selectSql,
                new { target.DeviceId, AuditPeriod = period },
                tx);

            var result = existing is null
                ? _conn.QuerySingle<AuditResultRecord>(
                    insertSql,
                    new { target.DeviceId, AuditPeriod = period, IAM = iamScore },
                    tx)
                : _conn.QuerySingle<AuditResultRecord>(
                    updateSql,
                    new { existing.AuditResultId, IAM = iamScore },
                    tx);

            _conn.Execute(deleteFindingSql, new { result.AuditResultId }, tx);

            var findings = ruleList
                .Where(x => x.IncludedInScore && x.Score.HasValue && x.Score.Value < 80)
                .SelectMany(rule =>
                {
                    var issues = rule.Issues.Count == 0
                        ? new List<string> { "此項目未達標準，但未取得詳細扣分原因" }
                        : rule.Issues;

                    return issues.Select(issue =>
                        $"{rule.SrMapping}｜{rule.RuleName}｜{issue}");
                })
                .Distinct()
                .ToList();

            foreach (var finding in findings)
            {
                _conn.Execute(insertFindingSql, new
                {
                    result.AuditResultId,
                    Reason = finding
                }, tx);
            }

            tx.Commit();
            return result;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public AuditResultRecord? GetAuditResult(
        string period,
        string deviceId)
    {
        const string sql = @"
SELECT
    AuditResultId,
    DeviceId,
    AuditPeriod,
    IAM,
    SWI,
    FWL,
    EVT,
    AUD,
    DAT,
    RES,
    TotalScore,
    CreatedAt
FROM dbo.AuditResult
WHERE AuditPeriod = @AuditPeriod
  AND DeviceId = @DeviceId;";

        return _conn.QueryFirstOrDefault<AuditResultRecord>(sql, new
        {
            AuditPeriod = period,
            DeviceId = deviceId
        });
    }

    public IEnumerable<AuditFindingRecord> GetAuditFindings(int auditResultId)
    {
        const string sql = @"
SELECT FindingId, AuditResultId, FRCode, Reason
FROM dbo.AuditFinding
WHERE AuditResultId = @AuditResultId
ORDER BY FindingId;";

        return _conn.Query<AuditFindingRecord>(sql, new { AuditResultId = auditResultId });
    }

    private static string NormalizeDeviceId(string hostName)
    {
        var cleaned = string.IsNullOrWhiteSpace(hostName)
            ? "UNKNOWN-HOST"
            : hostName.Trim();

        return cleaned.Length <= 50
            ? cleaned
            : cleaned[..50];
    }
}

public sealed class TargetDeviceInfo
{
    public string AreaCode { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string BuildingCode { get; set; } = "";
    public string BuildingName { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string? DeviceType { get; set; }
    public string? HostName { get; set; }
}

public sealed class AuditResultRecord
{
    public int AuditResultId { get; set; }
    public string DeviceId { get; set; } = "";
    public string AuditPeriod { get; set; } = "";
    public int IAM { get; set; }
    public int SWI { get; set; }
    public int FWL { get; set; }
    public int EVT { get; set; }
    public int AUD { get; set; }
    public int DAT { get; set; }
    public int RES { get; set; }
    public int TotalScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AuditFindingRecord
{
    public int FindingId { get; set; }
    public int AuditResultId { get; set; }
    public string FRCode { get; set; } = "";
    public string Reason { get; set; } = "";
}
