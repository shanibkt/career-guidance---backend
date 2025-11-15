using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using MyFirstApi.Models;
using System.Text.Json;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require JWT authentication
    public class ProfileController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ProfileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET /api/profile/{userId} - Get user basic info
        [HttpGet("{userId}")]
        public IActionResult GetUser(int userId)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new("my_database.sp_get_user_by_id", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_userId", userId);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return NotFound("User not found.");

                var user = new UserResponse
                {
                    Id = reader.GetInt32("Id"),
                    Username = reader.GetString("Username"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // PUT /api/profile/{userId} - Update user basic info
        [HttpPut("{userId}")]
        public IActionResult UpdateUser(int userId, UpdateUserDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Email))
                    return BadRequest("Username, FullName, and Email are required.");

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new("my_database.sp_update_user", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_userId", userId);
                cmd.Parameters.AddWithValue("p_username", dto.Username);
                cmd.Parameters.AddWithValue("p_fullName", dto.FullName);
                cmd.Parameters.AddWithValue("p_email", dto.Email);

                object result = cmd.ExecuteScalar();
                int affectedRows = Convert.ToInt32(result ?? 0);

                if (affectedRows == 0)
                    return NotFound("User not found.");

                return Ok(new { message = "User updated successfully." });
            }
            catch (MySqlException mex) when (mex.Number == 1062)
            {
                return Conflict("Username or email already exists.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // DELETE /api/profile/{userId} - Delete user account
        [HttpDelete("{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new("my_database.sp_delete_user", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_userId", userId);

                object result = cmd.ExecuteScalar();
                int affectedRows = Convert.ToInt32(result ?? 0);

                if (affectedRows == 0)
                    return NotFound("User not found.");

                return Ok(new { message = "User account deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public UserProfileController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // GET /api/userprofile/{userId} - Get user profile data
        [HttpGet("{userId}")]
        public IActionResult GetProfile(int userId)
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                using MySqlCommand cmd = new("my_database.sp_get_profile_by_userid", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_userId", userId);

                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read()) return NotFound("Profile not found.");

                var profile = new UserProfile
                {
                    Id = reader.GetInt32("Id"),
                    UserId = reader.GetInt32("UserId"),
                    PhoneNumber = reader.IsDBNull(reader.GetOrdinal("PhoneNumber")) ? null : reader.GetString("PhoneNumber"),
                    Age = reader.IsDBNull(reader.GetOrdinal("Age")) ? null : reader.GetInt32("Age"),
                    Gender = reader.IsDBNull(reader.GetOrdinal("Gender")) ? null : reader.GetString("Gender"),
                    EducationLevel = reader.IsDBNull(reader.GetOrdinal("EducationLevel")) ? null : reader.GetString("EducationLevel"),
                    FieldOfStudy = reader.IsDBNull(reader.GetOrdinal("FieldOfStudy")) ? null : reader.GetString("FieldOfStudy"),
                    Skills = reader.IsDBNull(reader.GetOrdinal("Skills")) ? null : JsonSerializer.Deserialize<List<string>>(reader.GetString("Skills")),
                    AreasOfInterest = reader.IsDBNull(reader.GetOrdinal("AreasOfInterest")) ? null : reader.GetString("AreasOfInterest"),
                    ProfileImagePath = reader.IsDBNull(reader.GetOrdinal("ProfileImagePath")) ? null : reader.GetString("ProfileImagePath"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UpdatedAt = reader.GetDateTime("UpdatedAt")
                };

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // POST /api/userprofile - Create or update profile
        [HttpPost]
        public IActionResult CreateOrUpdateProfile(UpdateProfileDto dto)
        {
            try
            {
                // Validate userId
                if (dto.UserId <= 0)
                    return BadRequest(new { message = "UserId is required" });

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Convert skills array to JSON string, use empty array if null
                string? skillsJson = dto.Skills != null && dto.Skills.Count > 0 
                    ? JsonSerializer.Serialize(dto.Skills) 
                    : null;

                // Log what we're about to save
                Console.WriteLine($"Saving profile for userId: {dto.UserId}");
                Console.WriteLine($"Phone: {dto.PhoneNumber}, Age: {dto.Age}, Gender: {dto.Gender}");
                Console.WriteLine($"Education: {dto.EducationLevel}, Field: {dto.FieldOfStudy}");

                using MySqlCommand cmd = new("my_database.sp_create_or_update_profile", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("p_userId", dto.UserId);
                cmd.Parameters.AddWithValue("p_phoneNumber", string.IsNullOrWhiteSpace(dto.PhoneNumber) ? DBNull.Value : dto.PhoneNumber);
                cmd.Parameters.AddWithValue("p_age", dto.Age.HasValue ? dto.Age.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("p_gender", string.IsNullOrWhiteSpace(dto.Gender) ? DBNull.Value : dto.Gender);
                cmd.Parameters.AddWithValue("p_educationLevel", string.IsNullOrWhiteSpace(dto.EducationLevel) ? DBNull.Value : dto.EducationLevel);
                cmd.Parameters.AddWithValue("p_fieldOfStudy", string.IsNullOrWhiteSpace(dto.FieldOfStudy) ? DBNull.Value : dto.FieldOfStudy);
                cmd.Parameters.AddWithValue("p_skills", string.IsNullOrWhiteSpace(skillsJson) ? DBNull.Value : skillsJson);
                cmd.Parameters.AddWithValue("p_areasOfInterest", string.IsNullOrWhiteSpace(dto.AreasOfInterest) ? DBNull.Value : dto.AreasOfInterest);
                cmd.Parameters.AddWithValue("p_profileImagePath", DBNull.Value);

                Console.WriteLine("Executing stored procedure...");
                
                using MySqlDataReader reader = cmd.ExecuteReader();
                if (!reader.Read())
                {
                    Console.WriteLine("ERROR: Stored procedure didn't return any rows");
                    return StatusCode(500, new { message = "Failed to create/update profile" });
                }

                int profileId = reader.GetInt32("Id");
                int userId = reader.GetInt32("UserId");
                
                Console.WriteLine($"SUCCESS: Profile saved - ID: {profileId}, UserId: {userId}");

                return Ok(new 
                { 
                    message = "Profile updated successfully.",
                    profileId = profileId,
                    userId = userId
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR saving profile: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error updating profile", error = ex.Message });
            }
        }

        // POST /api/userprofile/upload-image - Upload profile picture
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file, [FromQuery] int userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

                // Validate file type
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Invalid file type. Only jpg, jpeg, png, gif allowed.");

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                Directory.CreateDirectory(uploadsDir);

                // Generate unique filename
                var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                Console.WriteLine($"✅ File saved to: {filePath}");
                Console.WriteLine($"✅ File exists: {System.IO.File.Exists(filePath)}");
                Console.WriteLine($"✅ File size: {new FileInfo(filePath).Length} bytes");

                // Update profile with image path
                var relativePath = $"/uploads/profiles/{fileName}";
                Console.WriteLine($"✅ Relative path for DB: {relativePath}");
                
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Update only the profile image path
                string updateQuery = @"
                    UPDATE UserProfiles 
                    SET ProfileImagePath = @path, UpdatedAt = CURRENT_TIMESTAMP 
                    WHERE UserId = @userId";
                
                using MySqlCommand cmd = new(updateQuery, conn);
                cmd.Parameters.AddWithValue("@path", relativePath);
                cmd.Parameters.AddWithValue("@userId", userId);
                
                int affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    return NotFound("User profile not found. Create profile first.");

                return Ok(new 
                { 
                    message = "Profile image uploaded successfully.",
                    imagePath = relativePath
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }
    }
}
