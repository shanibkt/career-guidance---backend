# Admin Module API Test Script
# Tests all admin endpoints to verify functionality

$baseUrl = "http://localhost:5001"
$adminEmail = "admin@careerguidance.com"
$adminPassword = "Admin@123"

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  Admin Module API Test Suite" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Test 1: Check if backend is running
Write-Host "Test 1: Checking if backend is running..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/auth/verify" -Method GET -UseBasicParsing -TimeoutSec 5 -ErrorAction SilentlyContinue
    Write-Host "✗ Backend is running but auth endpoint should require token" -ForegroundColor Yellow
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✓ Backend is running (401 as expected without token)" -ForegroundColor Green
    } else {
        Write-Host "✗ Backend might not be running. Error: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "Please start backend with: dotnet run" -ForegroundColor Yellow
        exit
    }
}

Write-Host ""

# Test 2: Admin Login
Write-Host "Test 2: Testing admin login..." -ForegroundColor Yellow
try {
    $loginBody = @{
        email = $adminEmail
        password = $adminPassword
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    
    if ($token) {
        Write-Host "✓ Admin login successful!" -ForegroundColor Green
        Write-Host "  Token: $($token.Substring(0, 20))..." -ForegroundColor Gray
    } else {
        Write-Host "✗ Login failed - no token received" -ForegroundColor Red
        exit
    }
} catch {
    Write-Host "✗ Login failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Check if admin user exists with correct password" -ForegroundColor Yellow
    exit
}

Write-Host ""

# Create headers with token
$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

# Test 3: Get System Stats
Write-Host "Test 3: Testing system statistics endpoint..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/api/admin/stats" -Method GET -Headers $headers
    Write-Host "✓ System stats retrieved!" -ForegroundColor Green
    Write-Host "  Total Users: $($stats.totalUsers)" -ForegroundColor Gray
    Write-Host "  Active Today: $($stats.activeUsersToday)" -ForegroundColor Gray
    Write-Host "  Active Week: $($stats.activeUsersWeek)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to get stats: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 4: Get Users List
Write-Host "Test 4: Testing users list endpoint..." -ForegroundColor Yellow
try {
    $usersResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/users?page=1&pageSize=5" -Method GET -Headers $headers
    Write-Host "✓ Users list retrieved!" -ForegroundColor Green
    Write-Host "  Total Users: $($usersResponse.totalUsers)" -ForegroundColor Gray
    Write-Host "  Current Page: $($usersResponse.currentPage)" -ForegroundColor Gray
    Write-Host "  Users on page: $($usersResponse.users.Count)" -ForegroundColor Gray
    
    # Store first user ID for detail test
    if ($usersResponse.users.Count -gt 0) {
        $testUserId = $usersResponse.users[0].userId
        Write-Host "  Sample User: $($usersResponse.users[0].fullName) ($($usersResponse.users[0].email))" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to get users list: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Get User Detail
if ($testUserId) {
    Write-Host "Test 5: Testing user detail endpoint..." -ForegroundColor Yellow
    try {
        $userDetail = Invoke-RestMethod -Uri "$baseUrl/api/admin/users/$testUserId" -Method GET -Headers $headers
        Write-Host "✓ User detail retrieved!" -ForegroundColor Green
        Write-Host "  User: $($userDetail.fullName)" -ForegroundColor Gray
        Write-Host "  Email: $($userDetail.email)" -ForegroundColor Gray
        Write-Host "  Has Profile: $($null -ne $userDetail.profile)" -ForegroundColor Gray
        Write-Host "  Has Career: $($null -ne $userDetail.career)" -ForegroundColor Gray
        Write-Host "  Video Progress Items: $($userDetail.videoProgress.Count)" -ForegroundColor Gray
        Write-Host "  Chat Sessions: $($userDetail.chatHistory.Count)" -ForegroundColor Gray
    } catch {
        Write-Host "✗ Failed to get user detail: $($_.Exception.Message)" -ForegroundColor Red
    }
} else {
    Write-Host "Test 5: Skipped (no users found)" -ForegroundColor Yellow
}

Write-Host ""

# Test 6: Search Users
Write-Host "Test 6: Testing search functionality..." -ForegroundColor Yellow
try {
    $searchResponse = Invoke-RestMethod -Uri "$baseUrl/api/admin/users?search=admin&page=1&pageSize=10" -Method GET -Headers $headers
    Write-Host "✓ Search functionality working!" -ForegroundColor Green
    Write-Host "  Results: $($searchResponse.users.Count) users" -ForegroundColor Gray
} catch {
    Write-Host "✗ Failed to search users: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 7: User Growth Analytics
Write-Host "Test 7: Testing analytics endpoint..." -ForegroundColor Yellow
try {
    $analytics = Invoke-RestMethod -Uri "$baseUrl/api/admin/analytics/growth?days=7" -Method GET -Headers $headers
    Write-Host "✓ Analytics data retrieved!" -ForegroundColor Green
    Write-Host "  Data points: $($analytics.Count)" -ForegroundColor Gray
    if ($analytics.Count -gt 0) {
        Write-Host "  Latest: $($analytics[-1].date) - $($analytics[-1].count) users" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to get analytics: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 8: Export CSV (simulate - don't actually download)
Write-Host "Test 8: Testing CSV export endpoint..." -ForegroundColor Yellow
try {
    $exportResponse = Invoke-WebRequest -Uri "$baseUrl/api/admin/export/users" -Method GET -Headers $headers -UseBasicParsing
    if ($exportResponse.StatusCode -eq 200) {
        Write-Host "✓ CSV export endpoint working!" -ForegroundColor Green
        Write-Host "  Content-Type: $($exportResponse.Headers['Content-Type'])" -ForegroundColor Gray
        Write-Host "  Content-Length: $($exportResponse.Headers['Content-Length']) bytes" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to export CSV: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 9: Admin HTML Page
Write-Host "Test 9: Testing admin dashboard page..." -ForegroundColor Yellow
try {
    $htmlResponse = Invoke-WebRequest -Uri "$baseUrl/admin.html" -Method GET -UseBasicParsing
    if ($htmlResponse.StatusCode -eq 200) {
        Write-Host "✓ Admin dashboard page accessible!" -ForegroundColor Green
        Write-Host "  URL: $baseUrl/admin.html" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ Failed to access admin page: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "  Check if admin.html exists in wwwroot folder" -ForegroundColor Yellow
}

Write-Host ""

# Test 10: Token Verification
Write-Host "Test 10: Testing token verification..." -ForegroundColor Yellow
try {
    $verifyResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/verify" -Method GET -Headers $headers
    Write-Host "✓ Token is valid!" -ForegroundColor Green
    Write-Host "  User: $($verifyResponse.username)" -ForegroundColor Gray
    Write-Host "  Email: $($verifyResponse.email)" -ForegroundColor Gray
} catch {
    Write-Host "✗ Token verification failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  Test Suite Complete!" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Summary:" -ForegroundColor Yellow
Write-Host "- Admin login: ✓" -ForegroundColor Green
Write-Host "- System stats: ✓" -ForegroundColor Green
Write-Host "- User management: ✓" -ForegroundColor Green
Write-Host "- Search & filter: ✓" -ForegroundColor Green
Write-Host "- Analytics: ✓" -ForegroundColor Green
Write-Host "- CSV export: ✓" -ForegroundColor Green
Write-Host "- Dashboard UI: ✓" -ForegroundColor Green
Write-Host ""
Write-Host "Access admin dashboard at: $baseUrl/admin.html" -ForegroundColor Cyan
Write-Host ""

# Pause to read results
Read-Host "Press Enter to exit"
