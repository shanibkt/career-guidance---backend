using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyFirstApi.Models;
using MyFirstApi.Services;
using System.Security.Claims;

namespace MyFirstApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CompanyController : ControllerBase
    {
        private readonly CompanyService _companyService;
        private readonly HiringNotificationService _notificationService;
        private readonly JobApplicationService _applicationService;
        private readonly ILogger<CompanyController> _logger;

        public CompanyController(
            CompanyService companyService,
            HiringNotificationService notificationService,
            JobApplicationService applicationService,
            ILogger<CompanyController> logger)
        {
            _companyService = companyService;
            _notificationService = notificationService;
            _applicationService = applicationService;
            _logger = logger;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        private async Task<(Company? company, IActionResult? error)> GetCompanyOrError()
        {
            var company = await _companyService.GetCompanyByUserIdAsync(GetUserId());
            if (company == null)
                return (null, NotFound(new { message = "No company found for this user. Register a company first." }));
            return (company, null);
        }

        // ==========================================
        // Company Registration & Profile
        // ==========================================

        /// <summary>
        /// Register a new company (any authenticated user)
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegisterCompany([FromBody] CompanyRegisterRequest request)
        {
            try
            {
                var userId = GetUserId();

                // Check if user already has a company
                var existing = await _companyService.GetCompanyByUserIdAsync(userId);
                if (existing != null)
                    return BadRequest(new { message = "You already have a registered company." });

                var company = await _companyService.RegisterCompanyAsync(userId, request);
                return Ok(new { message = "Company registered successfully. Pending admin approval.", company });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering company: {ex.Message}");
                return StatusCode(500, new { message = "Failed to register company", error = ex.Message });
            }
        }

        /// <summary>
        /// Get company profile for logged-in company user
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;
                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting company profile: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get profile", error = ex.Message });
            }
        }

        /// <summary>
        /// Update company profile
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateCompanyRequest request)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var success = await _companyService.UpdateCompanyAsync(company!.Id, request);
                return success ? Ok(new { message = "Company profile updated" }) : StatusCode(500, new { message = "Failed to update" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating company profile: {ex.Message}");
                return StatusCode(500, new { message = "Failed to update profile", error = ex.Message });
            }
        }

        /// <summary>
        /// Get company dashboard stats
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var stats = await _companyService.GetDashboardStatsAsync(company!.Id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting company stats: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get stats", error = ex.Message });
            }
        }

        // ==========================================
        // Career Stats (for targeting)
        // ==========================================

        /// <summary>
        /// Get careers with student counts (for targeting hiring notifications)
        /// </summary>
        [HttpGet("career-stats")]
        public async Task<IActionResult> GetCareerStats()
        {
            try
            {
                var stats = await _notificationService.GetCareerStudentCountsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting career stats: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get career stats", error = ex.Message });
            }
        }

        // ==========================================
        // Hiring Notifications
        // ==========================================

        /// <summary>
        /// Create a new hiring notification (company must be approved)
        /// </summary>
        [HttpPost("hiring")]
        public async Task<IActionResult> CreateHiringNotification([FromBody] CreateHiringNotificationRequest request)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                if (!company!.IsApproved)
                    return BadRequest(new { message = "Your company is not yet approved. Please wait for admin approval." });

                if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Position))
                    return BadRequest(new { message = "Title and position are required." });

                if (!request.TargetCareerIds.Any())
                    return BadRequest(new { message = "At least one target career must be selected." });

                var notification = await _notificationService.CreateHiringNotificationAsync(company.Id, request);
                return Ok(new { message = "Hiring notification created and sent to targeted students.", notification });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating hiring notification: {ex.Message}");
                return StatusCode(500, new { message = "Failed to create notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all hiring notifications for this company
        /// </summary>
        [HttpGet("hiring")]
        public async Task<IActionResult> GetHiringNotifications()
        {
            try
            {
                var userId = GetUserId();
                _logger.LogInformation($"GetHiringNotifications called by userId={userId}");
                
                var (company, error) = await GetCompanyOrError();
                if (error != null)
                {
                    _logger.LogWarning($"GetCompanyOrError failed for userId={userId}");
                    return error;
                }

                _logger.LogInformation($"Fetching notifications for companyId={company!.Id}");
                var notifications = await _notificationService.GetCompanyNotificationsAsync(company!.Id);
                _logger.LogInformation($"Found {notifications.Count} notifications for companyId={company.Id}");
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting hiring notifications: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { message = "Failed to get notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Update a hiring notification
        /// </summary>
        [HttpPut("hiring/{id}")]
        public async Task<IActionResult> UpdateHiringNotification(int id, [FromBody] CreateHiringNotificationRequest request)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var success = await _notificationService.UpdateHiringNotificationAsync(id, company!.Id, request);
                return success ? Ok(new { message = "Notification updated" }) : NotFound(new { message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating notification: {ex.Message}");
                return StatusCode(500, new { message = "Failed to update notification", error = ex.Message });
            }
        }

        /// <summary>
        /// Deactivate a hiring notification
        /// </summary>
        [HttpDelete("hiring/{id}")]
        public async Task<IActionResult> DeactivateHiringNotification(int id)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var success = await _notificationService.DeactivateNotificationAsync(id, company!.Id);
                return success ? Ok(new { message = "Notification deactivated" }) : NotFound(new { message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating notification: {ex.Message}");
                return StatusCode(500, new { message = "Failed to deactivate notification", error = ex.Message });
            }
        }

        // ==========================================
        // Applications Management
        // ==========================================

        /// <summary>
        /// Get all applications for this company
        /// </summary>
        [HttpGet("applications")]
        public async Task<IActionResult> GetApplications([FromQuery] int? notificationId)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var applications = await _applicationService.GetApplicationsForCompanyAsync(company!.Id, notificationId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting applications: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get applications", error = ex.Message });
            }
        }

        /// <summary>
        /// Update application status (pending/reviewed/shortlisted/rejected)
        /// </summary>
        [HttpPut("applications/{applicationId}/status")]
        public async Task<IActionResult> UpdateApplicationStatus(int applicationId, [FromBody] UpdateApplicationStatusRequest request)
        {
            try
            {
                var (company, error) = await GetCompanyOrError();
                if (error != null) return error;

                var success = await _applicationService.UpdateApplicationStatusAsync(applicationId, company!.Id, request.Status);
                return success ? Ok(new { message = "Application status updated" }) : NotFound(new { message = "Application not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating application status: {ex.Message}");
                return StatusCode(500, new { message = "Failed to update status", error = ex.Message });
            }
        }
    }
}
