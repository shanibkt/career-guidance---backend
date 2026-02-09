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
    public class NotificationController : ControllerBase
    {
        private readonly HiringNotificationService _notificationService;
        private readonly JobApplicationService _applicationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            HiringNotificationService notificationService,
            JobApplicationService applicationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _applicationService = applicationService;
            _logger = logger;
        }

        private int GetUserId() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

        // ==========================================
        // Student Notifications
        // ==========================================

        /// <summary>
        /// Get all hiring notifications for this student
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            try
            {
                var userId = GetUserId();
                _logger.LogInformation($"GetNotifications called by userId={userId}");
                var notifications = await _notificationService.GetStudentNotificationsAsync(userId);
                _logger.LogInformation($"Returning {notifications.Count} notifications for userId={userId}");
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting notifications: {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, new { message = "Failed to get notifications", error = ex.Message });
            }
        }

        /// <summary>
        /// Get unread notification count
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(new { unreadCount = count });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting unread count: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get unread count", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark a notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                var userId = GetUserId();
                var success = await _notificationService.MarkAsReadAsync(userId, id);
                return success ? Ok(new { message = "Marked as read" }) : NotFound(new { message = "Notification not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error marking notification as read: {ex.Message}");
                return StatusCode(500, new { message = "Failed to mark as read", error = ex.Message });
            }
        }

        // ==========================================
        // Student Applications
        // ==========================================

        /// <summary>
        /// Apply to a hiring notification
        /// </summary>
        [HttpPost("{id}/apply")]
        public async Task<IActionResult> Apply(int id, [FromBody] ApplyToHiringRequest request)
        {
            try
            {
                var userId = GetUserId();

                // Check if already applied
                if (await _applicationService.HasAppliedAsync(userId, id))
                    return BadRequest(new { message = "You have already applied to this position." });

                var application = await _applicationService.ApplyAsync(userId, id, request.CoverMessage);
                return Ok(new { message = "Application submitted successfully!", application });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error applying: {ex.Message}");
                return StatusCode(500, new { message = "Failed to submit application", error = ex.Message });
            }
        }

        /// <summary>
        /// Get student's application history
        /// </summary>
        [HttpGet("my-applications")]
        public async Task<IActionResult> GetMyApplications()
        {
            try
            {
                var userId = GetUserId();
                var applications = await _applicationService.GetMyApplicationsAsync(userId);
                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting applications: {ex.Message}");
                return StatusCode(500, new { message = "Failed to get applications", error = ex.Message });
            }
        }
    }
}
