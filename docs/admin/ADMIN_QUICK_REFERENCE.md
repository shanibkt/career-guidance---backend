# 🎓 Admin Module - Quick Reference Card

## ⚡ Quick Commands

### Start Backend
```powershell
cd "c:\Users\Dell\Desktop\Career guidence\career-guidance---backend"
dotnet run
```

### Stop Backend
```
Ctrl + C
```

### Build Backend
```powershell
dotnet build
```

### Run Database Setup
```powershell
.\setup-admin.ps1
```

---

## 🔑 Default Credentials

| Field | Value |
|-------|-------|
| Email | `admin@careerguidance.com` |
| Password | `Admin@123` |
| URL | `http://localhost:5001/admin.html` |

---

## 📡 API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/auth/login` | POST | Login (get JWT token) |
| `/api/admin/users` | GET | List all users |
| `/api/admin/users/{id}` | GET | Get user details |
| `/api/admin/users/{id}` | DELETE | Delete user |
| `/api/admin/users/{id}/role` | PUT | Change user role |
| `/api/admin/stats` | GET | System statistics |
| `/api/admin/export/users` | GET | Export to CSV |
| `/api/admin/analytics/growth` | GET | User growth data |

---

## 🗄️ Database Quick Commands

### Check Admin User
```sql
SELECT Id, Username, Email, Role 
FROM Users 
WHERE Role = 'admin';
```

### Reset Admin Password
```sql
UPDATE Users 
SET PasswordHash = '$2a$11$xHZf8c8VVJKJKxjW6J5iYO8vZ0xZQGqL5Y7qZ8yZ9yZ0yZ1yZ2yZ3',
    Role = 'admin'
WHERE Email = 'admin@careerguidance.com';
```

### Make User Admin
```sql
UPDATE Users 
SET Role = 'admin' 
WHERE Email = 'user@example.com';
```

### Check Role Column
```sql
SHOW COLUMNS FROM Users LIKE 'Role';
```

### View All Admins
```sql
SELECT Id, Username, Email, Role, CreatedAt 
FROM Users 
WHERE Role = 'admin';
```

---

## 🔧 Troubleshooting Quick Fixes

### Backend Won't Start (Port Busy)
```powershell
# Find process using port 5001
netstat -ano | findstr :5001

# Kill process (replace XXXX with PID)
Stop-Process -Id XXXX -Force
```

### Can't Login
```sql
-- Verify admin user exists with correct role
SELECT Email, Role FROM Users WHERE Email = 'admin@careerguidance.com';
```

### Build Errors (File Locked)
```powershell
# Stop all running instances
Get-Process | Where-Object {$_.ProcessName -like "*MyFirstApi*"} | Stop-Process -Force

# Clean and rebuild
dotnet clean
dotnet build
```

### No Data Showing
```sql
-- Check if users exist
SELECT COUNT(*) FROM Users;

-- Check if Role column exists
SHOW COLUMNS FROM Users;
```

---

## 📊 Dashboard Features

| Tab | Features |
|-----|----------|
| **Dashboard** | Total users, active users, statistics |
| **Users** | List, search, view details, delete |
| **Analytics** | Popular careers, growth trends |
| **Export** | Download CSV of all users |

---

## 🔐 Security Checklist

- [ ] Change default admin password
- [ ] Use HTTPS in production
- [ ] Don't commit passwords to Git
- [ ] Restrict admin access to internal network
- [ ] Regular database backups
- [ ] Monitor admin activity logs

---

## 📁 Key Files

| File | Purpose |
|------|---------|
| `Controllers/AdminController.cs` | Admin API endpoints |
| `Controllers/AuthController.cs` | Login and JWT handling |
| `Models/User.cs` | User model with Role |
| `wwwroot/admin.html` | Admin dashboard UI |
| `setup_admin.sql` | Database migration |
| `setup-admin.ps1` | Automated setup script |

---

## 🌐 Network Access

### Local Access
```
http://localhost:5001/admin.html
```

### Network Access
1. Find IP: `ipconfig`
2. Access: `http://YOUR_IP:5001/admin.html`

---

## 💡 Common Tasks

### Create New Admin
```sql
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES ('newadmin', 'New Admin', 'new@admin.com', 
        '$2a$11$[BCRYPT_HASH]', 'admin', NOW());
```

### Generate BCrypt Hash (C#)
```csharp
var hash = BCrypt.Net.BCrypt.HashPassword("YourPassword");
Console.WriteLine(hash);
```

### View Dashboard Stats
```sql
SELECT * FROM vw_admin_dashboard;
```

### Check Recent Activity
```sql
SELECT * FROM admin_activity_log 
ORDER BY created_at DESC 
LIMIT 10;
```

---

## 📞 Quick Help

**Issue**: Can't access admin panel
**Fix**: Ensure backend is running: `dotnet run`

**Issue**: 401 Unauthorized
**Fix**: Verify admin user role in database

**Issue**: Port 5001 in use
**Fix**: Kill process or change port in launchSettings.json

**Issue**: Build fails - file locked
**Fix**: Stop running app, then `dotnet clean && dotnet build`

---

## ⚙️ Configuration

### Database Connection
File: `appsettings.json`
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=sql.freedb.tech;..."
}
```

### JWT Settings
```json
"Jwt": {
  "Key": "your-secret-key",
  "Issuer": "MyFirstApi",
  "Audience": "MyFirstApiUsers",
  "ExpireMinutes": 43200
}
```

### Application URL
File: `Properties/launchSettings.json`
```json
"applicationUrl": "http://0.0.0.0:5001"
```

---

## 🎯 Testing Checklist

- [ ] Backend starts without errors
- [ ] Can access admin.html in browser
- [ ] Can login with admin credentials
- [ ] Dashboard shows statistics
- [ ] Can view list of users
- [ ] Search functionality works
- [ ] Can view user details
- [ ] Export to CSV works
- [ ] Can delete test user
- [ ] All tabs load correctly

---

## 🔄 Update Process

1. Stop backend
2. Pull latest code
3. Run `dotnet build`
4. Run database migrations if any
5. Restart backend
6. Test admin panel

---

**Need detailed help?** See `ADMIN_MODULE_COMPLETE_GUIDE.md`

---

*v1.0 - January 2026*
