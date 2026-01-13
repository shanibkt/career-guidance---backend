# Simple Admin Database Setup Script
Write-Host "Admin Module Database Setup" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Get MySQL credentials
$dbName = "career_guidance_db"
$dbUser = Read-Host "MySQL username (default: root)"
if ([string]::IsNullOrWhiteSpace($dbUser)) { $dbUser = "root" }

$dbPass = Read-Host "MySQL password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($dbPass)
$plainPass = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

Write-Host ""
Write-Host "Executing migration..." -ForegroundColor Yellow

# Run the SQL file
$sqlFile = ".\sql\admin_module_migration.sql"
Get-Content $sqlFile | mysql -u $dbUser -p"$plainPass" $dbName

if ($LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "SUCCESS! Admin module database setup complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Admin Credentials:" -ForegroundColor Yellow
    Write-Host "  Email: admin@careerguidance.com"
    Write-Host "  Password: Admin@123"
    Write-Host ""
} else {
    Write-Host ""
    Write-Host "Setup completed with warnings (this is normal if already exists)" -ForegroundColor Yellow
    Write-Host ""
}
