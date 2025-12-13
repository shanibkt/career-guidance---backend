# Backend Endpoint Connection Test
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Backend Connection Test" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5001"

# 1. Test Auth
Write-Host "1. Testing AUTH..." -ForegroundColor Yellow
try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json"} -Body '{"email":"shanib@gmail.com","password":"shanib"}'
    $token = $loginResponse.token
    Write-Host "   ✅ Login: SUCCESS" -ForegroundColor Green
    Write-Host "   Token: $($token.Substring(0,30))..." -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Login: FAILED - $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
Write-Host ""

# 2. Test Profile
Write-Host "2. Testing PROFILE..." -ForegroundColor Yellow
try {
    $profile = Invoke-RestMethod -Uri "$baseUrl/api/userprofile/13" -Headers @{"Authorization"="Bearer $token"}
    Write-Host "   ✅ Get Profile: SUCCESS" -ForegroundColor Green
    Write-Host "   Phone: $($profile.phoneNumber), Age: $($profile.age)" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Get Profile: FAILED - Status $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}
Write-Host ""

# 3. Test Career Recommendations
Write-Host "3. Testing CAREER RECOMMENDATIONS..." -ForegroundColor Yellow
try {
    $careers = Invoke-RestMethod -Uri "$baseUrl/api/recommendations/careers" -Headers @{"Authorization"="Bearer $token"}
    Write-Host "   ✅ Get Careers: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($careers.Count) careers" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Get Careers: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 4. Test Chat
Write-Host "4. Testing CHATBOT..." -ForegroundColor Yellow
try {
    $chatBody = @{
        message = "Hello, test message"
    } | ConvertTo-Json
    $chatResponse = Invoke-RestMethod -Uri "$baseUrl/api/chat" -Method POST -Headers @{"Authorization"="Bearer $token"; "Content-Type"="application/json"} -Body $chatBody
    Write-Host "   ✅ Chat: SUCCESS" -ForegroundColor Green
    Write-Host "   Response: $($chatResponse.response.Substring(0, [Math]::Min(50, $chatResponse.response.Length)))..." -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Chat: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 5. Test Quiz Generation
Write-Host "5. Testing QUIZ GENERATION..." -ForegroundColor Yellow
try {
    $quizBody = @{
        careerName = "Software Developer"
        skillName = "Python"
        difficulty = "medium"
    } | ConvertTo-Json
    $quiz = Invoke-RestMethod -Uri "$baseUrl/api/quiz/generate" -Method POST -Headers @{"Authorization"="Bearer $token"; "Content-Type"="application/json"} -Body $quizBody
    Write-Host "   ✅ Quiz Generate: SUCCESS" -ForegroundColor Green
    Write-Host "   Generated $($quiz.questions.Count) questions" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Quiz Generate: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 6. Test Learning Videos
Write-Host "6. Testing LEARNING VIDEOS..." -ForegroundColor Yellow
try {
    $videos = Invoke-RestMethod -Uri "$baseUrl/api/learningvideos" -Headers @{"Authorization"="Bearer $token"}
    Write-Host "   ✅ Learning Videos: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($videos.Count) videos" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Learning Videos: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 7. Test Jobs
Write-Host "7. Testing PERSONALIZED JOBS..." -ForegroundColor Yellow
try {
    $jobs = Invoke-RestMethod -Uri "$baseUrl/api/jobs/personalized" -Headers @{"Authorization"="Bearer $token"}
    Write-Host "   ✅ Jobs: SUCCESS" -ForegroundColor Green
    Write-Host "   Found $($jobs.Count) jobs" -ForegroundColor Gray
} catch {
    Write-Host "   ❌ Jobs: FAILED - $($_.Exception.Message)" -ForegroundColor Red
}
Write-Host ""

# 8. Test Resume
Write-Host "8. Testing RESUME..." -ForegroundColor Yellow
try {
    $resume = Invoke-RestMethod -Uri "$baseUrl/api/resume/13" -Headers @{"Authorization"="Bearer $token"}
    Write-Host "   ✅ Resume: SUCCESS" -ForegroundColor Green
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    if ($statusCode -eq 404) {
        Write-Host "   ⚠️  Resume: No resume found (404) - This is OK for new users" -ForegroundColor Yellow
    } else {
        Write-Host "   ❌ Resume: FAILED - Status $statusCode" -ForegroundColor Red
    }
}
Write-Host ""

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Test Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
