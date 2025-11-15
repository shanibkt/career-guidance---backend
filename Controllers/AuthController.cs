using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using MyFirstApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
                using (MySqlCommand checkCmd = new("my_database.sp_get_user_by_email", conn))
                {
                    checkCmd.CommandType = CommandType.StoredProcedure;
                    checkCmd.Parameters.AddWithValue("p_email", req.Email);
                    using var reader = checkCmd.ExecuteReader();
                    if (reader.Read())
                    {
                        return BadRequest(new { message = "User already exists" });
                    }
                }

                string hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);

                using MySqlCommand cmd = new("my_database.sp_create_user", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_username", req.Username);
                cmd.Parameters.AddWithValue("p_fullName", req.FullName);
                cmd.Parameters.AddWithValue("p_email", req.Email);
                cmd.Parameters.AddWithValue("p_passwordHash", hashed);

                object idObj = cmd.ExecuteScalar();
                int newId = Convert.ToInt32(idObj ?? 0);

                // Create profile if additional data provided
                if (!string.IsNullOrWhiteSpace(req.Phone) || req.Age.HasValue || !string.IsNullOrWhiteSpace(req.Gender))
                {
                    using MySqlCommand profileCmd = new("my_database.sp_create_or_update_profile", conn);
                    profileCmd.CommandType = CommandType.StoredProcedure;
                    profileCmd.Parameters.AddWithValue("p_userId", newId);
                    profileCmd.Parameters.AddWithValue("p_phoneNumber", string.IsNullOrWhiteSpace(req.Phone) ? DBNull.Value : req.Phone);
                    profileCmd.Parameters.AddWithValue("p_age", req.Age.HasValue ? req.Age.Value : DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_gender", string.IsNullOrWhiteSpace(req.Gender) ? DBNull.Value : req.Gender);
                    profileCmd.Parameters.AddWithValue("p_educationLevel", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_fieldOfStudy", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_skills", DBNull.Value);
                    profileCmd.Parameters.AddWithValue("p_areasOfInterest", DBNull.Value);
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

                // Generate JWT token
                var token = GenerateJwtToken(newId, req.Username, req.Email);

                // Return response matching Flutter expectations
                return Ok(new
                {
                    token = token,
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

                using MySqlCommand cmd = new("my_database.sp_get_user_by_email", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_email", req.Email);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return Unauthorized("Invalid credentials.");

                // Read columns by name to avoid position mismatches
                int id = reader.GetInt32(reader.GetOrdinal("Id"));
                string username = reader.GetString(reader.GetOrdinal("Username"));
                string fullName = reader.GetString(reader.GetOrdinal("FullName"));
                string email = reader.GetString(reader.GetOrdinal("Email"));
                string hash = reader.GetString(reader.GetOrdinal("PasswordHash"));
                
                DateTime createdAt = DateTime.UtcNow;
                int createdAtOrdinal = reader.GetOrdinal("CreatedAt");
                if (!reader.IsDBNull(createdAtOrdinal))
                    createdAt = reader.GetDateTime(createdAtOrdinal);

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

                // Generate JWT token
                var tokenString = GenerateJwtToken(id, username, email);

                var loginResp = new LoginResponse { Token = tokenString, User = resp };
                return Ok(loginResp);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Helper method to generate JWT token
        private string GenerateJwtToken(int userId, string username, string email)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var jwtKey = jwtSection.GetValue<string>("Key");
            var jwtIssuer = jwtSection.GetValue<string>("Issuer");
            var jwtAudience = jwtSection.GetValue<string>("Audience");
            var expireMinutes = jwtSection.GetValue<int?>("ExpireMinutes") ?? 60;

            var claims = new List<Claim>
            {
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
    }
}
