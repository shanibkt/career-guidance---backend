using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly string _connectionString;

        public DiagnosticsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? "";
        }

        [HttpGet("db-check")]
        public async Task<IActionResult> CheckDb()
        {
            var results = new Dictionary<string, object>();
            try
            {
                using var conn = new MySqlConnection(_connectionString);
                await conn.OpenAsync();

                // 1. Check Careers Table
                var careers = new List<object>();
                using (var cmd = new MySqlCommand("SELECT * FROM careers", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.GetValue(i);
                        }
                        careers.Add(row);
                    }
                }
                results["careers"] = careers;

                // 2. Check User Stats
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Users", conn))
                results["totalUsers"] = await cmd.ExecuteScalarAsync();

                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM UserProfiles", conn))
                results["totalProfiles"] = await cmd.ExecuteScalarAsync();

                using (var cmd = new MySqlCommand("SELECT career_path, COUNT(*) as count FROM UserProfiles WHERE career_path IS NOT NULL GROUP BY career_path", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var profileCounts = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        profileCounts.Add(new { path = reader.GetString(0), count = reader.GetInt32(1) });
                    }
                    results["profileCareerCounts"] = profileCounts;
                }

                // 3. Check Career Progress Stats
                using (var cmd = new MySqlCommand("SELECT user_id, career_id, career_name FROM user_career_progress WHERE career_name LIKE '%mobile%' OR career_name LIKE '%android%'", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var details = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        details.Add(new 
                        { 
                            userId = reader.GetInt32(0), 
                            careerId = reader.IsDBNull(1) ? (int?)null : reader.GetInt32(1),
                            careerName = reader.GetString(2) 
                        });
                    }
                    results["mobileCareerDetails"] = details;
                }

                using (var cmd = new MySqlCommand("SELECT career_name, COUNT(*) as count FROM user_career_progress WHERE is_active = TRUE GROUP BY career_name", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var progressCounts = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        progressCounts.Add(new { name = reader.GetString(0), count = reader.GetInt32(1) });
                    }
                    results["progressCareerCounts"] = progressCounts;
                }

                return Ok(results);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
