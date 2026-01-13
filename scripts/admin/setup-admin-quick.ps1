# Quick Admin Setup - Run this to create admin user
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Admin Module - Database Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$dbUser = Read-Host "MySQL username (press Enter for 'root')"
if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = "root" }

$dbPassSecure = Read-Host "MySQL password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPassSecure)
$dbPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host ""
Write-Host "Running migration..." -ForegroundColor Yellow

Get-Content "sql\admin_module_migration.sql" | mysql -u $dbUser -p"$dbPass" career_guidance_db 2>&1

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Admin Credentials:" -ForegroundColor Yellow
Write-Host "  Email:    admin@careerguidance.com"
Write-Host "  Password: Admin@123"
Write-Host ""
Write-Host "Test the connection:" -ForegroundColor Yellow
Write-Host "  http://localhost:5001/test-connection.html"
Write-Host ""
Write-Host "Or login directly:" -ForegroundColor Yellow
Write-Host "  http://localhost:5001/admin.html"
Write-Host ""
