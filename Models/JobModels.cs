namespace MyFirstApi.Models
{
    // Job search filter request
    public class JobSearchRequest
    {
        public string? Query { get; set; }
        public string? Location { get; set; }
        public string? JobType { get; set; } // Full-time, Part-time, Contract
        public string? ExperienceLevel { get; set; } // Entry, Mid, Senior
        public string? SalaryMin { get; set; }
        public string? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; } = "USD";
        public string? Country { get; set; }
        public string? DatePosted { get; set; } = "all"; // all, 7, 30, 90
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<string>? Skills { get; set; }
    }

    // Job search response
    public class JobSearchResponse
    {
        public List<JobResponse> Jobs { get; set; } = new List<JobResponse>();
        public int TotalResults { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
    }

    // Individual job response
    public class JobResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Description { get; set; }
        public string? JobType { get; set; }
        public string? SalaryMin { get; set; }
        public string? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }
        public string? ExperienceLevel { get; set; }
        public List<string>? RequiredSkills { get; set; }
        public string? PostedDate { get; set; }
        public string? JobRole { get; set; }
        public string? EmploymentType { get; set; }
        public bool IsSaved { get; set; }
        public bool IsApplied { get; set; }
        public double? MatchPercentage { get; set; }
    }

    // Saved job model for database
    public class SavedJob
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string? Description { get; set; }
        public string? JobType { get; set; }
        public string? SalaryMin { get; set; }
        public string? SalaryMax { get; set; }
        public string? SalaryCurrency { get; set; }
        public string? ExperienceLevel { get; set; }
        public string? RequiredSkills { get; set; } // JSON array
        public string? PostedDate { get; set; }
        public DateTime SavedAt { get; set; } = DateTime.UtcNow;
    }

    // Job application model
    public class JobApplication
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string JobId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string? CoverLetter { get; set; }
        public string ApplicationStatus { get; set; } = "Applied"; // Applied, In Review, Rejected, Accepted
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    // Personalized job request
    public class PersonalizedJobsRequest
    {
        public string? CareerTitle { get; set; }
        public List<string>? Skills { get; set; }
    }

    // Save job request
    public class SaveJobRequest
    {
        public bool Save { get; set; }
    }

    // Job filter metadata for frontend
    public class JobFilterMetadata
    {
        public List<string> JobTypes { get; set; } = new List<string>
        {
            "Full-time", "Part-time", "Contract", "Temporary", "Freelance"
        };
        public List<string> ExperienceLevels { get; set; } = new List<string>
        {
            "Entry", "Mid", "Senior", "Executive"
        };
        public List<string> Countries { get; set; } = new List<string>
        {
            "us", "uk", "ca", "au", "in", "de", "fr", "sg", "jp", "ae"
        };
        public List<string> DatePostedOptions { get; set; } = new List<string>
        {
            "all", "7", "30", "90"
        };
    }
}
