namespace MyFirstApi.Models
{
    // Registration request
    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public int? Age { get; set; }
        public string? Dob { get; set; }
        public string? Gender { get; set; }
    }

    // Login request
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // User response (no password)
    public class UserResponse
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Login response with JWT
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime TokenExpiration { get; set; }
        public UserResponse User { get; set; } = new UserResponse();
    }

    // Refresh token request
    public class RefreshTokenRequest
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    // Update user DTO
    public class UpdateUserDto
    {
        public string FullName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    // Update profile DTO
    public class UpdateProfileDto
    {
        public int UserId { get; set; }
        public string? PhoneNumber { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public string? EducationLevel { get; set; }
        public string? FieldOfStudy { get; set; }
        public List<string>? Skills { get; set; }
    }
}
