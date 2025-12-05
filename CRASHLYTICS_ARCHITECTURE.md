# Firebase Crashlytics Implementation Architecture

## 📊 System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        API REQUESTS                              │
│    (Job Search, Apply, Save, Personalized, etc.)                │
└────────────────────────────┬────────────────────────────────────┘
                             │
                             ▼
        ┌────────────────────────────────────────┐
        │         JobsController                 │
        │  (7 Endpoints with Logging)           │
        │  ┌──────────────────────────────┐     │
        │  │ SearchJobs()                 │     │
        │  │ GetPersonalizedJobs()        │     │
        │  │ SaveJob()                    │     │
        │  │ ApplyForJob()                │     │
        │  │ GetSavedJobs()               │     │
        │  │ GetJobDetails()              │     │
        │  │ GetFilterMetadata()          │     │
        │  └──────────────────────────────┘     │
        └────────────┬─────────────────────────┘
                     │
                     │ Calls
                     ▼
        ┌─────────────────────────────────────────┐
        │  ICrashReportingService                 │
        │  (Interface - Dependency Injection)     │
        └────────────┬────────────────────────────┘
                     │ Implemented By
                     ▼
        ┌─────────────────────────────────────────┐
        │  LocalCrashReportingService             │
        │  ┌───────────────────────────────────┐  │
        │  │ LogErrorAsync()                   │  │
        │  │ LogInfoAsync()                    │  │
        │  │ LogWarningAsync()                 │  │
        │  │ GetRecentErrorsAsync()            │  │
        │  │ GetStatisticsAsync()              │  │
        │  └───────────────────────────────────┘  │
        └────────────┬────────────────────────────┘
                     │ Writes To
                     ▼
        ┌─────────────────────────────────────────┐
        │         Local Log Files                 │
        │  ┌───────────────────────────────────┐  │
        │  │ Logs/errors-2025-12-05.log        │  │
        │  │ Logs/info-2025-12-05.log          │  │
        │  │ Logs/warnings-2025-12-05.log      │  │
        │  └───────────────────────────────────┘  │
        │         (JSON Format)                   │
        └─────────────────────────────────────────┘
                     ▲
                     │ Read By
                     │
        ┌────────────┴─────────────────────────────┐
        │  LogsController (Admin Endpoints)        │
        │  ┌──────────────────────────────────┐   │
        │  │ GET /api/logs/errors             │   │
        │  │ GET /api/logs/statistics         │   │
        │  │ GET /api/logs/file/{type}        │   │
        │  └──────────────────────────────────┘   │
        │  (Admin Role Required)                  │
        └─────────────────────────────────────────┘
```

---

## 🔄 Data Flow Example

### Job Search Endpoint Flow

```
1. CLIENT REQUEST
   ┌─────────────────────┐
   │ POST /api/jobs/search
   │ Authorization: Bearer TOKEN
   │ { "query": "developer", "location": "chicago" }
   └────────────┬────────┘
                │
                ▼
   ┌─────────────────────────────────────────┐
   │ JobsController.SearchJobs()             │
   │                                         │
   │ 1. Extract userId from JWT              │
   │ 2. CALL: LogInfoAsync("initiated")      │ ──→ Logs to: info-2025-12-05.log
   │ 3. Validate query/location              │
   │ 4. Call JobApiService                   │
   │ 5. Mark jobs saved/applied              │
   │ 6. CALL: LogInfoAsync("completed")      │ ──→ Logs to: info-2025-12-05.log
   │ 7. Return 200 OK                        │
   │                                         │
   │ IF ERROR:                               │
   │ - CALL: LogErrorAsync(ex)               │ ──→ Logs to: errors-2025-12-05.log
   │ - Return 500 Error                      │
   └─────────────────────────────────────────┘
```

---

## 📝 Logging Hierarchy

```
┌──────────────────────────────────────────────────────────┐
│           LOGGING LEVELS (Severity)                      │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  🔴 ERROR (Highest Priority)                            │
│     ├─ Exception occurred                               │
│     ├─ Database operation failed                        │
│     ├─ API call failed                                  │
│     └─ Stored in: errors-2025-12-05.log               │
│                                                          │
│  🟠 WARNING (Medium Priority)                           │
│     ├─ Validation failed                                │
│     ├─ Unauthorized access attempt                      │
│     ├─ Resource not found                               │
│     └─ Stored in: warnings-2025-12-05.log             │
│                                                          │
│  🟢 INFO (Low Priority)                                 │
│     ├─ Operation started                                │
│     ├─ Operation completed                              │
│     ├─ Data retrieved                                   │
│     └─ Stored in: info-2025-12-05.log                 │
│                                                          │
└──────────────────────────────────────────────────────────┘
```

---

## 🗂️ File Organization

```
Backend Project Root
│
├── Controllers/
│   ├── JobsController.cs          ✅ Added logging calls
│   ├── LogsController.cs          ✨ NEW - View logs
│   ├── AuthController.cs
│   └── ...
│
├── Services/
│   ├── LocalCrashReportingService.cs   ✨ NEW - Main service
│   ├── JobApiService.cs
│   ├── JobDatabaseService.cs
│   └── ...
│
├── Models/
│   ├── LogEntry.cs                (inside LocalCrashReportingService.cs)
│   ├── LogStatistics.cs           (inside LocalCrashReportingService.cs)
│   └── ...
│
├── Logs/                           📁 AUTO-CREATED
│   ├── errors-2025-12-05.log
│   ├── info-2025-12-05.log
│   └── warnings-2025-12-05.log
│
├── Program.cs                      ✅ Updated
├── MyFirstApi.csproj
├── appsettings.json
├── CRASHLYTICS_IMPLEMENTATION.md   ✨ NEW - This guide
└── ...
```

---

## 🔌 Dependency Injection Flow

```
Program.cs
│
├── builder.Services.AddScoped<JobApiService>()
├── builder.Services.AddScoped<JobDatabaseService>()
├── builder.Services.AddScoped<ICrashReportingService, 
│                            LocalCrashReportingService>()  ← NEW
│
└── When JobsController is instantiated:
    │
    ├── JobApiService instance ✓
    ├── JobDatabaseService instance ✓
    ├── IConfiguration instance ✓
    ├── ICrashReportingService instance ✓ (LocalCrashReportingService)
    └── ILogger<JobsController> instance ✓
```

---

## 📊 Log Entry Structure

```json
LogEntry {
  "timestamp": "2025-12-05T14:30:45.1234567Z",      // When it happened (UTC)
  "level": "ERROR",                                  // Severity level
  "message": "Error searching jobs",                 // Human-readable message
  "exceptionType": "HttpRequestException",          // Exception class name
  "exceptionMessage": "The API call timed out",     // Exception message
  "stackTrace": "at JobApiService.SearchJobsAsync...",  // Call stack
  "customData": {                                    // Context-specific data
    "userId": "123",
    "endpoint": "SearchJobs",
    "query": "developer jobs",
    "location": "Chicago"
  }
}
```

---

## 🎯 Usage Example

### In JobsController

```csharp
[HttpPost("search")]
public async Task<IActionResult> SearchJobs([FromBody] JobSearchRequest request)
{
    try
    {
        var userId = GetUserId();
        
        // Log START
        await _crashReporting.LogInfoAsync("Job search initiated", 
            new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "query", request.Query ?? "none" }
            });
        
        // Business logic...
        var response = await _jobApiService.SearchJobsAsync(request);
        
        // Log SUCCESS
        await _crashReporting.LogInfoAsync("Job search completed", 
            new Dictionary<string, string>
            {
                { "userId", userId.ToString() },
                { "resultsCount", response.Jobs.Count.ToString() }
            });
        
        return Ok(response);
    }
    catch (Exception ex)
    {
        // Log ERROR
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

## 🔐 Role-Based Access Control

```
┌─────────────────────────────────────┐
│    User Roles & Permissions         │
├─────────────────────────────────────┤
│                                     │
│  Regular User                       │
│  └─ Can call job endpoints          │
│     (SearchJobs, SaveJob, etc.)    │
│                                     │
│  Admin User                         │
│  ├─ Can call job endpoints          │
│  └─ Can view logs                   │
│     ├─ GET /api/logs/errors         │
│     ├─ GET /api/logs/statistics     │
│     └─ GET /api/logs/file/errors    │
│                                     │
│  System                             │
│  └─ Automatically logs all actions  │
│                                     │
└─────────────────────────────────────┘
```

---

## 🚀 Deployment Checklist

- [x] LocalCrashReportingService created
- [x] LogsController created
- [x] JobsController updated
- [x] Program.cs updated
- [x] All 7 endpoints have logging
- [x] Error, Info, Warning levels
- [x] Admin endpoints secured
- [x] JSON log format
- [x] Daily log rotation
- [x] No breaking changes
- [x] Backward compatible
- [x] Ready for production

---

## 📞 Support

### How to View Logs

1. **Via Admin Endpoint**
   ```
   GET /api/logs/errors
   GET /api/logs/statistics
   ```

2. **Via File System**
   ```
   C:\...\career-guidance---backend\Logs\
   ```

3. **Via Console (Debug Mode)**
   ```
   Watch the colored output in terminal
   ```

### Common Issues

| Issue | Solution |
|-------|----------|
| Logs directory not created | Restart backend, it auto-creates on first error |
| Cannot access logs endpoint | Ensure you're logged in as Admin |
| Old logs still showing | Logs are per-day, check yesterday's date |

