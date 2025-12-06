using MySql.Data.MySqlClient;
using System.Text.Json;
using MyFirstApi.Models;

namespace MyFirstApi.Services
{
    public class JobDatabaseService
    {
        private readonly IConfiguration _configuration;

        public JobDatabaseService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Save a job for a user
        /// </summary>
        public async Task<bool> SaveJobAsync(int userId, JobResponse job)
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Check if job already saved
                var checkQuery = "SELECT id FROM saved_jobs WHERE user_id = @userId AND job_id = @jobId";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@jobId", job.Id);

                var existingJob = await checkCmd.ExecuteScalarAsync();
                if (existingJob != null)
                    return true; // Already saved

                // Insert new saved job
                var insertQuery = @"
                    INSERT INTO saved_jobs 
                    (user_id, job_id, title, company, location, url, description, job_type, 
                     salary_min, salary_max, salary_currency, experience_level, required_skills, posted_date, saved_at)
                    VALUES 
                    (@userId, @jobId, @title, @company, @location, @url, @description, @jobType, 
                     @salaryMin, @salaryMax, @salaryCurrency, @experienceLevel, @requiredSkills, @postedDate, @savedAt)";

                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@jobId", job.Id);
                insertCmd.Parameters.AddWithValue("@title", job.Title ?? "");
                insertCmd.Parameters.AddWithValue("@company", job.Company ?? "");
                insertCmd.Parameters.AddWithValue("@location", job.Location ?? "");
                insertCmd.Parameters.AddWithValue("@url", job.Url ?? "");
                insertCmd.Parameters.AddWithValue("@description", job.Description ?? "");
                insertCmd.Parameters.AddWithValue("@jobType", job.JobType ?? "");
                insertCmd.Parameters.AddWithValue("@salaryMin", job.SalaryMin ?? "");
                insertCmd.Parameters.AddWithValue("@salaryMax", job.SalaryMax ?? "");
                insertCmd.Parameters.AddWithValue("@salaryCurrency", job.SalaryCurrency ?? "USD");
                insertCmd.Parameters.AddWithValue("@experienceLevel", job.ExperienceLevel ?? "");
                insertCmd.Parameters.AddWithValue("@requiredSkills", JsonSerializer.Serialize(job.RequiredSkills ?? new List<string>()));
                insertCmd.Parameters.AddWithValue("@postedDate", job.PostedDate ?? "");
                insertCmd.Parameters.AddWithValue("@savedAt", DateTime.UtcNow);

                var result = await insertCmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error saving job: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove a saved job
        /// </summary>
        public async Task<bool> RemoveSavedJobAsync(int userId, string jobId)
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = "DELETE FROM saved_jobs WHERE user_id = @userId AND job_id = @jobId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@jobId", jobId);

                var result = await cmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error removing saved job: {ex.Message}");
            }
        }

        /// <summary>
        /// Get all saved jobs for a user
        /// </summary>
        public async Task<List<JobResponse>> GetSavedJobsAsync(int userId)
        {
            try
            {
                var jobs = new List<JobResponse>();

                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = @"
                    SELECT job_id, title, company, location, url, description, job_type, 
                           salary_min, salary_max, salary_currency, experience_level, required_skills, posted_date
                    FROM saved_jobs 
                    WHERE user_id = @userId 
                    ORDER BY saved_at DESC";

                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var job = new JobResponse
                    {
                        Id = reader.GetString(reader.GetOrdinal("job_id")),
                        Title = reader.GetString(reader.GetOrdinal("title")),
                        Company = reader.GetString(reader.GetOrdinal("company")),
                        Location = reader.GetString(reader.GetOrdinal("location")),
                        Url = reader.IsDBNull(reader.GetOrdinal("url")) ? null : reader.GetString(reader.GetOrdinal("url")),
                        Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                        JobType = reader.IsDBNull(reader.GetOrdinal("job_type")) ? null : reader.GetString(reader.GetOrdinal("job_type")),
                        SalaryMin = reader.IsDBNull(reader.GetOrdinal("salary_min")) ? null : reader.GetString(reader.GetOrdinal("salary_min")),
                        SalaryMax = reader.IsDBNull(reader.GetOrdinal("salary_max")) ? null : reader.GetString(reader.GetOrdinal("salary_max")),
                        SalaryCurrency = reader.IsDBNull(reader.GetOrdinal("salary_currency")) ? "USD" : reader.GetString(reader.GetOrdinal("salary_currency")),
                        ExperienceLevel = reader.IsDBNull(reader.GetOrdinal("experience_level")) ? null : reader.GetString(reader.GetOrdinal("experience_level")),
                        PostedDate = reader.IsDBNull(reader.GetOrdinal("posted_date")) ? null : reader.GetString(reader.GetOrdinal("posted_date")),
                        IsSaved = true,
                    };

                    if (!reader.IsDBNull(reader.GetOrdinal("required_skills")))
                    {
                        var skillsJson = reader.GetString(reader.GetOrdinal("required_skills"));
                        job.RequiredSkills = JsonSerializer.Deserialize<List<string>>(skillsJson) ?? new List<string>();
                    }

                    jobs.Add(job);
                }

                return jobs;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting saved jobs: {ex.Message}");
            }
        }

        /// <summary>
        /// Record job application
        /// </summary>
        public async Task<bool> ApplyForJobAsync(int userId, string jobId, JobResponse job, string? coverLetter = null)
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Check if already applied
                var checkQuery = "SELECT id FROM job_applications WHERE user_id = @userId AND job_id = @jobId";
                using var checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                checkCmd.Parameters.AddWithValue("@jobId", jobId);

                var existingApp = await checkCmd.ExecuteScalarAsync();
                if (existingApp != null)
                    return true; // Already applied

                // Insert job application
                var insertQuery = @"
                    INSERT INTO job_applications 
                    (user_id, job_id, title, company, location, cover_letter, application_status, applied_at)
                    VALUES 
                    (@userId, @jobId, @title, @company, @location, @coverLetter, 'Applied', @appliedAt)";

                using var insertCmd = new MySqlCommand(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@jobId", jobId);
                insertCmd.Parameters.AddWithValue("@title", job.Title ?? "");
                insertCmd.Parameters.AddWithValue("@company", job.Company ?? "");
                insertCmd.Parameters.AddWithValue("@location", job.Location ?? "");
                insertCmd.Parameters.AddWithValue("@coverLetter", coverLetter ?? "");
                insertCmd.Parameters.AddWithValue("@appliedAt", DateTime.UtcNow);

                var result = await insertCmd.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error applying for job: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if job is saved by user
        /// </summary>
        public async Task<bool> IsJobSavedAsync(int userId, string jobId)
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = "SELECT id FROM saved_jobs WHERE user_id = @userId AND job_id = @jobId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@jobId", jobId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking if job is saved: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if user has applied for job
        /// </summary>
        public async Task<bool> IsJobAppliedAsync(int userId, string jobId)
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var query = "SELECT id FROM job_applications WHERE user_id = @userId AND job_id = @jobId";
                using var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@jobId", jobId);

                var result = await cmd.ExecuteScalarAsync();
                return result != null;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error checking job application: {ex.Message}");
            }
        }
    }
}
