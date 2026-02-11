using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VideoProgressController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public VideoProgressController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Models
        public class VideoProgressRequest
        {
            public string VideoId { get; set; } = string.Empty;
            public string VideoTitle { get; set; } = string.Empty;
            public string SkillName { get; set; } = string.Empty;
            public string CareerName { get; set; } = string.Empty;
            public int CurrentPositionSeconds { get; set; }
            public int DurationSeconds { get; set; }
            public double WatchPercentage { get; set; }
            public bool IsCompleted { get; set; }
        }

        public class LearningPathSummary
        {
            public string CareerName { get; set; } = string.Empty;
            public string SkillName { get; set; } = string.Empty;
            public int TotalVideos { get; set; }
            public int CompletedVideos { get; set; }
            public int TotalDurationSeconds { get; set; }
            public int WatchedDurationSeconds { get; set; }
            public double ProgressPercentage { get; set; }
            public DateTime? LastAccessed { get; set; }
        }

        private string GetUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException("User ID not found");
        }

        // Save or Update Video Progress
        [HttpPost("save")]
        public async Task<IActionResult> SaveVideoProgress([FromBody] VideoProgressRequest request)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if progress already exists
                var checkQuery = @"
                    SELECT id FROM video_watch_history 
                    WHERE user_id = @UserId AND video_id = @VideoId AND career_name = @CareerName";

                using var checkCmd = new MySqlCommand(checkQuery, connection);
                checkCmd.Parameters.AddWithValue("@UserId", userId);
                checkCmd.Parameters.AddWithValue("@VideoId", request.VideoId);
                checkCmd.Parameters.AddWithValue("@CareerName", request.CareerName);

                var existingId = await checkCmd.ExecuteScalarAsync();

                string query;
                if (existingId != null)
                {
                    // Update existing progress
                    query = @"
                        UPDATE video_watch_history 
                        SET 
                            current_position_seconds = @CurrentPosition,
                            duration_seconds = @Duration,
                            watch_percentage = @WatchPercentage,
                            is_completed = @IsCompleted,
                            last_watched = NOW(),
                            watch_count = watch_count + 1
                        WHERE id = @Id";

                    using var updateCmd = new MySqlCommand(query, connection);
                    updateCmd.Parameters.AddWithValue("@CurrentPosition", request.CurrentPositionSeconds);
                    updateCmd.Parameters.AddWithValue("@Duration", request.DurationSeconds);
                    updateCmd.Parameters.AddWithValue("@WatchPercentage", request.WatchPercentage);
                    updateCmd.Parameters.AddWithValue("@IsCompleted", request.IsCompleted);
                    updateCmd.Parameters.AddWithValue("@Id", existingId);

                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert new progress
                    query = @"
                        INSERT INTO video_watch_history 
                        (user_id, career_name, skill_name, video_id, video_title, 
                         current_position_seconds, duration_seconds, watch_percentage, is_completed)
                        VALUES 
                        (@UserId, @CareerName, @SkillName, @VideoId, @VideoTitle, 
                         @CurrentPosition, @Duration, @WatchPercentage, @IsCompleted)";

                    using var insertCmd = new MySqlCommand(query, connection);
                    insertCmd.Parameters.AddWithValue("@UserId", userId);
                    insertCmd.Parameters.AddWithValue("@CareerName", request.CareerName);
                    insertCmd.Parameters.AddWithValue("@SkillName", request.SkillName);
                    insertCmd.Parameters.AddWithValue("@VideoId", request.VideoId);
                    insertCmd.Parameters.AddWithValue("@VideoTitle", request.VideoTitle);
                    insertCmd.Parameters.AddWithValue("@CurrentPosition", request.CurrentPositionSeconds);
                    insertCmd.Parameters.AddWithValue("@Duration", request.DurationSeconds);
                    insertCmd.Parameters.AddWithValue("@WatchPercentage", request.WatchPercentage);
                    insertCmd.Parameters.AddWithValue("@IsCompleted", request.IsCompleted);

                    await insertCmd.ExecuteNonQueryAsync();
                }

                return Ok(new { message = "Progress saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error saving progress", error = ex.Message });
            }
        }

        // Get Video Progress
        [HttpGet("{videoId}")]
        public async Task<IActionResult> GetVideoProgress(string videoId, [FromQuery] string careerName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT video_id, video_title, skill_name, career_name, 
                           current_position_seconds, duration_seconds, watch_percentage, 
                           is_completed, last_watched
                    FROM video_watch_history
                    WHERE user_id = @UserId AND video_id = @VideoId AND career_name = @CareerName";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@VideoId", videoId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var progress = new
                    {
                        videoId = reader.GetString(0),
                        videoTitle = reader.GetString(1),
                        skillName = reader.GetString(2),
                        careerName = reader.GetString(3),
                        currentPositionSeconds = reader.GetInt32(4),
                        durationSeconds = reader.GetInt32(5),
                        watchPercentage = reader.GetDouble(6),
                        isCompleted = reader.GetBoolean(7),
                        lastWatched = reader.GetDateTime(8)
                    };

                    return Ok(progress);
                }

                return NotFound(new { message = "No progress found for this video" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching progress", error = ex.Message });
            }
        }

        // Get All Video Progress for a Career
        [HttpGet("career/{careerName}")]
        public async Task<IActionResult> GetAllVideoProgress(string careerName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT video_id, video_title, skill_name, career_name, 
                           current_position_seconds, duration_seconds, watch_percentage, 
                           is_completed, last_watched
                    FROM video_watch_history
                    WHERE user_id = @UserId AND career_name = @CareerName
                    ORDER BY last_watched DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);

                var progressList = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    progressList.Add(new
                    {
                        videoId = reader.GetString(0),
                        videoTitle = reader.GetString(1),
                        skillName = reader.GetString(2),
                        careerName = reader.GetString(3),
                        currentPositionSeconds = reader.GetInt32(4),
                        durationSeconds = reader.GetInt32(5),
                        watchPercentage = reader.GetDouble(6),
                        isCompleted = reader.GetBoolean(7),
                        lastWatched = reader.GetDateTime(8)
                    });
                }

                return Ok(progressList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching progress", error = ex.Message });
            }
        }

        // Get Learning Path Summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetLearningPathSummary([FromQuery] string careerName, [FromQuery] string skillName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        career_name,
                        skill_name,
                        COUNT(*) as total_videos,
                        SUM(CASE WHEN is_completed = TRUE THEN 1 ELSE 0 END) as completed_videos,
                        SUM(duration_seconds) as total_duration,
                        SUM(current_position_seconds) as watched_duration,
                        AVG(watch_percentage) as avg_progress
                    FROM video_watch_history
                    WHERE user_id = @UserId AND career_name = @CareerName AND skill_name = @SkillName
                    GROUP BY career_name, skill_name";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);
                cmd.Parameters.AddWithValue("@SkillName", skillName);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var summary = new LearningPathSummary
                    {
                        CareerName = reader.GetString(0),
                        SkillName = reader.GetString(1),
                        TotalVideos = reader.GetInt32(2),
                        CompletedVideos = reader.GetInt32(3),
                        TotalDurationSeconds = reader.GetInt32(4),
                        WatchedDurationSeconds = reader.GetInt32(5),
                        ProgressPercentage = reader.GetDouble(6)
                    };

                    return Ok(summary);
                }

                return NotFound(new { message = "No progress found for this learning path" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching summary", error = ex.Message });
            }
        }

        // Get All Learning Paths Summary for a Career
        [HttpGet("career-summary/{careerName}")]
        public async Task<IActionResult> GetAllLearningPathsSummary(string careerName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        career_name,
                        skill_name,
                        COUNT(*) as total_videos,
                        SUM(CASE WHEN is_completed = TRUE THEN 1 ELSE 0 END) as completed_videos,
                        SUM(duration_seconds) as total_duration,
                        SUM(current_position_seconds) as watched_duration,
                        AVG(watch_percentage) as avg_progress,
                        MAX(last_watched) as last_accessed
                    FROM video_watch_history
                    WHERE user_id = @UserId AND career_name = @CareerName
                    GROUP BY career_name, skill_name
                    ORDER BY last_accessed DESC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);

                var summaries = new List<LearningPathSummary>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    summaries.Add(new LearningPathSummary
                    {
                        CareerName = reader.GetString(0),
                        SkillName = reader.GetString(1),
                        TotalVideos = reader.GetInt32(2),
                        CompletedVideos = reader.GetInt32(3),
                        TotalDurationSeconds = reader.GetInt32(4),
                        WatchedDurationSeconds = reader.GetInt32(5),
                        ProgressPercentage = reader.GetDouble(6),
                        LastAccessed = reader.GetDateTime(7)
                    });
                }

                return Ok(summaries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching summaries", error = ex.Message });
            }
        }

        // Get Recently Watched Videos
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentlyWatched([FromQuery] int limit = 10)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT video_id, video_title, skill_name, career_name, 
                           current_position_seconds, duration_seconds, watch_percentage, 
                           is_completed, last_watched
                    FROM video_watch_history
                    WHERE user_id = @UserId
                    ORDER BY last_watched DESC
                    LIMIT @Limit";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Limit", limit);

                var recentVideos = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    recentVideos.Add(new
                    {
                        videoId = reader.GetString(0),
                        videoTitle = reader.GetString(1),
                        skillName = reader.GetString(2),
                        careerName = reader.GetString(3),
                        currentPositionSeconds = reader.GetInt32(4),
                        durationSeconds = reader.GetInt32(5),
                        watchPercentage = reader.GetDouble(6),
                        isCompleted = reader.GetBoolean(7),
                        lastWatched = reader.GetDateTime(8)
                    });
                }

                return Ok(recentVideos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching recent videos", error = ex.Message });
            }
        }

        // Delete Video Progress
        [HttpDelete("{videoId}")]
        public async Task<IActionResult> DeleteVideoProgress(string videoId, [FromQuery] string careerName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    DELETE FROM video_watch_history 
                    WHERE user_id = @UserId AND video_id = @VideoId AND career_name = @CareerName";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@VideoId", videoId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Progress deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting progress", error = ex.Message });
            }
        }

        // Reset All Progress for a Career
        [HttpDelete("career/{careerName}/reset")]
        public async Task<IActionResult> ResetCareerProgress(string careerName)
        {
            try
            {
                var userId = GetUserId();
                var connectionString = _configuration.GetConnectionString("DefaultConnection");

                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    DELETE FROM video_watch_history 
                    WHERE user_id = @UserId AND career_name = @CareerName";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@CareerName", careerName);

                await cmd.ExecuteNonQueryAsync();
                return Ok(new { message = "Career progress reset successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error resetting progress", error = ex.Message });
            }
        }
    }
}
