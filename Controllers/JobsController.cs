using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using MyFirstApi.Models;
using MyFirstApi.Services;

namespace MyFirstApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class JobsController : ControllerBase
    {
        private readonly JobApiService _jobApiService;
        private readonly JobDatabaseService _jobDatabaseService;
        private readonly IConfiguration _configuration;
        private readonly ICrashReportingService _crashReporting;
        private readonly ILogger<JobsController> _logger;

        public JobsController(
            JobApiService jobApiService,
            JobDatabaseService jobDatabaseService,
            IConfiguration configuration,
            ICrashReportingService crashReporting,
            ILogger<JobsController> logger)
        {
            _jobApiService = jobApiService;
            _jobDatabaseService = jobDatabaseService;
            _configuration = configuration;
            _crashReporting = crashReporting;
            _logger = logger;
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        }

        /// <summary>
        /// Search for jobs with filters
        /// GET: api/jobs/search
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> SearchJobs([FromBody] JobSearchRequest request)
        {
            try
            {
                var userId = GetUserId();
                
                await _crashReporting.LogInfoAsync("Job search initiated", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "query", request.Query ?? "none" },
                    { "location", request.Location ?? "none" }
                });

                if (string.IsNullOrEmpty(request.Query) && string.IsNullOrEmpty(request.Location))
                {
                    await _crashReporting.LogWarningAsync("Job search: Missing query or location");
                    return BadRequest("Query or Location is required");
                }

                var response = await _jobApiService.SearchJobsAsync(request);

                // Mark jobs as saved/applied for this user
                foreach (var job in response.Jobs)
                {
                    job.IsSaved = await _jobDatabaseService.IsJobSavedAsync(userId, job.Id);
                    job.IsApplied = await _jobDatabaseService.IsJobAppliedAsync(userId, job.Id);
                }

                await _crashReporting.LogInfoAsync("Job search completed", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "resultsCount", response.Jobs.Count.ToString() }
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "SearchJobs" },
                    { "query", request?.Query ?? "unknown" },
                    { "location", request?.Location ?? "unknown" }
                };

                await _crashReporting.LogErrorAsync(
                    "Error searching jobs",
                    ex,
                    customData);

                return StatusCode(500, new { message = $"Error searching jobs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get personalized job recommendations
        /// POST: api/jobs/personalized
        /// </summary>
        [HttpPost("personalized")]
        public async Task<IActionResult> GetPersonalizedJobs([FromBody] PersonalizedJobsRequest request)
        {
            try
            {
                var userId = GetUserId();

                await _crashReporting.LogInfoAsync("Personalized jobs requested", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "careerTitle", request.CareerTitle ?? "unknown" },
                    { "skillsCount", request.Skills?.Count.ToString() ?? "0" }
                });

                var jobs = await _jobApiService.GetPersonalizedJobsAsync(
                    request.CareerTitle,
                    request.Skills,
                    _configuration
                );

                // Mark jobs as saved/applied
                foreach (var job in jobs)
                {
                    job.IsSaved = await _jobDatabaseService.IsJobSavedAsync(userId, job.Id);
                    job.IsApplied = await _jobDatabaseService.IsJobAppliedAsync(userId, job.Id);
                }

                await _crashReporting.LogInfoAsync("Personalized jobs generated", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "jobsCount", jobs.Count.ToString() }
                });

                return Ok(new { jobs = jobs });
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "GetPersonalizedJobs" },
                    { "careerTitle", request?.CareerTitle ?? "unknown" }
                };

                await _crashReporting.LogErrorAsync(
                    "Error getting personalized jobs",
                    ex,
                    customData);

                return StatusCode(500, new { message = $"Error getting personalized jobs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Save or unsave a job
        /// POST: api/jobs/{jobId}/save
        /// </summary>
        [HttpPost("{jobId}/save")]
        public async Task<IActionResult> SaveJob(string jobId, [FromBody] SaveJobRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    await _crashReporting.LogWarningAsync("Save job: Unauthorized user");
                    return Unauthorized();
                }

                await _crashReporting.LogInfoAsync(request.Save ? "Job saved" : "Job unsaved", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "jobId", jobId }
                });

                if (request.Save)
                {
                    // Create a minimal job object for saving
                    var job = new JobResponse { Id = jobId };
                    var result = await _jobDatabaseService.SaveJobAsync(userId, job);
                    if (!result)
                    {
                        await _crashReporting.LogErrorAsync("Failed to save job to database", null, 
                            new Dictionary<string, string> { { "jobId", jobId }, { "userId", userId.ToString() } });
                        return StatusCode(500, new { message = "Failed to save job" });
                    }
                }
                else
                {
                    var result = await _jobDatabaseService.RemoveSavedJobAsync(userId, jobId);
                    if (!result)
                    {
                        await _crashReporting.LogErrorAsync("Failed to remove saved job", null,
                            new Dictionary<string, string> { { "jobId", jobId }, { "userId", userId.ToString() } });
                        return StatusCode(500, new { message = "Failed to remove saved job" });
                    }
                }

                return Ok(new { message = request.Save ? "Job saved successfully" : "Job removed from saved" });
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "SaveJob" },
                    { "jobId", jobId },
                    { "action", request?.Save == true ? "save" : "unsave" }
                };

                await _crashReporting.LogErrorAsync("Error saving job", ex, customData);

                return StatusCode(500, new { message = $"Error saving job: {ex.Message}" });
            }
        }

        /// <summary>
        /// Apply for a job
        /// POST: api/jobs/{jobId}/apply
        /// </summary>
        [HttpPost("{jobId}/apply")]
        public async Task<IActionResult> ApplyForJob(string jobId, [FromBody] JobApplication? application = null)
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    await _crashReporting.LogWarningAsync("Apply for job: Unauthorized user");
                    return Unauthorized();
                }

                await _crashReporting.LogInfoAsync("Job application submitted", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "jobId", jobId },
                    { "hasNotes", (!string.IsNullOrEmpty(application?.Notes)).ToString() }
                });

                var job = new JobResponse { Id = jobId };
                var result = await _jobDatabaseService.ApplyForJobAsync(
                    userId,
                    jobId,
                    job,
                    application?.Notes
                );

                if (!result)
                {
                    await _crashReporting.LogErrorAsync("Failed to apply for job", null,
                        new Dictionary<string, string> { { "jobId", jobId }, { "userId", userId.ToString() } });
                    return StatusCode(500, new { message = "Failed to apply for job" });
                }

                return Ok(new { message = "Application submitted successfully" });
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "ApplyForJob" },
                    { "jobId", jobId }
                };

                await _crashReporting.LogErrorAsync("Error applying for job", ex, customData);

                return StatusCode(500, new { message = $"Error applying for job: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get saved jobs for current user
        /// GET: api/jobs/saved
        /// </summary>
        [HttpGet("saved")]
        public async Task<IActionResult> GetSavedJobs()
        {
            try
            {
                var userId = GetUserId();
                if (userId == 0)
                {
                    await _crashReporting.LogWarningAsync("Get saved jobs: Unauthorized user");
                    return Unauthorized();
                }

                var jobs = await _jobDatabaseService.GetSavedJobsAsync(userId);

                await _crashReporting.LogInfoAsync("Saved jobs retrieved", new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "jobsCount", jobs.Count.ToString() }
                });

                return Ok(new { jobs = jobs });
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "GetSavedJobs" }
                };

                await _crashReporting.LogErrorAsync("Error getting saved jobs", ex, customData);

                return StatusCode(500, new { message = $"Error getting saved jobs: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get job details
        /// GET: api/jobs/{jobId}
        /// </summary>
        [HttpGet("{jobId}")]
        public async Task<IActionResult> GetJobDetails(string jobId)
        {
            try
            {
                var userId = GetUserId();
                var jobs = await _jobDatabaseService.GetSavedJobsAsync(userId);
                var job = jobs.FirstOrDefault(j => j.Id == jobId);

                if (job == null)
                {
                    await _crashReporting.LogWarningAsync("Job not found", new Dictionary<string, string>
                    {
                        { "userId", userId.ToString() },
                        { "jobId", jobId }
                    });
                    return NotFound(new { message = "Job not found" });
                }

                return Ok(job);
            }
            catch (Exception ex)
            {
                var customData = new Dictionary<string, string>
                {
                    { "userId", GetUserId().ToString() },
                    { "endpoint", "GetJobDetails" },
                    { "jobId", jobId }
                };

                await _crashReporting.LogErrorAsync("Error getting job details", ex, customData);

                return StatusCode(500, new { message = $"Error getting job details: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get filter metadata (job types, experience levels, etc.)
        /// GET: api/jobs/filters/metadata
        /// </summary>
        [HttpGet("filters/metadata")]
        [AllowAnonymous]
        public IActionResult GetFilterMetadata()
        {
            try
            {
                var metadata = new JobFilterMetadata();
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error getting filter metadata: {ex.Message}" });
            }
        }
    }
}
