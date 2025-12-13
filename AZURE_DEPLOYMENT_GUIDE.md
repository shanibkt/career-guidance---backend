# How to Deploy to Azure Without Full Account Access

## Method 1: Using FileZilla (Recommended - Easiest)

### Step 1: Get FTP Credentials
Ask the Azure account owner to provide:
- **FTP Host**: `ftps://waws-prod-xxx.ftp.azurewebsites.net` or check Azure Portal
- **Username**: Usually `career-guaidance\$username` or similar
- **Password**: Deployment password

To get credentials, they should:
1. Go to Azure Portal (portal.azure.com)
2. Find your App Service: `career-guaidance-ahemf5fqfgayg0fw`
3. Go to **Deployment Center** > **FTPS credentials** tab
4. Copy the credentials

### Step 2: Download FileZilla
1. Download from: https://filezilla-project.org/download.php?type=client
2. Install FileZilla Client (it's free)

### Step 3: Connect to Azure
1. Open FileZilla
2. Fill in the top bar:
   - **Host**: `ftps://waws-prod-xxx.ftp.azurewebsites.net` (from owner)
   - **Username**: (from owner)
   - **Password**: (from owner)
   - **Port**: 21
3. Click "Quickconnect"
4. Accept any SSL certificate warnings

### Step 4: Upload Files
1. **Left panel**: Navigate to your local publish folder:
   ```
   C:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish
   ```
2. **Right panel**: Navigate to `/site/wwwroot`
3. **Delete** all old files in `/site/wwwroot` (if any)
4. **Select all files** from left panel
5. **Drag and drop** to right panel (or right-click > Upload)
6. Wait for upload to complete (shows in bottom panel)

---

## Method 2: Using Web Deploy (If FileZilla doesn't work)

Ask the account owner to:
1. Go to Azure Portal > App Service > Deployment Center
2. Click **"Get publish profile"** button (downloads a .PublishSettings file)
3. Send you that file

Then you can:
1. Open Visual Studio or VS Code
2. Right-click on the backend project
3. Select "Publish"
4. Import the publish profile
5. Click "Publish"

---

## Method 3: Ask Owner to Upload Via Kudu (Fastest)

If you can't get credentials, ask the owner to:
1. Go to Azure Portal > App Service > Advanced Tools
2. Click **"Go"** to open Kudu console
3. Navigate to **Debug Console** > **CMD**
4. Click on **site** > **wwwroot** folder
5. **Drag and drop** all files from your `publish` folder directly into browser
6. Wait for upload to complete

You can ZIP your publish folder and send it to them to make it easier:
```powershell
Compress-Archive -Path "C:\Users\Dell\Desktop\Career guidence\career-guidance---backend\publish\*" -DestinationPath "C:\Users\Dell\Desktop\backend-deploy.zip" -Force
```

---

## Method 4: Using PowerShell FTP (Manual)

If you have the FTP credentials, run:
```powershell
.\deploy-to-azure-ftp.ps1
```

---

## After Deployment

### Verify Deployment:
1. Visit: `https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net`
2. You should see Swagger UI or API documentation
3. Test login endpoint: `https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net/api/auth/login`

### Update Flutter App:
Change `api_config.dart` back to:
```dart
static const String baseUrl = 'https://career-guaidance-ahemf5fqfgayg0fw.canadacentral-01.azurewebsites.net';
```

### If Errors Occur:
Ask the owner to check Azure Portal > App Service > Log stream for error messages.

---

## Current Workaround (Until Azure is Deployed)

Keep using local backend:
1. Run backend: `dotnet run --urls "http://0.0.0.0:5001"`
2. Use `http://10.0.2.2:5001` for emulator
3. Use `http://192.168.40.1:5001` for physical device
