# Create Admin User via API
# Run this script to create an admin user account

$API_URL = "http://localhost:5001/api"

Write-Host "=== Creating Admin User ===" -ForegroundColor Green
Write-Host ""

# Register new admin account
$registerData = @{
    username = "admin"
    fullName = "System Administrator"
    email = "admin@careerguidance.com"
    password = "Admin@123"
} | ConvertTo-Json

Write-Host "Registering admin user..." -ForegroundColor Yellow

try {
    $response = Invoke-RestMethod -Uri "$API_URL/auth/register" `
        -Method Post `
        -ContentType "application/json" `
        -Body $registerData
    
    Write-Host "✅ Admin user created successfully!" -ForegroundColor Green
    Write-Host "User ID: $($response.user.id)" -ForegroundColor Cyan
    Write-Host "Username: $($response.user.username)" -ForegroundColor Cyan
    Write-Host "Email: $($response.user.email)" -ForegroundColor Cyan
    Write-Host ""
    
    # Now update to admin role in database
    Write-Host "Now run this SQL to grant admin privileges:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "UPDATE users SET Role = 'Admin', is_admin = 1 WHERE Email = 'admin@careerguidance.com';" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Or use this connection string in MySQL Workbench:" -ForegroundColor Yellow
    Write-Host "Server: sql.freedb.tech" -ForegroundColor Cyan
    Write-Host "Database: freedb_career_guidence" -ForegroundColor Cyan
    Write-Host "User: freedb_shanib" -ForegroundColor Cyan
    
} catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    
    if ($statusCode -eq 400) {
        Write-Host "⚠️ User already exists. Try logging in or use a different email." -ForegroundColor Yellow
    } else {
        Write-Host "❌ Error: $_" -ForegroundColor Red
        Write-Host ""
        Write-Host "Make sure backend is running: dotnet run" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "After updating database, you can login at:" -ForegroundColor Green
Write-Host "http://localhost:5001/admin.html" -ForegroundColor Cyan
Write-Host ""
Write-Host "Credentials:" -ForegroundColor Green
Write-Host "Email: admin@careerguidance.com" -ForegroundColor Cyan
Write-Host "Password: Admin@123" -ForegroundColor Cyan
