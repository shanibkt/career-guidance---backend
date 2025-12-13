using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MyFirstApi.Models;
using MyFirstApi.Services;
using System.Security.Claims;

namespace MyFirstApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly GroqService _groqService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatController> _logger;

    public ChatController(GroqService groqService, IConfiguration configuration, ILogger<ChatController> logger)
    {
        _groqService = groqService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            _logger.LogInformation("Chat endpoint called");

            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { error = "Message is required" });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            _logger.LogInformation($"User ID from token: {userId}");

            // Get or create session
            Guid sessionId = request.SessionId ?? Guid.NewGuid();
            
            // Optional: Load chat history for context (if you want conversation memory)
            List<ChatMessage>? history = null;
            string? userProfile = null;

            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                // Create session if new, or verify existing session exists
                if (request.SessionId == null)
                {
                    sessionId = await CreateSessionAsync(connection, userId);
                    _logger.LogInformation($"Created new session: {sessionId}");
                }
                else
                {
                    // Verify session exists and belongs to user
                    var verifyQuery = "SELECT COUNT(*) FROM chatsessions WHERE SessionId = @SessionId AND UserId = @UserId";
                    using var verifyCmd = new MySqlCommand(verifyQuery, connection);
                    verifyCmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
                    verifyCmd.Parameters.AddWithValue("@UserId", userId);
                    
                    var exists = Convert.ToInt32(await verifyCmd.ExecuteScalarAsync()) > 0;
                    
                    if (!exists)
                    {
                        _logger.LogWarning($"⚠️ Session {sessionId} not found or doesn't belong to user {userId}, creating new session");
                        // Create the session since it doesn't exist
                        sessionId = await CreateSessionWithIdAsync(connection, userId, sessionId);
                        _logger.LogInformation($"Created session with provided ID: {sessionId}");
                    }
                    else
                    {
                        _logger.LogInformation($"Using existing session: {sessionId}");
                    }
                }

                // Get user profile for context
                userProfile = await GetUserProfileAsync(connection, userId);
                
                if (!string.IsNullOrEmpty(userProfile))
                {
                    _logger.LogInformation("✅ User profile loaded and will be used for personalization");
                }
                else
                {
                    _logger.LogWarning("⚠️ No user profile found - responses will be generic");
                }

                // Get recent chat history (last 6 messages)
                history = await GetChatHistoryAsync(connection, sessionId);

                // Save user message
                await SaveMessageAsync(connection, sessionId, "user", request.Message);
            }

            _logger.LogInformation("Calling Groq API for chat response...");

            // Get AI response
            var aiResponse = await _groqService.GetChatResponse(request.Message, history, userProfile);

            _logger.LogInformation($"Got AI response: {aiResponse.Substring(0, Math.Min(50, aiResponse.Length))}...");

            // Save AI response
            using (var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                await SaveMessageAsync(connection, sessionId, "assistant", aiResponse);
            }

            return Ok(new ChatResponse
            {
                Response = aiResponse,
                SessionId = sessionId
            });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError($"🔴 Groq API HTTP Error: {ex.Message}");
            _logger.LogError($"🔴 Stack Trace: {ex.StackTrace}");
            return StatusCode(500, new { error = $"Groq API error: {ex.Message}" });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning($"⏱️ Chat request timed out: {ex.Message}");
            return StatusCode(500, new { error = "Request timed out. Please try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError($"🔴 Chat error: {ex.Message}");
            _logger.LogError($"🔴 Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                _logger.LogError($"🔴 Inner exception: {ex.InnerException.Message}");
            }
            return StatusCode(500, new { error = $"Failed to get AI response: {ex.Message}" });
        }
    }

    [HttpGet("test")]
    public async Task<IActionResult> TestGroqConnection()
    {
        try
        {
            _logger.LogInformation("Testing Groq API connection...");
            
            var testResponse = await _groqService.GetChatResponse("Hello, are you working?", null, null);
            
            return Ok(new 
            { 
                success = true,
                message = "Groq API is working!",
                response = testResponse
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"🔴 Groq API Test Failed: {ex.Message}");
            return StatusCode(500, new 
            { 
                success = false,
                error = ex.Message,
                type = ex.GetType().Name
            });
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] Guid? sessionId)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { error = "Invalid user token" });
            }

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            List<ChatMessage> messages;

            if (sessionId.HasValue)
            {
                // Get specific session history
                messages = await GetChatHistoryAsync(connection, sessionId.Value);
            }
            else
            {
                // Get all user's recent messages across sessions
                var query = @"
                    SELECT cm.Id, cm.SessionId, cm.Role, cm.Message, cm.Timestamp
                    FROM chatmessages cm
                    INNER JOIN chatsessions cs ON cm.SessionId = cs.SessionId
                    WHERE cs.UserId = @UserId
                    ORDER BY cm.Timestamp DESC
                    LIMIT 50";

                using var cmd = new MySqlCommand(query, connection);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using var reader = await cmd.ExecuteReaderAsync();
                messages = new List<ChatMessage>();

                while (await reader.ReadAsync())
                {
                    messages.Add(new ChatMessage
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        SessionId = Guid.Parse(reader.GetString(reader.GetOrdinal("SessionId"))),
                        Role = reader.GetString(reader.GetOrdinal("Role")),
                        Message = reader.GetString(reader.GetOrdinal("Message")),
                        Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                    });
                }
            }

            return Ok(new { messages });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting chat history: {ex.Message}");
            return StatusCode(500, new { error = "Failed to retrieve chat history" });
        }
    }

    // NOTE: GetSessions endpoint moved to ChatHistoryController to avoid route conflicts
    // Use ChatHistoryController.GetSessions() for retrieving chat sessions

    private async Task<Guid> CreateSessionAsync(MySqlConnection connection, int userId)
    {
        var sessionId = Guid.NewGuid();
        var query = @"
            INSERT INTO chatsessions (UserId, SessionId, Title, CreatedAt, UpdatedAt, IsDeleted)
            VALUES (@UserId, @SessionId, @Title, NOW(), NOW(), 0)";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
        cmd.Parameters.AddWithValue("@Title", "New Conversation");

        await cmd.ExecuteNonQueryAsync();
        return sessionId;
    }

    private async Task<Guid> CreateSessionWithIdAsync(MySqlConnection connection, int userId, Guid sessionId)
    {
        var query = @"
            INSERT INTO chatsessions (UserId, SessionId, Title, CreatedAt, UpdatedAt, IsDeleted)
            VALUES (@UserId, @SessionId, @Title, NOW(), NOW(), 0)";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@UserId", userId);
        cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
        cmd.Parameters.AddWithValue("@Title", "New Conversation");

        await cmd.ExecuteNonQueryAsync();
        return sessionId;
    }

    private async Task SaveMessageAsync(MySqlConnection connection, Guid sessionId, string role, string message)
    {
        // Save the message
        var insertQuery = @"
            INSERT INTO chatmessages (SessionId, Role, Message, Timestamp)
            VALUES (@SessionId, @Role, @Message, @Timestamp)";

        using var insertCmd = new MySqlCommand(insertQuery, connection);
        insertCmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());
        insertCmd.Parameters.AddWithValue("@Role", role);
        insertCmd.Parameters.AddWithValue("@Message", message);
        insertCmd.Parameters.AddWithValue("@Timestamp", DateTime.UtcNow);

        await insertCmd.ExecuteNonQueryAsync();

        // Update session's last message and timestamp
        var truncatedMessage = message.Length > 500 ? message.Substring(0, 500) : message;
        var updateQuery = @"
            UPDATE chatsessions 
            SET LastMessage = @LastMessage, UpdatedAt = NOW() 
            WHERE SessionId = @SessionId";

        using var updateCmd = new MySqlCommand(updateQuery, connection);
        updateCmd.Parameters.AddWithValue("@LastMessage", truncatedMessage);
        updateCmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

        await updateCmd.ExecuteNonQueryAsync();
    }

    private async Task<List<ChatMessage>> GetChatHistoryAsync(MySqlConnection connection, Guid sessionId)
    {
        var query = @"
            SELECT Id, SessionId, Role, Message, Timestamp
            FROM chatmessages
            WHERE SessionId = @SessionId
            ORDER BY Timestamp DESC
            LIMIT 6";

        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@SessionId", sessionId.ToString());

        using var reader = await cmd.ExecuteReaderAsync();
        var messages = new List<ChatMessage>();

        while (await reader.ReadAsync())
        {
            messages.Add(new ChatMessage
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                SessionId = Guid.Parse(reader.GetString(reader.GetOrdinal("SessionId"))),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                Message = reader.GetString(reader.GetOrdinal("Message")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
            });
        }

        return messages;
    }

    private async Task<string?> GetUserProfileAsync(MySqlConnection connection, int userId)
    {
        try
        {
            var query = @"
                SELECT u.FullName, up.EducationLevel, up.FieldOfStudy, up.Skills, up.Age
                FROM users u
                LEFT JOIN userprofiles up ON u.Id = up.UserId
                WHERE u.Id = @UserId
                LIMIT 1";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var fullName = reader["FullName"]?.ToString() ?? "Not provided";
                var education = reader["EducationLevel"]?.ToString() ?? "Not provided";
                var fieldOfStudy = reader["FieldOfStudy"]?.ToString() ?? "Not provided";
                var skills = reader["Skills"]?.ToString() ?? "[]";
                var age = reader["Age"]?.ToString() ?? "Not provided";

                var profileContext = $@"
USER PROFILE INFORMATION:
- Education Level: {education}
- Field of Study: {fieldOfStudy}
- Skills: {skills}
- Age: {age}

Use this information to personalize career advice. Reference their specific skills and education when relevant.";

                _logger.LogInformation($"📋 User profile loaded for {fullName}");
                _logger.LogInformation($"📋 Education: {education}, Field: {fieldOfStudy}");
                _logger.LogInformation($"📋 Skills: {skills}");
                _logger.LogInformation($"📋 Age: {age}");

                return profileContext;
            }
            
            _logger.LogWarning($"⚠️ No profile found for user {userId}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Could not load user profile: {ex.Message}");
            _logger.LogError($"Profile load error details: {ex.ToString()}");
        }

        return null;
    }
}
