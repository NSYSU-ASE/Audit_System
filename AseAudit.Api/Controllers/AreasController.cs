using AseAudit.Api.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace AseAudit.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AreasController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AreasController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetAreas()
        {
            var result = new List<AreaDto>();
            var connStr = _configuration.GetConnectionString("DefaultConnection");

            using var conn = new SqlConnection(connStr);
            conn.Open();

            var sql = @"
                SELECT AreaId, AreaName, OwnerName
                FROM dbo.Area
                ORDER BY AreaId
            ";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                result.Add(new AreaDto
                {
                    AreaId = reader["AreaId"] != DBNull.Value
                        ? Convert.ToInt32(reader["AreaId"])
                        : 0,
                    AreaName = reader["AreaName"]?.ToString() ?? "",
                    Owner = reader["OwnerName"]?.ToString()
                });
            }

            return Ok(result);
        }
    }
}