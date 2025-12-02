using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using MyFirstApi.Models;
using System.Security.Claims;

namespace MyFirstApi.Controllers;

[ApiController]
[Route("api/chat")]
[Authorize]
public class ChatHistoryController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatHistoryController> _logger;

    public ChatHistoryController(IConfiguration configuration, ILogger<ChatHistoryController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }
        return userId;
    }

    /// <summary>
    /// Create or update a chat session
    /// POST /api/chat/sessions
    /// </summary>
    [HttpPost("sessions")]
    public async Task<IActionResult> CreateOrUpdateSession([FromBody] ChatSessionRequest request)
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var checkQuery = "SELECT Id, Title, CreatedAt, UpdatedAt FROM ChatSessions WHERE SessionId = @SessionId AND UserId = @UserId LIMIT 1";
            using var checkCmd = new MySqlCommand(checkQuery, connection);
            checkCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
            checkCmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await checkCmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                // Session exists - update it
                var sessionId = reader.GetInt32(reader.GetOrdinal("Id"));
                var createdAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"));
                await reader.CloseAsync();

                var updateQuery = @"
                    UPDATE ChatSessions 
                    SET Title = @Title, LastMessage = @LastMessage, UpdatedAt = NOW() 
                    WHERE SessionId = @SessionId AND UserId = @UserId";
                
                using var updateCmd = new MySqlCommand(updateQuery, connection);
                updateCmd.Parameters.AddWithValue("@Title", request.Title);
                updateCmd.Parameters.AddWithValue("@LastMessage", request.LastMessage ?? (object)DBNull.Value);
                updateCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
                updateCmd.Parameters.AddWithValue("@UserId", userId);
                await updateCmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    sessionId = request.SessionId,
                    title = request.Title,
                    createdAt = createdAt,
                    updatedAt = DateTime.UtcNow
                });
            }
            else
            {
                // Create new session
                await reader.CloseAsync();

                var insertQuery = @"
                    INSERT INTO ChatSessions (UserId, SessionId, Title, LastMessage, CreatedAt, UpdatedAt) 
                    VALUES (@UserId, @SessionId, @Title, @LastMessage, NOW(), NOW())";
                
                using var insertCmd = new MySqlCommand(insertQuery, connection);
                insertCmd.Parameters.AddWithValue("@UserId", userId);
                insertCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
                insertCmd.Parameters.AddWithValue("@Title", request.Title);
                insertCmd.Parameters.AddWithValue("@LastMessage", request.LastMessage ?? (object)DBNull.Value);
                await insertCmd.ExecuteNonQueryAsync();

                return Ok(new
                {
                    sessionId = request.SessionId,
                    title = request.Title,
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error creating/updating session: {ex.Message}");
            return StatusCode(500, new { error = "Failed to save session" });
        }
    }

    /// <summary>
    /// Save a chat message
    /// POST /api/chat/messages
    /// </summary>
    [HttpPost("messages")]
    public async Task<IActionResult> SaveMessage([FromBody] ChatMessageRequest request)
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Verify session belongs to user
            var verifyQuery = "SELECT Id FROM ChatSessions WHERE SessionId = @SessionId AND UserId = @UserId LIMIT 1";
            using var verifyCmd = new MySqlCommand(verifyQuery, connection);
            verifyCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
            verifyCmd.Parameters.AddWithValue("@UserId", userId);

            var sessionExists = await verifyCmd.ExecuteScalarAsync();
            if (sessionExists == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Insert message
            var role = request.IsUser ? "user" : "assistant";
            var timestamp = request.Timestamp ?? DateTime.UtcNow;

            var insertQuery = @"
                INSERT INTO ChatMessages (SessionId, Role, Message, Timestamp) 
                VALUES (@SessionId, @Role, @Message, @Timestamp);
                SELECT LAST_INSERT_ID();";
            
            using var insertCmd = new MySqlCommand(insertQuery, connection);
            insertCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
            insertCmd.Parameters.AddWithValue("@Role", role);
            insertCmd.Parameters.AddWithValue("@Message", request.Message);
            insertCmd.Parameters.AddWithValue("@Timestamp", timestamp);
            
            var messageId = Convert.ToInt32(await insertCmd.ExecuteScalarAsync());

            // Update session's last message and timestamp
            var truncatedMessage = request.Message.Length > 500 
                ? request.Message.Substring(0, 500) 
                : request.Message;

            var updateQuery = @"
                UPDATE ChatSessions 
                SET LastMessage = @LastMessage, UpdatedAt = NOW() 
                WHERE SessionId = @SessionId";
            
            using var updateCmd = new MySqlCommand(updateQuery, connection);
            updateCmd.Parameters.AddWithValue("@LastMessage", truncatedMessage);
            updateCmd.Parameters.AddWithValue("@SessionId", request.SessionId);
            await updateCmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                id = messageId,
                sessionId = request.SessionId,
                message = request.Message,
                isUser = request.IsUser,
                timestamp = timestamp
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error saving message: {ex.Message}");
            return StatusCode(500, new { error = "Failed to save message" });
        }
    }

    /// <summary>
    /// Get all chat sessions for the user
    /// GET /api/chat/sessions
    /// </summary>
    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    cs.SessionId,
                    cs.Title,
                    cs.LastMessage,
                    cs.CreatedAt,
                    cs.UpdatedAt,
                    (SELECT COUNT(*) FROM ChatMessages WHERE SessionId = cs.SessionId) as MessageCount
                FROM ChatSessions cs
                WHERE cs.UserId = @UserId AND (cs.IsDeleted = 0 OR cs.IsDeleted IS NULL)
                ORDER BY cs.UpdatedAt DESC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            var sessions = new List<SessionListResponse>();

            while (await reader.ReadAsync())
            {
                sessions.Add(new SessionListResponse
                {
                    SessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    LastMessage = reader.IsDBNull(reader.GetOrdinal("LastMessage")) 
                        ? null 
                        : reader.GetString(reader.GetOrdinal("LastMessage")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UpdatedAt = reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
                    MessageCount = reader.GetInt32(reader.GetOrdinal("MessageCount"))
                });
            }

            return Ok(new { sessions });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting sessions: {ex.Message}");
            return StatusCode(500, new { error = "Failed to get sessions" });
        }
    }

    /// <summary>
    /// Get messages for a specific session
    /// GET /api/chat/sessions/{sessionId}/messages
    /// </summary>
    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(string sessionId)
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Verify session belongs to user
            var verifyQuery = "SELECT Id FROM ChatSessions WHERE SessionId = @SessionId AND UserId = @UserId LIMIT 1";
            using var verifyCmd = new MySqlCommand(verifyQuery, connection);
            verifyCmd.Parameters.AddWithValue("@SessionId", sessionId);
            verifyCmd.Parameters.AddWithValue("@UserId", userId);

            var sessionExists = await verifyCmd.ExecuteScalarAsync();
            if (sessionExists == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Get messages
            var query = @"
                SELECT Id, Message, Role, Timestamp 
                FROM ChatMessages 
                WHERE SessionId = @SessionId 
                ORDER BY Timestamp ASC";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@SessionId", sessionId);

            using var reader = await cmd.ExecuteReaderAsync();
            var messages = new List<MessageResponse>();

            while (await reader.ReadAsync())
            {
                messages.Add(new MessageResponse
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Message = reader.GetString(reader.GetOrdinal("Message")),
                    IsUser = reader.GetString(reader.GetOrdinal("Role")) == "user",
                    Timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                });
            }

            return Ok(new { sessionId, messages });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting messages: {ex.Message}");
            return StatusCode(500, new { error = "Failed to get messages" });
        }
    }

    /// <summary>
    /// Delete a specific chat session (soft delete)
    /// DELETE /api/chat/sessions/{sessionId}
    /// </summary>
    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> DeleteSession(string sessionId)
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Verify session belongs to user
            var verifyQuery = "SELECT Id FROM ChatSessions WHERE SessionId = @SessionId AND UserId = @UserId LIMIT 1";
            using var verifyCmd = new MySqlCommand(verifyQuery, connection);
            verifyCmd.Parameters.AddWithValue("@SessionId", sessionId);
            verifyCmd.Parameters.AddWithValue("@UserId", userId);

            var sessionExists = await verifyCmd.ExecuteScalarAsync();
            if (sessionExists == null)
            {
                return NotFound(new { error = "Session not found" });
            }

            // Soft delete
            var deleteQuery = "UPDATE ChatSessions SET IsDeleted = 1 WHERE SessionId = @SessionId";
            using var deleteCmd = new MySqlCommand(deleteQuery, connection);
            deleteCmd.Parameters.AddWithValue("@SessionId", sessionId);
            await deleteCmd.ExecuteNonQueryAsync();

            return Ok(new { message = "Chat session deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error deleting session: {ex.Message}");
            return StatusCode(500, new { error = "Failed to delete session" });
        }
    }

    /// <summary>
    /// Clear all chat history for the user (hard delete)
    /// DELETE /api/chat/sessions
    /// </summary>
    [HttpDelete("sessions")]
    public async Task<IActionResult> ClearAllHistory()
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // Get all session IDs
            var getSessionsQuery = "SELECT SessionId FROM ChatSessions WHERE UserId = @UserId";
            using var getCmd = new MySqlCommand(getSessionsQuery, connection);
            getCmd.Parameters.AddWithValue("@UserId", userId);

            var sessionIds = new List<string>();
            using (var reader = await getCmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    sessionIds.Add(reader.GetString(reader.GetOrdinal("SessionId")));
                }
            }

            if (sessionIds.Count == 0)
            {
                return Ok(new
                {
                    message = "No chat history to clear",
                    deletedSessions = 0,
                    deletedMessages = 0
                });
            }

            // Delete messages
            var deleteMessagesQuery = $"DELETE FROM ChatMessages WHERE SessionId IN ('{string.Join("','", sessionIds)}')";
            using var deleteMessagesCmd = new MySqlCommand(deleteMessagesQuery, connection);
            var deletedMessages = await deleteMessagesCmd.ExecuteNonQueryAsync();

            // Delete sessions
            var deleteSessionsQuery = "DELETE FROM ChatSessions WHERE UserId = @UserId";
            using var deleteSessionsCmd = new MySqlCommand(deleteSessionsQuery, connection);
            deleteSessionsCmd.Parameters.AddWithValue("@UserId", userId);
            var deletedSessions = await deleteSessionsCmd.ExecuteNonQueryAsync();

            return Ok(new
            {
                message = "All chat history cleared",
                deletedSessions = deletedSessions,
                deletedMessages = deletedMessages
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error clearing history: {ex.Message}");
            return StatusCode(500, new { error = "Failed to clear history" });
        }
    }

    /// <summary>
    /// Search chat history
    /// GET /api/chat/search?query=flutter
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchChats([FromQuery] string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return BadRequest(new { error = "Search query is required" });
            }

            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var searchQuery = @"
                SELECT 
                    cm.Id,
                    cm.SessionId,
                    cm.Message,
                    cm.Role,
                    cm.Timestamp,
                    cs.Title
                FROM ChatMessages cm
                INNER JOIN ChatSessions cs ON cm.SessionId = cs.SessionId
                WHERE cs.UserId = @UserId 
                    AND cm.Message LIKE @Query
                    AND (cs.IsDeleted = 0 OR cs.IsDeleted IS NULL)
                ORDER BY cm.Timestamp DESC
                LIMIT 50";

            using var cmd = new MySqlCommand(searchQuery, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);
            cmd.Parameters.AddWithValue("@Query", $"%{query}%");

            using var reader = await cmd.ExecuteReaderAsync();
            var results = new List<object>();

            while (await reader.ReadAsync())
            {
                results.Add(new
                {
                    id = reader.GetInt32(reader.GetOrdinal("Id")),
                    sessionId = reader.GetString(reader.GetOrdinal("SessionId")),
                    sessionTitle = reader.GetString(reader.GetOrdinal("Title")),
                    message = reader.GetString(reader.GetOrdinal("Message")),
                    isUser = reader.GetString(reader.GetOrdinal("Role")) == "user",
                    timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                });
            }

            return Ok(new { query, count = results.Count, results });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error searching chats: {ex.Message}");
            return StatusCode(500, new { error = "Failed to search chats" });
        }
    }

    /// <summary>
    /// Get chat statistics
    /// GET /api/chat/stats
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        try
        {
            var userId = GetUserId();

            using var connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var statsQuery = @"
                SELECT 
                    COUNT(DISTINCT cs.SessionId) as TotalSessions,
                    (SELECT COUNT(*) FROM ChatMessages cm 
                     INNER JOIN ChatSessions s ON cm.SessionId = s.SessionId 
                     WHERE s.UserId = @UserId) as TotalMessages,
                    MIN(cs.CreatedAt) as FirstChatDate
                FROM ChatSessions cs
                WHERE cs.UserId = @UserId AND (cs.IsDeleted = 0 OR cs.IsDeleted IS NULL)";

            using var cmd = new MySqlCommand(statsQuery, connection);
            cmd.Parameters.AddWithValue("@UserId", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                return Ok(new
                {
                    totalSessions = reader.GetInt32(reader.GetOrdinal("TotalSessions")),
                    totalMessages = reader.GetInt32(reader.GetOrdinal("TotalMessages")),
                    firstChatDate = reader.IsDBNull(reader.GetOrdinal("FirstChatDate")) 
                        ? (DateTime?)null 
                        : reader.GetDateTime(reader.GetOrdinal("FirstChatDate"))
                });
            }

            return Ok(new { totalSessions = 0, totalMessages = 0, firstChatDate = (DateTime?)null });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting stats: {ex.Message}");
            return StatusCode(500, new { error = "Failed to get stats" });
        }
    }
}
