using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;
using System.Text.Json;
using System.Security.Claims;
using MyFirstApi.Models;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly GroqService _groqService;
        private readonly ILogger<QuizController> _logger;

        public QuizController(IConfiguration configuration, GroqService groqService, ILogger<QuizController> logger)
        {
            _configuration = configuration;
            _groqService = groqService;
            _logger = logger;
        }

        // POST /api/quiz/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GenerateQuiz()
        {
            try
            {
                _logger.LogInformation("Quiz generate endpoint called");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"User ID from token: {userIdClaim ?? "NULL"}");
                
                var userId = int.Parse(userIdClaim ?? "0");
                if (userId == 0)
                {
                    _logger.LogWarning("User ID is 0 or null - unauthorized");
                    return Unauthorized(new { error = "Invalid token - user ID not found" });
                }

                // Get user profile
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Fetch user and profile data
                string query = @"
                    SELECT u.FullName, u.Email, 
                           p.PhoneNumber, p.Age, p.Gender, p.EducationLevel, 
                           p.FieldOfStudy, p.Skills
                    FROM Users u
                    LEFT JOIN UserProfiles p ON u.Id = p.UserId
                    WHERE u.Id = @userId";

                using MySqlCommand cmd = new(query, conn);
                cmd.Parameters.AddWithValue("@userId", userId);

                using var reader = cmd.ExecuteReader();
                if (!reader.Read()) return NotFound("User not found");

                var userProfile = new
                {
                    fullName = reader.GetString("FullName"),
                    education = reader.IsDBNull(reader.GetOrdinal("EducationLevel")) ? "Not specified" : reader.GetString("EducationLevel"),
                    fieldOfStudy = reader.IsDBNull(reader.GetOrdinal("FieldOfStudy")) ? "Not specified" : reader.GetString("FieldOfStudy"),
                    skills = reader.IsDBNull(reader.GetOrdinal("Skills")) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(reader.GetString("Skills")),
                    age = reader.IsDBNull(reader.GetOrdinal("Age")) ? 0 : reader.GetInt32("Age")
                };
                reader.Close();

                // Validate user has skills
                if (userProfile.skills?.Count == 0)
                {
                    return BadRequest(new { error = "No skills found in profile", details = "Please add skills to your profile before generating a quiz." });
                }

                // Generate questions using AI with timeout
                _logger.LogInformation("Step 2: Calling Groq API to generate skill-based quiz questions...");
                
                string aiResponse;
                try
                {
                    // Create cancellation token with 40 second timeout (increased for reliability)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                    aiResponse = await _groqService.GenerateQuizQuestions(JsonSerializer.Serialize(userProfile));
                    _logger.LogInformation($"Step 3: Got AI response ({aiResponse.Length} characters)");
                    
                    if (string.IsNullOrWhiteSpace(aiResponse))
                    {
                        _logger.LogError("AI returned empty response");
                        return StatusCode(500, new { error = "AI service error", details = "AI returned empty response. Please try again." });
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("Groq API timeout - AI took longer than 40 seconds");
                    return StatusCode(504, new { error = "AI service timeout", details = "Quiz generation is taking longer than expected. Please try again in a moment." });
                }
                catch (HttpRequestException httpEx)
                {
                    _logger.LogError($"Network error calling Groq API: {httpEx.Message}");
                    return StatusCode(503, new { error = "Network error", details = "Unable to reach AI service. Please check your internet connection and try again." });
                }
                catch (Exception aiEx)
                {
                    _logger.LogError($"Groq API error: {aiEx.Message}");
                    return StatusCode(500, new { error = "AI service error", details = $"AI service error: {aiEx.Message}. Please try again." });
                }
                
                // Parse AI response (remove markdown if present)
                _logger.LogInformation("Step 4: Parsing AI response...");
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                _logger.LogInformation("Step 5: Validating question format...");
                
                JsonElement questionsObj;
                try
                {
                    questionsObj = JsonSerializer.Deserialize<JsonElement>(cleanJson);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Failed to parse AI response as JSON: {jsonEx.Message}");
                    _logger.LogError($"AI Response was: {cleanJson.Substring(0, Math.Min(500, cleanJson.Length))}");
                    return StatusCode(500, new { error = "Invalid AI response", details = "AI returned malformed data. Please try again." });
                }
                
                if (!questionsObj.TryGetProperty("questions", out var questionsArray))
                {
                    _logger.LogError("AI response missing 'questions' property");
                    return StatusCode(500, new { error = "Invalid AI response", details = "AI response missing questions array. Please try again." });
                }
                
                // Validate question types and required fields
                var questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsArray.GetRawText());
                
                if (questionsList == null || questionsList.Count != 10)
                {
                    _logger.LogWarning($"AI generated {questionsList?.Count ?? 0} questions. Expected 10.");
                    return StatusCode(500, new { error = "AI did not generate correct number of questions", 
                        details = $"Got {questionsList?.Count ?? 0} questions. Expected 10." });
                }

                // Validate all questions have skill_category and correct_answer
                var missingFields = questionsList.Where(q => string.IsNullOrEmpty(q.SkillCategory) || string.IsNullOrEmpty(q.CorrectAnswer)).ToList();
                if (missingFields.Any())
                {
                    _logger.LogWarning($"{missingFields.Count} questions missing skill_category or correct_answer");
                    return StatusCode(500, new { error = "Invalid question format", 
                        details = "Some questions are missing skill_category or correct_answer fields." });
                }
                
                var questions = questionsArray.GetRawText();

                // Generate UUID for quiz
                var quizId = Guid.NewGuid().ToString();
                _logger.LogInformation($"Generated quiz_id: {quizId}");

                // Save quiz session
                _logger.LogInformation("Step 6: Saving quiz session to database...");
                string insertQuery = @"
                    INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions) 
                    VALUES (@quizId, @userId, @questions, @totalQuestions)";
                
                using MySqlCommand insertCmd = new(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@quizId", quizId);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@questions", questions);
                insertCmd.Parameters.AddWithValue("@totalQuestions", questionsList.Count);
                
                insertCmd.ExecuteNonQuery();
                _logger.LogInformation($"Step 7: Quiz session created with quiz_id: {quizId}");

                return Ok(new GenerateQuizResponse
                {
                    QuizId = quizId,
                    Questions = questionsList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating quiz: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to generate quiz", details = ex.Message });
            }
        }

        // POST /api/quiz/submit
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitQuiz([FromBody] SubmitQuizRequest request)
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                if (userId == 0) return Unauthorized();

                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Verify quiz belongs to user and get questions
                string checkQuery = "SELECT user_id, questions FROM quiz_sessions WHERE quiz_id = @quizId";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@quizId", request.QuizId);
                
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                    return BadRequest(new { error = "Invalid quiz_id" });

                var sessionUserId = reader.GetInt32("user_id");
                if (sessionUserId != userId)
                    return Unauthorized(new { error = "Quiz does not belong to this user" });

                var questionsJson = reader.GetString("questions");
                reader.Close();

                // Parse questions to get correct answers and skill categories
                var questionsObj = JsonSerializer.Deserialize<JsonElement>(questionsJson);
                var questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsObj.GetRawText());

                if (questionsList == null || questionsList.Count == 0)
                    return StatusCode(500, new { error = "No questions found for this quiz" });

                // Calculate scores per skill
                var skillScores = new Dictionary<string, (int correct, int total)>();
                int totalCorrect = 0;

                foreach (var answer in request.Answers)
                {
                    var question = questionsList.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) continue;

                    var skill = question.SkillCategory ?? "Unknown";
                    if (!skillScores.ContainsKey(skill))
                        skillScores[skill] = (0, 0);

                    var (correct, total) = skillScores[skill];
                    total++;

                    // Check if answer is correct (case-insensitive comparison)
                    if (!string.IsNullOrEmpty(question.CorrectAnswer) && 
                        answer.Answer?.Trim().ToUpper() == question.CorrectAnswer.Trim().ToUpper())
                    {
                        correct++;
                        totalCorrect++;
                    }

                    skillScores[skill] = (correct, total);
                }

                // Convert to SkillScore list with percentages
                var skillBreakdown = skillScores.Select(kvp => new SkillScore
                {
                    Skill = kvp.Key,
                    Correct = kvp.Value.correct,
                    Total = kvp.Value.total,
                    Percentage = kvp.Value.total > 0 ? (decimal)kvp.Value.correct / kvp.Value.total * 100 : 0
                }).ToList();

                var totalQuestions = questionsList.Count;
                var overallPercentage = totalQuestions > 0 ? (decimal)totalCorrect / totalQuestions * 100 : 0;

                // Load careers and calculate matches
                string careersQuery = "SELECT id, career_name, required_skills, skill_weights, min_score_percentage, salary_range FROM careers";
                using MySqlCommand careersCmd = new(careersQuery, conn);
                using var careersReader = careersCmd.ExecuteReader();

                var careerMatches = new List<CareerMatch>();

                while (careersReader.Read())
                {
                    var careerId = careersReader.GetInt32("id");
                    var careerName = careersReader.GetString("career_name");
                    var requiredSkillsJson = careersReader.GetString("required_skills");
                    var skillWeightsJson = careersReader.GetString("skill_weights");
                    var minScorePercentage = careersReader.GetDecimal("min_score_percentage");
                    var salaryRange = careersReader.IsDBNull(careersReader.GetOrdinal("salary_range")) 
                        ? null : careersReader.GetString("salary_range");

                    // Parse required skills and weights
                    var requiredSkills = JsonSerializer.Deserialize<List<string>>(requiredSkillsJson) ?? new List<string>();
                    var skillWeights = JsonSerializer.Deserialize<Dictionary<string, int>>(skillWeightsJson) ?? new Dictionary<string, int>();

                    // Calculate weighted match percentage
                    decimal totalWeight = 0;
                    decimal weightedScore = 0;
                    var matchingSkills = new List<string>();
                    var missingSkills = new List<string>();

                    foreach (var skill in requiredSkills)
                    {
                        var weight = skillWeights.ContainsKey(skill) ? skillWeights[skill] : 50; // default weight
                        totalWeight += weight;

                        var userSkillScore = skillBreakdown.FirstOrDefault(s => s.Skill.Equals(skill, StringComparison.OrdinalIgnoreCase));
                        if (userSkillScore != null && userSkillScore.Total > 0)
                        {
                            weightedScore += userSkillScore.Percentage * weight / 100;
                            matchingSkills.Add(skill);
                        }
                        else
                        {
                            missingSkills.Add(skill);
                        }
                    }

                    var matchPercentage = totalWeight > 0 ? weightedScore / totalWeight * 100 : 0;

                    // Only include careers that meet minimum score threshold
                    if (matchPercentage >= minScorePercentage)
                    {
                        careerMatches.Add(new CareerMatch
                        {
                            CareerId = careerId,
                            CareerName = careerName,
                            MatchPercentage = Math.Round(matchPercentage, 2),
                            MatchingSkills = matchingSkills,
                            MissingSkills = missingSkills,
                            SalaryRange = salaryRange
                        });
                    }
                }
                careersReader.Close();

                // Sort career matches by percentage descending
                careerMatches = careerMatches.OrderByDescending(c => c.MatchPercentage).ToList();

                // Save results to database
                string updateQuery = @"
                    UPDATE quiz_sessions 
                    SET answers = @answers, 
                        skill_scores = @skillScores,
                        total_score = @totalScore,
                        total_questions = @totalQuestions,
                        percentage = @percentage,
                        completed = TRUE, 
                        completed_at = NOW() 
                    WHERE quiz_id = @quizId";

                using MySqlCommand updateCmd = new(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@quizId", request.QuizId);
                updateCmd.Parameters.AddWithValue("@answers", JsonSerializer.Serialize(request.Answers));
                updateCmd.Parameters.AddWithValue("@skillScores", JsonSerializer.Serialize(skillBreakdown));
                updateCmd.Parameters.AddWithValue("@totalScore", totalCorrect);
                updateCmd.Parameters.AddWithValue("@totalQuestions", totalQuestions);
                updateCmd.Parameters.AddWithValue("@percentage", overallPercentage);
                updateCmd.ExecuteNonQuery();

                return Ok(new SubmitQuizResponse
                {
                    TotalScore = totalCorrect,
                    TotalQuestions = totalQuestions,
                    Percentage = Math.Round(overallPercentage, 2),
                    SkillBreakdown = skillBreakdown,
                    CareerMatches = careerMatches
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error submitting quiz: {ex.Message}");
                return StatusCode(500, new { error = "Failed to submit quiz", details = ex.Message });
            }
        }
    }
}
