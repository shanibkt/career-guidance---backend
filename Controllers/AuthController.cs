using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MySqlConnector;
using System.Data;
using MyFirstApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;

        public AuthController(IConfiguration configuration, IEmailService emailService)
        {
            _configuration = configuration;
            _emailService = emailService;
        }

        // Register: creates a new user. Password is hashed with BCrypt.
        [HttpPost("register")]
        [HttpPost("signup")]  // Alias for Flutter compatibility
        public IActionResult Register(RegisterRequest req)
        {
            try
            {
                // basic validation
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.FullName))
                    return BadRequest(new { message = "Missing required fields" });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if email already exists
                using (MySqlCommand checkEmailCmd = new("SELECT Id FROM Users WHERE Email = @email LIMIT 1", conn))
                {
                    checkEmailCmd.Parameters.AddWithValue("@email", req.Email);
                    using var reader = checkEmailCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return Conflict(new { message = "This email is already registered. Please login or use a different email." });
                    }
                }

                // Check if username already exists
                using (MySqlCommand checkUsernameCmd = new("SELECT Id FROM Users WHERE Username = @username LIMIT 1", conn))
                {
                    checkUsernameCmd.Parameters.AddWithValue("@username", req.Username);
                    using var reader = checkUsernameCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return Conflict(new { message = "This username is already taken. Please choose a different username." });
                    }
                }

                string hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);

                using MySqlCommand cmd = new("INSERT INTO Users (Username, FullName, Email, PasswordHash) VALUES (@username, @fullName, @email, @passwordHash)", conn);
                cmd.Parameters.AddWithValue("@username", req.Username);
                cmd.Parameters.AddWithValue("@fullName", req.FullName);
                cmd.Parameters.AddWithValue("@email", req.Email);
                cmd.Parameters.AddWithValue("@passwordHash", hashed);

                cmd.ExecuteNonQuery();
                int newId = (int)cmd.LastInsertedId;

                // Create profile if additional data provided
                if (!string.IsNullOrWhiteSpace(req.Phone) || req.Age.HasValue || !string.IsNullOrWhiteSpace(req.Gender))
                {
                    using MySqlCommand profileCmd = new("freedb_career_guidence.sp_create_or_update_profile", conn);
                    profileCmd.CommandType = CommandType.StoredProcedure;
                    profileCmd.Parameters.AddWithValue("p_userId", newId);
                    profileCmd.Parameters.AddWithValue("p_phoneNumber", string.IsNullOrWhiteSpace(req.Phone) ? DBNull.Value : req.Phone);
                    profileCmd.Parameters.AddWithValue("p_age", req.Age.HasValue ? req.Age.Value : DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_gender", string.IsNullOrWhiteSpace(req.Gender) ? DBNull.Value : req.Gender);
                    profileCmd.Parameters.AddWithValue("p_educationLevel", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_fieldOfStudy", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_skills", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_careerPath", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_profileImagePath", DBNull.Value);
                    profileCmd.ExecuteNonQuery();
                }

                var user = new UserResponse 
                { 
                    Id = newId, 
                    Username = req.Username, 
                    FullName = req.FullName, 
                    Email = req.Email, 
                    CreatedAt = DateTime.UtcNow 
                };

                // Generate JWT token and refresh token
                var token = GenerateJwtToken(newId, req.Username, req.Email);
                var refreshToken = GenerateRefreshToken();
                var tokenExpiration = DateTime.UtcNow.AddMinutes(_configuration.GetSection("Jwt").GetValue<int?>("ExpireMinutes") ?? 1440);

                // Save refresh token to database
                SaveRefreshToken(newId, refreshToken, tokenExpiration.AddDays(7), conn);

                // Return response matching Flutter expectations
                return Ok(new
                {
                    token = token,
                    refreshToken = refreshToken,
                    tokenExpiration = tokenExpiration,
                    user = new
                    {
                        id = newId,
                        fullName = req.FullName,
                        username = req.Username,
                        email = req.Email
                    }
                });
            }
            catch (MySqlException mex) when (mex.Number == 1062)
            {
                // Duplicate key error - try to determine which field
                string errorMsg = mex.Message.ToLower();
                if (errorMsg.Contains("email"))
                {
                    return Conflict(new { message = "This email is already registered. Please login or use a different email." });
                }
                else if (errorMsg.Contains("username"))
                {
                    return Conflict(new { message = "This username is already taken. Please choose a different username." });
                }
                else
                {
                    return Conflict(new { message = "A user with that email or username already exists." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Login: verifies email and password
        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest(new { message = "Email and password are required." });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Variables to store user data
                int id;
                string username;
                string fullName;
                string email;
                string hash;
                string role = "user";
                DateTime createdAt = DateTime.UtcNow;

                // Read user data and CLOSE the reader before doing anything else
                using (MySqlCommand cmd = new("SELECT Id, Username, FullName, Email, PasswordHash, Role, CreatedAt, UpdatedAt FROM Users WHERE Email = @email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@email", req.Email);

                    using MySqlDataReader reader = cmd.ExecuteReader();
                    if (!reader.Read()) return Unauthorized(new { message = "Invalid credentials." });

                    // Read columns by name to avoid position mismatches
                    id = reader.GetInt32(reader.GetOrdinal("Id"));
                    username = reader.GetString(reader.GetOrdinal("Username"));
                    fullName = reader.GetString(reader.GetOrdinal("FullName"));
                    email = reader.GetString(reader.GetOrdinal("Email"));
                    hash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                    
                    // Get role if column exists
                    try
                    {
                        int roleOrdinal = reader.GetOrdinal("Role");
                        if (!reader.IsDBNull(roleOrdinal))
                            role = reader.GetString(roleOrdinal);
                    }
                    catch { /* Role column might not exist in older databases */ }
                    
                    // Handle CreatedAt with MySQL timestamp compatibility
                    try
                    {
                        int createdAtOrdinal = reader.GetOrdinal("CreatedAt");
                        if (!reader.IsDBNull(createdAtOrdinal))
                        {
                            var value = reader.GetValue(createdAtOrdinal);
                            if (value is DateTime dt)
                                createdAt = dt;
                            else if (value is string str && DateTime.TryParse(str, out DateTime parsed))
                                createdAt = parsed;
                        }
                    }
                    catch { /* CreatedAt might not exist or have different type */ }
                } // DataReader is closed here

                // Debug logging to help diagnose password issues
                Console.WriteLine($"🔐 Login attempt for: {email}");
                Console.WriteLine($"   Password length: {req.Password?.Length ?? 0}");
                Console.WriteLine($"   Hash starts with: {(hash.Length > 10 ? hash.Substring(0, 10) : hash)}...");
                Console.WriteLine($"   Hash is BCrypt format: {hash.StartsWith("$2")}");

                // Now verify password with error handling
                bool ok = false;
                try
                {
                    ok = BCrypt.Net.BCrypt.Verify(req.Password, hash);
                    Console.WriteLine($"   BCrypt.Verify result: {ok}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ BCrypt.Verify exception: {ex.Message}");
                    return StatusCode(500, new { message = $"Password verification error: {ex.Message}" });
                }
                
                if (!ok)
                {
                    Console.WriteLine($"   ❌ Password does not match hash");
                    return Unauthorized(new { message = "Invalid credentials." });
                }

                var resp = new UserResponse 
                { 
                    Id = id, 
                    Username = username, 
                    FullName = fullName, 
                    Email = email, 
                    CreatedAt = createdAt 
                };

                // Generate JWT token and refresh token
                var tokenString = GenerateJwtToken(id, username, email, role);
                var refreshToken = GenerateRefreshToken();
                var tokenExpiration = DateTime.UtcNow.AddMinutes(_configuration.GetSection("Jwt").GetValue<int?>("ExpireMinutes") ?? 1440);

                // Save refresh token to database (DataReader is already closed, this is safe now)
                SaveRefreshToken(id, refreshToken, tokenExpiration.AddDays(7), conn);

                var loginResp = new LoginResponse 
                { 
                    Token = tokenString, 
                    RefreshToken = refreshToken,
                    TokenExpiration = tokenExpiration,
                    User = resp 
                };
                return Ok(loginResp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET /api/auth/user-by-username/{username}
        // Utility endpoint used by admin dashboard username fallback.
        [HttpGet("user-by-username/{username}")]
        public IActionResult GetUserByUsername(string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username))
                    return BadRequest(new { message = "Username is required." });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new(
                    "SELECT Id, Username, FullName, Email FROM Users WHERE Username = @username LIMIT 1",
                    conn
                );
                cmd.Parameters.AddWithValue("@username", username);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return NotFound(new { message = "User not found." });

                return Ok(new
                {
                    id = reader.GetInt32("Id"),
                    username = reader.GetString("Username"),
                    fullName = reader.GetString("FullName"),
                    email = reader.GetString("Email")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(int userId, string username, string email, string role = "user")
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var jwtKey = jwtSection.GetValue<string>("Key");
            var jwtIssuer = jwtSection.GetValue<string>("Issuer");
            var jwtAudience = jwtSection.GetValue<string>("Audience");
            var expireMinutes = jwtSection.GetValue<int?>("ExpireMinutes") ?? 60;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()), // Changed from JwtRegisteredClaimNames.Sub
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role), // Add role claim for authorization
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Email, email)
            };

            var keyBytes = Encoding.UTF8.GetBytes(jwtKey ?? string.Empty);
            var signingKey = new SymmetricSecurityKey(keyBytes);
            var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // Generate a secure random refresh token
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        // Save refresh token to database
        private void SaveRefreshToken(int userId, string refreshToken, DateTime expiresAt, MySqlConnection conn)
        {
            // First, revoke all old refresh tokens for this user
            string revokeQuery = "UPDATE refresh_tokens SET revoked = TRUE WHERE user_id = @userId AND revoked = FALSE";
            using (MySqlCommand revokeCmd = new(revokeQuery, conn))
            {
                revokeCmd.Parameters.AddWithValue("@userId", userId);
                revokeCmd.ExecuteNonQuery();
            }

            // Insert new refresh token
            string insertQuery = @"INSERT INTO refresh_tokens (user_id, token, expires_at) 
                                  VALUES (@userId, @token, @expiresAt)";
            using MySqlCommand cmd = new(insertQuery, conn);
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@token", refreshToken);
            cmd.Parameters.AddWithValue("@expiresAt", expiresAt);
            cmd.ExecuteNonQuery();
        }

        // POST /api/auth/refresh - Refresh expired token
        [HttpPost("refresh")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.RefreshToken))
                    return BadRequest(new { message = "Refresh token is required" });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Validate refresh token
                string query = @"SELECT rt.user_id, u.Username, u.Email, rt.expires_at, rt.revoked
                                FROM refresh_tokens rt
                                JOIN users u ON rt.user_id = u.id
                                WHERE rt.token = @token";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@token", request.RefreshToken);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read())
                    return Unauthorized(new { message = "Invalid refresh token" });

                int userId = reader.GetInt32("user_id");
                string username = reader.GetString("Username");
                string email = reader.GetString("Email");
                DateTime expiresAt = reader.GetDateTime("expires_at");
                bool revoked = reader.GetBoolean("revoked");

                reader.Close();

                // Check if token is revoked or expired
                if (revoked)
                    return Unauthorized(new { message = "Refresh token has been revoked" });

                if (expiresAt < DateTime.UtcNow)
                    return Unauthorized(new { message = "Refresh token expired" });

                // Generate new access token and refresh token
                var newAccessToken = GenerateJwtToken(userId, username, email);
                var newRefreshToken = GenerateRefreshToken();
                var tokenExpiration = DateTime.UtcNow.AddMinutes(_configuration.GetSection("Jwt").GetValue<int?>("ExpireMinutes") ?? 1440);

                // Save new refresh token
                SaveRefreshToken(userId, newRefreshToken, tokenExpiration.AddDays(7), conn);

                return Ok(new
                {
                    token = newAccessToken,
                    refreshToken = newRefreshToken,
                    tokenExpiration = tokenExpiration
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to refresh token", error = ex.Message });
            }
        }

        // POST /api/auth/logout - revoke refresh tokens for current user
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                    return Unauthorized(new { message = "Invalid user token." });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new(
                    "UPDATE refresh_tokens SET revoked = TRUE WHERE user_id = @userId AND revoked = FALSE",
                    conn
                );
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();

                return Ok(new { message = "Logged out successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        // GET /api/auth/verify - Test if token is valid
        [HttpGet("verify")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult VerifyToken()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            return Ok(new
            {
                message = "Token is valid",
                userId = userId,
                username = username,
                email = email,
                serverTime = DateTime.UtcNow,
                tokenExpiration = User.FindFirst("exp")?.Value
            });
        }

        // POST /api/auth/forgot-password
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email))
                    return BadRequest(new { message = "Email is required." });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                // Check if user exists
                int userId = 0;
                string username = "";
                using (var cmd = new MySqlCommand("SELECT Id, Username FROM Users WHERE Email = @email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@email", req.Email);
                    using var reader = await cmd.ExecuteReaderAsync();
                    if (await reader.ReadAsync())
                    {
                        userId = reader.GetInt32(0);
                        username = reader.GetString(1);
                    }
                }

                if (userId != 0)
                {
                    // Generate reset token (short lived, e.g., 15 mins)
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[] 
                        { 
                            new Claim("id", userId.ToString()),
                            new Claim("email", req.Email),
                            new Claim("type", "reset_password") 
                        }),
                        Expires = DateTime.UtcNow.AddMinutes(15),
                        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
                    };
                    var token = tokenHandler.WriteToken(tokenHandler.CreateToken(tokenDescriptor));

                    // Send email
                    var resetLink = $"https://your-frontend-app.com/reset-password?token={token}"; 
                    // Note: Update with actual frontend URL or deep link scheme
                    
                    var message = $@"
                        <h3>Password Reset Request</h3>
                        <p>Hi {username},</p>
                        <p>You requested a password reset. Please use the token below to reset your password within the app:</p>
                        <p><b>{token}</b></p>
                        <p>This token is valid for 15 minutes.</p>
                        <p>If you didn't request this, purely ignore this email.</p>
                    ";

                    await _emailService.SendEmailAsync(req.Email, "Reset Your Password", message);
                }

                // Security-friendly: do not reveal whether email exists (always say we sent it if it exists)
                return Ok(new
                {
                    message = "If the email exists, reset instructions have been sent."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error processing request", error = ex.Message });
            }
        }

        // POST /api/auth/reset-password - Reset password for a user
        [HttpPost("reset-password")]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.NewPassword))
                    return BadRequest(new { message = "New password is required." });

                // Accept both styles:
                // 1) { email, newPassword } (current backend contract)
                // 2) { token, newPassword } (frontend compatibility)
                var email = req.Email;
                if (string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(req.Token))
                {
                    if (req.Token.Contains("@"))
                    {
                        email = req.Token;
                    }
                    else
                    {
                        try
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwt = handler.ReadJwtToken(req.Token);
                            email = jwt.Claims.FirstOrDefault(c =>
                                c.Type == ClaimTypes.Email ||
                                c.Type == JwtRegisteredClaimNames.Email ||
                                c.Type == "email")?.Value;
                        }
                        catch
                        {
                            // Invalid/unsupported token format.
                        }
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                    return BadRequest(new { message = "Email (or valid token containing email) is required." });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if user exists
                string? existingHash = null;
                using (var checkCmd = new MySqlCommand("SELECT PasswordHash FROM Users WHERE Email = @email", conn))
                {
                    checkCmd.Parameters.AddWithValue("@email", req.Email);
                    existingHash = checkCmd.ExecuteScalar() as string;
                }

                if (existingHash == null)
                    return NotFound(new { message = "User not found." });

                // Hash new password with BCrypt
                string newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

                // Update password
                using (var updateCmd = new MySqlCommand("UPDATE Users SET PasswordHash = @hash WHERE Email = @email", conn))
                {
                    updateCmd.Parameters.AddWithValue("@hash", newHash);
                    updateCmd.Parameters.AddWithValue("@email", email);
                    updateCmd.ExecuteNonQuery();
                }

                Console.WriteLine($"✅ Password reset for: {req.Email}");
                Console.WriteLine($"   Old hash started with: {(existingHash.Length > 10 ? existingHash.Substring(0, 10) : existingHash)}");
                Console.WriteLine($"   New hash starts with: {newHash.Substring(0, 10)}");

                return Ok(new { message = "Password reset successfully. You can now login with the new password." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        public class ResetPasswordRequest
        {
            public string Email { get; set; } = "";
            public string NewPassword { get; set; } = "";
            public string Token { get; set; } = "";
        }

        public class ForgotPasswordRequest
        {
            public string Email { get; set; } = "";
        }
    }
}
