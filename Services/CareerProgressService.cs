using System.Text.Json;
using MySqlConnector;

namespace MyFirstApi.Services
{
    /// <summary>
    /// Service for managing user career progress
    /// </summary>
    public class CareerProgressService
    {
        private readonly DatabaseService _db;
        private readonly ILogger<CareerProgressService> _logger;

        public CareerProgressService(DatabaseService db, ILogger<CareerProgressService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<object?> GetUserCareerProgressAsync(int userId)
        {
            var query = @"
                SELECT id, user_id, career_id, career_name, required_skills, is_active,
                       overall_progress, completed_courses, total_courses, selected_at, last_accessed
                FROM user_career_progress
                WHERE user_id = @userId AND is_active = TRUE
                LIMIT 1";

            return await _db.ExecuteQuerySingleAsync(query, new Dictionary<string, object>
            {
                { "@userId", userId }
            }, reader => new
            {
                id = reader.GetInt32("id"),
                userId = reader.GetInt32("user_id"),
                careerId = DatabaseService.GetSafeInt(reader, "career_id"),
                careerName = reader.GetString("career_name"),
                requiredSkills = JsonSerializer.Deserialize<List<string>>(reader.GetString("required_skills")),
                isActive = reader.GetBoolean("is_active"),
                overallProgress = reader.GetDecimal("overall_progress"),
                completedCourses = reader.GetInt32("completed_courses"),
                totalCourses = reader.GetInt32("total_courses"),
                selectedAt = reader.GetDateTime("selected_at"),
                lastAccessed = reader.GetDateTime("last_accessed")
            });
        }

        public async Task<bool> SelectCareerAsync(int userId, string careerName, List<string> requiredSkills, int? careerId = null)
        {
            // Deactivate previous career selection
            var deactivateQuery = "UPDATE user_career_progress SET is_active = FALSE WHERE user_id = @userId";
            await _db.ExecuteNonQueryAsync(deactivateQuery, new Dictionary<string, object> { { "@userId", userId } });

            // Insert new career selection
            var insertQuery = @"
                INSERT INTO user_career_progress (user_id, career_id, career_name, required_skills, is_active, overall_progress, completed_courses, total_courses)
                VALUES (@userId, @careerId, @careerName, @requiredSkills, TRUE, 0.00, 0, @totalCourses)";

            var totalCourses = requiredSkills?.Count ?? 0;
            var skillsJson = JsonSerializer.Serialize(requiredSkills ?? new List<string>());

            var affected = await _db.ExecuteNonQueryAsync(insertQuery, new Dictionary<string, object>
            {
                { "@userId", userId },
                { "@careerId", careerId ?? (object)DBNull.Value },
                { "@careerName", careerName },
                { "@requiredSkills", skillsJson },
                { "@totalCourses", totalCourses }
            });

            return affected > 0;
        }

        public async Task<bool> UpdateVideoProgressAsync(int userId, string careerName, string skillName, string videoId, string videoTitle, int watchedPercentage, int watchTimeSeconds, int totalDurationSeconds)
        {
            var upsertQuery = @"
                INSERT INTO course_progress 
                    (user_id, career_name, course_id, skill_name, video_title, youtube_video_id, watched_percentage, watch_time_seconds, total_duration_seconds, is_completed)
                VALUES 
                    (@userId, @careerName, @courseId, @skillName, @videoTitle, @videoId, @watchedPercentage, @watchTime, @totalDuration, @isCompleted)
                ON DUPLICATE KEY UPDATE
                    watched_percentage = VALUES(watched_percentage),
                    watch_time_seconds = VALUES(watch_time_seconds),
                    total_duration_seconds = VALUES(total_duration_seconds),
                    is_completed = VALUES(is_completed),
                    last_watched = NOW(),
                    completed_at = IF(VALUES(is_completed) = TRUE, NOW(), completed_at)";

            var isCompleted = watchedPercentage >= 90;

            var affected = await _db.ExecuteNonQueryAsync(upsertQuery, new Dictionary<string, object>
            {
                { "@userId", userId },
                { "@careerName", careerName },
                { "@courseId", $"{skillName}_{videoId}" },
                { "@skillName", skillName },
                { "@videoTitle", videoTitle },
                { "@videoId", videoId },
                { "@watchedPercentage", watchedPercentage },
                { "@watchTime", watchTimeSeconds },
                { "@totalDuration", totalDurationSeconds },
                { "@isCompleted", isCompleted }
            });

            // Update overall progress
            if (affected > 0)
            {
                await UpdateOverallProgressAsync(userId, careerName);
            }

            return affected > 0;
        }

        private async Task UpdateOverallProgressAsync(int userId, string careerName)
        {
            var progressQuery = @"
                SELECT 
                    COALESCE(COUNT(CASE WHEN is_completed = TRUE THEN 1 END), 0) as completedCount,
                    COALESCE(COUNT(*), 1) as totalCount
                FROM course_progress
                WHERE user_id = @userId AND career_name = @careerName";

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            using var progressCmd = new MySqlCommand(progressQuery, conn);
            progressCmd.Parameters.AddWithValue("@userId", userId);
            progressCmd.Parameters.AddWithValue("@careerName", careerName);

            using var reader = await progressCmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var completedCount = reader.GetInt32(0);  // Use ordinal instead of column name
                var totalCount = reader.GetInt32(1);      // Use ordinal instead of column name
                var progressPercent = totalCount > 0 ? Math.Round((decimal)completedCount / totalCount * 100, 2) : 0;

                await reader.CloseAsync();

                var updateQuery = @"
                    UPDATE user_career_progress 
                    SET overall_progress = @progress, 
                        completed_courses = @completed, 
                        last_accessed = NOW()
                    WHERE user_id = @userId AND career_name = @careerName AND is_active = TRUE";

                using var updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@progress", progressPercent);
                updateCmd.Parameters.AddWithValue("@completed", completedCount);
                updateCmd.Parameters.AddWithValue("@userId", userId);
                updateCmd.Parameters.AddWithValue("@careerName", careerName);

                await updateCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<object>> GetCourseProgressAsync(int userId, string careerName)
        {
            var query = @"
                SELECT course_id, skill_name, video_title, youtube_video_id, watched_percentage, 
                       watch_time_seconds, total_duration_seconds, is_completed, started_at, 
                       completed_at, last_watched
                FROM course_progress
                WHERE user_id = @userId AND career_name = @careerName
                ORDER BY last_watched DESC";

            return await _db.ExecuteQueryListAsync<object>(query, new Dictionary<string, object>
            {
                { "@userId", userId },
                { "@careerName", careerName }
            }, reader => new
            {
                courseId = reader.GetString("course_id"),
                skillName = reader.GetString("skill_name"),
                videoTitle = DatabaseService.GetSafeString(reader, "video_title"),
                youtubeVideoId = DatabaseService.GetSafeString(reader, "youtube_video_id"),
                watchedPercentage = reader.GetDecimal("watched_percentage"),
                watchTimeSeconds = reader.GetInt32("watch_time_seconds"),
                totalDurationSeconds = reader.GetInt32("total_duration_seconds"),
                isCompleted = reader.GetBoolean("is_completed"),
                startedAt = reader.GetDateTime("started_at"),
                completedAt = DatabaseService.GetSafeDateTime(reader, "completed_at"),
                lastWatched = reader.GetDateTime("last_watched")
            });
        }
    }
}
