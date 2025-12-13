using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using MyFirstApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public AuthController(IConfiguration configuration)
        {
            _configuration = configuration;
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

                // Check if user already exists
                using (MySqlCommand checkCmd = new("SELECT Id FROM users WHERE Email = @email LIMIT 1", conn))
                {
                    checkCmd.Parameters.AddWithValue("@email", req.Email);
                    using var reader = checkCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return BadRequest(new { message = "User already exists" });
                    }
                }

                string hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);

                using MySqlCommand cmd = new("INSERT INTO users (Username, FullName, Email, PasswordHash) VALUES (@username, @fullName, @email, @passwordHash); SELECT LAST_INSERT_ID();", conn);
                cmd.Parameters.AddWithValue("@username", req.Username);
                cmd.Parameters.AddWithValue("@fullName", req.FullName);
                cmd.Parameters.AddWithValue("@email", req.Email);
                cmd.Parameters.AddWithValue("@passwordHash", hashed);

                object idObj = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(idObj ?? 0);

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
                return Conflict("A user with that email or username already exists.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Login: verifies email and password
        [HttpPost("login")]
        public IActionResult Login(LoginRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                    return BadRequest("Email and password are required.");

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
                using (MySqlCommand cmd = new("SELECT Id, Username, FullName, Email, PasswordHash, CreatedAt, UpdatedAt FROM users WHERE Email = @email LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@email", req.Email);

                    using MySqlDataReader reader = cmd.ExecuteReader();
                    if (!reader.Read()) return Unauthorized("Invalid credentials.");

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

                // Now verify password
                bool ok = BCrypt.Net.BCrypt.Verify(req.Password, hash);
                if (!ok) return Unauthorized("Invalid credentials.");

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
                return StatusCode(500, ex.Message);
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
    }
}
