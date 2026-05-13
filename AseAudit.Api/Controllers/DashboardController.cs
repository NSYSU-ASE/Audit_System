using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AseAudit.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDbConnection _dbConnection;

        public DashboardController(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        [HttpGet("main")]
        public IActionResult GetMain([FromQuery] string period = "2025-12")
        {
            try
            {
                using var conn = (SqlConnection)_dbConnection;
                conn.Open();

                var result = new
                {
                    period,
                    siteAvg = 0,
                    fr = new List<int>(),
                    regions = new List<object>()
                };

                // 1. 全場模組平均
                const string frSql = @"
SELECT
    AVG(CAST(IAM AS FLOAT)) AS IAM,
    AVG(CAST(SWI AS FLOAT)) AS SWI,
    AVG(CAST(FWL AS FLOAT)) AS FWL,
    AVG(CAST(EVT AS FLOAT)) AS EVT,
    AVG(CAST(AUD AS FLOAT)) AS AUD,
    AVG(CAST(DAT AS FLOAT)) AS DAT,
    AVG(CAST(RES AS FLOAT)) AS RES
FROM dbo.AuditResult
WHERE AuditPeriod = @Period;";

                using var frCmd = new SqlCommand(frSql, conn);
                frCmd.Parameters.AddWithValue("@Period", period);

                var frList = new List<int>();
                double siteAvg = 0;

                using (var reader = frCmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            var value = reader.IsDBNull(i) ? 0 : Convert.ToDouble(reader.GetValue(i));
                            frList.Add((int)Math.Round(value));
                            siteAvg += value;
                        }
                    }
                }

                siteAvg = frList.Count > 0 ? siteAvg / 7.0 : 0;

                // 2. 各區平均
                const string regionSql = @"
SELECT
    a.AreaName,
    a.OwnerName,
    AVG(CAST((ar.IAM + ar.SWI + ar.FWL + ar.EVT + ar.AUD + ar.DAT + ar.RES) / 7.0 AS FLOAT)) AS AvgScore
FROM dbo.Area a
LEFT JOIN dbo.Building b ON a.AreaId = b.AreaId
LEFT JOIN dbo.Device d ON b.BuildingId = d.BuildingId
LEFT JOIN dbo.AuditResult ar ON d.DeviceId = ar.DeviceId AND ar.AuditPeriod = @Period
GROUP BY a.AreaId, a.AreaName, a.OwnerName
ORDER BY a.AreaId;";

                using var regionCmd = new SqlCommand(regionSql, conn);
                regionCmd.Parameters.AddWithValue("@Period", period);

                var regions = new List<object>();

                using (var reader = regionCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        regions.Add(new
                        {
                            areaName = reader["AreaName"]?.ToString(),
                            ownerName = reader["OwnerName"]?.ToString(),
                            avgScore = reader["AvgScore"] == DBNull.Value ? 0 : (int)Math.Round(Convert.ToDouble(reader["AvgScore"]))
                        });
                    }
                }

                // 3. 棟別模組平均，供風險地圖使用
                const string buildingSql = @"
SELECT
    b.BuildingName,
    AVG(CAST(ar.IAM AS FLOAT)) AS IAM,
    AVG(CAST(ar.SWI AS FLOAT)) AS SWI,
    AVG(CAST(ar.FWL AS FLOAT)) AS FWL,
    AVG(CAST(ar.EVT AS FLOAT)) AS EVT,
    AVG(CAST(ar.AUD AS FLOAT)) AS AUD,
    AVG(CAST(ar.DAT AS FLOAT)) AS DAT,
    AVG(CAST(ar.RES AS FLOAT)) AS RES
FROM dbo.Building b
LEFT JOIN dbo.Device d ON b.BuildingId = d.BuildingId
LEFT JOIN dbo.AuditResult ar ON d.DeviceId = ar.DeviceId AND ar.AuditPeriod = @Period
GROUP BY b.BuildingId, b.BuildingName
ORDER BY b.BuildingId;";

                using var buildingCmd = new SqlCommand(buildingSql, conn);
                buildingCmd.Parameters.AddWithValue("@Period", period);

                var buildingFr = new Dictionary<string, List<int>>();
                var moduleColumns = new[] { "IAM", "SWI", "FWL", "EVT", "AUD", "DAT", "RES" };

                using (var reader = buildingCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var buildingName = reader["BuildingName"]?.ToString();
                        if (string.IsNullOrWhiteSpace(buildingName))
                        {
                            continue;
                        }

                        var values = new List<int>();
                        for (int i = 1; i <= 7; i++)
                        {
                            var column = $"FR{i}";
                            var value = reader[column] == DBNull.Value
                                ? 0
                                : Convert.ToDouble(reader[column]);
                            values.Add((int)Math.Round(value));
                        }

                        buildingFr[buildingName] = values;
                    }
                }

                return Ok(new
                {
                    period,
                    siteAvg = (int)Math.Round(siteAvg),
                    fr = frList,
                    regions,
                    buildingFR = buildingFr
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    detail = ex.ToString()
                });
            }
        }

        [HttpGet("building")]
        public IActionResult GetBuilding(
            [FromQuery] string period = "2025-12",
            [FromQuery] string? buildingCode = null)
        {
            try
            {
                using var conn = (SqlConnection)_dbConnection;
                conn.Open();

                const string sql = @"
SELECT
    d.DeviceId,
    COALESCE(NULLIF(d.DeviceType, N''), N'--') AS DeviceType,
    b.BuildingCode,
    b.BuildingName,
    a.AreaName,
    ar.IAM,
    ar.SWI,
    ar.FWL,
    ar.EVT,
    ar.AUD,
    ar.DAT,
    ar.RES,
    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Identification_AM_Account ia
            WHERE ia.HostName = d.HostName
        )
        THEN CAST(1 AS BIT)
        ELSE CAST(0 AS BIT)
    END AS HasIdentityData
FROM dbo.Building b
INNER JOIN dbo.Area a ON b.AreaId = a.AreaId
INNER JOIN dbo.Device d ON b.BuildingId = d.BuildingId
LEFT JOIN dbo.AuditResult ar ON d.DeviceId = ar.DeviceId AND ar.AuditPeriod = @Period
WHERE (
    @BuildingCode IS NULL
    OR b.BuildingCode = @BuildingCode
    OR b.BuildingName = @BuildingCode
)
AND NOT (
    d.DeviceId LIKE N'DEV-%-001'
    AND EXISTS (
        SELECT 1
        FROM dbo.Device identityDevice
        INNER JOIN dbo.Identification_AM_Account ia ON ia.HostName = identityDevice.HostName
        WHERE identityDevice.BuildingId = b.BuildingId
    )
    AND NOT EXISTS (
        SELECT 1
        FROM dbo.Identification_AM_Account ia
        WHERE ia.HostName = d.HostName
    )
)
ORDER BY d.DeviceId;";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@Period", period);
                cmd.Parameters.AddWithValue("@BuildingCode",
                    string.IsNullOrWhiteSpace(buildingCode) ? DBNull.Value : buildingCode);

                var devices = new List<object>();

                using var reader = cmd.ExecuteReader();
                var moduleColumns = new[] { "IAM", "SWI", "FWL", "EVT", "AUD", "DAT", "RES" };
                while (reader.Read())
                {
                    var hasIdentityData = Convert.ToBoolean(reader["HasIdentityData"]);
                    var fr = new List<int?>();
                    for (var i = 1; i <= 7; i++)
                    {
                        var column = $"FR{i}";
                        if (reader[column] == DBNull.Value)
                        {
                            fr.Add(null);
                            continue;
                        }

                        var value = Convert.ToInt32(reader[column]);
                        fr.Add(hasIdentityData && i > 0 && value == 0 ? null : value);
                    }

                    devices.Add(new
                    {
                        id = reader["DeviceId"]?.ToString(),
                        type = reader["DeviceType"]?.ToString(),
                        buildingCode = reader["BuildingCode"]?.ToString(),
                        buildingName = reader["BuildingName"]?.ToString(),
                        areaName = reader["AreaName"]?.ToString(),
                        fr
                    });
                }

                return Ok(new
                {
                    period,
                    buildingCode,
                    devices
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message,
                    detail = ex.ToString()
                });
            }
        }
    }
}
