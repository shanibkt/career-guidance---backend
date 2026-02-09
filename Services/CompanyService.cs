using MySqlConnector;
using MyFirstApi.Models;
using System.Text.Json;

namespace MyFirstApi.Services
{
    public class CompanyService
    {
        private readonly DatabaseService _db;
        private readonly ILogger<CompanyService> _logger;

        public CompanyService(DatabaseService db, ILogger<CompanyService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ==========================================
        // Company Registration & Profile
        // ==========================================

        public async Task<Company> RegisterCompanyAsync(int userId, CompanyRegisterRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();
            using var transaction = await conn.BeginTransactionAsync();

            try
            {
                // 1. Insert company
                var insertCompany = @"INSERT INTO companies (name, description, industry, website, location, contact_email) 
                                      VALUES (@name, @desc, @industry, @website, @location, @contactEmail);
                                      SELECT LAST_INSERT_ID();";
                using var cmd = new MySqlCommand(insertCompany, conn, transaction);
                cmd.Parameters.AddWithValue("@name", request.Name);
                cmd.Parameters.AddWithValue("@desc", request.Description ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@industry", request.Industry ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@website", request.Website ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@contactEmail", request.ContactEmail ?? (object)DBNull.Value);

                var companyId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                // 2. Link user to company
                var insertLink = @"INSERT INTO company_users (user_id, company_id, role) VALUES (@userId, @companyId, 'owner')";
                using var cmd2 = new MySqlCommand(insertLink, conn, transaction);
                cmd2.Parameters.AddWithValue("@userId", userId);
                cmd2.Parameters.AddWithValue("@companyId", companyId);
                await cmd2.ExecuteNonQueryAsync();

                // 3. Update user role to 'company'
                var updateRole = @"UPDATE Users SET Role = 'company' WHERE Id = @userId";
                using var cmd3 = new MySqlCommand(updateRole, conn, transaction);
                cmd3.Parameters.AddWithValue("@userId", userId);
                await cmd3.ExecuteNonQueryAsync();

                await transaction.CommitAsync();

                return new Company
                {
                    Id = companyId,
                    Name = request.Name,
                    Description = request.Description,
                    Industry = request.Industry,
                    Website = request.Website,
                    Location = request.Location,
                    ContactEmail = request.ContactEmail,
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Company?> GetCompanyByUserIdAsync(int userId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"SELECT c.* FROM companies c 
                        JOIN company_users cu ON c.id = cu.company_id 
                        WHERE cu.user_id = @userId LIMIT 1";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapCompany(reader);
            }
            return null;
        }

        public async Task<Company?> GetCompanyByIdAsync(int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "SELECT * FROM companies WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);

            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapCompany(reader);
            }
            return null;
        }

        public async Task<bool> UpdateCompanyAsync(int companyId, UpdateCompanyRequest request)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = @"UPDATE companies SET 
                        name = COALESCE(@name, name),
                        description = COALESCE(@desc, description),
                        industry = COALESCE(@industry, industry),
                        website = COALESCE(@website, website),
                        location = COALESCE(@location, location),
                        logo_url = COALESCE(@logoUrl, logo_url),
                        contact_email = COALESCE(@contactEmail, contact_email)
                        WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);
            cmd.Parameters.AddWithValue("@name", request.Name ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@desc", request.Description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@industry", request.Industry ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@website", request.Website ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@location", request.Location ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@logoUrl", request.LogoUrl ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@contactEmail", request.ContactEmail ?? (object)DBNull.Value);

            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ==========================================
        // Admin: Company Approval
        // ==========================================

        public async Task<List<Company>> GetAllCompaniesAsync(bool? approvedOnly = null)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "SELECT * FROM companies";
            if (approvedOnly.HasValue)
            {
                sql += approvedOnly.Value ? " WHERE is_approved = TRUE" : " WHERE is_approved = FALSE";
            }
            sql += " ORDER BY created_at DESC";

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            var companies = new List<Company>();
            while (await reader.ReadAsync())
            {
                companies.Add(MapCompany(reader));
            }
            return companies;
        }

        public async Task<bool> ApproveCompanyAsync(int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE companies SET is_approved = TRUE WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> RejectCompanyAsync(int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var sql = "UPDATE companies SET is_approved = FALSE WHERE id = @id";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", companyId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ==========================================
        // Dashboard Stats
        // ==========================================

        public async Task<CompanyDashboardStats> GetDashboardStatsAsync(int companyId)
        {
            using var conn = _db.GetConnection();
            await conn.OpenAsync();

            var stats = new CompanyDashboardStats();

            // Total postings
            var sql1 = "SELECT COUNT(*) FROM hiring_notifications WHERE company_id = @cid";
            using (var cmd = new MySqlCommand(sql1, conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.TotalPostings = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Active postings
            var sql2 = "SELECT COUNT(*) FROM hiring_notifications WHERE company_id = @cid AND is_active = TRUE";
            using (var cmd = new MySqlCommand(sql2, conn))
            {
                cmd.Parameters.AddWithValue("@cid", companyId);
                stats.ActivePostings = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            // Check if job_applications has company_id column before querying
            bool hasCompanyIdCol = false;
            try
            {
                using var checkCmd = new MySqlCommand(
                    "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'job_applications' AND COLUMN_NAME = 'company_id'", conn);
                hasCompanyIdCol = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
            }
            catch { }

            if (hasCompanyIdCol)
            {
                // Total applications
                var sql3 = "SELECT COUNT(*) FROM job_applications WHERE company_id = @cid";
                using (var cmd = new MySqlCommand(sql3, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    stats.TotalApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Pending
                var sql4 = "SELECT COUNT(*) FROM job_applications WHERE company_id = @cid AND status = 'pending'";
                using (var cmd = new MySqlCommand(sql4, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    stats.PendingApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }

                // Shortlisted
                var sql5 = "SELECT COUNT(*) FROM job_applications WHERE company_id = @cid AND status = 'shortlisted'";
                using (var cmd = new MySqlCommand(sql5, conn))
                {
                    cmd.Parameters.AddWithValue("@cid", companyId);
                    stats.ShortlistedApplications = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                }
            }

            return stats;
        }

        private Company MapCompany(MySqlDataReader reader)
        {
            return new Company
            {
                Id = reader.GetInt32("id"),
                Name = reader.GetString("name"),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString("description"),
                Industry = reader.IsDBNull(reader.GetOrdinal("industry")) ? null : reader.GetString("industry"),
                LogoUrl = reader.IsDBNull(reader.GetOrdinal("logo_url")) ? null : reader.GetString("logo_url"),
                Website = reader.IsDBNull(reader.GetOrdinal("website")) ? null : reader.GetString("website"),
                Location = reader.IsDBNull(reader.GetOrdinal("location")) ? null : reader.GetString("location"),
                ContactEmail = reader.IsDBNull(reader.GetOrdinal("contact_email")) ? null : reader.GetString("contact_email"),
                IsApproved = reader.GetBoolean("is_approved"),
                CreatedAt = reader.GetDateTime("created_at"),
                UpdatedAt = reader.GetDateTime("updated_at")
            };
        }
    }
}
