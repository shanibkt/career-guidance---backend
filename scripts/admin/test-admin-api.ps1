# Test Admin API Endpoints
Write-Host "`n=== Testing Admin API ===" -ForegroundColor Cyan

# Step 1: Login
Write-Host "`n1. Logging in..." -ForegroundColor Yellow
$loginBody = @{
    Email = "admin@careerguidance.com"
    Password = "Admin@123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" `
        -Method Post `
        -Body $loginBody `
        -ContentType "application/json"
    
    $token = $loginResponse.token
    Write-Host "✅ Login successful" -ForegroundColor Green
    Write-Host "Token: $($token.Substring(0, 30))..." -ForegroundColor Gray
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit
}

# Step 2: Test GET videos
Write-Host "`n2. Getting all videos..." -ForegroundColor Yellow
try {
    $videos = Invoke-RestMethod -Uri "http://localhost:5001/api/learningvideos" `
        -Method Get
    Write-Host "✅ Found $($videos.videos.Count) videos" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to get videos: $_" -ForegroundColor Red
}

# Step 3: Test POST create video
Write-Host "`n3. Testing video creation..." -ForegroundColor Yellow
$newVideo = @{
    skillName = "Test Skill PowerShell"
    videoTitle = "Test Video Title"
    videoDescription = "Test Description"
    youtubeVideoId = "dQw4w9WgXcQ"
    durationMinutes = 10
    thumbnailUrl = "https://example.com/thumb.jpg"
    transcript = "This is a test transcript content"
} | ConvertTo-Json

Write-Host "Request body:" -ForegroundColor Gray
Write-Host $newVideo -ForegroundColor Gray

try {
    $createResponse = Invoke-RestMethod -Uri "http://localhost:5001/api/learningvideos" `
        -Method Post `
        -Body $newVideo `
        -ContentType "application/json" `
        -Headers @{ Authorization = "Bearer $token" }
    
    Write-Host "✅ Video created successfully!" -ForegroundColor Green
    Write-Host "Response: $($createResponse | ConvertTo-Json -Depth 3)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to create video" -ForegroundColor Red
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host "Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $errorBody = $reader.ReadToEnd()
        Write-Host "Error body: $errorBody" -ForegroundColor Red
    }
}

# Step 4: Test GET users (admin endpoint)
Write-Host "`n4. Getting users..." -ForegroundColor Yellow
try {
    $users = Invoke-RestMethod -Uri "http://localhost:5001/api/admin/users?page=1&pageSize=20" `
        -Method Get `
        -Headers @{ Authorization = "Bearer $token" }
    
    Write-Host "✅ Found $($users.users.Count) users" -ForegroundColor Green
} catch {
    Write-Host "❌ Failed to get users: $_" -ForegroundColor Red
}

# Step 5: Test admin stats
Write-Host "`n5. Getting admin stats..." -ForegroundColor Yellow
try {
    $stats = Invoke-RestMethod -Uri "http://localhost:5001/api/admin/stats" `
        -Method Get `
        -Headers @{ Authorization = "Bearer $token" }
    
    Write-Host "✅ Stats retrieved successfully" -ForegroundColor Green
    Write-Host "Stats: $($stats | ConvertTo-Json -Depth 2)" -ForegroundColor Gray
} catch {
    Write-Host "❌ Failed to get stats: $_" -ForegroundColor Red
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
