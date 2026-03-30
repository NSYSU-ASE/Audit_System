using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AseAudit.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AssetController : ControllerBase
    {
        private readonly IDbConnection _dbConnection;

        public AssetController(IDbConnection dbConnection)
        {
            _dbConnection = dbConnection;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var result = new List<object>();

            using var conn = (SqlConnection)_dbConnection;
            conn.Open();

            const string sql = @"
                SELECT DeviceId, HostName, IP, Area, Building, Owner, DeviceType
                FROM dbo.Asset_Device
            ";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new
                {
                    DeviceId = reader["DeviceId"]?.ToString(),
                    HostName = reader["HostName"]?.ToString(),
                    IP = reader["IP"]?.ToString(),
                    Area = reader["Area"]?.ToString(),
                    Building = reader["Building"]?.ToString(),
                    Owner = reader["Owner"]?.ToString(),
                    DeviceType = reader["DeviceType"]?.ToString()
                });
            }

            return Ok(result);
        }
    }
}