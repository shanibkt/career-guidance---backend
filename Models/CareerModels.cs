namespace MyFirstApi.Models
{
    using System.Text.Json.Serialization;

    // Career Models
    public class Career
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? RequiredEducation { get; set; }
        public string? AverageSalary { get; set; }
        public string? GrowthOutlook { get; set; }
        public List<string>? KeySkills { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // Quiz Models
    public class QuizQuestion
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("question")]
        public string Question { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "multiple_choice" or "open_ended"
        
        [JsonPropertyName("options")]
        public List<string>? Options { get; set; }

        [JsonPropertyName("skill_category")]
        public string SkillCategory { get; set; } = string.Empty;

        [JsonPropertyName("correct_answer")]
        public string CorrectAnswer { get; set; } = string.Empty;
    }

    public class QuizSession
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
        public List<QuizAnswer>? Answers { get; set; }
        public bool Completed { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class QuizAnswer
    {
        [JsonPropertyName("question_id")]
        public int QuestionId { get; set; }

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }

    public class SkillScore
    {
        [JsonPropertyName("skill")]
        public string Skill { get; set; } = string.Empty;

        [JsonPropertyName("correct")]
        public int Correct { get; set; }

        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }
    }

    public class CareerMatch
    {
        [JsonPropertyName("career_id")]
        public int CareerId { get; set; }

        [JsonPropertyName("career_name")]
        public string CareerName { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("match_percentage")]
        public decimal MatchPercentage { get; set; }

        [JsonPropertyName("matching_skills")]
        public List<string> MatchingSkills { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("salary_range")]
        public string SalaryRange { get; set; } = string.Empty;
    }

    // Recommendation Models
    public class Recommendation
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CareerId { get; set; }
        public string? CareerName { get; set; }
        public decimal MatchPercentage { get; set; }
        public string? Reasoning { get; set; }
        public List<string>? Strengths { get; set; }
        public List<string>? AreasToDevelop { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // DTOs
    public class GenerateQuizResponse
    {
        [JsonPropertyName("quiz_id")]
        public string QuizId { get; set; } = string.Empty;

        [JsonPropertyName("questions")]
        public List<QuizQuestion> Questions { get; set; } = new();
    }

    public class SubmitQuizRequest
    {
        [JsonPropertyName("quiz_id")]
        public string QuizId { get; set; } = string.Empty;

        [JsonPropertyName("answers")]
        public List<QuizAnswer> Answers { get; set; } = new();
    }

    public class TranscriptQuizRequest
    {
        [JsonPropertyName("transcript")]
        public string Transcript { get; set; } = string.Empty;

        [JsonPropertyName("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("video_title")]
        public string? VideoTitle { get; set; }
    }

    public class SkillQuizRequest
    {
        [JsonPropertyName("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("video_title")]
        public string? VideoTitle { get; set; }
    }

    public class VideoQuizRequest
    {
        [JsonPropertyName("video_id")]
        public string VideoId { get; set; } = string.Empty;

        [JsonPropertyName("skill_name")]
        public string SkillName { get; set; } = string.Empty;

        [JsonPropertyName("video_title")]
        public string? VideoTitle { get; set; }
    }

    public class SubmitQuizResponse
    {
        [JsonPropertyName("quiz_id")]
        public string QuizId { get; set; } = string.Empty;

        [JsonPropertyName("total_score")]
        public int TotalScore { get; set; }

        [JsonPropertyName("total_questions")]
        public int TotalQuestions { get; set; }

        [JsonPropertyName("percentage")]
        public decimal Percentage { get; set; }

        [JsonPropertyName("skill_breakdown")]
        public List<SkillScore> SkillBreakdown { get; set; } = new();

        [JsonPropertyName("career_matches")]
        public List<CareerMatch> CareerMatches { get; set; } = new();
    }

    public class GenerateRecommendationsRequest
    {
        public int SessionId { get; set; }
    }

    public class RecommendationsResponse
    {
        public List<Recommendation> Recommendations { get; set; } = new();
    }
}
