using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SetupController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SetupController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET /api/setup/db-test
        [HttpGet("db-test")]
        public IActionResult DbTest()
        {
            try
            {
                var connString = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
                using var conn = new MySqlConnection(connString);
                conn.Open();

                using var cmd = new MySqlCommand("SELECT 1", conn);
                var result = cmd.ExecuteScalar();

                return Ok(new
                {
                    success = true,
                    message = "Database connection successful",
                    result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database connection failed",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/create-refresh-tokens-table
        [HttpGet("create-refresh-tokens-table")]
        public IActionResult CreateRefreshTokensTable()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS refresh_tokens (
                        id INT PRIMARY KEY AUTO_INCREMENT,
                        user_id INT NOT NULL,
                        token VARCHAR(500) NOT NULL UNIQUE,
                        expires_at DATETIME NOT NULL,
                        created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                        revoked BOOLEAN DEFAULT FALSE,
                        FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
                        INDEX idx_user_id (user_id),
                        INDEX idx_token (token),
                        INDEX idx_expires (expires_at)
                    )";

                using MySqlCommand cmd = new(createTableSql, conn);
                cmd.ExecuteNonQuery();

                // Check if table was created
                string checkSql = "SHOW TABLES LIKE 'refresh_tokens'";
                using MySqlCommand checkCmd = new(checkSql, conn);
                var result = checkCmd.ExecuteScalar();

                if (result != null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "refresh_tokens table created successfully!",
                        table = "refresh_tokens"
                    });
                }
                else
                {
                    return StatusCode(500, new { success = false, message = "Table creation failed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to create table",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/create-chat-tables
        [HttpGet("create-chat-tables")]
        public async Task<IActionResult> CreateChatTables()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Create chat_sessions table
                string createSessionsTableSql = @"
                    CREATE TABLE IF NOT EXISTS chat_sessions (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        UserId INT NOT NULL,
                        SessionId VARCHAR(36) NOT NULL UNIQUE,
                        Title VARCHAR(255) NOT NULL DEFAULT 'New Conversation',
                        LastMessage TEXT NULL,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        IsDeleted TINYINT(1) DEFAULT 0,
                        INDEX idx_user_sessions (UserId, CreatedAt),
                        INDEX idx_session_id (SessionId)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                using (var cmd = new MySqlCommand(createSessionsTableSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Create chat_messages table
                string createMessagesTableSql = @"
                    CREATE TABLE IF NOT EXISTS chat_messages (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        SessionId VARCHAR(36) NOT NULL,
                        Role VARCHAR(20) NOT NULL,
                        Message TEXT NOT NULL,
                        Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
                        INDEX idx_session_messages (SessionId, Timestamp),
                        INDEX idx_timestamp (Timestamp)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                using (var cmd = new MySqlCommand(createMessagesTableSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                return Ok(new
                {
                    success = true,
                    message = "Chat tables (chat_sessions and chat_messages) created successfully!",
                    tables = new[] { "chat_sessions", "chat_messages" }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to create chat tables",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/verify-tables
        [HttpGet("verify-tables")]
        public IActionResult VerifyTables()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                var tables = new List<string>();
                string sql = "SHOW TABLES";
                using MySqlCommand cmd = new(sql, conn);
                using var reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    tables.Add(reader.GetString(0));
                }

                var hasRefreshTokens = tables.Contains("refresh_tokens");
                var hasCareers = tables.Contains("careers");
                var hasQuizSessions = tables.Contains("quiz_sessions");
                var hasRecommendations = tables.Contains("recommendations");
                var hasChatSessions = tables.Contains("ChatSessions");
                var hasChatMessages = tables.Contains("ChatMessages");

                return Ok(new
                {
                    success = true,
                    tables = tables,
                    requiredTables = new
                    {
                        refresh_tokens = hasRefreshTokens ? "✅" : "❌",
                        careers = hasCareers ? "✅" : "❌",
                        quiz_sessions = hasQuizSessions ? "✅" : "❌",
                        recommendations = hasRecommendations ? "✅" : "❌",
                        ChatSessions = hasChatSessions ? "✅" : "❌",
                        ChatMessages = hasChatMessages ? "✅" : "❌"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to verify tables",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/update-chat-tables
        [HttpGet("update-chat-tables")]
        public async Task<IActionResult> UpdateChatTables()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                var updates = new List<string>();

                // Add Title column
                try
                {
                    string addTitleSql = "ALTER TABLE ChatSessions ADD COLUMN Title VARCHAR(200) DEFAULT 'New Conversation'";
                    using var cmd1 = new MySqlCommand(addTitleSql, conn);
                    await cmd1.ExecuteNonQueryAsync();
                    updates.Add("✅ Added Title column");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate column"))
                {
                    updates.Add("⚠️ Title column already exists");
                }

                // Add LastMessage column
                try
                {
                    string addLastMessageSql = "ALTER TABLE ChatSessions ADD COLUMN LastMessage VARCHAR(500)";
                    using var cmd2 = new MySqlCommand(addLastMessageSql, conn);
                    await cmd2.ExecuteNonQueryAsync();
                    updates.Add("✅ Added LastMessage column");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate column"))
                {
                    updates.Add("⚠️ LastMessage column already exists");
                }

                // Add UpdatedAt column
                try
                {
                    string addUpdatedAtSql = "ALTER TABLE ChatSessions ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP";
                    using var cmd3 = new MySqlCommand(addUpdatedAtSql, conn);
                    await cmd3.ExecuteNonQueryAsync();
                    updates.Add("✅ Added UpdatedAt column");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate column"))
                {
                    updates.Add("⚠️ UpdatedAt column already exists");
                }

                // Add IsDeleted column
                try
                {
                    string addIsDeletedSql = "ALTER TABLE ChatSessions ADD COLUMN IsDeleted TINYINT(1) DEFAULT 0";
                    using var cmd4 = new MySqlCommand(addIsDeletedSql, conn);
                    await cmd4.ExecuteNonQueryAsync();
                    updates.Add("✅ Added IsDeleted column");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate column"))
                {
                    updates.Add("⚠️ IsDeleted column already exists");
                }

                // Add indexes
                try
                {
                    string addIndex1Sql = "CREATE INDEX idx_sessions_updated ON ChatSessions(UserId, UpdatedAt DESC)";
                    using var cmd5 = new MySqlCommand(addIndex1Sql, conn);
                    await cmd5.ExecuteNonQueryAsync();
                    updates.Add("✅ Added idx_sessions_updated index");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate key"))
                {
                    updates.Add("⚠️ idx_sessions_updated index already exists");
                }

                try
                {
                    string addIndex2Sql = "CREATE INDEX idx_sessions_deleted ON ChatSessions(UserId, IsDeleted)";
                    using var cmd6 = new MySqlCommand(addIndex2Sql, conn);
                    await cmd6.ExecuteNonQueryAsync();
                    updates.Add("✅ Added idx_sessions_deleted index");
                }
                catch (MySqlException ex) when (ex.Message.Contains("Duplicate key"))
                {
                    updates.Add("⚠️ idx_sessions_deleted index already exists");
                }

                return Ok(new
                {
                    success = true,
                    message = "Chat tables updated successfully!",
                    updates = updates
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to update chat tables",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/create-admin
        [HttpGet("create-admin")]
        public IActionResult CreateAdminUser()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                var results = new List<string>();

                // 1. Add Role column if not exists
                try
                {
                    using var alterCmd = new MySqlCommand(
                        "ALTER TABLE Users ADD COLUMN Role VARCHAR(20) DEFAULT 'user'", conn);
                    alterCmd.ExecuteNonQuery();
                    results.Add("✅ Role column added");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ Role column already exists");
                    else
                        throw;
                }

                // 2. Delete existing admin user if any
                using (var deleteCmd = new MySqlCommand(
                    "DELETE FROM Users WHERE Email = 'admin@careerguidance.com'", conn))
                {
                    int deleted = deleteCmd.ExecuteNonQuery();
                    if (deleted > 0)
                        results.Add($"🗑️ Deleted {deleted} existing admin user(s)");
                }

                // 3. Create new admin user with BCrypt hash
                var passwordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123");
                
                using (var insertCmd = new MySqlCommand(@"
                    INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
                    VALUES (@username, @fullName, @email, @passwordHash, @role, NOW())", conn))
                {
                    insertCmd.Parameters.AddWithValue("@username", "admin");
                    insertCmd.Parameters.AddWithValue("@fullName", "System Administrator");
                    insertCmd.Parameters.AddWithValue("@email", "admin@careerguidance.com");
                    insertCmd.Parameters.AddWithValue("@passwordHash", passwordHash);
                    insertCmd.Parameters.AddWithValue("@role", "admin");
                    
                    insertCmd.ExecuteNonQuery();
                    results.Add("✅ Admin user created");
                }

                // 4. Create admin_activity_log table if not exists
                try
                {
                    using var createTableCmd = new MySqlCommand(@"
                        CREATE TABLE IF NOT EXISTS admin_activity_log (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            admin_id INT NOT NULL,
                            action_type VARCHAR(50) NOT NULL,
                            target_user_id INT NULL,
                            description TEXT NOT NULL,
                            ip_address VARCHAR(45) NULL,
                            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                            FOREIGN KEY (admin_id) REFERENCES Users(Id) ON DELETE CASCADE
                        )", conn);
                    createTableCmd.ExecuteNonQuery();
                    results.Add("✅ admin_activity_log table created");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("already exists"))
                        results.Add("⚠️ admin_activity_log table already exists");
                    else
                        throw;
                }

                // 5. Verify admin user
                using (var verifyCmd = new MySqlCommand(
                    "SELECT Id, Username, Email, Role, CreatedAt FROM Users WHERE Email = 'admin@careerguidance.com'", conn))
                {
                    using var reader = verifyCmd.ExecuteReader();
                    
                    if (reader.Read())
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "Admin module setup completed!",
                            results = results,
                            credentials = new
                            {
                                email = "admin@careerguidance.com",
                                password = "Admin@123",
                                loginUrl = "http://localhost:5001/admin.html"
                            },
                            adminUser = new
                            {
                                id = reader.GetInt32(0),
                                username = reader.GetString(1),
                                email = reader.GetString(2),
                                role = reader.GetString(3),
                                createdAt = reader.GetDateTime(4)
                            }
                        });
                    }
                }

                return StatusCode(500, "Admin user created but verification failed");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET /api/setup/populate-careers
        [HttpGet("populate-careers")]
        public async Task<IActionResult> PopulateCareers()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Create careers table if not exists
                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS careers (
                        id INT PRIMARY KEY AUTO_INCREMENT,
                        name VARCHAR(200) NOT NULL,
                        description TEXT,
                        required_education VARCHAR(200),
                        average_salary VARCHAR(100),
                        growth_outlook VARCHAR(100),
                        key_skills JSON,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
                    )";

                using (var cmd = new MySqlCommand(createTableSql, conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }

                // Check if careers already exist
                string checkSql = "SELECT COUNT(*) FROM careers";
                using (var checkCmd = new MySqlCommand(checkSql, conn))
                {
                    var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                    if (count > 0)
                    {
                        return Ok(new { success = true, message = $"Careers table already has {count} careers", count });
                    }
                }

                // Insert sample careers
                string insertSql = @"
                    INSERT INTO careers (id, name, description, required_education, average_salary, growth_outlook, key_skills) VALUES
                    (1, 'Software Engineer', 'Design, develop, and maintain software applications', 'Bachelor in Computer Science', '$80,000 - $150,000', 'Strong growth', '[""Programming"", ""Problem Solving"", ""Algorithms"", ""Teamwork""]'),
                    (2, 'Data Scientist', 'Analyze complex data to help organizations make decisions', 'Bachelor/Master in Data Science or Statistics', '$90,000 - $160,000', 'Very strong growth', '[""Statistics"", ""Python/R"", ""Machine Learning"", ""Communication""]'),
                    (3, 'UX/UI Designer', 'Create user-friendly interfaces and experiences', 'Bachelor in Design or HCI', '$60,000 - $120,000', 'Strong growth', '[""Design Tools"", ""User Research"", ""Creativity"", ""Empathy""]'),
                    (4, 'Product Manager', 'Lead product strategy and development', 'Bachelor in Business or related field', '$100,000 - $180,000', 'Strong growth', '[""Leadership"", ""Communication"", ""Strategic Thinking"", ""Technical Knowledge""]'),
                    (5, 'DevOps Engineer', 'Automate and optimize software deployment', 'Bachelor in Computer Science', '$85,000 - $140,000', 'Very strong growth', '[""Linux"", ""CI/CD"", ""Cloud Platforms"", ""Scripting""]'),
                    (6, 'Cybersecurity Analyst', 'Protect systems and data from threats', 'Bachelor in Cybersecurity or IT', '$70,000 - $130,000', 'Very strong growth', '[""Security Protocols"", ""Risk Analysis"", ""Ethical Hacking"", ""Attention to Detail""]'),
                    (7, 'Marketing Manager', 'Plan and execute marketing strategies', 'Bachelor in Marketing or Business', '$60,000 - $120,000', 'Moderate growth', '[""Communication"", ""Creativity"", ""Analytics"", ""Social Media""]'),
                    (8, 'Financial Analyst', 'Analyze financial data and trends', 'Bachelor in Finance or Economics', '$60,000 - $110,000', 'Moderate growth', '[""Excel"", ""Financial Modeling"", ""Analysis"", ""Attention to Detail""]'),
                    (9, 'Mechanical Engineer', 'Design and develop mechanical systems', 'Bachelor in Mechanical Engineering', '$65,000 - $110,000', 'Moderate growth', '[""CAD"", ""Problem Solving"", ""Physics"", ""Teamwork""]'),
                    (10, 'Teacher/Educator', 'Educate and mentor students', 'Bachelor in Education', '$40,000 - $70,000', 'Stable', '[""Communication"", ""Patience"", ""Subject Expertise"", ""Empathy""]')";

                using (var insertCmd = new MySqlCommand(insertSql, conn))
                {
                    var rowsAffected = await insertCmd.ExecuteNonQueryAsync();

                    // Verify insertion
                    using (var verifyCmd = new MySqlCommand("SELECT id, name FROM careers ORDER BY id", conn))
                    {
                        var careers = new List<object>();
                        using (var reader = await verifyCmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                careers.Add(new
                                {
                                    id = reader.GetInt32(0),
                                    name = reader.GetString(1)
                                });
                            }
                        }

                        return Ok(new
                        {
                            success = true,
                            message = "Careers populated successfully!",
                            rowsInserted = rowsAffected,
                            careers
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error populating careers",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET /api/setup/create-UserProfiles-table
        [HttpGet("create-UserProfiles-table")]
        public IActionResult CreateUserProfilesTable()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string createTableSql = @"
                    CREATE TABLE IF NOT EXISTS UserProfiles (
                        Id INT PRIMARY KEY AUTO_INCREMENT,
                        UserId INT NOT NULL UNIQUE,
                        PhoneNumber VARCHAR(20),
                        Age INT,
                        Gender VARCHAR(20),
                        EducationLevel VARCHAR(100),
                        FieldOfStudy VARCHAR(200),
                        Skills JSON,
                        AreasOfInterest TEXT,
                        ProfileImagePath VARCHAR(500),
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                        INDEX idx_profile_userid (UserId)
                    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";

                using var cmd = new MySqlCommand(createTableSql, conn);
                cmd.ExecuteNonQuery();

                return Ok(new
                {
                    success = true,
                    message = "UserProfiles table created successfully!"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error creating UserProfiles table",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/fix-quiz-sessions-table
        [HttpGet("fix-quiz-sessions-table")]
        public IActionResult FixQuizSessionsTable()
        {
            try
            {
                using var conn = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                var results = new List<string>();

                // Add quiz_id column
                try
                {
                    using var addQuizIdCmd = new MySqlCommand(
                        "ALTER TABLE quiz_sessions ADD COLUMN quiz_id VARCHAR(36) UNIQUE AFTER id", conn);
                    addQuizIdCmd.ExecuteNonQuery();
                    results.Add("✅ Added quiz_id column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ quiz_id column already exists");
                    else
                        results.Add($"⚠️ quiz_id: {ex.Message}");
                }

                // Add total_questions column
                try
                {
                    using var addTotalCmd = new MySqlCommand(
                        "ALTER TABLE quiz_sessions ADD COLUMN total_questions INT DEFAULT 0 AFTER questions", conn);
                    addTotalCmd.ExecuteNonQuery();
                    results.Add("✅ Added total_questions column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ total_questions column already exists");
                    else
                        results.Add($"⚠️ total_questions: {ex.Message}");
                }

                // Add score column
                try
                {
                    using var addScoreCmd = new MySqlCommand(
                        "ALTER TABLE quiz_sessions ADD COLUMN score INT DEFAULT 0 AFTER answers", conn);
                    addScoreCmd.ExecuteNonQuery();
                    results.Add("✅ Added score column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ score column already exists");
                    else
                        results.Add($"⚠️ score: {ex.Message}");
                }

                // Add index on quiz_id
                try
                {
                    using var addIndexCmd = new MySqlCommand(
                        "CREATE INDEX idx_quiz_id ON quiz_sessions(quiz_id)", conn);
                    addIndexCmd.ExecuteNonQuery();
                    results.Add("✅ Added index on quiz_id");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate key name") || ex.Message.Contains("already exists"))
                        results.Add("⚠️ Index already exists");
                    else
                        results.Add($"⚠️ Index: {ex.Message}");
                }

                return Ok(new
                {
                    success = true,
                    message = "quiz_sessions table updated!",
                    results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating quiz_sessions table",
                    error = ex.Message
                });
            }
        }

        // GET /api/setup/add-resume-columns
        [HttpGet("add-resume-columns")]
        public IActionResult AddResumeColumns()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                var results = new List<string>();

                // First, create the user_resumes table if it doesn't exist
                try
                {
                    string createTableSql = @"
                        CREATE TABLE IF NOT EXISTS user_resumes (
                            id INT AUTO_INCREMENT PRIMARY KEY,
                            user_id INT NOT NULL,
                            full_name VARCHAR(255) NOT NULL DEFAULT '',
                            job_title VARCHAR(255) NOT NULL DEFAULT '',
                            email VARCHAR(255) NOT NULL DEFAULT '',
                            phone VARCHAR(50) NOT NULL DEFAULT '',
                            location VARCHAR(255) NOT NULL DEFAULT '',
                            linkedin VARCHAR(500) NOT NULL DEFAULT '',
                            professional_summary TEXT,
                            skills JSON,
                            experiences JSON,
                            education JSON,
                            certifications JSON DEFAULT NULL,
                            projects JSON DEFAULT NULL,
                            languages JSON DEFAULT NULL,
                            achievements JSON DEFAULT NULL,
                            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
                            UNIQUE KEY unique_user_resume (user_id),
                            FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
                            INDEX idx_user_id (user_id)
                        ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci";
                    using var createCmd = new MySqlCommand(createTableSql, conn);
                    createCmd.ExecuteNonQuery();
                    results.Add("✅ user_resumes table created/verified");
                }
                catch (MySqlException ex)
                {
                    results.Add($"⚠️ Table creation: {ex.Message}");
                }

                // Add certifications column
                try
                {
                    using var cmd = new MySqlCommand(
                        "ALTER TABLE user_resumes ADD COLUMN certifications JSON DEFAULT NULL", conn);
                    cmd.ExecuteNonQuery();
                    results.Add("✅ Added certifications column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ certifications column already exists");
                    else
                        results.Add($"⚠️ certifications: {ex.Message}");
                }

                // Add projects column
                try
                {
                    using var cmd = new MySqlCommand(
                        "ALTER TABLE user_resumes ADD COLUMN projects JSON DEFAULT NULL", conn);
                    cmd.ExecuteNonQuery();
                    results.Add("✅ Added projects column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ projects column already exists");
                    else
                        results.Add($"⚠️ projects: {ex.Message}");
                }

                // Add languages column
                try
                {
                    using var cmd = new MySqlCommand(
                        "ALTER TABLE user_resumes ADD COLUMN languages JSON DEFAULT NULL", conn);
                    cmd.ExecuteNonQuery();
                    results.Add("✅ Added languages column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ languages column already exists");
                    else
                        results.Add($"⚠️ languages: {ex.Message}");
                }

                // Add achievements column
                try
                {
                    using var cmd = new MySqlCommand(
                        "ALTER TABLE user_resumes ADD COLUMN achievements JSON DEFAULT NULL", conn);
                    cmd.ExecuteNonQuery();
                    results.Add("✅ Added achievements column");
                }
                catch (MySqlException ex)
                {
                    if (ex.Message.Contains("Duplicate column"))
                        results.Add("⚠️ achievements column already exists");
                    else
                        results.Add($"⚠️ achievements: {ex.Message}");
                }

                return Ok(new
                {
                    success = true,
                    message = "Resume table columns updated!",
                    results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating resume table",
                    error = ex.Message
                });
            }
        }
    }
}
