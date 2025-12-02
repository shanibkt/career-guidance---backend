using System.Text.Json.Serialization;

namespace MyFirstApi.Models;

public class ChatRequest
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public Guid? SessionId { get; set; }
}

public class ChatResponse
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;

    [JsonPropertyName("sessionId")]
    public Guid? SessionId { get; set; }
}

public class ChatMessage
{
    public int Id { get; set; }
    public Guid SessionId { get; set; }
    public string Role { get; set; } = string.Empty; // "user" or "assistant"
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ChatSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public Guid SessionId { get; set; }
    public string Title { get; set; } = "New Conversation";
    public string? LastMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
}

// Request DTOs for chat history
public class ChatSessionRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("lastMessage")]
    public string? LastMessage { get; set; }
}

public class ChatMessageRequest
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("isUser")]
    public bool IsUser { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime? Timestamp { get; set; }
}

// Response DTOs
public class SessionListResponse
{
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("lastMessage")]
    public string? LastMessage { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("messageCount")]
    public int MessageCount { get; set; }
}

public class MessageResponse
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("isUser")]
    public bool IsUser { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
}

// Groq API Response Models
public class GroqChatResponse
{
    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; set; } = new();
}

public class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqMessageContent Message { get; set; } = new();
}

public class GroqMessageContent
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
