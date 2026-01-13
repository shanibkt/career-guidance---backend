# Firebase Crashlytics - Testing Guide

## 🧪 Testing the Implementation

### Prerequisites
- Backend running: `dotnet run`
- Postman or similar API testing tool
- Valid JWT token (from login)
- Admin token (if testing admin endpoints)

---

## 📌 Test Cases

### Test 1: Successful Job Search (INFO Logging)

**Endpoint**: `POST /api/jobs/search`

**Headers**:
```
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body**:
```json
{
  "query": "developer",
  "location": "chicago",
  "page": 1,
  "pageSize": 10
}
```

**Expected Logs**:
- ✅ `info-2025-12-05.log` - "Job search initiated"
- ✅ `info-2025-12-05.log` - "Job search completed"

**Where to Check**:
```
Logs/info-2025-12-05.log
```

---

### Test 2: Missing Query/Location (WARNING Logging)

**Endpoint**: `POST /api/jobs/search`

**Body**:
```json
{
  "query": "",
  "location": ""
}
```

**Expected Logs**:
- ✅ `warnings-2025-12-05.log` - "Job search: Missing query or location"

**Where to Check**:
```
Logs/warnings-2025-12-05.log
```

---

### Test 3: Save Job (INFO Logging)

**Endpoint**: `POST /api/jobs/{jobId}/save`

**Headers**:
```
Authorization: Bearer YOUR_TOKEN
Content-Type: application/json
```

**Body**:
```json
{
  "save": true
}
```

**Expected Logs**:
- ✅ `info-2025-12-05.log` - "Job saved"

---

### Test 4: Apply for Job (INFO Logging)

**Endpoint**: `POST /api/jobs/{jobId}/apply`

**Body**:
```json
{
  "notes": "I'm interested in this role!"
}
```

**Expected Logs**:
- ✅ `info-2025-12-05.log` - "Job application submitted"

---

### Test 5: Unauthorized Access (WARNING Logging)

**Endpoint**: `POST /api/jobs/{jobId}/save`

**Without Authorization Header**

**Expected Response**: `401 Unauthorized`

---

### Test 6: Simulate Error (ERROR Logging)

**Trigger a database error by**:
1. Stopping the database connection
2. Calling any endpoint

**Expected Logs**:
- ✅ `errors-2025-12-05.log` - "Error searching jobs"
- ✅ Full stack trace included
- ✅ Exception type and message

---

## 🔍 Viewing Logs

### Method 1: File System

```powershell
# Navigate to Logs folder
cd "C:\Users\More\Desktop\shanib\project\career-guidance---backend\Logs"

# List files
ls

# View error log
Get-Content errors-2025-12-05.log

# View info log
Get-Content info-2025-12-05.log

# View warnings
Get-Content warnings-2025-12-05.log
```

### Method 2: Admin API Endpoints

#### Get Recent Errors
```
GET http://localhost:5000/api/logs/errors?count=50
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**Response**:
```json
{
  "count": 3,
  "errors": [
    {
      "timestamp": "2025-12-05T14:30:45.1234567Z",
      "level": "ERROR",
      "message": "Error searching jobs",
      "exceptionType": "HttpRequestException",
      "exceptionMessage": "The API call timed out",
      "stackTrace": "at JobApiService.SearchJobsAsync...",
      "customData": {
        "userId": "123",
        "endpoint": "SearchJobs"
      }
    }
  ]
}
```

#### Get Statistics
```
GET http://localhost:5000/api/logs/statistics
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**Response**:
```json
{
  "totalErrors": 5,
  "totalWarnings": 12,
  "logDirectory": "C:\\...\\Logs",
  "lastUpdated": "2025-12-05T14:30:45.1234567Z"
}
```

#### Get Raw Log File
```
GET http://localhost:5000/api/logs/file/errors
Authorization: Bearer YOUR_ADMIN_TOKEN
```

**Response**:
```json
{
  "fileName": "errors-2025-12-05.log",
  "content": "[raw JSON content...]"
}
```

---

## 📊 Expected Output in Console (Debug Mode)

When running in DEBUG mode, you should see colored output:

```
[INFO] Job search initiated
[INFO] Job search completed
[INFO] Job saved
[INFO] Saved jobs retrieved
[WARNING] Job search: Missing query or location
[ERROR] Error searching jobs
Exception: The API call timed out
  at JobApiService.SearchJobsAsync(JobSearchRequest request) in JobApiService.cs:line 123
```

---

## 📈 Sample Log File Content

### errors-2025-12-05.log
```json
{
  "timestamp": "2025-12-05T14:35:22.5678901Z",
  "level": "ERROR",
  "message": "Error searching jobs",
  "exceptionType": "HttpRequestException",
  "exceptionMessage": "Unable to connect to the remote server",
  "stackTrace": "System.Net.Http.HttpRequestException: Unable to connect to the remote server\n   at System.Net.Http.HttpClientHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)\n   at MyFirstApi.Services.JobApiService.SearchJobsAsync(JobSearchRequest request) in C:\\...\\JobApiService.cs:line 45",
  "customData": {
    "userId": "123",
    "endpoint": "SearchJobs",
    "query": "developer",
    "location": "chicago"
  }
}
--------------------------------------------------------------------------------
{
  "timestamp": "2025-12-05T14:36:10.1234567Z",
  "level": "ERROR",
  "message": "Error saving job",
  "exceptionType": "MySqlException",
  "exceptionMessage": "Connection string not initialized",
  "stackTrace": "MySql.Data.MySqlClient.MySqlException: Connection string not initialized\n   at MyFirstApi.Services.JobDatabaseService.SaveJobAsync(Int32 userId, JobResponse job) in C:\\...\\JobDatabaseService.cs:line 78",
  "customData": {
    "userId": "123",
    "endpoint": "SaveJob",
    "jobId": "job_12345",
    "action": "save"
  }
}
--------------------------------------------------------------------------------
```

### info-2025-12-05.log
```json
{
  "timestamp": "2025-12-05T14:30:00.0000000Z",
  "level": "INFO",
  "message": "Job search initiated",
  "customData": {
    "userId": "123",
    "query": "developer",
    "location": "chicago"
  }
}
--------------------------------------------------------------------------------
{
  "timestamp": "2025-12-05T14:30:02.5555555Z",
  "level": "INFO",
  "message": "Job search completed",
  "customData": {
    "userId": "123",
    "resultsCount": "25"
  }
}
--------------------------------------------------------------------------------
{
  "timestamp": "2025-12-05T14:31:00.1111111Z",
  "level": "INFO",
  "message": "Job saved",
  "customData": {
    "userId": "123",
    "jobId": "job_12345"
  }
}
--------------------------------------------------------------------------------
```

### warnings-2025-12-05.log
```json
{
  "timestamp": "2025-12-05T14:32:00.2222222Z",
  "level": "WARNING",
  "message": "Job search: Missing query or location",
  "customData": {}
}
--------------------------------------------------------------------------------
{
  "timestamp": "2025-12-05T14:33:00.3333333Z",
  "level": "WARNING",
  "message": "Save job: Unauthorized user",
  "customData": {}
}
--------------------------------------------------------------------------------
```

---

## 🧩 Integration Test Script

### PowerShell Test Script

```powershell
# Set variables
$baseUrl = "http://localhost:5000"
$token = "YOUR_JWT_TOKEN"
$adminToken = "YOUR_ADMIN_TOKEN"

# Test 1: Search jobs
Write-Host "Test 1: Searching jobs..." -ForegroundColor Green
$searchResponse = Invoke-RestMethod -Uri "$baseUrl/api/jobs/search" `
  -Method POST `
  -Headers @{ Authorization = "Bearer $token"; "Content-Type" = "application/json" } `
  -Body '{"query":"developer","location":"chicago"}'

Write-Host "Response: $($searchResponse.jobs.Count) jobs found" -ForegroundColor Green

# Test 2: Get statistics
Write-Host "`nTest 2: Getting log statistics..." -ForegroundColor Green
$statsResponse = Invoke-RestMethod -Uri "$baseUrl/api/logs/statistics" `
  -Method GET `
  -Headers @{ Authorization = "Bearer $adminToken" }

Write-Host "Total Errors: $($statsResponse.totalErrors)" -ForegroundColor Yellow
Write-Host "Total Warnings: $($statsResponse.totalWarnings)" -ForegroundColor Yellow

# Test 3: Get recent errors
Write-Host "`nTest 3: Getting recent errors..." -ForegroundColor Green
$errorsResponse = Invoke-RestMethod -Uri "$baseUrl/api/logs/errors?count=10" `
  -Method GET `
  -Headers @{ Authorization = "Bearer $adminToken" }

Write-Host "Error Count: $($errorsResponse.count)" -ForegroundColor Red

# Test 4: Check log files
Write-Host "`nTest 4: Checking log files..." -ForegroundColor Green
$logsDir = "C:\Users\More\Desktop\shanib\project\career-guidance---backend\Logs"
if (Test-Path $logsDir) {
  Get-ChildItem $logsDir | ForEach-Object {
    $size = (Get-Item $_.FullName).Length / 1KB
    Write-Host "$($_.Name) - $([Math]::Round($size, 2)) KB" -ForegroundColor Cyan
  }
} else {
  Write-Host "Logs directory not found yet. Run the backend first." -ForegroundColor Red
}
```

---

## ✅ Verification Checklist

After testing, verify:

- [ ] `Logs/` directory created in project root
- [ ] `errors-2025-12-05.log` file exists
- [ ] `info-2025-12-05.log` file exists
- [ ] `warnings-2025-12-05.log` file exists
- [ ] Log files contain JSON formatted entries
- [ ] Each entry has: timestamp, level, message, customData
- [ ] Error logs include exceptionType and stackTrace
- [ ] Admin endpoints return data
- [ ] Console shows colored output (if DEBUG mode)
- [ ] Logs are properly separated by type
- [ ] Custom data matches request data

---

## 🎯 Success Criteria

✅ Implementation successful when:

1. Backend starts without errors
2. API calls generate log entries
3. Log files appear in `/Logs` directory
4. Admin can retrieve logs via endpoints
5. Console shows colored output
6. Errors include full stack traces
7. Custom data is properly captured
8. No data is lost

---

## 📞 Troubleshooting

| Issue | Solution |
|-------|----------|
| Logs directory not created | Make first API call to trigger directory creation |
| Log files are empty | Check if exceptions are occurring; INFO logs appear on success |
| Cannot access admin endpoints | Ensure user has "Admin" role in database |
| Console not showing colors | Ensure running in DEBUG mode |
| Old logs still showing | Logs are per-day; check date in filename |
| High file size | Use `/api/logs/statistics` to monitor; consider archiving old logs |

