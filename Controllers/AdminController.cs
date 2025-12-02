using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Models
        public class UserActivitySummary
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public string? SelectedCareer { get; set; }
            public int TotalVideosWatched { get; set; }
            public int CompletedVideos { get; set; }
            public double OverallProgress { get; set; }
            public bool HasResume { get; set; }
            public DateTime? LastActive { get; set; }
            public int TotalWatchTimeMinutes { get; set; }
        }

        public class UserDetailResponse
        {
            public int UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public DateTime CreatedAt { get; set; }
            public object? Profile { get; set; }
            public object? Career { get; set; }
            public List<object> VideoProgress { get; set; } = new();
            public object? Resume { get; set; }
            public List<object> ChatHistory { get; set; } = new();
        }

        public class SystemStats
        {
            public int TotalUsers { get; set; }
            public int ActiveUsersToday { get; set; }
            public int ActiveUsersWeek { get; set; }
            public int TotalCareersSelected { get; set; }
            public int TotalVideosWatched { get; set; }
            public int TotalResumes { get; set; }
            public int TotalChatSessions { get; set; }
            public double AverageProgress { get; set; }
            public List<object> PopularCareers { get; set; } = new();
            public List<object> RecentActivities { get; set; } = new();
        }

        private bool IsAdmin()
        {
            var userRole = User.FindFirstValue(ClaimTypes.Role);
            return userRole == "admin" || userRole == "Admin";
        }

        // Get all users with activity summary
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null,
            [FromQuery] string? sortBy = "createdAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var offset = (page - 1) * pageSize;
                var searchCondition = string.IsNullOrEmpty(search)
                    ? ""
                    : "AND (u.Username LIKE @Search OR u.FullName LIKE @Search OR u.Email LIKE @Search)";

                var orderByColumn = sortBy?.ToLower() switch
                {
                    "username" => "u.Username",
                    "fullname" => "u.FullName",
                    "email" => "u.Email",
                    "lastactive" => "last_active",
                    _ => "u.CreatedAt"
                };

                var orderDirection = sortOrder?.ToLower() == "asc" ? "ASC" : "DESC";

                var query = $@"
                    SELECT 
                        u.Id,
                        u.Username,
                        u.FullName,
                        u.Email,
                        u.CreatedAt,
                        ucp.career_name as SelectedCareer,
                        COUNT(DISTINCT vwh.video_id) as TotalVideosWatched,
                        SUM(CASE WHEN vwh.is_completed = TRUE THEN 1 ELSE 0 END) as CompletedVideos,
                        COALESCE(ucp.overall_progress, 0) as OverallProgress,
                        CASE WHEN ur.id IS NOT NULL THEN TRUE ELSE FALSE END as HasResume,
                        GREATEST(
                            COALESCE(vwh.max_last_watched, u.CreatedAt),
                            COALESCE(ch.max_created_at, u.CreatedAt),
                            u.CreatedAt
                        ) as LastActive,
                        COALESCE(SUM(vwh.current_position_seconds), 0) / 60 as TotalWatchTimeMinutes
                    FROM Users u
                    LEFT JOIN user_career_progress ucp ON u.Id = ucp.user_id AND ucp.is_active = TRUE
                    LEFT JOIN video_watch_history vwh ON u.Id = vwh.user_id
                    LEFT JOIN user_resumes ur ON u.Id = ur.user_id
                    LEFT JOIN (
                        SELECT user_id, MAX(created_at) as max_created_at
                        FROM chat_history
                        GROUP BY user_id
                    ) ch ON u.Id = ch.user_id
                    LEFT JOIN (
                        SELECT user_id, MAX(last_watched) as max_last_watched
                        FROM video_watch_history
                        GROUP BY user_id
                    ) vwh ON u.Id = vwh.user_id
                    WHERE 1=1 {searchCondition}
                    GROUP BY u.Id, u.Username, u.FullName, u.Email, u.CreatedAt, 
                             ucp.career_name, ucp.overall_progress, ur.id, 
                             ch.max_created_at, vwh.max_last_watched
                    ORDER BY {orderByColumn} {orderDirection}
                    LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@Search", $"%{search}%");

                var users = new List<UserActivitySummary>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new UserActivitySummary
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        FullName = reader.GetString(2),
                        Email = reader.GetString(3),
                        CreatedAt = reader.GetDateTime(4),
                        SelectedCareer = reader.IsDBNull(5) ? null : reader.GetString(5),
                        TotalVideosWatched = reader.GetInt32(6),
                        CompletedVideos = reader.GetInt32(7),
                        OverallProgress = reader.GetDouble(8),
                        HasResume = reader.GetBoolean(9),
                        LastActive = reader.IsDBNull(10) ? null : reader.GetDateTime(10),
                        TotalWatchTimeMinutes = reader.GetInt32(11)
                    });
                }

                // Get total count
                var countQuery = $@"
                    SELECT COUNT(DISTINCT u.Id)
                    FROM Users u
                    WHERE 1=1 {searchCondition}";

                using var countCmd = new MySqlCommand(countQuery, connection);
                if (!string.IsNullOrEmpty(search))
                    countCmd.Parameters.AddWithValue("@Search", $"%{search}%");

                var totalUsers = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                return Ok(new
                {
                    users,
                    totalUsers,
                    currentPage = page,
                    pageSize,
                    totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching users", error = ex.Message });
            }
        }

        // Get detailed user information
        [HttpGet("users/{userId}")]
        public async Task<IActionResult> GetUserDetail(int userId)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Get user basic info
                var userQuery = "SELECT Id, Username, FullName, Email, CreatedAt FROM Users WHERE Id = @UserId";
                using var userCmd = new MySqlCommand(userQuery, connection);
                userCmd.Parameters.AddWithValue("@UserId", userId);

                UserDetailResponse? userDetail = null;
                using (var reader = await userCmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                        return NotFound(new { message = "User not found" });

                    userDetail = new UserDetailResponse
                    {
                        UserId = reader.GetInt32(0),
                        Username = reader.GetString(1),
                        FullName = reader.GetString(2),
                        Email = reader.GetString(3),
                        CreatedAt = reader.GetDateTime(4)
                    };
                }

                // Get profile
                var profileQuery = "SELECT * FROM UserProfiles WHERE UserId = @UserId";
                using var profileCmd = new MySqlCommand(profileQuery, connection);
                profileCmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await profileCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        var ageOrdinal = reader.GetOrdinal("Age");
                        var genderOrdinal = reader.GetOrdinal("Gender");
                        var educationOrdinal = reader.GetOrdinal("EducationLevel");
                        var fieldOrdinal = reader.GetOrdinal("FieldOfStudy");
                        var skillsOrdinal = reader.GetOrdinal("Skills");
                        var phoneOrdinal = reader.GetOrdinal("PhoneNumber");
                        
                        userDetail!.Profile = new
                        {
                            age = reader.IsDBNull(ageOrdinal) ? 0 : reader.GetInt32(ageOrdinal),
                            gender = reader.IsDBNull(genderOrdinal) ? "" : reader.GetString(genderOrdinal),
                            educationLevel = reader.IsDBNull(educationOrdinal) ? "" : reader.GetString(educationOrdinal),
                            fieldOfStudy = reader.IsDBNull(fieldOrdinal) ? "" : reader.GetString(fieldOrdinal),
                            skills = reader.IsDBNull(skillsOrdinal) ? "" : reader.GetString(skillsOrdinal),
                            phoneNumber = reader.IsDBNull(phoneOrdinal) ? "" : reader.GetString(phoneOrdinal)
                        };
                    }
                }

                // Get career progress
                var careerQuery = "SELECT career_name, overall_progress, completed_courses, total_courses, selected_at, last_accessed FROM user_career_progress WHERE user_id = @UserId AND is_active = TRUE";
                using var careerCmd = new MySqlCommand(careerQuery, connection);
                careerCmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await careerCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userDetail!.Career = new
                        {
                            careerName = reader.GetString(0),
                            overallProgress = reader.GetDouble(1),
                            completedCourses = reader.GetInt32(2),
                            totalCourses = reader.GetInt32(3),
                            selectedAt = reader.GetDateTime(4),
                            lastAccessed = reader.GetDateTime(5)
                        };
                    }
                }

                // Get video progress
                var videoQuery = @"
                    SELECT video_id, video_title, skill_name, career_name, 
                           watch_percentage, is_completed, last_watched
                    FROM video_watch_history 
                    WHERE user_id = @UserId 
                    ORDER BY last_watched DESC 
                    LIMIT 50";
                using var videoCmd = new MySqlCommand(videoQuery, connection);
                videoCmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await videoCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        userDetail!.VideoProgress.Add(new
                        {
                            videoId = reader.GetString(0),
                            videoTitle = reader.GetString(1),
                            skillName = reader.GetString(2),
                            careerName = reader.GetString(3),
                            watchPercentage = reader.GetDouble(4),
                            isCompleted = reader.GetBoolean(5),
                            lastWatched = reader.GetDateTime(6)
                        });
                    }
                }

                // Get resume
                var resumeQuery = "SELECT full_name, job_title, email, phone, location, linkedin, professional_summary, skills, experiences, education, created_at, updated_at FROM user_resumes WHERE user_id = @UserId";
                using var resumeCmd = new MySqlCommand(resumeQuery, connection);
                resumeCmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await resumeCmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        userDetail!.Resume = new
                        {
                            fullName = reader.GetString(0),
                            jobTitle = reader.GetString(1),
                            email = reader.GetString(2),
                            phone = reader.GetString(3),
                            location = reader.GetString(4),
                            linkedin = reader.GetString(5),
                            professionalSummary = reader.GetString(6),
                            skills = reader.GetString(7),
                            experiences = reader.GetString(8),
                            education = reader.GetString(9),
                            createdAt = reader.GetDateTime(10),
                            updatedAt = reader.GetDateTime(11)
                        };
                    }
                }

                // Get chat history
                var chatQuery = @"
                    SELECT session_id, title, created_at, updated_at
                    FROM chat_history 
                    WHERE user_id = @UserId 
                    ORDER BY updated_at DESC 
                    LIMIT 20";
                using var chatCmd = new MySqlCommand(chatQuery, connection);
                chatCmd.Parameters.AddWithValue("@UserId", userId);
                using (var reader = await chatCmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        userDetail.ChatHistory.Add(new
                        {
                            sessionId = reader.GetString(0),
                            title = reader.GetString(1),
                            createdAt = reader.GetDateTime(2),
                            updatedAt = reader.GetDateTime(3)
                        });
                    }
                }

                return Ok(userDetail);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching user details", error = ex.Message });
            }
        }

        // Get system statistics
        [HttpGet("stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var stats = new SystemStats();

                // Total users
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Users", connection))
                {
                    stats.TotalUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Active users today
                var todayQuery = @"
                    SELECT COUNT(DISTINCT user_id) 
                    FROM video_watch_history 
                    WHERE DATE(last_watched) = CURDATE()";
                using (var cmd = new MySqlCommand(todayQuery, connection))
                {
                    stats.ActiveUsersToday = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Active users this week
                var weekQuery = @"
                    SELECT COUNT(DISTINCT user_id) 
                    FROM video_watch_history 
                    WHERE last_watched >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)";
                using (var cmd = new MySqlCommand(weekQuery, connection))
                {
                    stats.ActiveUsersWeek = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Total careers selected
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_career_progress WHERE is_active = TRUE", connection))
                {
                    stats.TotalCareersSelected = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Total videos watched
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM video_watch_history", connection))
                {
                    stats.TotalVideosWatched = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Total resumes
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_resumes", connection))
                {
                    stats.TotalResumes = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Total chat sessions
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM chat_history", connection))
                {
                    stats.TotalChatSessions = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Average progress
                using (var cmd = new MySqlCommand("SELECT AVG(overall_progress) FROM user_career_progress WHERE is_active = TRUE", connection))
                {
                    var result = await cmd.ExecuteScalarAsync();
                    stats.AverageProgress = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                }

                // Popular careers
                var popularQuery = @"
                    SELECT career_name, COUNT(*) as count 
                    FROM user_career_progress 
                    WHERE is_active = TRUE 
                    GROUP BY career_name 
                    ORDER BY count DESC 
                    LIMIT 10";
                using (var cmd = new MySqlCommand(popularQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.PopularCareers.Add(new
                        {
                            careerName = reader.GetString(0),
                            count = reader.GetInt32(1)
                        });
                    }
                }

                // Recent activities
                var activityQuery = @"
                    SELECT 
                        u.Username,
                        u.FullName,
                        'Video Watched' as activity_type,
                        vwh.video_title as activity_detail,
                        vwh.last_watched as activity_time
                    FROM video_watch_history vwh
                    JOIN Users u ON vwh.user_id = u.Id
                    ORDER BY vwh.last_watched DESC
                    LIMIT 20";
                using (var cmd = new MySqlCommand(activityQuery, connection))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        stats.RecentActivities.Add(new
                        {
                            username = reader.GetString(0),
                            fullName = reader.GetString(1),
                            activityType = reader.GetString(2),
                            activityDetail = reader.GetString(3),
                            activityTime = reader.GetDateTime(4)
                        });
                    }
                }

                return Ok(stats);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching statistics", error = ex.Message });
            }
        }

        // Delete user
        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Delete user (cascades to all related tables)
                var query = "DELETE FROM Users WHERE Id = @UserId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting user", error = ex.Message });
            }
        }

        // Update user role (promote to admin)
        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateRoleRequest request)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = "UPDATE Users SET Role = @Role WHERE Id = @UserId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Role", request.Role);
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "User role updated successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating role", error = ex.Message });
            }
        }

        public class UpdateRoleRequest
        {
            public string Role { get; set; } = "user";
        }

        // Export users data to CSV
        [HttpGet("export/users")]
        public async Task<IActionResult> ExportUsers()
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT 
                        u.Id, u.Username, u.FullName, u.Email, u.CreatedAt,
                        ucp.career_name, ucp.overall_progress,
                        COUNT(DISTINCT vwh.video_id) as videos_watched,
                        SUM(CASE WHEN vwh.is_completed THEN 1 ELSE 0 END) as completed_videos
                    FROM Users u
                    LEFT JOIN user_career_progress ucp ON u.Id = ucp.user_id AND ucp.is_active = TRUE
                    LEFT JOIN video_watch_history vwh ON u.Id = vwh.user_id
                    GROUP BY u.Id, u.Username, u.FullName, u.Email, u.CreatedAt, ucp.career_name, ucp.overall_progress";

                using var cmd = new MySqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();

                var csv = new System.Text.StringBuilder();
                csv.AppendLine("Id,Username,FullName,Email,CreatedAt,Career,Progress,VideosWatched,CompletedVideos");

                while (await reader.ReadAsync())
                {
                    csv.AppendLine($"{reader.GetInt32(0)}," +
                        $"\"{reader.GetString(1)}\"," +
                        $"\"{reader.GetString(2)}\"," +
                        $"\"{reader.GetString(3)}\"," +
                        $"{reader.GetDateTime(4):yyyy-MM-dd}," +
                        $"\"{(reader.IsDBNull(5) ? "" : reader.GetString(5))}\"," +
                        $"{(reader.IsDBNull(6) ? 0 : reader.GetDouble(6))}," +
                        $"{reader.GetInt32(7)}," +
                        $"{reader.GetInt32(8)}");
                }

                return File(System.Text.Encoding.UTF8.GetBytes(csv.ToString()), 
                    "text/csv", 
                    $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error exporting users", error = ex.Message });
            }
        }

        // Get user growth analytics
        [HttpGet("analytics/growth")]
        public async Task<IActionResult> GetUserGrowth([FromQuery] int days = 30)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT DATE(CreatedAt) as date, COUNT(*) as count
                    FROM Users
                    WHERE CreatedAt >= DATE_SUB(CURDATE(), INTERVAL @Days DAY)
                    GROUP BY DATE(CreatedAt)
                    ORDER BY date ASC";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@Days", days);

                var growth = new List<object>();
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    growth.Add(new
                    {
                        date = reader.GetDateTime(0).ToString("yyyy-MM-dd"),
                        count = reader.GetInt32(1)
                    });
                }

                return Ok(growth);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching growth analytics", error = ex.Message });
            }
        }
    }
}
