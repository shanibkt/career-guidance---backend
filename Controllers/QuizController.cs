using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using System.Data;
using System.Text.Json;
using System.Security.Claims;
using MyFirstApi.Models;
using MyFirstApi.Services;
using YoutubeExplode;
using YoutubeExplode.Videos.ClosedCaptions;

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

                // Validate questions JSON is not empty
                if (string.IsNullOrWhiteSpace(questions))
                {
                    _logger.LogError("Questions JSON is null or empty!");
                    return StatusCode(500, new { error = "Failed to serialize questions", details = "Questions data is empty" });
                }

                _logger.LogInformation($"Questions JSON length: {questions.Length} characters");
                _logger.LogInformation($"Questions preview: {questions.Substring(0, Math.Min(200, questions.Length))}...");

                // Generate UUID for quiz
                var quizId = Guid.NewGuid().ToString();
                _logger.LogInformation($"Generated quiz_id: {quizId}");

                // Save quiz session
                _logger.LogInformation("Step 6: Saving quiz session to database...");
                string insertQuery = @"
                    INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions, completed) 
                    VALUES (@quizId, @userId, @questions, @totalQuestions, FALSE)";
                
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
                _logger.LogInformation($"Looking up quiz_id: {request.QuizId}");
                string checkQuery = "SELECT user_id, questions FROM quiz_sessions WHERE quiz_id = @quizId";
                using MySqlCommand checkCmd = new(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@quizId", request.QuizId);
                
                using var reader = checkCmd.ExecuteReader();
                if (!reader.Read())
                {
                    _logger.LogWarning($"Quiz not found: {request.QuizId}");
                    return BadRequest(new { error = "Invalid quiz_id" });
                }
                
                _logger.LogInformation("Quiz found, checking user_id...");

                var sessionUserId = reader.GetInt32("user_id");
                _logger.LogInformation($"Quiz belongs to user_id: {sessionUserId}, requesting user: {userId}");
                if (sessionUserId != userId)
                    return Unauthorized(new { error = "Quiz does not belong to this user" });

                // Check if questions column is null
                var questionsOrdinal = reader.GetOrdinal("questions");
                _logger.LogInformation($"Questions column ordinal: {questionsOrdinal}");
                
                if (reader.IsDBNull(questionsOrdinal))
                {
                    _logger.LogError("Questions column is NULL in database!");
                    reader.Close();
                    return StatusCode(500, new { error = "Quiz data is corrupted", details = "Questions data is missing from database" });
                }

                var questionsJson = reader.GetString("questions");
                _logger.LogInformation($"Retrieved questions JSON, length: {questionsJson?.Length ?? 0} characters");
                _logger.LogInformation($"Questions JSON preview: {questionsJson?.Substring(0, Math.Min(200, questionsJson?.Length ?? 0))}");
                reader.Close();

                // Parse questions to get correct answers and skill categories
                List<QuizQuestion>? questionsList;
                try
                {
                    var questionsObj = JsonSerializer.Deserialize<JsonElement>(questionsJson!);
                    _logger.LogInformation("✅ Deserialized to JsonElement successfully");
                    questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsObj.GetRawText());
                    _logger.LogInformation($"✅ Deserialized to List<QuizQuestion>, count: {questionsList?.Count ?? 0}");
                }
                catch (Exception parseEx)
                {
                    _logger.LogError($"❌ JSON parse error: {parseEx.Message}");
                    _logger.LogError($"❌ JSON content: {questionsJson}");
                    return StatusCode(500, new { error = "Failed to parse quiz questions", details = parseEx.Message });
                }

                if (questionsList == null || questionsList.Count == 0)
                    return StatusCode(500, new { error = "No questions found for this quiz" });

                _logger.LogInformation($"📊 Starting score calculation for {request.Answers?.Count ?? 0} answers");
                
                // Calculate scores per skill
                var skillScores = new Dictionary<string, (int correct, int total)>();
                int totalCorrect = 0;

                foreach (var answer in request.Answers)
                {
                    _logger.LogInformation($"Processing answer for question {answer.QuestionId}");
                    var question = questionsList.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question == null) 
                    {
                        _logger.LogWarning($"Question {answer.QuestionId} not found in list");
                        continue;
                    }

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
                
                _logger.LogInformation($"✅ Score calculation complete. Total correct: {totalCorrect}");

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
                _logger.LogInformation("🎯 Loading careers for matching...");
                string careersQuery = "SELECT id, name, key_skills, average_salary FROM careers";
                using MySqlCommand careersCmd = new(careersQuery, conn);
                using var careersReader = careersCmd.ExecuteReader();

                var careerMatches = new List<CareerMatch>();

                while (careersReader.Read())
                {
                    try
                    {
                        var careerId = careersReader.GetInt32("id");
                        var careerName = careersReader.IsDBNull(careersReader.GetOrdinal("name")) 
                            ? "Unknown" 
                            : careersReader.GetString("name");
                        
                        if (careersReader.IsDBNull(careersReader.GetOrdinal("key_skills")))
                        {
                            _logger.LogWarning($"Career {careerId} has null key_skills, skipping");
                            continue;
                        }
                        
                        var requiredSkillsJson = careersReader.GetString("key_skills");
                        var skillWeightsJson = "{}";
                        var minScorePercentage = 0m;
                        var salaryRange = careersReader.IsDBNull(careersReader.GetOrdinal("average_salary")) 
                            ? null : careersReader.GetString("average_salary");

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
                    catch (Exception careerEx)
                    {
                        _logger.LogError($"❌ Error processing career: {careerEx.Message}");
                        continue;
                    }
                }
                careersReader.Close();
                
                _logger.LogInformation($"✅ Career matching complete. Found {careerMatches.Count} matches");

                // Sort career matches by percentage descending
                careerMatches = careerMatches.OrderByDescending(c => c.MatchPercentage).ToList();

                // Save results to database
                string updateQuery = @"
                    UPDATE quiz_sessions 
                    SET answers = @answers, 
                        score = @totalScore,
                        total_questions = @totalQuestions,
                        completed = TRUE, 
                        completed_at = NOW() 
                    WHERE quiz_id = @quizId";

                using MySqlCommand updateCmd = new(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@quizId", request.QuizId);
                updateCmd.Parameters.AddWithValue("@answers", JsonSerializer.Serialize(request.Answers));
                updateCmd.Parameters.AddWithValue("@totalScore", totalCorrect);
                updateCmd.Parameters.AddWithValue("@totalQuestions", totalQuestions);
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

        // POST /api/quiz/generate-from-skill
        [HttpPost("generate-from-skill")]
        public async Task<IActionResult> GenerateQuizFromSkillName([FromBody] SkillQuizRequest request)
        {
            try
            {
                _logger.LogInformation($"Skill-based quiz generation called for skill: {request.SkillName}");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = int.Parse(userIdClaim ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                if (string.IsNullOrWhiteSpace(request.SkillName))
                {
                    return BadRequest(new { error = "Skill name is required" });
                }

                // Generate quiz using AI with timeout
                _logger.LogInformation($"Calling AI to generate quiz for skill: {request.SkillName}");
                
                string aiResponse;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                    aiResponse = await _groqService.GenerateQuizFromSkillName(
                        request.SkillName,
                        request.VideoTitle ?? "General Topics"
                    );
                    
                    if (string.IsNullOrWhiteSpace(aiResponse))
                    {
                        _logger.LogError("AI returned empty response for skill-based quiz");
                        return StatusCode(500, new { error = "AI service error", details = "AI returned empty response" });
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning("AI timeout for skill-based quiz");
                    return StatusCode(504, new { error = "AI service timeout", details = "Please try again" });
                }

                // Parse AI response
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                JsonElement questionsObj;
                try
                {
                    questionsObj = JsonSerializer.Deserialize<JsonElement>(cleanJson);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Failed to parse AI response: {jsonEx.Message}");
                    return StatusCode(500, new { error = "Invalid AI response", details = "AI returned malformed data" });
                }

                if (!questionsObj.TryGetProperty("questions", out var questionsArray))
                {
                    _logger.LogError("AI response missing 'questions' property");
                    return StatusCode(500, new { error = "Invalid AI response" });
                }

                var questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsArray.GetRawText());
                if (questionsList == null || questionsList.Count != 10)
                {
                    _logger.LogWarning($"Expected 10 questions, got {questionsList?.Count ?? 0}");
                    return StatusCode(500, new { error = "Invalid question count" });
                }

                // Generate quiz ID and save to database
                var quizId = Guid.NewGuid().ToString();
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                var questions = JsonSerializer.Serialize(questionsList);
                
                string insertQuery = @"
                    INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions) 
                    VALUES (@quizId, @userId, @questions, @totalQuestions)";
                
                using MySqlCommand insertCmd = new(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@quizId", quizId);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@questions", questions);
                insertCmd.Parameters.AddWithValue("@totalQuestions", questionsList.Count);
                
                await insertCmd.ExecuteNonQueryAsync();

                _logger.LogInformation($"Skill-based quiz generated successfully: {quizId}");
                return Ok(new { quizId, questions = questionsList });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating skill-based quiz: {ex.Message}");
                return StatusCode(500, new { error = "Quiz generation failed", details = ex.Message });
            }
        }

        // POST /api/quiz/generate-from-transcript
        [HttpPost("generate-from-transcript")]
        public async Task<IActionResult> GenerateQuizFromTranscript([FromBody] TranscriptQuizRequest request)
        {
            try
            {
                _logger.LogInformation("Quiz generate-from-transcript endpoint called");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"User ID from token: {userIdClaim ?? "NULL"}");
                
                var userId = int.Parse(userIdClaim ?? "0");
                if (userId == 0)
                {
                    _logger.LogWarning("User ID is 0 or null - unauthorized");
                    return Unauthorized(new { error = "Invalid token - user ID not found" });
                }

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Transcript))
                {
                    return BadRequest(new { error = "Transcript is required" });
                }

                if (string.IsNullOrWhiteSpace(request.SkillName))
                {
                    return BadRequest(new { error = "Skill name is required" });
                }

                _logger.LogInformation($"Generating quiz for skill: {request.SkillName}, video: {request.VideoTitle}");
                _logger.LogInformation($"Transcript length: {request.Transcript.Length} characters");

                // Generate questions using AI with transcript
                _logger.LogInformation("Calling Groq API to generate transcript-based quiz questions...");
                
                string aiResponse;
                try
                {
                    // Truncate transcript if too long (max 8000 characters to stay within token limits)
                    var transcript = request.Transcript.Length > 8000 
                        ? request.Transcript.Substring(0, 8000) + "... [truncated]"
                        : request.Transcript;

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                    aiResponse = await _groqService.GenerateQuizFromTranscript(
                        transcript, 
                        request.SkillName, 
                        request.VideoTitle ?? "Video Tutorial"
                    );
                    _logger.LogInformation($"Got AI response ({aiResponse.Length} characters)");
                    
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
                catch (Exception aiEx)
                {
                    _logger.LogError($"Groq API error: {aiEx.Message}");
                    return StatusCode(500, new { error = "AI service error", details = $"AI service error: {aiEx.Message}. Please try again." });
                }
                
                // Parse AI response
                _logger.LogInformation("Parsing AI response...");
                var cleanJson = aiResponse.Trim();
                if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                cleanJson = cleanJson.Trim();

                JsonElement questionsObj;
                try
                {
                    questionsObj = JsonSerializer.Deserialize<JsonElement>(cleanJson);
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError($"Failed to parse AI response as JSON: {jsonEx.Message}");
                    return StatusCode(500, new { error = "Invalid AI response", details = "AI returned malformed data. Please try again." });
                }
                
                if (!questionsObj.TryGetProperty("questions", out var questionsArray))
                {
                    _logger.LogError("AI response missing 'questions' property");
                    return StatusCode(500, new { error = "Invalid AI response", details = "AI response missing questions array. Please try again." });
                }
                
                var questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsArray.GetRawText());
                
                if (questionsList == null || questionsList.Count == 0)
                {
                    _logger.LogWarning($"AI generated {questionsList?.Count ?? 0} questions.");
                    return StatusCode(500, new { error = "AI did not generate questions", 
                        details = $"Got {questionsList?.Count ?? 0} questions. Please try again." });
                }

                var questions = questionsArray.GetRawText();
                var quizId = Guid.NewGuid().ToString();

                // Save quiz session
                using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                conn.Open();

                string insertQuery = @"
                    INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions) 
                    VALUES (@quizId, @userId, @questions, @totalQuestions)";
                
                using MySqlCommand insertCmd = new(insertQuery, conn);
                insertCmd.Parameters.AddWithValue("@quizId", quizId);
                insertCmd.Parameters.AddWithValue("@userId", userId);
                insertCmd.Parameters.AddWithValue("@questions", questions);
                insertCmd.Parameters.AddWithValue("@totalQuestions", questionsList.Count);
                
                insertCmd.ExecuteNonQuery();
                _logger.LogInformation($"Quiz session created with quiz_id: {quizId}");

                return Ok(new GenerateQuizResponse
                {
                    QuizId = quizId,
                    Questions = questionsList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating quiz from transcript: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to generate quiz", details = ex.Message });
            }
        }

        // POST /api/quiz/generate-from-video
        // This endpoint extracts captions from YouTube video and generates quiz
        [HttpPost("generate-from-video")]
        public async Task<IActionResult> GenerateQuizFromVideo([FromBody] VideoQuizRequest request)
        {
            try
            {
                _logger.LogInformation($"Generate quiz from video called - Video ID: {request.VideoId}");
                
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = int.Parse(userIdClaim ?? "0");
                if (userId == 0)
                {
                    return Unauthorized(new { error = "Invalid token" });
                }

                if (string.IsNullOrWhiteSpace(request.VideoId))
                {
                    return BadRequest(new { error = "Video ID is required" });
                }

                if (string.IsNullOrWhiteSpace(request.SkillName))
                {
                    return BadRequest(new { error = "Skill name is required" });
                }

                _logger.LogInformation($"Fetching transcript from database for video: {request.VideoId}");

                // Fetch transcript from database instead of trying to extract from YouTube
                string? transcript = null;

                try
                {
                    using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                    conn.Open();

                    string query = @"
                        SELECT transcript 
                        FROM learning_videos 
                        WHERE youtube_video_id = @videoId 
                        LIMIT 1";

                    using MySqlCommand cmd = new(query, conn);
                    cmd.Parameters.AddWithValue("@videoId", request.VideoId);

                    var result = cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        transcript = result.ToString();
                        if (!string.IsNullOrWhiteSpace(transcript))
                        {
                            _logger.LogInformation($"✅ Transcript found in database: {transcript.Length} characters");
                        }
                        else
                        {
                            _logger.LogInformation("Transcript column exists but is empty");
                        }
                    }
                    else
                    {
                        _logger.LogInformation("No transcript found in database for this video");
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogWarning($"Database query failed: {dbEx.Message}");
                }

                // If transcript extraction failed, fall back to skill-based quiz
                if (string.IsNullOrWhiteSpace(transcript))
                {
                    _logger.LogInformation("No transcript available, generating skill-based quiz");
                    
                    string aiResponse;
                    try
                    {
                        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                        aiResponse = await _groqService.GenerateQuizFromSkillName(
                            request.SkillName,
                            request.VideoTitle ?? "General Topics"
                        );
                    }
                    catch (TaskCanceledException)
                    {
                        return StatusCode(504, new { error = "AI service timeout" });
                    }

                    if (string.IsNullOrWhiteSpace(aiResponse))
                    {
                        return StatusCode(500, new { error = "AI returned empty response" });
                    }

                    // Parse and return skill-based quiz
                    var cleanJson = aiResponse.Trim();
                    if (cleanJson.StartsWith("```json")) cleanJson = cleanJson.Substring(7);
                    if (cleanJson.StartsWith("```")) cleanJson = cleanJson.Substring(3);
                    if (cleanJson.EndsWith("```")) cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
                    cleanJson = cleanJson.Trim();

                    var questionsObj = JsonSerializer.Deserialize<JsonElement>(cleanJson);
                    if (!questionsObj.TryGetProperty("questions", out var questionsArray))
                    {
                        return StatusCode(500, new { error = "Invalid AI response" });
                    }

                    var questionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(questionsArray.GetRawText());
                    if (questionsList == null || questionsList.Count == 0)
                    {
                        return StatusCode(500, new { error = "No questions generated" });
                    }

                    var questions = questionsArray.GetRawText();
                    var quizId = Guid.NewGuid().ToString();

                    using MySqlConnection conn = new(_configuration.GetConnectionString("DefaultConnection"));
                    conn.Open();

                    string insertQuery = @"
                        INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions) 
                        VALUES (@quizId, @userId, @questions, @totalQuestions)";
                    
                    using MySqlCommand insertCmd = new(insertQuery, conn);
                    insertCmd.Parameters.AddWithValue("@quizId", quizId);
                    insertCmd.Parameters.AddWithValue("@userId", userId);
                    insertCmd.Parameters.AddWithValue("@questions", questions);
                    insertCmd.Parameters.AddWithValue("@totalQuestions", questionsList.Count);
                    
                    insertCmd.ExecuteNonQuery();

                    return Ok(new { 
                        quiz_id = quizId, 
                        questions = questionsList,
                        transcript_available = false,
                        message = "Quiz generated from skill knowledge (captions unavailable)"
                    });
                }

                // Generate quiz from transcript
                _logger.LogInformation("Generating quiz from video transcript");
                
                // Smart chunking: Take samples from beginning, middle, and end
                // Reduced to ~6k chars to fit within Groq API total payload limits (transcript + prompt)
                const int maxChars = 6000;
                string processedTranscript;
                
                if (transcript.Length <= maxChars)
                {
                    processedTranscript = transcript;
                    _logger.LogInformation($"Using full transcript: {transcript.Length} characters");
                }
                else
                {
                    // Take 3 chunks: beginning (40%), middle (30%), end (30%)
                    int chunkSize = maxChars / 3;
                    int midStart = (transcript.Length / 2) - (chunkSize / 2);
                    int endStart = transcript.Length - chunkSize;
                    
                    var beginningChunk = transcript.Substring(0, chunkSize);
                    var middleChunk = transcript.Substring(midStart, chunkSize);
                    var endChunk = transcript.Substring(endStart, chunkSize);
                    
                    processedTranscript = $"{beginningChunk}\n\n[... middle section ...]\n\n{middleChunk}\n\n[... final section ...]\n\n{endChunk}";
                    _logger.LogInformation($"Transcript too long ({transcript.Length} chars). Using strategic samples: {processedTranscript.Length} chars");
                }
                
                var truncatedTranscript = processedTranscript;
                
                string transcriptAiResponse;
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(40));
                    transcriptAiResponse = await _groqService.GenerateQuizFromTranscript(
                        truncatedTranscript,
                        request.SkillName,
                        request.VideoTitle ?? "Video Tutorial"
                    );
                }
                catch (TaskCanceledException)
                {
                    return StatusCode(504, new { error = "AI service timeout" });
                }

                if (string.IsNullOrWhiteSpace(transcriptAiResponse))
                {
                    return StatusCode(500, new { error = "AI returned empty response" });
                }

                // Parse AI response
                var cleanTranscriptJson = transcriptAiResponse.Trim();
                if (cleanTranscriptJson.StartsWith("```json")) cleanTranscriptJson = cleanTranscriptJson.Substring(7);
                if (cleanTranscriptJson.StartsWith("```")) cleanTranscriptJson = cleanTranscriptJson.Substring(3);
                if (cleanTranscriptJson.EndsWith("```")) cleanTranscriptJson = cleanTranscriptJson.Substring(0, cleanTranscriptJson.Length - 3);
                cleanTranscriptJson = cleanTranscriptJson.Trim();

                var transcriptQuestionsObj = JsonSerializer.Deserialize<JsonElement>(cleanTranscriptJson);
                if (!transcriptQuestionsObj.TryGetProperty("questions", out var transcriptQuestionsArray))
                {
                    return StatusCode(500, new { error = "Invalid AI response" });
                }

                var transcriptQuestionsList = JsonSerializer.Deserialize<List<QuizQuestion>>(transcriptQuestionsArray.GetRawText());
                if (transcriptQuestionsList == null || transcriptQuestionsList.Count == 0)
                {
                    return StatusCode(500, new { error = "No questions generated" });
                }

                var transcriptQuestions = transcriptQuestionsArray.GetRawText();
                var transcriptQuizId = Guid.NewGuid().ToString();

                using MySqlConnection transcriptConn = new(_configuration.GetConnectionString("DefaultConnection"));
                transcriptConn.Open();

                string transcriptInsertQuery = @"
                    INSERT INTO quiz_sessions (quiz_id, user_id, questions, total_questions) 
                    VALUES (@quizId, @userId, @questions, @totalQuestions)";
                
                using MySqlCommand transcriptInsertCmd = new(transcriptInsertQuery, transcriptConn);
                transcriptInsertCmd.Parameters.AddWithValue("@quizId", transcriptQuizId);
                transcriptInsertCmd.Parameters.AddWithValue("@userId", userId);
                transcriptInsertCmd.Parameters.AddWithValue("@questions", transcriptQuestions);
                transcriptInsertCmd.Parameters.AddWithValue("@totalQuestions", transcriptQuestionsList.Count);
                
                transcriptInsertCmd.ExecuteNonQuery();

                _logger.LogInformation($"✅ Generated video-based quiz: {transcriptQuizId}");

                return Ok(new { 
                    quiz_id = transcriptQuizId, 
                    questions = transcriptQuestionsList,
                    transcript_available = true,
                    transcript_length = transcript.Length,
                    message = "Quiz generated from video transcript"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error generating quiz from video: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = "Failed to generate quiz", details = ex.Message });
            }
        }
    }
}
