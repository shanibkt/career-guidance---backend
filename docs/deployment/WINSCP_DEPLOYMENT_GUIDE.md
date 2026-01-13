# WinSCP Deployment Guide for Azure App Service

## Step-by-Step Instructions

### Step 1: Get Your FTP Credentials from Azure Owner

Ask them to provide:
- **Host/Server**: Usually `ftps://waws-prod-xxx.ftp.azurewebsites.net`
- **Username**: Usually in format `career-guaidance\$username` or just the username part
- **Password**: The deployment password

To get these, they should:
1. Go to Azure Portal (portal.azure.com)
2. Navigate to: App Services → career-guaidance-ahemf5fqfgayg0fw
3. Click: **Deployment Center** (left menu)
4. Click: **FTPS credentials** tab
5. Copy the details shown there

### Step 2: Open WinSCP and Connect

1. **Open WinSCP**

2. **In the Login Dialog**, enter:
   - **File protocol**: `FTP` or `FTPS`
   - **Encryption**: `TLS/SSL Explicit encryption`
   - **Host name**: `waws-prod-xxx.ftp.azurewebsites.net` (remove ftps:// prefix)
   - **Port number**: `21`
   - **User name**: Your deployment username
   - **Password**: Your deployment password

3. Click **"Login"**

4. If certificate warning appears, click **"Yes"** to trust the certificate

### Step 3: Navigate to Deployment Folder

Once connected:

**Left Panel (Your Computer)**:
```
C:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish
```

**Right Panel (Azure Server)**:
Navigate to:
```
/site/wwwroot
```

### Step 4: Clear Old Files (Important!)

In the **Right Panel** (Azure):
1. Select all files and folders in `/site/wwwroot`
2. Press `Delete` key
3. Confirm deletion

### Step 5: Upload New Files

1. In the **Left Panel**, navigate to your publish folder:
   ```
   C:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish
   ```

2. **Select ALL files and folders** (Ctrl+A)

3. **Drag and drop** to the Right Panel

   OR

   Right-click → **Upload**

4. Wait for upload to complete (watch the progress at bottom)

### Step 6: Verify Critical Files Are Uploaded

Make sure these files exist in `/site/wwwroot`:
- ✅ `MyFirstApi.dll`
- ✅ `web.config`
- ✅ `appsettings.json`
- ✅ `appsettings.Development.json`
- ✅ All DLL files (BCrypt, MySQL, etc.)
- ✅ `wwwroot` folder
- ✅ `runtimes` folder

### Step 7: Restart the App Service

After upload completes, ask the Azure owner to:
1. Go to Azure Portal → App Service
2. Click **"Restart"** button at the top
3. Wait 30 seconds

OR

You can trigger restart by creating a file:
- In WinSCP, right-click in `/site/wwwroot`
- Create new file named `restart.txt`
- Delete it immediately
- This triggers an app restart

### Step 8: Test Deployment

Wait 1-2 minutes after restart, then test:

1. **Visit in browser**:
   ```
   https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net
   ```
   - Should show Swagger UI or API documentation

2. **Test API endpoint**:
   Open PowerShell and run:
   ```powershell
   Invoke-WebRequest -Uri "https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net/api/auth/login" -Method POST -Headers @{"Content-Type"="application/json"} -Body '{"email":"test@test.com","password":"test"}' -UseBasicParsing
   ```
   - Should return 401 Unauthorized (expected - user doesn't exist yet)
   - NOT 404 Not Found

### Step 9: Update Flutter App

Once Azure is working, update your Flutter app:

Edit `lib/core/config/api_config.dart`:
```dart
static const String baseUrl = 'https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net';
```

---

## Troubleshooting

### "Connection Failed" in WinSCP
- Try changing **File protocol** to just `FTP` (without S)
- Try port `990` instead of `21`
- Make sure you removed `ftps://` from hostname

### "Permission Denied"
- Double-check username format (might need `appname\username` or just `username`)
- Verify password is correct

### Still Getting 404 After Upload
- Make sure `web.config` was uploaded
- Check if `/site/wwwroot` has all the files
- Ask owner to check Azure Portal → Log Stream for errors

### App Not Starting
- Ask owner to check Application Logs in Azure Portal
- Common issues:
  - Missing database connection
  - Missing environment variables
  - .NET runtime version mismatch

---

## Quick Connection Settings Summary

**Session Settings:**
```
File protocol: FTP or FTPS
Encryption: TLS/SSL Explicit encryption
Host: waws-prod-xxx.ftp.azurewebsites.net (get from Azure)
Port: 21
Username: [Get from Azure owner]
Password: [Get from Azure owner]
```

**Upload Path:**
```
Local:  C:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish
Remote: /site/wwwroot
```
