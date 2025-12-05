using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LogsController : ControllerBase
    {
        private readonly LocalCrashReportingService _crashReporting;

        public LogsController(ICrashReportingService crashReporting)
        {
            _crashReporting = (LocalCrashReportingService)crashReporting;
        }

        /// <summary>
        /// Get recent errors for monitoring
        /// GET: api/logs/errors
        /// </summary>
        [HttpGet("errors")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRecentErrors([FromQuery] int count = 50)
        {
            try
            {
                var errors = await _crashReporting.GetRecentErrorsAsync(count);
                return Ok(new { count = errors.Count, errors = errors });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving logs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get log statistics
        /// GET: api/logs/statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var stats = await _crashReporting.GetStatisticsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving statistics: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get log file content (raw)
        /// GET: api/logs/file/errors
        /// </summary>
        [HttpGet("file/{type}")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetLogFile(string type)
        {
            try
            {
                var logsDir = Path.Combine(AppContext.BaseDirectory, "Logs");
                var today = DateTime.Now.ToString("yyyy-MM-dd");
                var filePath = Path.Combine(logsDir, $"{type}-{today}.log");

                if (!System.IO.File.Exists(filePath))
                    return NotFound(new { message = "Log file not found" });

                var content = System.IO.File.ReadAllText(filePath);
                return Ok(new { fileName = Path.GetFileName(filePath), content = content });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error reading log file: {ex.Message}" });
            }
        }
    }
}
