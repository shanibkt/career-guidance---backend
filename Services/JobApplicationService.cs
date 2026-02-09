using MySqlConnector;
using MyFirstApi.Models;

namespace MyFirstApi.Services
{
    public class JobApplicationService
    {
        private readonly DatabaseService _db;
        private readonly ILogger<JobApplicationService> _logger;

        public JobApplicationService(DatabaseService db, ILogger<JobApplicationService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ==========================================
        // Student: Apply to Hiring Notification
        // ==========================================

        public async Task<CompanyJobApplication> ApplyAsync(int userId, int hiringNotificationId, string? coverMessage)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            // 1. Get the company_id from the hiring notification
            int companyId;
            using (var cmd = new MySqlCommand("SELECT company_id FROM hiring_notifications WHERE id = @nid", conn))
            {
                cmd.Parameters.AddWithValue("@nid", hiringNotificationId);
                var result = await cmd.ExecuteScalarAsync();
                if (result == null) throw new Exception("Hiring notification not found");
                companyId = Convert.ToInt32(result);
            }

            // 2. Get student's resume data (snapshot it as JSON)
            string? resumeData = null;
            try
            {
                using var resumeCmd = new MySqlCommand(
                    @"SELECT full_name, job_title, email, phone, location, linkedin, 
                      professional_summary, skills, experiences, education,
                      certifications, projects, languages, achievements
                      FROM user_resumes WHERE user_id = @uid ORDER BY updated_at DESC LIMIT 1", conn);
                resumeCmd.Parameters.AddWithValue("@uid", userId);
                using var reader = await resumeCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var resumeObj = new Dictionary<string, object?>();
                    for (int i = 0; i < reader.FieldCount; i++)
                        resumeObj[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    resumeData = System.Text.Json.JsonSerializer.Serialize(resumeObj);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not fetch resume for user {userId}: {ex.Message}");
            }

            // 3. Insert the application
            var insertSql = @"INSERT INTO job_applications 
                (user_id, hiring_notification_id, company_id, cover_message, resume_data, status)
                VALUES (@uid, @nid, @cid, @cover, @resume, 'pending');
                SELECT LAST_INSERT_ID();";

            using var insertCmd = new MySqlCommand(insertSql, conn);
            insertCmd.Parameters.AddWithValue("@uid", userId);
            insertCmd.Parameters.AddWithValue("@nid", hiringNotificationId);
            insertCmd.Parameters.AddWithValue("@cid", companyId);
            insertCmd.Parameters.AddWithValue("@cover", coverMessage ?? (object)DBNull.Value);
            insertCmd.Parameters.AddWithValue("@resume", resumeData ?? (object)DBNull.Value);

            var applicationId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            _logger.LogInformation($"Student {userId} applied to hiring notification {hiringNotificationId}");

            return new CompanyJobApplication
            {
                Id = applicationId,
                UserId = userId,
                HiringNotificationId = hiringNotificationId,
                CompanyId = companyId,
                CoverMessage = coverMessage,
                ResumeData = resumeData,
                Status = "pending",
                AppliedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> HasAppliedAsync(int userId, int hiringNotificationId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "SELECT COUNT(*) FROM job_applications WHERE user_id = @uid AND hiring_notification_id = @nid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@nid", hiringNotificationId);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
        }

        // ==========================================
        // Student: My Applications
        // ==========================================

        public async Task<List<CompanyJobApplication>> GetMyApplicationsAsync(int userId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT ja.*, hn.title as notification_title, hn.position,
                        c.name as company_name
                        FROM job_applications ja
                        JOIN hiring_notifications hn ON ja.hiring_notification_id = hn.id
                        JOIN companies c ON ja.company_id = c.id
                        WHERE ja.user_id = @uid
                        ORDER BY ja.applied_at DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            var applications = new List<CompanyJobApplication>();
            while (await reader.ReadAsync())
            {
                applications.Add(MapApplication(reader));
            }
            return applications;
        }

        // ==========================================
        // Company: View & Manage Applications
        // ==========================================

        public async Task<List<CompanyJobApplication>> GetApplicationsForCompanyAsync(int companyId, int? notificationId = null)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT ja.*, hn.title as notification_title, hn.position,
                u.FullName as student_name, u.Email as student_email,
                ucp.career_name as student_career,
                c.name as company_name
                FROM job_applications ja
                JOIN hiring_notifications hn ON ja.hiring_notification_id = hn.id
                JOIN Users u ON ja.user_id = u.Id
                JOIN companies c ON ja.company_id = c.id
                LEFT JOIN user_career_progress ucp ON ja.user_id = ucp.user_id AND ucp.is_active = TRUE
                WHERE ja.company_id = @cid";

            if (notificationId.HasValue)
            {
                sql += " AND ja.hiring_notification_id = @nid";
            }
            sql += " ORDER BY ja.applied_at DESC";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@cid", companyId);
            if (notificationId.HasValue)
            {
                cmd.Parameters.AddWithValue("@nid", notificationId.Value);
            }

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            var applications = new List<CompanyJobApplication>();
            while (await reader.ReadAsync())
            {
                var app = MapApplication(reader);
                // Map student info (from JOIN)
                app.StudentName = reader.IsDBNull(reader.GetOrdinal("student_name")) ? null : reader.GetString("student_name");
                app.StudentEmail = reader.IsDBNull(reader.GetOrdinal("student_email")) ? null : reader.GetString("student_email");
                app.StudentCareer = reader.IsDBNull(reader.GetOrdinal("student_career")) ? null : reader.GetString("student_career");
                applications.Add(app);
            }
            return applications;
        }

        public async Task<bool> UpdateApplicationStatusAsync(int applicationId, int companyId, string newStatus)
        {
            var validStatuses = new[] { "pending", "reviewed", "shortlisted", "rejected" };
            if (!validStatuses.Contains(newStatus.ToLower()))
            {
                throw new ArgumentException($"Invalid status: {newStatus}. Must be one of: {string.Join(", ", validStatuses)}");
            }

            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE job_applications SET status = @status WHERE id = @id AND company_id = @cid";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", applicationId);
            cmd.Parameters.AddWithValue("@cid", companyId);
            cmd.Parameters.AddWithValue("@status", newStatus.ToLower());
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        private CompanyJobApplication MapApplication(MySqlDataReader reader)
        {
            return new CompanyJobApplication
            {
                Id = reader.GetInt32("id"),
                UserId = reader.GetInt32("user_id"),
                HiringNotificationId = reader.GetInt32("hiring_notification_id"),
                CompanyId = reader.GetInt32("company_id"),
                CoverMessage = reader.IsDBNull(reader.GetOrdinal("cover_message")) ? null : reader.GetString("cover_message"),
                ResumeData = reader.IsDBNull(reader.GetOrdinal("resume_data")) ? null : reader.GetString("resume_data"),
                Status = reader.GetString("status"),
                AppliedAt = reader.IsDBNull(reader.GetOrdinal("applied_at")) ? null : (DateTime?)reader.GetDateTime("applied_at"),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("updated_at")) ? null : (DateTime?)reader.GetDateTime("updated_at"),
                NotificationTitle = reader.IsDBNull(reader.GetOrdinal("notification_title")) ? null : reader.GetString("notification_title"),
                Position = reader.IsDBNull(reader.GetOrdinal("position")) ? null : reader.GetString("position"),
                CompanyName = reader.IsDBNull(reader.GetOrdinal("company_name")) ? null : reader.GetString("company_name")
            };
        }
    }
}
