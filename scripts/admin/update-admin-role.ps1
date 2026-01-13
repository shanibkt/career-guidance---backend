# ============================================
# Update Admin Role Case
# Connects to MySQL and updates admin role from 'admin' to 'Admin'
# ============================================

$MySQLAdminPath = "C:\Program Files\MySQL\MySQL Server 8.0\bin\mysql.exe"

# Database credentials
$dbHost = "sql.freedb.tech"
$dbUser = "freedb_career_user"
$dbPassword = "pnD6F@R8YX9MNEu"
$dbName = "freedb_career_guidence"

Write-Host "================================" -ForegroundColor Cyan
Write-Host "Updating Admin Role Case" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan

# Check if MySQL is installed
if (!(Test-Path $MySQLAdminPath)) {
    Write-Host "⚠ MySQL not found at: $MySQLAdminPath" -ForegroundColor Yellow
    Write-Host "Please update the admin role manually in your database:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "SQL Command:" -ForegroundColor White
    Write-Host "UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@careerguidance.com';" -ForegroundColor Green
    Write-Host ""
    
    # Open browser to freedb.tech
    Start-Process "https://www.freedb.tech/phpMyAdmin/"
    Write-Host "✓ Opened phpMyAdmin in browser" -ForegroundColor Green
    Write-Host ""
    Write-Host "Manual Steps:" -ForegroundColor Yellow
    Write-Host "1. Login to phpMyAdmin" -ForegroundColor White
    Write-Host "2. Select database: freedb_career_guidence" -ForegroundColor White
    Write-Host "3. Go to SQL tab" -ForegroundColor White
    Write-Host "4. Paste and execute:" -ForegroundColor White
    Write-Host "   UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@careerguidance.com';" -ForegroundColor Green
    Write-Host ""
    exit
}

# SQL query to update role
$sqlQuery = @"
UPDATE Users SET Role = 'Admin' WHERE Email = 'admin@careerguidance.com' OR Username = 'admin' OR Role = 'admin';
SELECT Id, Username, Email, Role FROM Users WHERE Role = 'Admin' OR Role = 'admin';
"@

# Create temp SQL file
$tempSqlFile = "$env:TEMP\update_admin_role.sql"
$sqlQuery | Out-File -FilePath $tempSqlFile -Encoding ASCII

try {
    Write-Host "Connecting to database..." -ForegroundColor Yellow
    
    # Execute MySQL command
    & $MySQLAdminPath -h $dbHost -u $dbUser -p$dbPassword $dbName -e $sqlQuery
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "✓ Admin role updated successfully!" -ForegroundColor Green
        Write-Host "  Role is now 'Admin' (with capital A)" -ForegroundColor Green
    } else {
        Write-Host ""
        Write-Host "✗ Update failed with exit code: $LASTEXITCODE" -ForegroundColor Red
        Write-Host "  Please update manually using phpMyAdmin" -ForegroundColor Yellow
    }
} catch {
    Write-Host ""
    Write-Host "✗ Error: $_" -ForegroundColor Red
    Write-Host "  Please update manually using phpMyAdmin" -ForegroundColor Yellow
} finally {
    # Cleanup temp file
    if (Test-Path $tempSqlFile) {
        Remove-Item $tempSqlFile -Force
    }
}

Write-Host ""
Write-Host "================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host "1. Refresh your admin panel" -ForegroundColor White
Write-Host "2. Login again with: admin@careerguidance.com / Admin@123" -ForegroundColor White
Write-Host "3. The 403 error should now be resolved" -ForegroundColor White
Write-Host ""
