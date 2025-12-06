using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MyFirstApi.Filters
{
    /// <summary>
    /// Global exception handler for all API endpoints
    /// Provides consistent error responses
    /// </summary>
    public class GlobalExceptionFilter : IExceptionFilter
    {
        private readonly ILogger<GlobalExceptionFilter> _logger;

        public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger)
        {
            _logger = logger;
        }

        public void OnException(ExceptionContext context)
        {
            _logger.LogError(context.Exception, "Unhandled exception occurred");

            var response = new
            {
                error = "An error occurred processing your request",
                message = context.Exception.Message,
                type = context.Exception.GetType().Name,
                timestamp = DateTime.UtcNow
            };

            context.Result = new ObjectResult(response)
            {
                StatusCode = 500
            };

            context.ExceptionHandled = true;
        }
    }
}
