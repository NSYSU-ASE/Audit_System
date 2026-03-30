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

                // 1. 全場 FR 平均
                const string frSql = @"
SELECT
    AVG(CAST(FR1 AS FLOAT)) AS FR1,
    AVG(CAST(FR2 AS FLOAT)) AS FR2,
    AVG(CAST(FR3 AS FLOAT)) AS FR3,
    AVG(CAST(FR4 AS FLOAT)) AS FR4,
    AVG(CAST(FR5 AS FLOAT)) AS FR5,
    AVG(CAST(FR6 AS FLOAT)) AS FR6,
    AVG(CAST(FR7 AS FLOAT)) AS FR7
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
    AVG(CAST((ar.FR1 + ar.FR2 + ar.FR3 + ar.FR4 + ar.FR5 + ar.FR6 + ar.FR7) / 7.0 AS FLOAT)) AS AvgScore
FROM dbo.Area a
INNER JOIN dbo.Building b ON a.AreaId = b.AreaId
INNER JOIN dbo.Device d ON b.BuildingId = d.BuildingId
INNER JOIN dbo.AuditResult ar ON d.DeviceId = ar.DeviceId
WHERE ar.AuditPeriod = @Period
GROUP BY a.AreaName, a.OwnerName
ORDER BY a.AreaName;";

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

                return Ok(new
                {
                    period,
                    siteAvg = (int)Math.Round(siteAvg),
                    fr = frList,
                    regions
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