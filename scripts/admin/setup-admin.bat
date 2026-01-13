@echo off
echo ========================================
echo Admin Module - Quick Database Setup
echo ========================================
echo.

REM Check if MySQL is available
where mysql >nul 2>nul
if %ERRORLEVEL% NEQ 0 (
    echo ERROR: MySQL not found in PATH
    echo Please install MySQL or add it to your PATH variable
    pause
    exit /b 1
)

echo MySQL found!
echo.

REM Get MySQL credentials
set /p MYSQL_USER="MySQL username (default: root): "
if "%MYSQL_USER%"=="" set MYSQL_USER=root

set /p MYSQL_PASS="MySQL password: "

set DB_NAME=career_guidance_db

echo.
echo Running database migration...
echo.

REM Run the SQL file
mysql -u %MYSQL_USER% -p%MYSQL_PASS% %DB_NAME% < sql\admin_module_migration.sql

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo SUCCESS! Database setup complete!
    echo ========================================
    echo.
    echo Admin credentials:
    echo   Email: admin@careerguidance.com
    echo   Password: Admin@123
    echo.
    echo Next steps:
    echo   1. Refresh test page: http://localhost:5001/test-connection.html
    echo   2. Or login: http://localhost:5001/admin.html
    echo.
) else (
    echo.
    echo ========================================
    echo Setup completed with warnings
    echo ========================================
    echo This is normal if the admin user already exists.
    echo Try logging in at: http://localhost:5001/admin.html
    echo.
)

pause
