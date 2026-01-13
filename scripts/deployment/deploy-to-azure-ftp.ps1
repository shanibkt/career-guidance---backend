# Azure App Service FTP Deployment Script
# Deploy publish folder to Azure using FTP credentials

Write-Host "======================================" -ForegroundColor Cyan
Write-Host "Azure App Service FTP Deployment" -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

# Configuration
$ftpHost = "ftps://waws-prod-bn1-347.ftp.azurewebsites.windows.net"  # Change this to your actual FTP host
$ftpUsername = ""  # Your deployment username (e.g., career-guaidance\$username or username)
$ftpPassword = ""  # Your deployment password
$localPath = "c:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish"
$remotePath = "/site/wwwroot"

Write-Host "IMPORTANT: You need to provide your Azure FTP credentials" -ForegroundColor Yellow
Write-Host ""
Write-Host "To get your FTP credentials:" -ForegroundColor Green
Write-Host "1. Ask the Azure account owner to send you the deployment credentials" -ForegroundColor White
Write-Host "2. OR ask them to go to Azure Portal > App Service > Deployment Center" -ForegroundColor White
Write-Host "3. Click 'FTPS Credentials' tab" -ForegroundColor White
Write-Host "4. Copy the FTPS endpoint, username, and password" -ForegroundColor White
Write-Host ""

# Prompt for credentials if not set
if ([string]::IsNullOrEmpty($ftpUsername)) {
    Write-Host "Enter FTP Username (e.g., career-guaidance\deployuser):" -ForegroundColor Cyan
    $ftpUsername = Read-Host
}

if ([string]::IsNullOrEmpty($ftpPassword)) {
    Write-Host "Enter FTP Password:" -ForegroundColor Cyan
    $securePassword = Read-Host -AsSecureString
    $ftpPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
    )
}

Write-Host ""
Write-Host "Starting deployment..." -ForegroundColor Green
Write-Host "Local Path: $localPath" -ForegroundColor White
Write-Host "Remote Path: $remotePath" -ForegroundColor White
Write-Host ""

# Create FTP request using WinSCP or native PowerShell
# Option 1: Using WinSCP (if installed)
if (Get-Command "winscp.com" -ErrorAction SilentlyContinue) {
    Write-Host "Using WinSCP for deployment..." -ForegroundColor Green
    
    $winscpScript = @"
open ftp://${ftpUsername}:${ftpPassword}@${ftpHost}
lcd "$localPath"
cd $remotePath
synchronize remote -delete
exit
"@
    
    $winscpScript | winscp.com /script=
    
} else {
    Write-Host "WinSCP not found. Using FileZilla or manual upload recommended." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "=== Alternative: Use FileZilla ===" -ForegroundColor Cyan
    Write-Host "1. Download FileZilla Client: https://filezilla-project.org/download.php?type=client" -ForegroundColor White
    Write-Host "2. Open FileZilla and enter:" -ForegroundColor White
    Write-Host "   Host: $ftpHost" -ForegroundColor Yellow
    Write-Host "   Username: $ftpUsername" -ForegroundColor Yellow
    Write-Host "   Password: [Your Password]" -ForegroundColor Yellow
    Write-Host "   Port: 21" -ForegroundColor Yellow
    Write-Host "3. Navigate to /site/wwwroot on the right panel" -ForegroundColor White
    Write-Host "4. Select all files in: $localPath" -ForegroundColor White
    Write-Host "5. Drag and drop to upload (or right-click > Upload)" -ForegroundColor White
    Write-Host ""
    Write-Host "=== Alternative: Use Azure Portal ===" -ForegroundColor Cyan
    Write-Host "Ask the account owner to:" -ForegroundColor White
    Write-Host "1. Go to Azure Portal > App Service > Advanced Tools (Kudu)" -ForegroundColor White
    Write-Host "2. Click 'Go' to open Kudu" -ForegroundColor White
    Write-Host "3. Navigate to Debug Console > CMD" -ForegroundColor White
    Write-Host "4. Go to site/wwwroot folder" -ForegroundColor White
    Write-Host "5. Drag and drop all files from publish folder" -ForegroundColor White
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
