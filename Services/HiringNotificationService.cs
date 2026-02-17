using MySqlConnector;
using MyFirstApi.Models;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;

namespace MyFirstApi.Services
{
    public class HiringNotificationService
    {
        private readonly DatabaseService _db;
        private readonly IMemoryCache _cache;
        private readonly ILogger<HiringNotificationService> _logger;

        public HiringNotificationService(DatabaseService db, IMemoryCache cache, ILogger<HiringNotificationService> logger)
        {
            _db = db;
            _cache = cache;
            _logger = logger;
        }

        // ==========================================
        // Company: Create & Manage Hiring Notifications
        // ==========================================

        public async Task<HiringNotification> CreateHiringNotificationAsync(int companyId, CreateHiringNotificationRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 1. Insert the hiring notification
                var careerIdsJson = JsonSerializer.Serialize(request.TargetCareerIds);

                var insertSql = @"INSERT INTO hiring_notifications 
                    (company_id, title, description, position, location, salary_range, requirements, target_career_ids, application_deadline)
                    VALUES (@companyId, @title, @desc, @position, @location, @salary, @requirements, @careerIds, @deadline)";

                using var cmd = new MySqlCommand(insertSql, conn, transaction);
                cmd.Parameters.AddWithValue("@companyId", companyId);
                cmd.Parameters.AddWithValue("@title", request.Title);
                cmd.Parameters.AddWithValue("@desc", request.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@position", request.Position);
                cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@salary", request.SalaryRange ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@requirements", request.Requirements ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@careerIds", careerIdsJson);
                cmd.Parameters.AddWithValue("@deadline", request.ApplicationDeadline ?? (object)DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                var notificationId = (int)cmd.LastInsertedId;

                if (notificationId <= 0)
                {
                    throw new Exception("Failed to retrieve notification ID after insertion.");
                }

                // 2. Find students with matching active careers and create student_notifications
                // Match by both career_id AND career_name since Flutter app often sends only careerName
                if (request.TargetCareerIds.Any())
                {
                    // Build IN clause for career IDs
                    var careerParams = new List<string>();
                    for (int i = 0; i < request.TargetCareerIds.Count; i++)
                    {
                        careerParams.Add($"@cid{i}");
                    }
                    var inClause = string.Join(",", careerParams);

                    // Determine the correct column name for career name
                    string nameColumn = "name";
                    try
                    {
                        using var checkColCmd = new MySqlCommand(
                            "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'careers' AND COLUMN_NAME IN ('name', 'career_name') LIMIT 1", conn, transaction);
                        var colResult = await checkColCmd.ExecuteScalarAsync();
                        if (colResult != null) nameColumn = colResult.ToString();
                    }
                    catch { }

                    // Match students by career_id OR by career_name (via careers table lookup) from both progress and profiles
                    var findStudentsSql = $@"SELECT DISTINCT enrollment.user_id 
                                           FROM (
                                               SELECT user_id, career_id, career_name FROM user_career_progress WHERE is_active = TRUE
                                               UNION
                                               SELECT UserId as user_id, NULL as career_id, career_path as career_name FROM UserProfiles WHERE career_path IS NOT NULL
                                           ) enrollment
                                           WHERE (
                                               enrollment.career_id IN ({inClause})
                                               OR enrollment.career_name IN (SELECT {nameColumn} FROM careers WHERE id IN ({inClause}))
                                           )";

                    using var cmd2 = new MySqlCommand(findStudentsSql, conn, transaction);
                    for (int i = 0; i < request.TargetCareerIds.Count; i++)
                    {
                        cmd2.Parameters.AddWithValue($"@cid{i}", request.TargetCareerIds[i]);
                    }

                    var studentIds = new List<int>();
                    using (var reader = (MySqlDataReader)await cmd2.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            studentIds.Add(reader.GetInt32("user_id"));
                        }
                    }

                    // 3. Bulk insert student notifications
                    if (studentIds.Any())
                    {
                        var values = new List<string>();
                        var insertStudentCmd = new MySqlCommand("", conn, transaction);

                        for (int i = 0; i < studentIds.Count; i++)
                        {
                            values.Add($"(@uid{i}, @nid)");
                            insertStudentCmd.Parameters.AddWithValue($"@uid{i}", studentIds[i]);
                        }
                        insertStudentCmd.Parameters.AddWithValue("@nid", notificationId);

                        insertStudentCmd.CommandText = $@"INSERT IGNORE INTO student_notifications 
                            (user_id, hiring_notification_id) VALUES {string.Join(",", values)}";
                        await insertStudentCmd.ExecuteNonQueryAsync();

                        _logger.LogInformation($"Created hiring notification {notificationId} targeting {studentIds.Count} students");
                    }
                }

                await transaction.CommitAsync();

                return new HiringNotification
                {
                    Id = notificationId,
                    CompanyId = companyId,
                    Title = request.Title,
                    Description = request.Description,
                    Position = request.Position,
                    Location = request.Location,
                    SalaryRange = request.SalaryRange,
                    Requirements = request.Requirements,
                    TargetCareerIds = request.TargetCareerIds,
                    ApplicationDeadline = request.ApplicationDeadline,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<HiringNotification>> GetCompanyNotificationsAsync(int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Check if job_applications has hiring_notification_id column
            bool hasHiringNotifCol = false;
            try
            {
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'job_applications' AND COLUMN_NAME = 'hiring_notification_id'", conn);
                hasHiringNotifCol = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            }
            catch { }

            var appCountSubquery = hasHiringNotifCol
                ? "(SELECT COUNT(*) FROM job_applications ja WHERE ja.hiring_notification_id = hn.id)"
                : "0";

            var sql = $@"SELECT hn.*, c.name as company_name, c.logo_url as company_logo,
                        {appCountSubquery} as application_count,
                        (SELECT COUNT(*) FROM student_notifications sn WHERE sn.hiring_notification_id = hn.id) as target_student_count
                        FROM hiring_notifications hn
                        JOIN companies c ON hn.company_id = c.id
                        WHERE hn.company_id = @companyId
                        ORDER BY hn.created_at DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@companyId", companyId);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            var notifications = new List<HiringNotification>();
            while (await reader.ReadAsync())
            {
                notifications.Add(MapHiringNotification(reader));
            }
            return notifications;
        }

        public async Task<bool> DeactivateNotificationAsync(int notificationId, int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE hiring_notifications SET is_active = FALSE WHERE id = @id AND company_id = @cid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", notificationId);
            cmd.Parameters.AddWithValue("@cid", companyId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateHiringNotificationAsync(int notificationId, int companyId, CreateHiringNotificationRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var careerIdsJson = JsonSerializer.Serialize(request.TargetCareerIds);

            var sql = @"UPDATE hiring_notifications SET 
                        title = @title, description = @desc, position = @position,
                        location = @location, salary_range = @salary, requirements = @requirements,
                        target_career_ids = @careerIds, application_deadline = @deadline
                        WHERE id = @id AND company_id = @cid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", notificationId);
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@title", request.Title);
            cmd.Parameters.AddWithValue("@desc", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@position", request.Position);
            cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@salary", request.SalaryRange ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@requirements", request.Requirements ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@careerIds", careerIdsJson);
            cmd.Parameters.AddWithValue("@deadline", request.ApplicationDeadline ?? (object)DBNull.Value);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ==========================================
        // Student: Get Notifications
        // ==========================================

        public async Task<List<StudentNotificationItem>> GetStudentNotificationsAsync(int userId)
        {
            var cacheKey = $"notifications_{userId}";
            if (_cache.TryGetValue(cacheKey, out List<StudentNotificationItem>? cachedNotifications))
            {
                return cachedNotifications!;
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            await SyncStudentNotificationsInternalAsync(conn, userId);

            var sql = @"SELECT sn.*, hn.title, hn.description, hn.position, hn.location, 
                        hn.salary_range, hn.requirements, hn.application_deadline,
                        c.name as company_name, c.logo_url as company_logo, c.website as company_website,
                        (SELECT COUNT(*) FROM job_applications ja WHERE ja.hiring_notification_id = hn.id AND ja.user_id = @userId) as has_applied
                        FROM student_notifications sn
                        JOIN hiring_notifications hn ON sn.hiring_notification_id = hn.id
                        JOIN companies c ON hn.company_id = c.id
                        WHERE sn.user_id = @userId AND hn.is_active = TRUE
                        ORDER BY sn.is_read ASC, sn.created_at DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            var notifications = new List<StudentNotificationItem>();
            while (await reader.ReadAsync())
            {
                notifications.Add(new StudentNotificationItem
                {
                    Id = reader.GetInt32("id"),
                    UserId = reader.GetInt32("user_id"),
                    HiringNotificationId = reader.GetInt32("hiring_notification_id"),
                    IsRead = reader.GetBoolean("is_read"),
                    ReadAt = reader.IsDBNull(reader.GetOrdinal("read_at")) ? null : reader.GetDateTime("read_at"),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                    Title = reader.GetString("title"),
                    Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                    Position = reader.GetString("position"),
                    Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location"),
                    SalaryRange = reader.IsDBNull(reader.GetOrdinal("salary_range")) ? null : reader.GetString("salary_range"),
                    Requirements = reader.IsDBNull(reader.GetOrdinal("requirements")) ? null : reader.GetString("requirements"),
                    ApplicationDeadline = reader.IsDBNull(reader.GetOrdinal("application_deadline")) ? null : reader.GetDateTime("application_deadline").ToString("yyyy-MM-dd"),
                    CompanyName = reader.GetString("company_name"),
                    CompanyLogo = reader.IsDBNull(reader.GetOrdinal("company_logo")) ? null : reader.GetString("company_logo"),
                    CompanyWebsite = reader.IsDBNull(reader.GetOrdinal("company_website")) ? null : reader.GetString("company_website"),
                    HasApplied = reader.GetInt32("has_applied") > 0
                });
            }

            _cache.Set(cacheKey, notifications, TimeSpan.FromMinutes(2));
            return notifications;
        }

        private async Task SyncStudentNotificationsInternalAsync(MySqlConnection conn, int userId)
        {
            var syncCacheKey = $"last_sync_{userId}";
            if (_cache.TryGetValue(syncCacheKey, out _)) return;

            try
            {
                // Determine the correct column name for career name
                string nameColumn = "name";
                try
                {
                    using var checkColCmd = new MySqlCommand(
                        "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'careers' AND COLUMN_NAME IN ('name', 'career_name') LIMIT 1", conn);
                    var colResult = await checkColCmd.ExecuteScalarAsync();
                    if (colResult != null) nameColumn = colResult.ToString();
                }
                catch { }

                var syncSql = $@"INSERT IGNORE INTO student_notifications (user_id, hiring_notification_id)
                    SELECT @userId, hn.id
                    FROM hiring_notifications hn
                    JOIN companies c ON hn.company_id = c.id AND c.is_approved = TRUE
                    -- Join with either active career progress OR profile career path
                    JOIN (
                        SELECT user_id, career_id, career_name, selected_at 
                        FROM user_career_progress 
                        WHERE user_id = @userId AND is_active = TRUE
                        UNION
                        SELECT UserId as user_id, NULL as career_id, career_path as career_name, CreatedAt as selected_at
                        FROM UserProfiles
                        WHERE UserId = @userId AND career_path IS NOT NULL
                    ) enrollment ON (
                        JSON_CONTAINS(hn.target_career_ids, CAST(enrollment.career_id AS JSON))
                        OR EXISTS (
                            SELECT 1 FROM careers c2 
                            WHERE c2.{nameColumn} = enrollment.career_name 
                            AND JSON_CONTAINS(hn.target_career_ids, CAST(c2.id AS JSON))
                        )
                    )
                    WHERE hn.is_active = TRUE
                    -- 🔥 Filter: Only show notifications created after (or just before) career selection
                    AND hn.created_at >= DATE_SUB(enrollment.selected_at, INTERVAL 3 DAY)
                    AND NOT EXISTS (
                        SELECT 1 FROM student_notifications sn2 
                        WHERE sn2.user_id = @userId AND sn2.hiring_notification_id = hn.id
                    )";
                using var syncCmd = new MySqlCommand(syncSql, conn);
                syncCmd.Parameters.AddWithValue("@userId", userId);
                var inserted = await syncCmd.ExecuteNonQueryAsync();
                
                if (inserted > 0)
                    _logger.LogInformation($"Auto-created {inserted} student_notifications for userId={userId}");

                _cache.Set(syncCacheKey, true, TimeSpan.FromMinutes(5));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to auto-sync student_notifications for userId={userId}: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            var cacheKey = $"unread_count_{userId}";
            if (_cache.TryGetValue(cacheKey, out int cachedCount))
            {
                return cachedCount;
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            await SyncStudentNotificationsInternalAsync(conn, userId);

            var sql = @"SELECT COUNT(*) FROM student_notifications sn
                        JOIN hiring_notifications hn ON sn.hiring_notification_id = hn.id
                        WHERE sn.user_id = @userId AND sn.is_read = FALSE AND hn.is_active = TRUE";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            _cache.Set(cacheKey, count, TimeSpan.FromMinutes(1));
            return count;
        }

        public async Task<bool> MarkAsReadAsync(int userId, int notificationId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE student_notifications SET is_read = TRUE, read_at = NOW() 
                        WHERE user_id = @userId AND hiring_notification_id = @nid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@nid", notificationId);
            
            var success = await cmd.ExecuteNonQueryAsync() > 0;
            if (success)
            {
                _cache.Remove($"notifications_{userId}");
                _cache.Remove($"unread_count_{userId}");
            }
            return success;
        }

        // ==========================================
        // Career Stats (for company targeting UI)
        // ==========================================

        public async Task<List<CareerStudentCount>> GetCareerStudentCountsAsync()
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // Determine the correct column name for career name
            string nameColumn = "name";
            try
            {
                using var checkColCmd = new MySqlCommand(
                    "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'careers' AND COLUMN_NAME IN ('name', 'career_name') LIMIT 1", conn);
                var colResult = await checkColCmd.ExecuteScalarAsync();
                if (colResult != null) nameColumn = colResult.ToString();
            }
            catch { }

            // Match by both career_id AND career_name since Flutter app often sends only careerName (career_id is NULL)
            // Union both career progress and user profiles to get a comprehensive student count
            var sql = $@"SELECT c.id as career_id, c.{nameColumn} as career_name, 
                        COUNT(DISTINCT enrollment.user_id) as student_count
                        FROM careers c
                        LEFT JOIN (
                            SELECT user_id, career_id, career_name FROM user_career_progress WHERE is_active = TRUE
                            UNION
                            SELECT UserId as user_id, NULL as career_id, career_path as career_name FROM UserProfiles WHERE career_path IS NOT NULL
                        ) enrollment ON (c.id = enrollment.career_id OR c.{nameColumn} = enrollment.career_name)
                        GROUP BY c.id, c.{nameColumn}
                        ORDER BY student_count DESC, c.{nameColumn} ASC";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            var results = new List<CareerStudentCount>();
            while (await reader.ReadAsync())
            {
                results.Add(new CareerStudentCount
                {
                    CareerId = reader.GetInt32("career_id"),
                    CareerName = reader.GetString("career_name"),
                    StudentCount = Convert.ToInt32(reader["student_count"])
                });
            }
            return results;
        }

        private HiringNotification MapHiringNotification(MySqlDataReader reader)
        {
            List<int>? careerIds = null;
            if (!reader.IsDBNull(reader.GetOrdinal("target_career_ids")))
            {
                try
                {
                    careerIds = JsonSerializer.Deserialize<List<int>>(reader.GetString("target_career_ids"));
                }
                catch { careerIds = new List<int>(); }
            }

            return new HiringNotification
            {
                Id = reader.GetInt32("id"),
                CompanyId = reader.GetInt32("company_id"),
                CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString("company_name"),
                CompanyLogo = reader.IsDBNull(reader.GetOrdinal("company_logo")) ? null : reader.GetString("company_logo"),
                Title = reader.GetString("title"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Position = reader.GetString("position"),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location"),
                SalaryRange = reader.IsDBNull(reader.GetOrdinal("salary_range")) ? null : reader.GetString("salary_range"),
                Requirements = reader.IsDBNull(reader.GetOrdinal("requirements")) ? null : reader.GetString("requirements"),
                TargetCareerIds = careerIds,
                ApplicationDeadline = reader.IsDBNull(reader.GetOrdinal("application_deadline")) ? null : reader.GetDateTime("application_deadline").ToString("yyyy-MM-dd"),
                IsActive = reader.GetBoolean("is_active"),
                ApplicationCount = reader.IsDBNull(reader.GetOrdinal("application_count")) ? 0 : Convert.ToInt32(reader["application_count"]),
                TargetStudentCount = reader.IsDBNull(reader.GetOrdinal("target_student_count")) ? 0 : Convert.ToInt32(reader["target_student_count"]),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }
    }
}
