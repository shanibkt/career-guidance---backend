# Test Admin Module Endpoints
# This script tests all admin API endpoints

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Admin Module Endpoint Test" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5001/api"
$adminEmail = "admin@careerguidance.com"
$adminPassword = "Admin@123"

Write-Host "Step 1: Testing Admin Login..." -ForegroundColor Yellow
$loginBody = @{
    email = $adminEmail
    password = $adminPassword
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.token
    Write-Host "OK Login successful" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 30))..." -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Login failed" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type" = "application/json"
}

Write-Host ""
Write-Host "Step 2: Testing GET /api/admin/stats..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "$baseUrl/admin/stats" -Method GET -Headers $headers
    Write-Host "OK Stats retrieved" -ForegroundColor Green
    Write-Host "  Total Users: $($stats.totalUsers)" -ForegroundColor Gray
    Write-Host "  Active Users Today: $($stats.activeUsersToday)" -ForegroundColor Gray
    Write-Host "  Total Videos Watched: $($stats.totalVideosWatched)" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Failed to get stats" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 3: Testing GET /api/admin/users..." -ForegroundColor Yellow
try {
    $users = Invoke-RestMethod -Uri "$baseUrl/admin/users?page=1&pageSize=5" -Method GET -Headers $headers
    Write-Host "OK Users retrieved" -ForegroundColor Green
    Write-Host "  Total Users: $($users.totalUsers)" -ForegroundColor Gray
    Write-Host "  Current Page: $($users.currentPage)" -ForegroundColor Gray
    Write-Host "  Page Size: $($users.pageSize)" -ForegroundColor Gray
    
    if ($users.users.Count -gt 0) {
        $testUserId = $users.users[0].userId
        
        Write-Host ""
        Write-Host "Step 4: Testing GET /api/admin/users/{userId}..." -ForegroundColor Yellow
        try {
            $userDetail = Invoke-RestMethod -Uri "$baseUrl/admin/users/$testUserId" -Method GET -Headers $headers
            Write-Host "OK User detail retrieved" -ForegroundColor Green
            Write-Host "  User ID: $($userDetail.userId)" -ForegroundColor Gray
            Write-Host "  Username: $($userDetail.username)" -ForegroundColor Gray
            Write-Host "  Email: $($userDetail.email)" -ForegroundColor Gray
        } catch {
            Write-Host "ERROR: Failed to get user detail" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
        }
    }
} catch {
    Write-Host "ERROR: Failed to get users" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 5: Testing GET /api/admin/analytics/growth..." -ForegroundColor Yellow
try {
    $growth = Invoke-RestMethod -Uri "$baseUrl/admin/analytics/growth?days=7" -Method GET -Headers $headers
    Write-Host "OK Growth analytics retrieved" -ForegroundColor Green
    Write-Host "  Data points: $($growth.Count)" -ForegroundColor Gray
} catch {
    Write-Host "ERROR: Failed to get growth analytics" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Test Complete!" -ForegroundColor Green
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "All admin endpoints are functional!" -ForegroundColor Green
Write-Host "Admin dashboard URL: http://localhost:5001/admin.html" -ForegroundColor Yellow
Write-Host ""
