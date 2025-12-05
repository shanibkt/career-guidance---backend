using System.Text.Json;
using System.IO;

namespace MyFirstApi.Services
{
    public interface ICrashReportingService
    {
        Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, string>? customData = null);
        Task LogInfoAsync(string message, Dictionary<string, string>? customData = null);
        Task LogWarningAsync(string message, Dictionary<string, string>? customData = null);
    }

    public class LocalCrashReportingService : ICrashReportingService
    {
        private readonly ILogger<LocalCrashReportingService> _logger;
        private readonly string _logDirectory;
        private readonly string _errorLogPath;
        private readonly string _infoLogPath;
        private readonly string _warningLogPath;

        public LocalCrashReportingService(ILogger<LocalCrashReportingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            // Create logs directory
            _logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
            if (!Directory.Exists(_logDirectory))
                Directory.CreateDirectory(_logDirectory);

            // Set log file paths
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            _errorLogPath = Path.Combine(_logDirectory, $"errors-{today}.log");
            _infoLogPath = Path.Combine(_logDirectory, $"info-{today}.log");
            _warningLogPath = Path.Combine(_logDirectory, $"warnings-{today}.log");
        }

        public async Task LogErrorAsync(
            string message,
            Exception? exception = null,
            Dictionary<string, string>? customData = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "ERROR",
                    Message = message,
                    ExceptionType = exception?.GetType().Name,
                    ExceptionMessage = exception?.Message,
                    StackTrace = exception?.StackTrace,
                    CustomData = customData ?? new Dictionary<string, string>()
                };

                // Log to console
                _logger.LogError($"{message} - {exception?.Message}");

                // Log to file
                await WriteToFileAsync(_errorLogPath, logEntry);

                // Also log to console in development
                #if DEBUG
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {message}");
                if (exception != null)
                    Console.WriteLine($"Exception: {exception.Message}\n{exception.StackTrace}");
                Console.ResetColor();
                #endif
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to log error: {ex.Message}");
            }
        }

        public async Task LogInfoAsync(
            string message,
            Dictionary<string, string>? customData = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "INFO",
                    Message = message,
                    CustomData = customData ?? new Dictionary<string, string>()
                };

                _logger.LogInformation(message);
                await WriteToFileAsync(_infoLogPath, logEntry);

                #if DEBUG
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[INFO] {message}");
                Console.ResetColor();
                #endif
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to log info: {ex.Message}");
            }
        }

        public async Task LogWarningAsync(
            string message,
            Dictionary<string, string>? customData = null)
        {
            try
            {
                var logEntry = new LogEntry
                {
                    Timestamp = DateTime.UtcNow,
                    Level = "WARNING",
                    Message = message,
                    CustomData = customData ?? new Dictionary<string, string>()
                };

                _logger.LogWarning(message);
                await WriteToFileAsync(_warningLogPath, logEntry);

                #if DEBUG
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] {message}");
                Console.ResetColor();
                #endif
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to log warning: {ex.Message}");
            }
        }

        private async Task WriteToFileAsync(string filePath, LogEntry logEntry)
        {
            try
            {
                var json = JsonSerializer.Serialize(logEntry, new JsonSerializerOptions { WriteIndented = true });
                
                await File.AppendAllTextAsync(filePath, json + Environment.NewLine + new string('-', 80) + Environment.NewLine);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Get recent errors for dashboard
        /// </summary>
        public async Task<List<LogEntry>> GetRecentErrorsAsync(int count = 50)
        {
            try
            {
                if (!File.Exists(_errorLogPath))
                    return new List<LogEntry>();

                var lines = await File.ReadAllLinesAsync(_errorLogPath);
                var entries = new List<LogEntry>();
                var currentJson = "";

                foreach (var line in lines)
                {
                    if (line.StartsWith("{"))
                        currentJson = line;
                    else if (line.StartsWith("}"))
                    {
                        currentJson += line;
                        try
                        {
                            var entry = JsonSerializer.Deserialize<LogEntry>(currentJson);
                            if (entry != null)
                                entries.Add(entry);
                        }
                        catch { }
                        currentJson = "";
                    }
                    else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("-"))
                        currentJson += line;
                }

                return entries.TakeLast(count).Reverse().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get recent errors: {ex.Message}");
                return new List<LogEntry>();
            }
        }

        /// <summary>
        /// Get log statistics
        /// </summary>
        public async Task<LogStatistics> GetStatisticsAsync()
        {
            try
            {
                var errorCount = File.Exists(_errorLogPath) ? 
                    (await File.ReadAllLinesAsync(_errorLogPath)).Count(l => l.StartsWith("{")) : 0;
                
                var warningCount = File.Exists(_warningLogPath) ? 
                    (await File.ReadAllLinesAsync(_warningLogPath)).Count(l => l.StartsWith("{")) : 0;

                return new LogStatistics
                {
                    TotalErrors = errorCount,
                    TotalWarnings = warningCount,
                    LogDirectory = _logDirectory,
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to get statistics: {ex.Message}");
                return new LogStatistics();
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
        public string? ExceptionType { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? StackTrace { get; set; }
        public Dictionary<string, string> CustomData { get; set; } = new();
    }

    public class LogStatistics
    {
        public int TotalErrors { get; set; }
        public int TotalWarnings { get; set; }
        public string LogDirectory { get; set; } = "";
        public DateTime LastUpdated { get; set; }
    }
}
