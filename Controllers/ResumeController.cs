using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Security.Claims;
using System.Text.Json;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResumeController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GroqService _groqService;

        public ResumeController(IConfiguration configuration, GroqService groqService)
        {
            _configuration = configuration;
            _groqService = groqService;
        }

        // POST /api/resume/save - Save or update resume data
        [HttpPost("save")]
        public IActionResult SaveResume([FromBody] ResumeDataRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Check if resume exists
                string checkQuery = "SELECT id FROM user_resumes WHERE user_id = @userId";
                int? resumeId = null;

                using (MySqlCommand checkCmd = new(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@userId", userId);
                    var result = checkCmd.ExecuteScalar();
                    if (result != null) resumeId = Convert.ToInt32(result);
                }

                if (resumeId.HasValue)
                {
                    // Update existing resume
                    string updateQuery = @"
                        UPDATE user_resumes SET
                            full_name = @fullName,
                            job_title = @jobTitle,
                            email = @email,
                            phone = @phone,
                            location = @location,
                            linkedin = @linkedin,
                            professional_summary = @summary,
                            skills = @skills,
                            experiences = @experiences,
                            education = @education,
                            certifications = @certifications,
                            projects = @projects,
                            languages = @languages,
                            achievements = @achievements,
                            updated_at = NOW()
                        WHERE id = @resumeId";

                    using MySqlCommand updateCmd = new(updateQuery, conn);
                    updateCmd.Parameters.AddWithValue("@resumeId", resumeId.Value);
                    updateCmd.Parameters.AddWithValue("@fullName", request.FullName ?? "");
                    updateCmd.Parameters.AddWithValue("@jobTitle", request.JobTitle ?? "");
                    updateCmd.Parameters.AddWithValue("@email", request.Email ?? "");
                    updateCmd.Parameters.AddWithValue("@phone", request.Phone ?? "");
                    updateCmd.Parameters.AddWithValue("@location", request.Location ?? "");
                    updateCmd.Parameters.AddWithValue("@linkedin", request.LinkedIn ?? "");
                    updateCmd.Parameters.AddWithValue("@summary", request.ProfessionalSummary ?? "");
                    updateCmd.Parameters.AddWithValue("@skills", JsonSerializer.Serialize(request.Skills ?? new List<string>()));
                    updateCmd.Parameters.AddWithValue("@experiences", JsonSerializer.Serialize(request.Experiences ?? new List<Experience>()));
                    updateCmd.Parameters.AddWithValue("@education", JsonSerializer.Serialize(request.Education ?? new List<Education>()));
                    updateCmd.Parameters.AddWithValue("@certifications", JsonSerializer.Serialize(request.Certifications ?? new List<Certification>()));
                    updateCmd.Parameters.AddWithValue("@projects", JsonSerializer.Serialize(request.Projects ?? new List<Project>()));
                    updateCmd.Parameters.AddWithValue("@languages", JsonSerializer.Serialize(request.Languages ?? new List<Language>()));
                    updateCmd.Parameters.AddWithValue("@achievements", JsonSerializer.Serialize(request.Achievements ?? new List<Achievement>()));
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    // Insert new resume
                    string insertQuery = @"
                        INSERT INTO user_resumes 
                        (user_id, full_name, job_title, email, phone, location, linkedin, 
                         professional_summary, skills, experiences, education, certifications, projects, languages, achievements)
                        VALUES (@userId, @fullName, @jobTitle, @email, @phone, @location, @linkedin,
                                @summary, @skills, @experiences, @education, @certifications, @projects, @languages, @achievements)";

                    using MySqlCommand insertCmd = new(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@fullName", request.FullName ?? "");
                    insertCmd.Parameters.AddWithValue("@jobTitle", request.JobTitle ?? "");
                    insertCmd.Parameters.AddWithValue("@email", request.Email ?? "");
                    insertCmd.Parameters.AddWithValue("@phone", request.Phone ?? "");
                    insertCmd.Parameters.AddWithValue("@location", request.Location ?? "");
                    insertCmd.Parameters.AddWithValue("@linkedin", request.LinkedIn ?? "");
                    insertCmd.Parameters.AddWithValue("@summary", request.ProfessionalSummary ?? "");
                    insertCmd.Parameters.AddWithValue("@skills", JsonSerializer.Serialize(request.Skills ?? new List<string>()));
                    insertCmd.Parameters.AddWithValue("@experiences", JsonSerializer.Serialize(request.Experiences ?? new List<Experience>()));
                    insertCmd.Parameters.AddWithValue("@education", JsonSerializer.Serialize(request.Education ?? new List<Education>()));
                    insertCmd.Parameters.AddWithValue("@certifications", JsonSerializer.Serialize(request.Certifications ?? new List<Certification>()));
                    insertCmd.Parameters.AddWithValue("@projects", JsonSerializer.Serialize(request.Projects ?? new List<Project>()));
                    insertCmd.Parameters.AddWithValue("@languages", JsonSerializer.Serialize(request.Languages ?? new List<Language>()));
                    insertCmd.Parameters.AddWithValue("@achievements", JsonSerializer.Serialize(request.Achievements ?? new List<Achievement>()));
                    insertCmd.ExecuteNonQuery();

                    resumeId = (int)insertCmd.LastInsertedId;
                }

                return Ok(new { message = "Resume saved successfully", resumeId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving resume: {ex.Message}");
                return StatusCode(500, new { error = "Failed to save resume", details = ex.Message });
            }
        }

        // GET /api/resume - Get user's resume
        [HttpGet]
        public IActionResult GetResume()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, full_name, job_title, email, phone, location, linkedin,
                           professional_summary, skills, experiences, education,
                           certifications, projects, languages, achievements,
                           created_at, updated_at
                    FROM user_resumes
                    WHERE user_id = @userId";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    var skillsJson = reader.GetString("skills");
                    var experiencesJson = reader.GetString("experiences");
                    var educationJson = reader.GetString("education");
                    var certificationsJson = reader.IsDBNull(reader.GetOrdinal("certifications")) ? "[]" : reader.GetString("certifications");
                    var projectsJson = reader.IsDBNull(reader.GetOrdinal("projects")) ? "[]" : reader.GetString("projects");
                    var languagesJson = reader.IsDBNull(reader.GetOrdinal("languages")) ? "[]" : reader.GetString("languages");
                    var achievementsJson = reader.IsDBNull(reader.GetOrdinal("achievements")) ? "[]" : reader.GetString("achievements");

                    var result = new
                    {
                        id = reader.GetInt32("id"),
                        fullName = reader.GetString("full_name"),
                        jobTitle = reader.GetString("job_title"),
                        email = reader.GetString("email"),
                        phone = reader.GetString("phone"),
                        location = reader.GetString("location"),
                        linkedin = reader.GetString("linkedin"),
                        professionalSummary = reader.GetString("professional_summary"),
                        skills = JsonSerializer.Deserialize<List<string>>(skillsJson),
                        experiences = JsonSerializer.Deserialize<List<Experience>>(experiencesJson),
                        education = JsonSerializer.Deserialize<List<Education>>(educationJson),
                        certifications = JsonSerializer.Deserialize<List<Certification>>(certificationsJson),
                        projects = JsonSerializer.Deserialize<List<Project>>(projectsJson),
                        languages = JsonSerializer.Deserialize<List<Language>>(languagesJson),
                        achievements = JsonSerializer.Deserialize<List<Achievement>>(achievementsJson),
                        createdAt = reader.GetDateTime("created_at"),
                        updatedAt = reader.GetDateTime("updated_at")
                    };

                    return Ok(result);
                }

                return NotFound(new { message = "Resume not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting resume: {ex.Message}");
                return StatusCode(500, new { error = "Failed to get resume", details = ex.Message });
            }
        }

        // DELETE /api/resume - Delete resume
        [HttpDelete]
        public IActionResult DeleteResume()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string deleteQuery = "DELETE FROM user_resumes WHERE user_id = @userId";
                using MySqlCommand cmd = new(deleteQuery, conn);
                cmd.Parameters.AddWithValue("@userId", userId);
                
                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Resume deleted successfully" });
                }

                return NotFound(new { message = "Resume not found" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting resume: {ex.Message}");
                return StatusCode(500, new { error = "Failed to delete resume", details = ex.Message });
            }
        }

        // POST /api/resume/enhance-summary - AI enhance professional summary
        [HttpPost("enhance-summary")]
        public async Task<IActionResult> EnhanceSummary([FromBody] EnhanceSummaryRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                Console.WriteLine($"📝 Enhancing summary for user {userId}");
                Console.WriteLine($"📝 Job Title: {request.JobTitle}");
                Console.WriteLine($"📝 Current Summary: {request.CurrentSummary?.Substring(0, Math.Min(50, request.CurrentSummary?.Length ?? 0))}...");

                // Extract experience descriptions for context
                var experienceDescriptions = request.Experiences?
                    .Select(e => $"{e.Role} at {e.Company}: {e.Description}")
                    .ToList() ?? new List<string>();

                // Call AI service to enhance summary with timeout handling
                string enhancedSummary;
                try
                {
                    enhancedSummary = await _groqService.EnhanceProfessionalSummary(
                        request.CurrentSummary ?? "",
                        request.JobTitle ?? "Professional",
                        request.Skills ?? new List<string>(),
                        experienceDescriptions
                    );
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine($"⚠️ Groq API timeout - AI took longer than 40 seconds");
                    return StatusCode(500, new { 
                        error = "AI service timeout", 
                        details = "AI service took too long. Please try again." 
                    });
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"⚠️ Groq API HTTP error: {ex.Message}");
                    return StatusCode(500, new { 
                        error = "AI service error", 
                        details = ex.Message 
                    });
                }

                Console.WriteLine($"✅ Enhanced summary generated: {enhancedSummary.Substring(0, Math.Min(100, enhancedSummary.Length))}...");

                return Ok(new { 
                    enhancedSummary = enhancedSummary.Trim(),
                    message = "Summary enhanced successfully" 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error enhancing summary: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { 
                    error = "Failed to enhance summary", 
                    details = ex.Message 
                });
            }
        }
    }

    public class EnhanceSummaryRequest
    {
        public string? CurrentSummary { get; set; }
        public string? JobTitle { get; set; }
        public List<string>? Skills { get; set; }
        public List<Experience>? Experiences { get; set; }
    }

    public class ResumeDataRequest
    {
        public string? FullName { get; set; }
        public string? JobTitle { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public string? LinkedIn { get; set; }
        public string? ProfessionalSummary { get; set; }
        public List<string>? Skills { get; set; }
        public List<Experience>? Experiences { get; set; }
        public List<Education>? Education { get; set; }
        public List<Certification>? Certifications { get; set; }
        public List<Project>? Projects { get; set; }
        public List<Language>? Languages { get; set; }
        public List<Achievement>? Achievements { get; set; }
    }

    public class Experience
    {
        public string Role { get; set; } = "";
        public string Company { get; set; } = "";
        public string Period { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class Education
    {
        public string Degree { get; set; } = "";
        public string Institution { get; set; } = "";
        public string Year { get; set; } = "";
    }

    public class Certification
    {
        public string Name { get; set; } = "";
        public string Issuer { get; set; } = "";
        public string Date { get; set; } = "";
        public string CredentialId { get; set; } = "";
    }

    public class Project
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Technologies { get; set; } = "";
        public string Link { get; set; } = "";
    }

    public class Language
    {
        public string Name { get; set; } = "";
        public string Proficiency { get; set; } = "";
    }

    public class Achievement
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Date { get; set; } = "";
    }
}
