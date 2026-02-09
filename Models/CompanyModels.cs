using System.Text.Json.Serialization;

namespace MyFirstApi.Models
{
    // ==========================================
    // Company Models
    // ==========================================

    public class Company
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Industry { get; set; }
        public string? LogoUrl { get; set; }
        public string? Website { get; set; }
        public string? Location { get; set; }
        public string? ContactEmail { get; set; }
        public bool IsApproved { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CompanyUser
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string Role { get; set; } = "owner";
        public DateTime CreatedAt { get; set; }
    }

    // ==========================================
    // Hiring Notification Models
    // ==========================================

    public class HiringNotification
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyLogo { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? SalaryRange { get; set; }
        public string? Requirements { get; set; }
        public List<int>? TargetCareerIds { get; set; }
        public string? ApplicationDeadline { get; set; }
        public bool IsActive { get; set; } = true;
        public int ApplicationCount { get; set; }
        public int TargetStudentCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class StudentNotificationItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int HiringNotificationId { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }

        // Joined data from hiring_notifications + companies
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Position { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? SalaryRange { get; set; }
        public string? Requirements { get; set; }
        public string? ApplicationDeadline { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? CompanyLogo { get; set; }
        public string? CompanyWebsite { get; set; }
        public bool HasApplied { get; set; }
    }

    // ==========================================
    // Job Application Models
    // ==========================================

    public class CompanyJobApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int HiringNotificationId { get; set; }
        public int CompanyId { get; set; }
        public string? CoverMessage { get; set; }
        public string? ResumeData { get; set; }
        public string Status { get; set; } = "pending";
        public DateTime? AppliedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Joined student data
        public string? StudentName { get; set; }
        public string? StudentEmail { get; set; }
        public string? StudentCareer { get; set; }
        public string? NotificationTitle { get; set; }
        public string? Position { get; set; }
        public string? CompanyName { get; set; }
    }

    // ==========================================
    // Request DTOs
    // ==========================================

    public class CompanyRegisterRequest
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("contactEmail")]
        public string? ContactEmail { get; set; }
    }

    public class CreateHiringNotificationRequest
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("position")]
        public string Position { get; set; } = string.Empty;

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("salaryRange")]
        public string? SalaryRange { get; set; }

        [JsonPropertyName("requirements")]
        public string? Requirements { get; set; }

        [JsonPropertyName("targetCareerIds")]
        public List<int> TargetCareerIds { get; set; } = new();

        [JsonPropertyName("applicationDeadline")]
        public string? ApplicationDeadline { get; set; }
    }

    public class ApplyToHiringRequest
    {
        [JsonPropertyName("coverMessage")]
        public string? CoverMessage { get; set; }
    }

    public class UpdateApplicationStatusRequest
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // pending, reviewed, shortlisted, rejected
    }

    public class UpdateCompanyRequest
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("industry")]
        public string? Industry { get; set; }

        [JsonPropertyName("website")]
        public string? Website { get; set; }

        [JsonPropertyName("location")]
        public string? Location { get; set; }

        [JsonPropertyName("logoUrl")]
        public string? LogoUrl { get; set; }

        [JsonPropertyName("contactEmail")]
        public string? ContactEmail { get; set; }
    }

    // ==========================================
    // Response DTOs
    // ==========================================

    public class CareerStudentCount
    {
        public int CareerId { get; set; }
        public string CareerName { get; set; } = string.Empty;
        public int StudentCount { get; set; }
    }

    public class CompanyDashboardStats
    {
        public int TotalPostings { get; set; }
        public int ActivePostings { get; set; }
        public int TotalApplications { get; set; }
        public int PendingApplications { get; set; }
        public int ShortlistedApplications { get; set; }
    }
}
