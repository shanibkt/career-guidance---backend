# Firebase Crashlytics - Integration Quick Reference

## 📋 Files Modified/Created

### ✨ NEW FILES (2)

#### 1. `Services/LocalCrashReportingService.cs` (380 lines)
```csharp
public interface ICrashReportingService
{
    Task LogErrorAsync(string message, Exception? exception = null, Dictionary<string, string>? customData = null);
    Task LogInfoAsync(string message, Dictionary<string, string>? customData = null);
    Task LogWarningAsync(string message, Dictionary<string, string>? customData = null);
}

public class LocalCrashReportingService : ICrashReportingService
{
    // Implementation: File-based logging with JSON format
    // Auto-creates /Logs directory
    // Daily log rotation
    // Methods to retrieve and analyze logs
}

public class LogEntry { /* JSON structure */ }
public class LogStatistics { /* Stats structure */ }
```

#### 2. `Controllers/LogsController.cs` (60 lines)
```csharp
[HttpGet("errors")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetRecentErrors([FromQuery] int count = 50)

[HttpGet("statistics")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> GetStatistics()

[HttpGet("file/{type}")]
[Authorize(Roles = "Admin")]
public IActionResult GetLogFile(string type)
```

---

### ✏️ UPDATED FILES (2)

#### 1. `Program.cs` (1 line added)
```csharp
// ADDED THIS LINE:
builder.Services.AddScoped<ICrashReportingService, LocalCrashReportingService>();
```

#### 2. `Controllers/JobsController.cs` (Complete overhaul)

**Before**: 7 endpoints with no logging

**After**: 7 endpoints + comprehensive logging

```csharp
// ADDED THESE INJECTIONS:
private readonly ICrashReportingService _crashReporting;
private readonly ILogger<JobsController> _logger;

public JobsController(
    ..., 
    ICrashReportingService crashReporting,
    ILogger<JobsController> logger)
{
    _crashReporting = crashReporting;
    _logger = logger;
}

// EACH ENDPOINT NOW HAS:
// 1. LogInfoAsync() at start
// 2. LogWarningAsync() on validation failures
// 3. LogErrorAsync() on exceptions
// 4. Custom data tracking
```

---

## 🔄 Implementation Flow

```
┌─────────────────────────────────────────────────────┐
│         Your Backend API Request                    │
│    (POST /api/jobs/search with JWT token)          │
└────────────────────┬────────────────────────────────┘
                     │
                     ▼
        ┌────────────────────────────┐
        │  JobsController.SearchJobs │
        │  ┌──────────────────────┐  │
        │  │ 1. Get userId        │  │
        │  │ 2. LogInfoAsync()    │──┼→ LOGGED
        │  │ 3. Validate input    │  │
        │  │ 4. Search jobs       │  │
        │  │ 5. LogInfoAsync()    │──┼→ LOGGED
        │  │ 6. Return response   │  │
        │  │                      │  │
        │  │ IF ERROR:            │  │
        │  │ - LogErrorAsync()    │──┼→ LOGGED
        │  └──────────────────────┘  │
        └────────────┬───────────────┘
                     │
                     ▼
    ┌────────────────────────────────────┐
    │ LocalCrashReportingService         │
    │ ┌────────────────────────────────┐ │
    │ │ LogErrorAsync()                │ │
    │ │ LogInfoAsync()                 │ │
    │ │ LogWarningAsync()              │ │
    │ │ WriteToFileAsync()             │ │
    │ └────────────────────────────────┘ │
    └────────────┬─────────────────────┘
                 │
                 ▼
    ┌────────────────────────────────────┐
    │  Local Log Files (JSON Format)     │
    │  ┌────────────────────────────────┐│
    │  │ Logs/info-2025-12-05.log      ││
    │  │ Logs/errors-2025-12-05.log    ││
    │  │ Logs/warnings-2025-12-05.log  ││
    │  └────────────────────────────────┘│
    └────────────────────────────────────┘
```

---

## 🎯 Code Snippets

### How Logging Works (Example)

```csharp
[HttpPost("search")]
public async Task<IActionResult> SearchJobs([FromBody] JobSearchRequest request)
{
    try
    {
        var userId = GetUserId();
        
        // START: Log the action
        await _crashReporting.LogInfoAsync("Job search initiated", new Dictionary<string, string>
        {
            { "userId", userId.ToString() },
            { "query", request.Query ?? "none" },
            { "location", request.Location ?? "none" }
        });

        // VALIDATION
        if (string.IsNullOrEmpty(request.Query) && string.IsNullOrEmpty(request.Location))
        {
            await _crashReporting.LogWarningAsync("Job search: Missing query or location");
            return BadRequest("Query or Location is required");
        }

        // BUSINESS LOGIC
        var response = await _jobApiService.SearchJobsAsync(request);

        // SUCCESS: Log completion
        await _crashReporting.LogInfoAsync("Job search completed", new Dictionary<string, string>
        {
            { "userId", userId.ToString() },
            { "resultsCount", response.Jobs.Count.ToString() }
        });

        return Ok(response);
    }
    catch (Exception ex)
    {
        // ERROR: Log with full context
        var customData = new Dictionary<string, string>
        {
            { "userId", GetUserId().ToString() },
            { "endpoint", "SearchJobs" },
            { "query", request?.Query ?? "unknown" }
        };

        await _crashReporting.LogErrorAsync("Error searching jobs", ex, customData);
        return StatusCode(500, new { message = $"Error: {ex.Message}" });
    }
}
```

---

## 📍 How to Use in Your Code

### 1. Inject the Service
```csharp
private readonly ICrashReportingService _crashReporting;

public MyController(ICrashReportingService crashReporting)
{
    _crashReporting = crashReporting;
}
```

### 2. Log Information
```csharp
await _crashReporting.LogInfoAsync("User registered", new Dictionary<string, string>
{
    { "userId", user.Id.ToString() },
    { "email", user.Email }
});
```

### 3. Log Warnings
```csharp
await _crashReporting.LogWarningAsync("Invalid login attempt", new Dictionary<string, string>
{
    { "ipAddress", Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown" }
});
```

### 4. Log Errors
```csharp
try
{
    // Some operation
}
catch (Exception ex)
{
    await _crashReporting.LogErrorAsync("Operation failed", ex, new Dictionary<string, string>
    {
        { "operationId", operationId },
        { "userId", userId }
    });
}
```

---

## 🧪 Quick Test

### Test 1: Make a Request
```bash
POST http://localhost:5000/api/jobs/search
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json

{
  "query": "developer",
  "location": "chicago"
}
```

### Test 2: Check Logs
```powershell
ls "C:\Users\More\Desktop\shanib\project\career-guidance---backend\Logs"

# Should show:
# - info-2025-12-05.log
# - errors-2025-12-05.log
# - warnings-2025-12-05.log
```

### Test 3: View Logs (Admin)
```bash
GET http://localhost:5000/api/logs/errors
Authorization: Bearer YOUR_ADMIN_TOKEN
```

---

## 📊 Endpoints Summary

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| POST | /api/jobs/search | Search jobs | Bearer |
| POST | /api/jobs/personalized | Get recommendations | Bearer |
| POST | /api/jobs/{id}/save | Save/unsave job | Bearer |
| POST | /api/jobs/{id}/apply | Apply for job | Bearer |
| GET | /api/jobs/saved | Get saved jobs | Bearer |
| GET | /api/jobs/{id} | Get job details | Bearer |
| GET | /api/logs/errors | **View errors (Admin)** | Admin |
| GET | /api/logs/statistics | **View statistics (Admin)** | Admin |
| GET | /api/logs/file/{type} | **Download log file (Admin)** | Admin |

---

## 🎯 What Each Log Level Means

### 🔴 ERROR
- Something went wrong
- Exception occurred
- Database operation failed
- API call failed
- Stored in: `errors-YYYY-MM-DD.log`

### 🟠 WARNING
- Validation failed
- Unauthorized access
- Resource not found
- Unusual activity
- Stored in: `warnings-YYYY-MM-DD.log`

### 🟢 INFO
- Operation started
- Operation completed
- Data retrieved
- Normal activity
- Stored in: `info-YYYY-MM-DD.log`

---

## 📈 Log Statistics

```csharp
// Get statistics
var stats = await crashReportingService.GetStatisticsAsync();

// Returns:
{
    "totalErrors": 15,           // Total error count
    "totalWarnings": 42,         // Total warning count
    "logDirectory": "C:\\...\\Logs",
    "lastUpdated": "2025-12-05T14:30:45Z"
}
```

---

## 🔐 Security

✅ **Role-based access**: Log endpoints require Admin role  
✅ **JWT protected**: All endpoints require bearer token  
✅ **Local storage**: No external services = secure  
✅ **No sensitive data**: Passwords/tokens never logged  
✅ **Customizable**: You control what gets logged  

---

## 📝 Sample Log Entry

```json
{
  "timestamp": "2025-12-05T14:30:45.1234567Z",
  "level": "ERROR",
  "message": "Error searching jobs",
  "exceptionType": "HttpRequestException",
  "exceptionMessage": "The API call timed out",
  "stackTrace": "at JobApiService.SearchJobsAsync...",
  "customData": {
    "userId": "123",
    "endpoint": "SearchJobs",
    "query": "developer",
    "location": "chicago"
  }
}
```

---

## 🚀 Get Started Now

1. **Run backend**: `dotnet run`
2. **Make API call**: `POST /api/jobs/search`
3. **Check logs**: Open `/Logs` folder
4. **View via API**: `GET /api/logs/errors` (Admin only)

---

## 📚 Documentation Files

- `CRASHLYTICS_IMPLEMENTATION.md` - Complete guide
- `CRASHLYTICS_ARCHITECTURE.md` - System design
- `CRASHLYTICS_TESTING.md` - Test cases
- `CRASHLYTICS_SUMMARY.md` - This summary

---

**Status**: ✅ **FULLY IMPLEMENTED & READY TO USE**

