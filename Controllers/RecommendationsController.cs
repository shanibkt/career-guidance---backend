using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Security.Claims;
using System.Text.Json;
using MyFirstApi.Models;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RecommendationsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GroqService _groqService;

        public RecommendationsController(IConfiguration configuration, GroqService groqService)
        {
            _configuration = configuration;
            _groqService = groqService;
        }

        // POST /api/recommendations/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateRecommendations([FromBody] GenerateRecommendationsRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Get quiz session
                string sessionQuery = "SELECT questions, answers, completed FROM quiz_sessions WHERE id = @sessionId AND user_id = @userId";
                using MySqlCommand sessionCmd = new(sessionQuery, conn);
                sessionCmd.Parameters.AddWithValue("@sessionId", request.SessionId);
                sessionCmd.Parameters.AddWithValue("@userId", userId);

                using var sessionReader = sessionCmd.ExecuteReader();
                if (!sessionReader.Read()) return NotFound("Quiz session not found");
                if (!sessionReader.GetBoolean("completed")) return BadRequest("Quiz not completed");

                var questions = sessionReader.GetString("questions");
                var answers = sessionReader.GetString("answers");
                sessionReader.Close();

                // Get user profile
                string profileQuery = @"
                    SELECT u.FullName, p.EducationLevel, p.FieldOfStudy, p.Skills, p.career_path
                    FROM users u
                    LEFT JOIN UserProfiles p ON u.Id = p.UserId
                    WHERE u.Id = @userId";

                using MySqlCommand profileCmd = new(profileQuery, conn);
                profileCmd.Parameters.AddWithValue("@userId", userId);

                using var profileReader = profileCmd.ExecuteReader();
                if (!profileReader.Read()) return NotFound("User profile not found");

                var userProfile = new
                {
                    fullName = profileReader.GetString("FullName"),
                    education = profileReader.IsDBNull(profileReader.GetOrdinal("EducationLevel")) ? "Not specified" : profileReader.GetString("EducationLevel"),
                    fieldOfStudy = profileReader.IsDBNull(profileReader.GetOrdinal("FieldOfStudy")) ? "Not specified" : profileReader.GetString("FieldOfStudy"),
                    skills = profileReader.IsDBNull(profileReader.GetOrdinal("Skills")) ? "[]" : profileReader.GetString("Skills"),
                    careerPath = profileReader.IsDBNull(profileReader.GetOrdinal("career_path")) ? "Not specified" : profileReader.GetString("career_path")
                };
                profileReader.Close();

                // Determine the correct column name for career name
                string nameColumn = "name";
                try
                {
                    using var checkColCmd = new MySqlCommand(
                        "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'careers' AND COLUMN_NAME IN ('name', 'career_name')", conn);
                    using var colReader = checkColCmd.ExecuteReader();
                    if (colReader.Read()) nameColumn = colReader.GetString(0);
                    colReader.Close();
                }
                catch { }

                // Get all careers
                string careersQuery = $"SELECT id, {nameColumn}, description, required_education, key_skills FROM careers";
                using MySqlCommand careersCmd = new(careersQuery, conn);
                using var careersReader = careersCmd.ExecuteReader();

                var careers = new List<object>();
                while (careersReader.Read())
                {
                    careers.Add(new
                    {
                        id = careersReader.GetInt32("id"),
                        career_name = careersReader.GetString(nameColumn),
                        description = careersReader.IsDBNull(careersReader.GetOrdinal("description")) ? "" : careersReader.GetString("description"),
                        required_education = careersReader.IsDBNull(careersReader.GetOrdinal("required_education")) ? "" : careersReader.GetString("required_education"),
                        key_skills = careersReader.IsDBNull(careersReader.GetOrdinal("key_skills")) ? "[]" : careersReader.GetString("key_skills")
                    });
                }
                careersReader.Close();

                // Call AI to generate recommendations
                Console.WriteLine("Calling Groq API to generate recommendations...");
                var aiResponse = await _groqService.GenerateCareerRecommendations(
                    JsonSerializer.Serialize(userProfile),
                    answers,
                    JsonSerializer.Serialize(careers)
                );

                // Parse AI response
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                var recommendationsObj = JsonSerializer.Deserialize<JsonElement>(cleanJson);
                var recommendationsArray = recommendationsObj.GetProperty("recommendations");

                var recommendations = new List<Recommendation>();

                // Save recommendations to database
                foreach (var rec in recommendationsArray.EnumerateArray())
                {
                    // Handle both career_id and careerId for flexibility
                    var careerId = rec.TryGetProperty("career_id", out var cId) ? cId.GetInt32() : 
                                   rec.TryGetProperty("careerId", out var cId2) ? cId2.GetInt32() : 0;
                    
                    var matchPercentage = rec.TryGetProperty("match_percentage", out var mp) ? mp.GetDecimal() :
                                         rec.TryGetProperty("matchPercentage", out var mp2) ? mp2.GetDecimal() : 0;
                    
                    var reasoning = rec.TryGetProperty("reasoning", out var r) ? r.GetString() : "";
                    var strengthsJson = rec.TryGetProperty("strengths", out var s) ? s.GetRawText() : "[]";
                    var areasJson = rec.TryGetProperty("areas_to_develop", out var a) ? a.GetRawText() : 
                                   rec.TryGetProperty("areasToDevelop", out var a2) ? a2.GetRawText() : "[]";

                    // Insert or update recommendation
                    string upsertQuery = @"
                        INSERT INTO recommendations (user_id, career_id, match_percentage, reasoning, strengths, areas_to_develop)
                        VALUES (@userId, @careerId, @matchPercentage, @reasoning, @strengths, @areas)
                        ON DUPLICATE KEY UPDATE 
                            match_percentage = @matchPercentage,
                            reasoning = @reasoning,
                            strengths = @strengths,
                            areas_to_develop = @areas,
                            updated_at = NOW()";

                    using MySqlCommand upsertCmd = new(upsertQuery, conn);
                    upsertCmd.Parameters.AddWithValue("@userId", userId);
                    upsertCmd.Parameters.AddWithValue("@careerId", careerId);
                    upsertCmd.Parameters.AddWithValue("@matchPercentage", matchPercentage);
                    upsertCmd.Parameters.AddWithValue("@reasoning", reasoning);
                    upsertCmd.Parameters.AddWithValue("@strengths", strengthsJson);
                    upsertCmd.Parameters.AddWithValue("@areas", areasJson);
                    upsertCmd.ExecuteNonQuery();

                    // Get career name
                    var career = careers.FirstOrDefault(c => ((dynamic)c).id == careerId);
                    var careerName = career != null ? ((dynamic)career).career_name : "Unknown";

                    recommendations.Add(new Recommendation
                    {
                        CareerId = careerId,
                        CareerName = careerName,
                        MatchPercentage = matchPercentage,
                        Reasoning = reasoning,
                        Strengths = JsonSerializer.Deserialize<List<string>>(strengthsJson),
                        AreasToDevelop = JsonSerializer.Deserialize<List<string>>(areasJson)
                    });
                }

                return Ok(new RecommendationsResponse { Recommendations = recommendations });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating recommendations: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to generate recommendations", details = ex.Message });
            }
        }

        // GET /api/recommendations
        [HttpGet]
        public IActionResult GetRecommendations()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT r.id, r.career_id, c.name as career_name, r.match_percentage, 
                           r.reasoning, r.strengths, r.areas_to_develop, r.created_at
                    FROM recommendations r
                    JOIN careers c ON r.career_id = c.id
                    WHERE r.user_id = @userId
                    ORDER BY r.match_percentage DESC";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = cmd.ExecuteReader();
                var recommendations = new List<Recommendation>();

                while (reader.Read())
                {
                    recommendations.Add(new Recommendation
                    {
                        Id = reader.GetInt32("id"),
                        CareerId = reader.GetInt32("career_id"),
                        CareerName = reader.GetString("career_name"),
                        MatchPercentage = reader.GetDecimal("match_percentage"),
                        Reasoning = reader.IsDBNull(reader.GetOrdinal("reasoning")) ? null : reader.GetString("reasoning"),
                        Strengths = reader.IsDBNull(reader.GetOrdinal("strengths")) ? null : JsonSerializer.Deserialize<List<string>>(reader.GetString("strengths")),
                        AreasToDevelop = reader.IsDBNull(reader.GetOrdinal("areas_to_develop")) ? null : JsonSerializer.Deserialize<List<string>>(reader.GetString("areas_to_develop")),
                        CreatedAt = reader.GetDateTime("created_at")
                    });
                }

                return Ok(new RecommendationsResponse { Recommendations = recommendations });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to get recommendations", details = ex.Message });
            }
        }

        // GET /api/recommendations/careers
        [HttpGet("careers")]
        [AllowAnonymous]
        public IActionResult GetAllCareers()
        {
            try
            {
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string query = @"
                    SELECT id, name, description, key_skills
                    FROM careers
                    ORDER BY name";

                using MySqlCommand cmd = new(query, conn);
                using var reader = cmd.ExecuteReader();

                var careers = new List<object>();
                while (reader.Read())
                {
                    var requiredSkillsJson = reader.IsDBNull(reader.GetOrdinal("key_skills")) 
                        ? "[]" 
                        : reader.GetString("key_skills");

                    careers.Add(new
                    {
                        id = reader.GetInt32("id"),
                        name = reader.GetString("name"),
                        description = reader.IsDBNull(reader.GetOrdinal("description")) 
                            ? "" 
                            : reader.GetString("description"),
                        requiredSkills = JsonSerializer.Deserialize<List<string>>(requiredSkillsJson)
                    });
                }

                return Ok(new { careers });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching careers: {ex.Message}");
                return StatusCode(500, new { error = "Failed to fetch careers", details = ex.Message });
            }
        }
    }
}
