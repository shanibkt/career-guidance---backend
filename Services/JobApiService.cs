using MySql.Data.MySqlClient;
using System.Text.Json;
using MyFirstApi.Models;

namespace MyFirstApi.Services
{
    public class JobApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _jsearchApiKey = "c7176de2d9mshfd38021e3ce01a3p14702ejsn8dff493f4d86";
        private readonly string _jsearchHost = "jsearch.p.rapidapi.com";
        private const string JSEARCH_BASE_URL = "https://jsearch.p.rapidapi.com";

        public JobApiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        /// <summary>
        /// Search jobs using JSearch API with filters
        /// </summary>
        public async Task<JobSearchResponse> SearchJobsAsync(JobSearchRequest request)
        {
            try
            {
                // Build query string for JSearch API
                var queryBuilder = new System.Text.StringBuilder();
                queryBuilder.Append("search?");

                if (!string.IsNullOrEmpty(request.Query))
                {
                    queryBuilder.Append($"query={Uri.EscapeDataString(request.Query)}");
                }
                else if (!string.IsNullOrEmpty(request.Location))
                {
                    queryBuilder.Append($"query={Uri.EscapeDataString($"{request.JobType ?? "jobs"} in {request.Location}")}");
                }

                if (!string.IsNullOrEmpty(request.Location))
                {
                    queryBuilder.Append($"&locations={Uri.EscapeDataString(request.Location)}");
                }

                queryBuilder.Append($"&page={request.Page}");
                queryBuilder.Append($"&num_pages=1");
                queryBuilder.Append($"&country={request.Country ?? "us"}");
                queryBuilder.Append($"&date_posted={request.DatePosted ?? "all"}");

                if (!string.IsNullOrEmpty(request.JobType))
                {
                    queryBuilder.Append($"&employment_type={Uri.EscapeDataString(request.JobType.ToUpper())}");
                }

                if (!string.IsNullOrEmpty(request.ExperienceLevel))
                {
                    queryBuilder.Append($"&experience_level={Uri.EscapeDataString(request.ExperienceLevel.ToLower())}");
                }

                var requestUri = new Uri($"{JSEARCH_BASE_URL}/{queryBuilder}");

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUri)
                {
                    Headers =
                    {
                        { "x-rapidapi-key", _jsearchApiKey },
                        { "x-rapidapi-host", _jsearchHost },
                    }
                };

                using var response = await _httpClient.SendAsync(httpRequest);

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"JSearch API error: {response.StatusCode}");
                }

                var content = await response.Content.ReadAsStringAsync();
                var jsonDocument = JsonDocument.Parse(content);
                var root = jsonDocument.RootElement;

                var jobs = new List<JobResponse>();
                if (root.TryGetProperty("data", out var jobsData))
                {
                    foreach (var job in jobsData.EnumerateArray())
                    {
                        jobs.Add(ParseJobFromJSearch(job));
                    }
                }

                var totalResults = jobs.Count;
                var totalPages = (int)Math.Ceiling((double)totalResults / request.PageSize);

                return new JobSearchResponse
                {
                    Jobs = jobs.Take(request.PageSize).ToList(),
                    TotalResults = totalResults,
                    CurrentPage = request.Page,
                    TotalPages = totalPages,
                    HasNextPage = request.Page < totalPages
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error searching jobs: {ex.Message}");
            }
        }

        /// <summary>
        /// Get personalized job recommendations based on career and skills
        /// </summary>
        public async Task<List<JobResponse>> GetPersonalizedJobsAsync(
            string? careerTitle,
            List<string>? skills,
            IConfiguration configuration)
        {
            try
            {
                // Build search query from career and skills
                var searchQuery = careerTitle;
                if (skills?.Count > 0)
                {
                    searchQuery += $" {string.Join(" ", skills.Take(3))}";
                }

                var request = new JobSearchRequest
                {
                    Query = searchQuery,
                    PageSize = 15
                };

                var response = await SearchJobsAsync(request);

                // Calculate match percentage based on skills match
                foreach (var job in response.Jobs)
                {
                    job.MatchPercentage = CalculateSkillsMatch(job, skills);
                }

                // Sort by match percentage
                response.Jobs = response.Jobs.OrderByDescending(j => j.MatchPercentage).ToList();

                return response.Jobs;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error getting personalized jobs: {ex.Message}");
            }
        }

        private JobResponse ParseJobFromJSearch(JsonElement jobElement)
        {
            var job = new JobResponse();

            if (jobElement.TryGetProperty("job_id", out var jobId))
                job.Id = jobId.GetString() ?? string.Empty;

            if (jobElement.TryGetProperty("job_title", out var title))
                job.Title = title.GetString() ?? string.Empty;

            if (jobElement.TryGetProperty("employer_name", out var company))
                job.Company = company.GetString() ?? string.Empty;

            if (jobElement.TryGetProperty("job_city", out var city) && jobElement.TryGetProperty("job_country", out var country))
            {
                var cityStr = city.GetString() ?? "";
                var countryStr = country.GetString() ?? "";
                job.Location = $"{cityStr}, {countryStr}".TrimStart(',').Trim();
            }

            if (jobElement.TryGetProperty("job_apply_link", out var url))
                job.Url = url.GetString();

            if (jobElement.TryGetProperty("job_description", out var desc))
                job.Description = desc.GetString();

            if (jobElement.TryGetProperty("job_employment_type", out var empType))
                job.JobType = empType.GetString();

            if (jobElement.TryGetProperty("job_min_salary", out var minSalary) && minSalary.ValueKind != JsonValueKind.Null)
                job.SalaryMin = minSalary.ToString();

            if (jobElement.TryGetProperty("job_max_salary", out var maxSalary) && maxSalary.ValueKind != JsonValueKind.Null)
                job.SalaryMax = maxSalary.ToString();

            if (jobElement.TryGetProperty("job_posted_at_datetime_utc", out var posted))
                job.PostedDate = posted.GetString();

            job.SalaryCurrency = "USD";

            return job;
        }

        private double CalculateSkillsMatch(JobResponse job, List<string>? userSkills)
        {
            if (userSkills == null || userSkills.Count == 0)
                return 50.0; // Default match if no skills

            var description = (job.Description ?? "").ToLower();
            var matchedSkills = 0;

            foreach (var skill in userSkills)
            {
                if (description.Contains(skill.ToLower()))
                {
                    matchedSkills++;
                }
            }

            return (double)(matchedSkills * 100) / userSkills.Count;
        }
    }
}
