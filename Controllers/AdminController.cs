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
            public string Role { get; set; } = "user";
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
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

                // Simplified query - just get basic user info
                var query = $@"
                    SELECT 
                        u.Id,
                        u.Username,
                        u.FullName,
                        u.Email,
                        u.CreatedAt
                    FROM users u
                    WHERE 1=1 {searchCondition}
                    ORDER BY {orderByColumn} {orderDirection}
                    LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@Search", $"%{search}%");

                var users = new List<UserActivitySummary>();
                
                // Read all users first, then close the reader
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new UserActivitySummary
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            FullName = reader.GetString(2),
                            Email = reader.GetString(3),
                            CreatedAt = reader.GetDateTime(4),
                            SelectedCareer = null,
                            TotalVideosWatched = 0,
                            CompletedVideos = 0,
                            OverallProgress = 0.0,
                            HasResume = false,
                            LastActive = reader.GetDateTime(4), // Use CreatedAt as LastActive
                            TotalWatchTimeMinutes = 0
                        });
                    }
                } // DataReader is closed here

                // Now get total count (DataReader is closed, this is safe now)
                var countQuery = $@"
                    SELECT COUNT(DISTINCT u.Id)
                    FROM users u
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
                Console.WriteLine($"GetAllUsers Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { message = "Error fetching users", error = ex.Message, details = ex.InnerException?.Message });
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

                // Get user basic info - try with Role first, fallback if it doesn't exist
                string userQuery;
                UserDetailResponse? userDetail = null;
                
                try
                {
                    userQuery = @"SELECT Id, Username, FullName, Email, Role, CreatedAt, UpdatedAt 
                                 FROM users WHERE Id = @UserId";
                    using var userCmd = new MySqlCommand(userQuery, connection);
                    userCmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await userCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return NotFound(new { message = "User not found" });

                        var roleOrdinal = reader.GetOrdinal("Role");
                        userDetail = new UserDetailResponse
                        {
                            UserId = reader.GetInt32(reader.GetOrdinal("Id")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Role = reader.IsDBNull(roleOrdinal) ? "user" : reader.GetString(roleOrdinal),
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        };
                    }
                }
                catch (MySqlException ex) when (ex.Message.Contains("Unknown column 'Role'"))
                {
                    // Role column doesn't exist, query without it
                    userQuery = @"SELECT Id, Username, FullName, Email, CreatedAt, UpdatedAt 
                                 FROM users WHERE Id = @UserId";
                    using var userCmd = new MySqlCommand(userQuery, connection);
                    userCmd.Parameters.AddWithValue("@UserId", userId);

                    using (var reader = await userCmd.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                            return NotFound(new { message = "User not found" });

                        userDetail = new UserDetailResponse
                        {
                            UserId = reader.GetInt32(reader.GetOrdinal("Id")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            FullName = reader.GetString(reader.GetOrdinal("FullName")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Role = "user",
                            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt"))
                        };
                    }
                }

                // Get profile (wrapped in try-catch for missing table)
                try
                {
                    // Try UserProfiles first (capital case) - without AreasOfInterest
                    string profileQuery = @"SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, 
                                        FieldOfStudy, Skills, ProfileImagePath, 
                                        CreatedAt, UpdatedAt 
                                        FROM UserProfiles WHERE UserId = @UserId";
                    
                    MySqlCommand? profileCmd = null;
                    MySqlDataReader? profileReader = null;
                    
                    try
                    {
                        profileCmd = new MySqlCommand(profileQuery, connection);
                        profileCmd.Parameters.AddWithValue("@UserId", userId);
                        profileReader = (MySqlDataReader)await profileCmd.ExecuteReaderAsync();
                    }
                    catch (MySqlException ex) when (ex.Message.Contains("doesn't exist"))
                    {
                        // Try lowercase userprofiles
                        profileReader?.Close();
                        profileCmd?.Dispose();
                        
                        profileQuery = @"SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, 
                                        FieldOfStudy, Skills, ProfileImagePath, 
                                        CreatedAt, UpdatedAt 
                                        FROM userprofiles WHERE UserId = @UserId";
                        profileCmd = new MySqlCommand(profileQuery, connection);
                        profileCmd.Parameters.AddWithValue("@UserId", userId);
                        profileReader = (MySqlDataReader)await profileCmd.ExecuteReaderAsync();
                    }
                    
                    using (profileReader)
                    {
                        if (await profileReader.ReadAsync())
                        {
                            var idOrdinal = profileReader.GetOrdinal("Id");
                            var userIdOrdinal = profileReader.GetOrdinal("UserId");
                            var phoneOrdinal = profileReader.GetOrdinal("PhoneNumber");
                            var ageOrdinal = profileReader.GetOrdinal("Age");
                            var genderOrdinal = profileReader.GetOrdinal("Gender");
                            var educationOrdinal = profileReader.GetOrdinal("EducationLevel");
                            var fieldOrdinal = profileReader.GetOrdinal("FieldOfStudy");
                            var skillsOrdinal = profileReader.GetOrdinal("Skills");
                            var imageOrdinal = profileReader.GetOrdinal("ProfileImagePath");
                            var createdOrdinal = profileReader.GetOrdinal("CreatedAt");
                            var updatedOrdinal = profileReader.GetOrdinal("UpdatedAt");

                            userDetail!.Profile = new
                            {
                                id = profileReader.GetInt32(idOrdinal),
                                userId = profileReader.GetInt32(userIdOrdinal),
                                phoneNumber = profileReader.IsDBNull(phoneOrdinal) ? null : profileReader.GetString(phoneOrdinal),
                                age = profileReader.IsDBNull(ageOrdinal) ? (int?)null : profileReader.GetInt32(ageOrdinal),
                                gender = profileReader.IsDBNull(genderOrdinal) ? null : profileReader.GetString(genderOrdinal),
                                educationLevel = profileReader.IsDBNull(educationOrdinal) ? null : profileReader.GetString(educationOrdinal),
                                fieldOfStudy = profileReader.IsDBNull(fieldOrdinal) ? null : profileReader.GetString(fieldOrdinal),
                                skills = profileReader.IsDBNull(skillsOrdinal) ? null : profileReader.GetString(skillsOrdinal),
                                profileImagePath = profileReader.IsDBNull(imageOrdinal) ? null : profileReader.GetString(imageOrdinal),
                                createdAt = profileReader.GetDateTime(createdOrdinal),
                                updatedAt = profileReader.GetDateTime(updatedOrdinal)
                            };
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ No profile found for user {userId}");
                        }
                    }
                }
                catch (Exception profileEx)
                {
                    Console.WriteLine($"❌ Could not load profile: {profileEx.Message}");
                    Console.WriteLine($"   Stack: {profileEx.StackTrace}");
                    userDetail!.Profile = null;
                }

                // Get career progress (wrapped in try-catch)
                try
                {
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
                }
                catch (Exception careerEx)
                {
                    Console.WriteLine($"⚠️ Could not load career: {careerEx.Message}");
                    userDetail!.Career = null;
                }

                // Get video progress (wrapped in try-catch)
                try
                {
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
                }
                catch (Exception videoEx)
                {
                    Console.WriteLine($"⚠️ Could not load video progress: {videoEx.Message}");
                }
                // Get resume (wrapped in try-catch)
                try
                {
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
                }
                catch (Exception resumeEx)
                {
                    Console.WriteLine($"⚠️ Could not load resume: {resumeEx.Message}");
                    userDetail!.Resume = null;
                }

                // Get chat history (wrapped in try-catch)
                try
                {
                    var chatQuery = @"
                        SELECT session_id, title, started_at, last_message_at
                        FROM chat_sessions 
                        WHERE user_id = @UserId 
                        ORDER BY last_message_at DESC 
                        LIMIT 20";
                    using var chatCmd = new MySqlCommand(chatQuery, connection);
                    chatCmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = await chatCmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var titleOrdinal = reader.GetOrdinal("title");
                            var startedOrdinal = reader.GetOrdinal("started_at");
                            var lastMessageOrdinal = reader.GetOrdinal("last_message_at");
                            
                            userDetail!.ChatHistory.Add(new
                            {
                                sessionId = reader.GetString(0),
                                title = reader.IsDBNull(titleOrdinal) ? "Untitled Chat" : reader.GetString(titleOrdinal),
                                createdAt = reader.GetDateTime(startedOrdinal),
                                updatedAt = reader.IsDBNull(lastMessageOrdinal) ? (DateTime?)null : reader.GetDateTime(lastMessageOrdinal)
                            });
                        }
                    }
                }
                catch (Exception chatEx)
                {
                    Console.WriteLine($"⚠️ Could not load chat history: {chatEx.Message}");
                }
                return Ok(userDetail);
            }
            catch (MySqlException ex)
            {
                Console.WriteLine($"❌ MySQL Error in GetUserDetail: {ex.Message}");
                Console.WriteLine($"   Error Number: {ex.Number}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Database error fetching user details", error = ex.Message, sqlError = ex.Number });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in GetUserDetail: {ex.Message}");
                Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error fetching user details", error = ex.Message, stackTrace = ex.StackTrace });
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
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM users", connection))
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
                    JOIN users u ON vwh.user_id = u.Id
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
                var query = "DELETE FROM users WHERE Id = @UserId";
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

                var query = "UPDATE users SET Role = @Role WHERE Id = @UserId";
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
                    FROM users u
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
                    FROM users
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

        // GET /api/admin/careers
        [HttpGet("careers")]
        public async Task<IActionResult> GetAllCareers()
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if table exists
                var checkTableQuery = "SHOW TABLES LIKE 'careers'";
                using (var checkCmd = new MySqlCommand(checkTableQuery, connection))
                {
                    var tableExists = await checkCmd.ExecuteScalarAsync();
                    if (tableExists == null)
                    {
                        Console.WriteLine("Careers table does not exist");
                        return Ok(new List<object>()); // Return empty array
                    }
                }

                // Get column information
                var columnsQuery = "SHOW COLUMNS FROM careers";
                using (var colCmd = new MySqlCommand(columnsQuery, connection))
                {
                    using (var colReader = await colCmd.ExecuteReaderAsync())
                    {
                        Console.WriteLine("=== CAREERS TABLE COLUMNS ===");
                        while (await colReader.ReadAsync())
                        {
                            Console.WriteLine($"Column: {colReader.GetString(0)}, Type: {colReader.GetString(1)}");
                        }
                    }
                }

                // Determine the correct column name for career name
                string nameColumn = "name";
                var checkColumnQuery = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'careers' AND COLUMN_NAME IN ('name', 'career_name')";
                using (var checkColCmd = new MySqlCommand(checkColumnQuery, connection))
                {
                    using (var colNameReader = await checkColCmd.ExecuteReaderAsync())
                    {
                        if (await colNameReader.ReadAsync())
                        {
                            nameColumn = colNameReader.GetString(0);
                            Console.WriteLine($"Using column name: {nameColumn}");
                        }
                    }
                }

                var query = $@"
                    SELECT 
                        id, 
                        {nameColumn}, 
                        description, 
                        required_education, 
                        average_salary, 
                        growth_outlook, 
                        key_skills,
                        created_at,
                        updated_at
                    FROM careers
                    ORDER BY {nameColumn}";

                using var cmd = new MySqlCommand(query, connection);
                var careers = new List<object>();
                
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    careers.Add(new
                    {
                        id = reader.GetInt32(0),
                        name = reader.GetString(1),
                        career_name = reader.GetString(1),
                        description = reader.IsDBNull(2) ? null : reader.GetString(2),
                        required_education = reader.IsDBNull(3) ? null : reader.GetString(3),
                        average_salary = reader.IsDBNull(4) ? null : reader.GetString(4),
                        growth_outlook = reader.IsDBNull(5) ? null : reader.GetString(5),
                        key_skills = reader.IsDBNull(6) ? null : reader.GetString(6),
                        created_at = reader.GetDateTime(7),
                        updated_at = reader.GetDateTime(8)
                    });
                }

                Console.WriteLine($"Loaded {careers.Count} careers");
                return Ok(careers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching careers: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error fetching careers", error = ex.Message });
            }
        }

        // POST /api/admin/careers - Create new career
        [HttpPost("careers")]
        public async Task<IActionResult> CreateCareer([FromBody] CareerRequest request)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var insertQuery = @"
                    INSERT INTO careers (career_name, description, required_education, average_salary, growth_outlook, key_skills)
                    VALUES (@name, @description, @education, @salary, @outlook, @skills)";

                using var cmd = new MySqlCommand(insertQuery, connection);
                cmd.Parameters.AddWithValue("@name", request.Name);
                cmd.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@education", request.RequiredEducation ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@salary", request.AverageSalary ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@outlook", request.GrowthOutlook ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@skills", request.KeySkills ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                var newId = cmd.LastInsertedId;

                return Ok(new { success = true, message = "Career created successfully", id = newId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating career: {ex.Message}");
                return StatusCode(500, new { message = "Error creating career", error = ex.Message });
            }
        }

        // PUT /api/admin/careers/{id} - Update career
        [HttpPut("careers/{id}")]
        public async Task<IActionResult> UpdateCareer(int id, [FromBody] CareerRequest request)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var updateQuery = @"
                    UPDATE careers 
                    SET career_name = @name, 
                        description = @description, 
                        required_education = @education, 
                        average_salary = @salary, 
                        growth_outlook = @outlook, 
                        key_skills = @skills,
                        updated_at = CURRENT_TIMESTAMP
                    WHERE id = @id";

                using var cmd = new MySqlCommand(updateQuery, connection);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.Parameters.AddWithValue("@name", request.Name);
                cmd.Parameters.AddWithValue("@description", request.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@education", request.RequiredEducation ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@salary", request.AverageSalary ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@outlook", request.GrowthOutlook ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@skills", request.KeySkills ?? (object)DBNull.Value);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                    return NotFound(new { message = "Career not found" });

                return Ok(new { success = true, message = "Career updated successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating career: {ex.Message}");
                return StatusCode(500, new { message = "Error updating career", error = ex.Message });
            }
        }

        // DELETE /api/admin/careers/{id} - Delete career
        [HttpDelete("careers/{id}")]
        public async Task<IActionResult> DeleteCareer(int id)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                var deleteQuery = "DELETE FROM careers WHERE id = @id";
                using var cmd = new MySqlCommand(deleteQuery, connection);
                cmd.Parameters.AddWithValue("@id", id);

                var rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                    return NotFound(new { message = "Career not found" });

                return Ok(new { success = true, message = "Career deleted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting career: {ex.Message}");
                return StatusCode(500, new { message = "Error deleting career", error = ex.Message });
            }
        }

        // GET /api/admin/careers/{id}/videos - Get videos for a career based on skills
        [HttpGet("careers/{id}/videos")]
        public async Task<IActionResult> GetCareerVideos(int id)
        {
            try
            {
                if (!IsAdmin())
                    return Forbid();

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new MySqlConnection(connectionString);
                await connection.OpenAsync();

                // Get career skills
                var careerQuery = "SELECT key_skills FROM careers WHERE id = @id";
                using var careerCmd = new MySqlCommand(careerQuery, connection);
                careerCmd.Parameters.AddWithValue("@id", id);

                var skillsJson = await careerCmd.ExecuteScalarAsync() as string;
                if (string.IsNullOrEmpty(skillsJson))
                    return Ok(new List<object>());

                var skills = JsonSerializer.Deserialize<List<string>>(skillsJson);
                if (skills == null || skills.Count == 0)
                    return Ok(new List<object>());

                // Get videos matching these skills
                var placeholders = string.Join(",", skills.Select((_, i) => $"@skill{i}"));
                var videoQuery = $@"
                    SELECT id, skill_name, video_title, video_description, 
                           youtube_video_id, duration_minutes, thumbnail_url
                    FROM learning_videos
                    WHERE skill_name IN ({placeholders})
                    ORDER BY skill_name, video_title";

                using var videoCmd = new MySqlCommand(videoQuery, connection);
                for (int i = 0; i < skills.Count; i++)
                {
                    videoCmd.Parameters.AddWithValue($"@skill{i}", skills[i]);
                }

                var videos = new List<object>();
                using var reader = await videoCmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    videos.Add(new
                    {
                        id = reader.GetInt32(0),
                        skillName = reader.GetString(1),
                        videoTitle = reader.GetString(2),
                        videoDescription = reader.IsDBNull(3) ? "" : reader.GetString(3),
                        youtubeVideoId = reader.GetString(4),
                        durationMinutes = reader.GetInt32(5),
                        thumbnailUrl = reader.IsDBNull(6) 
                            ? $"https://img.youtube.com/vi/{reader.GetString(4)}/maxresdefault.jpg"
                            : reader.GetString(6)
                    });
                }

                return Ok(videos);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching career videos: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching videos", error = ex.Message });
            }
        }

        public class CareerRequest
        {
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string? RequiredEducation { get; set; }
            public string? AverageSalary { get; set; }
            public string? GrowthOutlook { get; set; }
            public string? KeySkills { get; set; }
        }
    }
}
