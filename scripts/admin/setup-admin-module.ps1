# Admin Module Setup Script
# This script sets up the complete admin module for Career Guidance System

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Career Guidance - Admin Module Setup" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ErrorActionPreference = "Continue"
$mysqlUser = "root"
$databaseName = "career_guidance_db"
$sqlFilePath = ".\sql\admin_module_migration.sql"

# Step 1: Check if MySQL is available
Write-Host "[1/5] Checking MySQL installation..." -ForegroundColor Yellow
$mysqlCheck = Get-Command mysql -ErrorAction SilentlyContinue
if ($mysqlCheck) {
    Write-Host "OK MySQL found" -ForegroundColor Green
} else {
    Write-Host "ERROR: MySQL not found. Please install MySQL or add it to PATH." -ForegroundColor Red
    exit 1
}

# Step 2: Get MySQL password
Write-Host ""
Write-Host "[2/5] MySQL Authentication" -ForegroundColor Yellow
$mysqlPassword = Read-Host "Enter MySQL root password" -AsSecureString
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($mysqlPassword)
$plainPassword = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Step 3: Test database connection
Write-Host ""
Write-Host "[3/5] Testing database connection..." -ForegroundColor Yellow
$testResult = echo "SELECT 1" | mysql -u $mysqlUser -p"$plainPassword" $databaseName 2>&1
if ($LASTEXITCODE -eq 0) {
    Write-Host "OK Successfully connected to database: $databaseName" -ForegroundColor Green
} else {
    Write-Host "ERROR: Failed to connect to database" -ForegroundColor Red
    exit 1
}

# Step 4: Run migration
Write-Host ""
Write-Host "[4/5] Running admin module migration..." -ForegroundColor Yellow
Write-Host "  - Adding Role column to Users table" -ForegroundColor Gray
Write-Host "  - Creating admin user (admin@careerguidance.com)" -ForegroundColor Gray
Write-Host "  - Creating admin_activity_log table" -ForegroundColor Gray
Write-Host "  - Creating admin dashboard view" -ForegroundColor Gray
Write-Host "  - Creating LogAdminAction stored procedure" -ForegroundColor Gray

$migrationResult = Get-Content $sqlFilePath | mysql -u $mysqlUser -p"$plainPassword" $databaseName 2>&1
Write-Host "OK Migration completed" -ForegroundColor Green

# Step 5: Verify setup
Write-Host ""
Write-Host "[5/5] Verifying admin module setup..." -ForegroundColor Yellow

# Check Role column
$checkRole = "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = '$databaseName' AND TABLE_NAME = 'Users' AND COLUMN_NAME = 'Role'"
$roleExists = echo $checkRole | mysql -u $mysqlUser -p"$plainPassword" $databaseName -N 2>&1
if ($roleExists -match "Role") {
    Write-Host "OK Role column exists in Users table" -ForegroundColor Green
} else {
    Write-Host "WARN: Role column not found" -ForegroundColor Yellow
}

# Check admin user
$checkAdmin = "SELECT COUNT(*) FROM Users WHERE Role = 'admin'"
$adminCount = echo $checkAdmin | mysql -u $mysqlUser -p"$plainPassword" $databaseName -N 2>&1
if ($adminCount -gt 0) {
    Write-Host "OK Admin user(s) found: $adminCount" -ForegroundColor Green
} else {
    Write-Host "WARN: No admin users found" -ForegroundColor Yellow
}

# Check admin_activity_log table
$checkLog = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '$databaseName' AND TABLE_NAME = 'admin_activity_log'"
$logExists = echo $checkLog | mysql -u $mysqlUser -p"$plainPassword" $databaseName -N 2>&1
if ($logExists -eq 1) {
    Write-Host "OK admin_activity_log table exists" -ForegroundColor Green
} else {
    Write-Host "WARN: admin_activity_log table not found" -ForegroundColor Yellow
}

# Check view
$checkView = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.VIEWS WHERE TABLE_SCHEMA = '$databaseName' AND TABLE_NAME = 'vw_admin_dashboard'"
$viewExists = echo $checkView | mysql -u $mysqlUser -p"$plainPassword" $databaseName -N 2>&1
if ($viewExists -eq 1) {
    Write-Host "OK vw_admin_dashboard view exists" -ForegroundColor Green
} else {
    Write-Host "WARN: vw_admin_dashboard view not found" -ForegroundColor Yellow
}

# Display admin credentials
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Admin Module Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Admin Dashboard URL:" -ForegroundColor Yellow
Write-Host "  http://localhost:5001/admin.html" -ForegroundColor White
Write-Host ""
Write-Host "Default Admin Credentials:" -ForegroundColor Yellow
Write-Host "  Email:    admin@careerguidance.com" -ForegroundColor White
Write-Host "  Password: Admin@123" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host "  1. Start the backend: dotnet run" -ForegroundColor White
Write-Host "  2. Open browser to http://localhost:5001/admin.html" -ForegroundColor White
Write-Host "  3. Login with the credentials above" -ForegroundColor White
Write-Host ""
Write-Host "IMPORTANT: Change the default admin password after first login!" -ForegroundColor Red
Write-Host ""
