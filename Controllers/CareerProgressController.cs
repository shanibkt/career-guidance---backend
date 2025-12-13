using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CareerProgressController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CareerProgressController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // POST /api/careerprogress/select - Save selected career
        [HttpPost("select")]
        public IActionResult SelectCareer([FromBody] SelectCareerRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Deactivate previous active careers
                string deactivateQuery = "UPDATE user_career_progress SET is_active = FALSE WHERE user_id = @userId";
                using (MySqlCommand deactivateCmd = new(deactivateQuery, conn))
                {
                    deactivateCmd.Parameters.AddWithValue("@userId", userId);
                    deactivateCmd.ExecuteNonQuery();
                }

                // Insert new selected career
                string insertQuery = @"
                    INSERT INTO user_career_progress 
                    (user_id, career_id, career_name, required_skills, is_active, total_courses)
                    VALUES (@userId, @careerId, @careerName, @requiredSkills, TRUE, @totalCourses)";

                using MySqlCommand insertCmd = new(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@careerId", request.CareerId ?? (object)DBNull.Value);
                insertCmd.Parameters.AddWithValue("@careerName", request.CareerName);
                insertCmd.Parameters.AddWithValue("@requiredSkills", JsonSerializer.Serialize(request.RequiredSkills));
                insertCmd.Parameters.AddWithValue("@totalCourses", request.RequiredSkills?.Count ?? 0);
                insertCmd.ExecuteNonQuery();

                // Update user profile with career path
                string updateProfileQuery = "UPDATE userprofiles SET career_path = @careerName WHERE UserId = @userId";
                using (MySqlCommand updateCmd = new(updateProfileQuery, conn))
                {
                    updateCmd.Parameters.AddWithValue("@careerName", request.CareerName);
                    updateCmd.Parameters.AddWithValue("@userId", userId);
                    updateCmd.ExecuteNonQuery();
                }

                return Ok(new { message = "Career selected successfully", careerName = request.CareerName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error selecting career: {ex.Message}");
                return StatusCode(500, new { error = "Failed to select career", details = ex.Message });
            }
        }

        // GET /api/careerprogress/selected - Get selected career
        [HttpGet("selected")]
        public IActionResult GetSelectedCareer()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, career_id, career_name, required_skills, selected_at, 
                           overall_progress, completed_courses, total_courses, last_accessed
                    FROM user_career_progress
                    WHERE user_id = @userId AND is_active = TRUE
                    ORDER BY selected_at DESC
                    LIMIT 1";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var requiredSkillsJson = reader.GetString("required_skills");
                    var result = new
                    {
                        id = reader.GetInt32("id"),
                        careerId = reader.IsDBNull(reader.GetOrdinal("career_id")) ? null : (int?)reader.GetInt32("career_id"),
                        careerName = reader.GetString("career_name"),
                        requiredSkills = JsonSerializer.Deserialize<List<string>>(requiredSkillsJson),
                        selectedAt = reader.GetDateTime("selected_at"),
                        overallProgress = reader.GetDecimal("overall_progress"),
                        completedCourses = reader.GetInt32("completed_courses"),
                        totalCourses = reader.GetInt32("total_courses"),
                        lastAccessed = reader.GetDateTime("last_accessed")
                    };
                    return Ok(result);
                }

                return NotFound(new { message = "No career selected" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting selected career: {ex.Message}");
                return StatusCode(500, new { error = "Failed to get selected career", details = ex.Message });
            }
        }

        // POST /api/careerprogress/course - Save course progress
        [HttpPost("course")]
        public IActionResult SaveCourseProgress([FromBody] CourseProgressRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string upsertQuery = @"
                    INSERT INTO course_progress 
                    (user_id, career_name, course_id, skill_name, video_title, youtube_video_id, 
                     watched_percentage, watch_time_seconds, total_duration_seconds, is_completed, completed_at)
                    VALUES (@userId, @careerName, @courseId, @skillName, @videoTitle, @youtubeVideoId,
                            @watchedPercentage, @watchTimeSeconds, @totalDurationSeconds, @isCompleted, @completedAt)
                    ON DUPLICATE KEY UPDATE
                        watched_percentage = @watchedPercentage,
                        watch_time_seconds = @watchTimeSeconds,
                        total_duration_seconds = @totalDurationSeconds,
                        is_completed = @isCompleted,
                        completed_at = @completedAt,
                        last_watched = NOW()";

                using MySqlCommand cmd = new(upsertQuery, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@careerName", request.CareerName);
                cmd.Parameters.AddWithValue("@courseId", request.CourseId);
                cmd.Parameters.AddWithValue("@skillName", request.SkillName);
                cmd.Parameters.AddWithValue("@videoTitle", request.VideoTitle);
                cmd.Parameters.AddWithValue("@youtubeVideoId", request.YoutubeVideoId);
                cmd.Parameters.AddWithValue("@watchedPercentage", request.WatchedPercentage);
                cmd.Parameters.AddWithValue("@watchTimeSeconds", request.WatchTimeSeconds);
                cmd.Parameters.AddWithValue("@totalDurationSeconds", request.TotalDurationSeconds);
                cmd.Parameters.AddWithValue("@isCompleted", request.IsCompleted);
                cmd.Parameters.AddWithValue("@completedAt", request.IsCompleted ? DateTime.Now : (object)DBNull.Value);
                cmd.ExecuteNonQuery();

                // Update overall progress
                UpdateOverallProgress(conn, userId, request.CareerName);

                return Ok(new { message = "Progress saved successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving course progress: {ex.Message}");
                return StatusCode(500, new { error = "Failed to save progress", details = ex.Message });
            }
        }

        // GET /api/careerprogress/courses - Get all course progress for selected career
        [HttpGet("courses")]
        public IActionResult GetCourseProgress([FromQuery] string? careerName = null)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // If no career name provided, get the active career
                if (string.IsNullOrEmpty(careerName))
                {
                    string getCareerQuery = "SELECT career_name FROM user_career_progress WHERE user_id = @userId AND is_active = TRUE LIMIT 1";
                    using MySqlCommand getCareerCmd = new(getCareerQuery, conn);
                    getCareerCmd.Parameters.AddWithValue("@userId", userId);
                    var result = getCareerCmd.ExecuteScalar();
                    if (result == null) return NotFound(new { message = "No active career found" });
                    careerName = result.ToString();
                }

                string query = @"
                    SELECT course_id, skill_name, video_title, youtube_video_id, 
                           watched_percentage, watch_time_seconds, total_duration_seconds, 
                           is_completed, started_at, completed_at, last_watched
                    FROM course_progress
                    WHERE user_id = @userId AND career_name = @careerName
                    ORDER BY last_watched DESC";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@careerName", careerName);

                using var reader = cmd.ExecuteReader();
                var courses = new List<object>();

                while (reader.Read())
                {
                    courses.Add(new
                    {
                        courseId = reader.GetString("course_id"),
                        skillName = reader.GetString("skill_name"),
                        videoTitle = reader.IsDBNull(reader.GetOrdinal("video_title")) ? null : reader.GetString("video_title"),
                        youtubeVideoId = reader.IsDBNull(reader.GetOrdinal("youtube_video_id")) ? null : reader.GetString("youtube_video_id"),
                        watchedPercentage = reader.GetDecimal("watched_percentage"),
                        watchTimeSeconds = reader.GetInt32("watch_time_seconds"),
                        totalDurationSeconds = reader.GetInt32("total_duration_seconds"),
                        isCompleted = reader.GetBoolean("is_completed"),
                        startedAt = reader.GetDateTime("started_at"),
                        completedAt = reader.IsDBNull(reader.GetOrdinal("completed_at")) ? null : (DateTime?)reader.GetDateTime("completed_at"),
                        lastWatched = reader.GetDateTime("last_watched")
                    });
                }

                return Ok(new { courses, careerName });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting course progress: {ex.Message}");
                return StatusCode(500, new { error = "Failed to get progress", details = ex.Message });
            }
        }

        private void UpdateOverallProgress(MySqlConnection conn, int userId, string careerName)
        {
            string progressQuery = @"
                SELECT 
                    COUNT(*) as total,
                    SUM(CASE WHEN is_completed = TRUE THEN 1 ELSE 0 END) as completed,
                    AVG(watched_percentage) as avg_progress
                FROM course_progress
                WHERE user_id = @userId AND career_name = @careerName";

            using MySqlCommand progressCmd = new(progressQuery, conn);
            progressCmd.Parameters.AddWithValue("@userId", userId);
            progressCmd.Parameters.AddWithValue("@careerName", careerName);

            using var reader = progressCmd.ExecuteReader();
            if (reader.Read())
            {
                var completedCourses = reader.GetInt32("completed");
                var avgProgress = reader.IsDBNull(reader.GetOrdinal("avg_progress")) ? 0.0 : reader.GetDouble("avg_progress");
                reader.Close();

                string updateQuery = @"
                    UPDATE user_career_progress 
                    SET overall_progress = @overallProgress, 
                        completed_courses = @completedCourses
                    WHERE user_id = @userId AND career_name = @careerName AND is_active = TRUE";

                using MySqlCommand updateCmd = new(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@overallProgress", avgProgress);
                updateCmd.Parameters.AddWithValue("@completedCourses", completedCourses);
                updateCmd.Parameters.AddWithValue("@userId", userId);
                updateCmd.Parameters.AddWithValue("@careerName", careerName);
                updateCmd.ExecuteNonQuery();
            }
        }
    }

    public class SelectCareerRequest
    {
        public int? CareerId { get; set; }
        public string CareerName { get; set; } = "";
        public List<string>? RequiredSkills { get; set; }
    }

    public class CourseProgressRequest
    {
        public string CareerName { get; set; } = "";
        public string CourseId { get; set; } = "";
        public string SkillName { get; set; } = "";
        public string? VideoTitle { get; set; }
        public string? YoutubeVideoId { get; set; }
        public decimal WatchedPercentage { get; set; }
        public int WatchTimeSeconds { get; set; }
        public int TotalDurationSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }
}
