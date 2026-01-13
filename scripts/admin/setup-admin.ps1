# Admin Module Setup Script for PowerShell
# This script helps set up the admin module for Career Guidance Backend

Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  Career Guidance - Admin Module Setup" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""

# Database connection details from appsettings.json
$dbServer = "sql.freedb.tech"
$dbPort = "3306"
$dbName = "freedb_career_guidence"
$dbUser = "freedb_shanib"
$dbPassword = "pSh6TDn`$Ma!wWrk"

Write-Host "Step 1: Checking MySQL availability..." -ForegroundColor Yellow

# Check if mysql client is available
try {
    $mysqlVersion = mysql --version 2>&1
    Write-Host "✓ MySQL client found: $mysqlVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ MySQL client not found!" -ForegroundColor Red
    Write-Host "Please install MySQL client or use MySQL Workbench to run setup_admin.sql manually" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Manual Setup Instructions:" -ForegroundColor Cyan
    Write-Host "1. Open MySQL Workbench or your preferred MySQL client" -ForegroundColor White
    Write-Host "2. Connect to: $dbServer (port: $dbPort)" -ForegroundColor White
    Write-Host "3. Database: $dbName" -ForegroundColor White
    Write-Host "4. Run the file: setup_admin.sql" -ForegroundColor White
    Read-Host "Press Enter to continue"
    exit
}

Write-Host ""
Write-Host "Step 2: Running database migration..." -ForegroundColor Yellow
Write-Host "Connecting to: $dbServer/$dbName" -ForegroundColor Gray

# Execute the SQL script
$sqlFile = "setup_admin.sql"
if (Test-Path $sqlFile) {
    try {
        # Use mysql command to execute the script
        $env:MYSQL_PWD = $dbPassword
        $result = mysql -h $dbServer -P $dbPort -u $dbUser $dbName -e "source $sqlFile" 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✓ Database migration completed successfully!" -ForegroundColor Green
        } else {
            Write-Host "✗ Migration failed. Error output:" -ForegroundColor Red
            Write-Host $result -ForegroundColor Red
            Write-Host ""
            Write-Host "You can manually run the SQL file using:" -ForegroundColor Yellow
            Write-Host "mysql -h $dbServer -P $dbPort -u $dbUser -p $dbName < $sqlFile" -ForegroundColor White
        }
        Remove-Item Env:\MYSQL_PWD
    } catch {
        Write-Host "✗ Error executing SQL: $_" -ForegroundColor Red
    }
} else {
    Write-Host "✗ setup_admin.sql file not found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "Step 3: Verifying admin user..." -ForegroundColor Yellow

# Verify admin user exists
try {
    $env:MYSQL_PWD = $dbPassword
    $adminCheck = mysql -h $dbServer -P $dbPort -u $dbUser $dbName -e "SELECT Username, Email, Role FROM Users WHERE Role='admin' LIMIT 1" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Admin user verified:" -ForegroundColor Green
        Write-Host $adminCheck -ForegroundColor White
    }
    Remove-Item Env:\MYSQL_PWD
} catch {
    Write-Host "✗ Could not verify admin user" -ForegroundColor Red
}

Write-Host ""
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host "  Setup Complete!" -ForegroundColor Green
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Default Admin Credentials:" -ForegroundColor Yellow
Write-Host "  Email: admin@careerguidance.com" -ForegroundColor White
Write-Host "  Password: Admin@123" -ForegroundColor White
Write-Host ""
Write-Host "Access Admin Dashboard at:" -ForegroundColor Yellow
Write-Host "  http://localhost:5001/admin.html" -ForegroundColor White
Write-Host ""
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "1. Stop the running backend (if any)" -ForegroundColor White
Write-Host "2. Run: dotnet run" -ForegroundColor White
Write-Host "3. Open browser to: http://localhost:5001/admin.html" -ForegroundColor White
Write-Host "4. Login with admin credentials above" -ForegroundColor White
Write-Host ""

Read-Host "Press Enter to exit"
