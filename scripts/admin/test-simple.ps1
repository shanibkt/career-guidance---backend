Write-Host "Testing API..." -ForegroundColor Cyan

# Login
$body = '{"email":"admin@careerguidance.com","password":"Admin@123"}'
$login = Invoke-RestMethod -Uri "http://localhost:5001/api/auth/login" -Method Post -Body $body -ContentType "application/json"
Write-Host "Login: OK - Token: $($login.token.Substring(0,20))..." -ForegroundColor Green

# Get videos
$videos = Invoke-RestMethod -Uri "http://localhost:5001/api/learningvideos"
Write-Host "Videos: $($videos.videos.Count) found" -ForegroundColor Green

# Create video - TEST WITH BOTH CASINGS
Write-Host "`nTesting camelCase..." -ForegroundColor Yellow
$testVideo1 = '{"skillName":"TestSkill1","videoTitle":"Test1","videoDescription":"Desc","youtubeVideoId":"test123","durationMinutes":5,"thumbnailUrl":"http://test.com","transcript":"test transcript"}'

try {
    $result1 = Invoke-RestMethod -Uri "http://localhost:5001/api/learningvideos" -Method Post -Body $testVideo1 -ContentType "application/json" -Headers @{Authorization="Bearer $($login.token)"}
    Write-Host "✅ camelCase works: $($result1 | ConvertTo-Json)" -ForegroundColor Green
} catch {
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $errorBody = $reader.ReadToEnd()
    Write-Host "❌ camelCase failed: $errorBody" -ForegroundColor Red
}

Write-Host "`nTesting PascalCase..." -ForegroundColor Yellow
$testVideo2 = '{"SkillName":"TestSkill2","VideoTitle":"Test2","VideoDescription":"Desc","YoutubeVideoId":"test456","DurationMinutes":5,"ThumbnailUrl":"http://test.com","Transcript":"test transcript"}'

try {
    $result2 = Invoke-RestMethod -Uri "http://localhost:5001/api/learningvideos" -Method Post -Body $testVideo2 -ContentType "application/json" -Headers @{Authorization="Bearer $($login.token)"}
    Write-Host "✅ PascalCase works: $($result2 | ConvertTo-Json)" -ForegroundColor Green
} catch {
    $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
    $errorBody = $reader.ReadToEnd()
    Write-Host "❌ PascalCase failed: $errorBody" -ForegroundColor Red
}
