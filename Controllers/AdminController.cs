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
                    u.CreatedAt,
                    COALESCE(cp.career_name, up.career_path) as SelectedCareer,
                    (SELECT COUNT(*) FROM video_watch_history WHERE user_id = u.Id) as TotalVideos,
                    (SELECT COUNT(*) FROM video_watch_history WHERE user_id = u.Id AND is_completed = TRUE) as CompletedVideos,
                    COALESCE(cp.overall_progress, 0.0) as OverallProgress,
                    EXISTS(SELECT 1 FROM user_resumes WHERE user_id = u.Id) as HasResume,
                    COALESCE((SELECT MAX(last_watched) FROM video_watch_history WHERE user_id = u.Id), u.CreatedAt) as LastActive,
                    COALESCE((SELECT SUM(duration_minutes) FROM video_watch_history v JOIN learning_videos lv ON v.video_id = lv.video_id WHERE v.user_id = u.Id), 0) as TotalWatchTime
                FROM Users u
                LEFT JOIN user_career_progress cp ON u.Id = cp.user_id AND cp.is_active = TRUE
                LEFT JOIN UserProfiles up ON u.Id = up.UserId
                WHERE 1=1 {searchCondition}
                ORDER BY {orderByColumn} {orderDirection}
                LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", (page - 1) * pageSize);
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
                            SelectedCareer = reader.IsDBNull(5) ? null : reader.GetString(5),
                            TotalVideosWatched = reader.GetInt32(6),
                            CompletedVideos = reader.GetInt32(7),
                            OverallProgress = reader.GetDouble(8),
                            HasResume = reader.GetBoolean(9),
                            LastActive = reader.GetDateTime(10),
                            TotalWatchTimeMinutes = Convert.ToInt32(reader.GetValue(11))
                        });
                    }
                } // DataReader is closed here

                // Now get total count (DataReader is closed, this is safe now)
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
                                 FROM Users WHERE Id = @UserId";
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
                                 FROM Users WHERE Id = @UserId";
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
                        // Try lowercase UserProfiles
                        profileReader?.Close();
                        profileCmd?.Dispose();
                        
                        profileQuery = @"SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, 
                                        FieldOfStudy, Skills, ProfileImagePath, 
                                        CreatedAt, UpdatedAt 
                                        FROM UserProfiles WHERE UserId = @UserId";
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
                using (var cmd = new MySqlCommand("SELECT COUNT(*) FROM Users", connection))
                {
                    stats.TotalUsers = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Active users today
                try
                {
                    var todayQuery = @"
                        SELECT COUNT(DISTINCT user_id) 
                        FROM video_watch_history 
                        WHERE DATE(last_watched) = CURDATE()";
                    using var cmd = new MySqlCommand(todayQuery, connection);
                    stats.ActiveUsersToday = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch { stats.ActiveUsersToday = 0; }

                // Active users this week
                try
                {
                    var weekQuery = @"
                        SELECT COUNT(DISTINCT user_id) 
                        FROM video_watch_history 
                        WHERE last_watched >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)";
                    using var cmd = new MySqlCommand(weekQuery, connection);
                    stats.ActiveUsersWeek = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch { stats.ActiveUsersWeek = 0; }

                // Total careers selected (Students with goals)
        try
        {
            var countQuery = @"
                SELECT COUNT(DISTINCT user_id) 
                FROM (
                    SELECT user_id FROM user_career_progress WHERE is_active = TRUE
                    UNION
                    SELECT UserId as user_id FROM UserProfiles WHERE career_path IS NOT NULL
                ) enrollment";
            using var cmd = new MySqlCommand(countQuery, connection);
            stats.TotalCareersSelected = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }
                catch { stats.TotalCareersSelected = 0; }

                // Total videos watched
                try
                {
                    using var cmd = new MySqlCommand("SELECT COUNT(*) FROM video_watch_history", connection);
                    stats.TotalVideosWatched = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch { stats.TotalVideosWatched = 0; }

                // Total resumes
                try
                {
                    using var cmd = new MySqlCommand("SELECT COUNT(*) FROM user_resumes", connection);
                    stats.TotalResumes = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch { stats.TotalResumes = 0; }

                // Total chat sessions
                try
                {
                    using var cmd = new MySqlCommand("SELECT COUNT(*) FROM chat_sessions", connection);
                    stats.TotalChatSessions = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
                catch { stats.TotalChatSessions = 0; }

                // Average progress
                try
                {
                    using var cmd = new MySqlCommand("SELECT AVG(overall_progress) FROM user_career_progress WHERE is_active = TRUE", connection);
                    var result = await cmd.ExecuteScalarAsync();
                    stats.AverageProgress = result != DBNull.Value ? Convert.ToDouble(result) : 0;
                }
                catch { stats.AverageProgress = 0; }

                // Popular careers
        try
        {
            var popularQuery = @"
                SELECT career_name, COUNT(DISTINCT user_id) as student_count 
                FROM (
                    SELECT user_id, career_name FROM user_career_progress WHERE is_active = TRUE
                    UNION ALL
                    SELECT UserId as user_id, career_path as career_name FROM UserProfiles WHERE career_path IS NOT NULL
                ) enrollment
                GROUP BY career_name 
                ORDER BY student_count DESC 
                LIMIT 10";
            using var cmd = new MySqlCommand(popularQuery, connection);
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        stats.PopularCareers.Add(new
                        {
                            careerName = reader.GetString(0),
                            count = reader.GetInt32(1)
                        });
                    }
                }
                catch { /* Table may not exist */ }

                // Recent activities
                try
                {
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
                    using var cmd = new MySqlCommand(activityQuery, connection);
                    using var reader = await cmd.ExecuteReaderAsync();
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
                catch { /* Table may not exist */ }

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

                // First, delete any companies owned by this user
                // (cascade will handle hiring_notifications, job_applications, etc.)
                var deleteCompaniesQuery = @"
                    DELETE FROM companies 
                    WHERE id IN (
                        SELECT company_id FROM company_users 
                        WHERE user_id = @UserId AND role = 'owner'
                    )";
                using (var deleteCompaniesCmd = new MySqlCommand(deleteCompaniesQuery, connection))
                {
                    deleteCompaniesCmd.Parameters.AddWithValue("@UserId", userId);
                    var companiesDeleted = await deleteCompaniesCmd.ExecuteNonQueryAsync();
                    Console.WriteLine($"Deleted {companiesDeleted} companies owned by user {userId}");
                }

                // Now delete user (cascades to user-related tables)
                var query = "DELETE FROM Users WHERE Id = @UserId";
                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);
                await cmd.ExecuteNonQueryAsync();

                return Ok(new { message = "User and associated companies deleted successfully" });
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

        // Get all user resumes
        [HttpGet("resumes")]
        public async Task<IActionResult> GetAllResumes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
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
                    : "AND (u.FullName LIKE @Search OR u.Email LIKE @Search OR r.job_title LIKE @Search)";

                // Get total count
                var countQuery = $@"
                    SELECT COUNT(*) 
                    FROM user_resumes r
                    JOIN Users u ON r.user_id = u.Id
                    WHERE 1=1 {searchCondition}";

                using var countCmd = new MySqlCommand(countQuery, connection);
                if (!string.IsNullOrEmpty(search))
                    countCmd.Parameters.AddWithValue("@Search", $"%{search}%");
                var totalCount = Convert.ToInt32(await countCmd.ExecuteScalarAsync());

                // Get resumes
                var query = $@"
                    SELECT 
                        r.id, r.user_id, r.full_name, r.job_title, r.email, r.phone,
                        r.location, r.linkedin, r.professional_summary,
                        r.skills, r.experiences, r.education,
                        r.certifications, r.projects, r.languages, r.achievements,
                        r.created_at, r.updated_at,
                        u.Username, u.Email as UserEmail
                    FROM user_resumes r
                    JOIN Users u ON r.user_id = u.Id
                    WHERE 1=1 {searchCondition}
                    ORDER BY r.updated_at DESC
                    LIMIT @PageSize OFFSET @Offset";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@PageSize", pageSize);
                cmd.Parameters.AddWithValue("@Offset", offset);
                if (!string.IsNullOrEmpty(search))
                    cmd.Parameters.AddWithValue("@Search", $"%{search}%");

                using var reader = await cmd.ExecuteReaderAsync();
                var resumes = new List<object>();

                while (await reader.ReadAsync())
                {
                    var idIdx = reader.GetOrdinal("id");
                    var userIdIdx = reader.GetOrdinal("user_id");
                    var fullNameIdx = reader.GetOrdinal("full_name");
                    var jobTitleIdx = reader.GetOrdinal("job_title");
                    var emailIdx = reader.GetOrdinal("email");
                    var phoneIdx = reader.GetOrdinal("phone");
                    var locationIdx = reader.GetOrdinal("location");
                    var linkedinIdx = reader.GetOrdinal("linkedin");
                    var summaryIdx = reader.GetOrdinal("professional_summary");
                    var skillsIdx = reader.GetOrdinal("skills");
                    var experiencesIdx = reader.GetOrdinal("experiences");
                    var educationIdx = reader.GetOrdinal("education");
                    var certificationsIdx = reader.GetOrdinal("certifications");
                    var projectsIdx = reader.GetOrdinal("projects");
                    var languagesIdx = reader.GetOrdinal("languages");
                    var achievementsIdx = reader.GetOrdinal("achievements");
                    var createdAtIdx = reader.GetOrdinal("created_at");
                    var updatedAtIdx = reader.GetOrdinal("updated_at");
                    var usernameIdx = reader.GetOrdinal("Username");
                    var userEmailIdx = reader.GetOrdinal("UserEmail");

                    resumes.Add(new
                    {
                        id = reader.GetInt32(idIdx),
                        userId = reader.GetInt32(userIdIdx),
                        fullName = reader.IsDBNull(fullNameIdx) ? "" : reader.GetString(fullNameIdx),
                        jobTitle = reader.IsDBNull(jobTitleIdx) ? "" : reader.GetString(jobTitleIdx),
                        email = reader.IsDBNull(emailIdx) ? "" : reader.GetString(emailIdx),
                        phone = reader.IsDBNull(phoneIdx) ? "" : reader.GetString(phoneIdx),
                        location = reader.IsDBNull(locationIdx) ? "" : reader.GetString(locationIdx),
                        linkedin = reader.IsDBNull(linkedinIdx) ? "" : reader.GetString(linkedinIdx),
                        professionalSummary = reader.IsDBNull(summaryIdx) ? "" : reader.GetString(summaryIdx),
                        skills = reader.IsDBNull(skillsIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(skillsIdx)),
                        experiences = reader.IsDBNull(experiencesIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(experiencesIdx)),
                        education = reader.IsDBNull(educationIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(educationIdx)),
                        certifications = reader.IsDBNull(certificationsIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(certificationsIdx)),
                        projects = reader.IsDBNull(projectsIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(projectsIdx)),
                        languages = reader.IsDBNull(languagesIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(languagesIdx)),
                        achievements = reader.IsDBNull(achievementsIdx) ? null : JsonSerializer.Deserialize<object>(reader.GetString(achievementsIdx)),
                        createdAt = reader.GetDateTime(createdAtIdx),
                        updatedAt = reader.GetDateTime(updatedAtIdx),
                        username = reader.GetString(usernameIdx),
                        userEmail = reader.GetString(userEmailIdx)
                    });
                }

                return Ok(new
                {
                    resumes,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting resumes: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching resumes", error = ex.Message });
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
                    INSERT INTO careers (name, description, required_education, average_salary, growth_outlook, key_skills)
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
                    SET name = @name, 
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

        // ==========================================
        // Company Management (Admin)
        // ==========================================

        /// <summary>
        /// Get all companies (admin only)
        /// </summary>
        [HttpGet("companies")]
        public async Task<IActionResult> GetAllCompanies([FromQuery] string? status = null)
        {
            try
            {
                if (!IsAdmin())
                    return StatusCode(403, new { message = "Admin access required" });

                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var sql = "SELECT * FROM companies";
                if (status == "pending") sql += " WHERE is_approved = FALSE";
                else if (status == "approved") sql += " WHERE is_approved = TRUE";
                sql += " ORDER BY created_at DESC";

                using var cmd = new MySqlCommand(sql, conn);
                using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                var companies = new List<object>();
                while (await reader.ReadAsync())
                {
                    companies.Add(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name"),
                        description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                        industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString("industry"),
                        logoUrl = reader.IsDBNull(reader.GetOrdinal("logo_url")) ? null : reader.GetString("logo_url"),
                        website = reader.IsDBNull(reader.GetOrdinal("website")) ? null : reader.GetString("website"),
                        location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location"),
                        contactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString("contact_email"),
                        isApproved = reader.GetBoolean("is_approved"),
                        createdAt = reader.GetDateTime("created_at")
                    });
                }

                return Ok(companies);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching companies: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching companies", error = ex.Message });
            }
        }

        /// <summary>
        /// Approve a company (admin only)
        /// </summary>
        [HttpPut("companies/{id}/approve")]
        public async Task<IActionResult> ApproveCompany(int id)
        {
            try
            {
                if (!IsAdmin())
                    return StatusCode(403, new { message = "Admin access required" });

                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var sql = "UPDATE companies SET is_approved = TRUE WHERE id = @id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                var rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? Ok(new { message = "Company approved successfully" })
                    : NotFound(new { message = "Company not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error approving company: {ex.Message}");
                return StatusCode(500, new { message = "Error approving company", error = ex.Message });
            }
        }

        /// <summary>
        /// Reject/revoke a company (admin only)
        /// </summary>
        [HttpPut("companies/{id}/reject")]
        public async Task<IActionResult> RejectCompany(int id)
        {
            try
            {
                if (!IsAdmin())
                    return StatusCode(403, new { message = "Admin access required" });

                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var sql = "UPDATE companies SET is_approved = FALSE WHERE id = @id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                var rows = await cmd.ExecuteNonQueryAsync();

                return rows > 0
                    ? Ok(new { message = "Company rejected" })
                    : NotFound(new { message = "Company not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rejecting company: {ex.Message}");
                return StatusCode(500, new { message = "Error rejecting company", error = ex.Message });
            }
        }

        /// <summary>
        /// Get company detail with posting stats (admin only)
        /// </summary>
        [HttpGet("companies/{id}")]
        public async Task<IActionResult> GetCompanyDetail(int id)
        {
            try
            {
                if (!IsAdmin())
                    return StatusCode(403, new { message = "Admin access required" });

                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Company info
                var sql = "SELECT * FROM companies WHERE id = @id";
                using var cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                    return NotFound(new { message = "Company not found" });

                var company = new
                {
                    id = reader.GetInt32("id"),
                    name = reader.GetString("name"),
                    description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                    industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString("industry"),
                    website = reader.IsDBNull(reader.GetOrdinal("website")) ? null : reader.GetString("website"),
                    location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location"),
                    contactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString("contact_email"),
                    isApproved = reader.GetBoolean("is_approved"),
                    createdAt = reader.GetDateTime("created_at")
                };
                reader.Close();

                // Posting stats
                var statsSql = @"SELECT 
                    (SELECT COUNT(*) FROM hiring_notifications WHERE company_id = @cid) as total_postings,
                    (SELECT COUNT(*) FROM hiring_notifications WHERE company_id = @cid AND is_active = TRUE) as active_postings,
                    (SELECT COUNT(*) FROM job_applications WHERE company_id = @cid) as total_applications";
                using var cmd2 = new MySqlCommand(statsSql, conn);
                cmd2.Parameters.AddWithValue("@cid", id);
                using var reader2 = await cmd2.ExecuteReaderAsync();
                await reader2.ReadAsync();

                return Ok(new
                {
                    company,
                    stats = new
                    {
                        totalPostings = Convert.ToInt32(reader2["total_postings"]),
                        activePostings = Convert.ToInt32(reader2["active_postings"]),
                        totalApplications = Convert.ToInt32(reader2["total_applications"])
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching company detail: {ex.Message}");
                return StatusCode(500, new { message = "Error fetching company detail", error = ex.Message });
            }
        }
    }
}
