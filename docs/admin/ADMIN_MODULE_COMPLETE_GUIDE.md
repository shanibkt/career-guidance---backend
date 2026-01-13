# 🎓 Admin Module - Complete Setup & User Guide

## 📋 Table of Contents
1. [Overview](#overview)
2. [Quick Start (5 Minutes)](#quick-start)
3. [Detailed Setup Instructions](#detailed-setup)
4. [Accessing the Admin Panel](#accessing-admin-panel)
5. [Features & Capabilities](#features)
6. [API Documentation](#api-documentation)
7. [Troubleshooting](#troubleshooting)
8. [Security Best Practices](#security)

---

## 🎯 Overview

The Admin Module provides a complete administrative interface for managing your Career Guidance application. It includes:

- **User Management**: View, search, filter, and delete users
- **Analytics Dashboard**: Real-time statistics and insights
- **Progress Tracking**: Monitor user engagement and learning progress
- **Export Functionality**: Download user data as CSV
- **Role Management**: Promote users to admin status

### Architecture Components

```
┌─────────────────────────────────────────────────────────┐
│                    Admin Dashboard                       │
│              (wwwroot/admin.html)                        │
└──────────────────────┬──────────────────────────────────┘
                       │ JWT Authentication
                       ▼
┌─────────────────────────────────────────────────────────┐
│              Backend API (ASP.NET Core)                  │
│    ┌──────────────┐  ┌──────────────────────────┐      │
│    │AuthController│  │   AdminController         │      │
│    │(Login/Token) │  │(7 endpoints for mgmt)     │      │
│    └──────────────┘  └──────────────────────────┘      │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              MySQL Database                              │
│   - Users (with Role column)                            │
│   - admin_activity_log                                  │
│   - vw_admin_dashboard (view)                           │
│   - All user data tables                                │
└─────────────────────────────────────────────────────────┘
```

---

## 🚀 Quick Start

### Prerequisites
- ASP.NET Core 9.0 SDK installed
- MySQL client (optional, for automated setup)
- Backend application configured and running

### 3-Step Setup

#### Step 1: Run Database Setup (2 minutes)

**Option A: Automated Setup (If you have MySQL client)**
```powershell
cd "c:\Users\Dell\Desktop\Career guidence\career-guidance---backend"
.\setup-admin.ps1
```

**Option B: Manual Setup (Using MySQL Workbench or any MySQL client)**
```bash
# Connect to your MySQL database
mysql -h sql.freedb.tech -P 3306 -u freedb_shanib -p freedb_career_guidence

# Or use MySQL Workbench to run:
# File > Open SQL Script > setup_admin.sql > Execute
```

#### Step 2: Stop and Restart Backend (1 minute)

```powershell
# Stop any running instance (Ctrl+C in the terminal)

# Restart the backend
cd "c:\Users\Dell\Desktop\Career guidence\career-guidance---backend"
dotnet run
```

You should see:
```
Now listening on: http://0.0.0.0:5001
```

#### Step 3: Access Admin Dashboard (30 seconds)

1. Open your browser
2. Navigate to: `http://localhost:5001/admin.html`
3. Login with default credentials:
   - **Email**: `admin@careerguidance.com`
   - **Password**: `Admin@123`

✅ **You're all set!** You should now see the admin dashboard.

---

## 📖 Detailed Setup Instructions

### Database Migration Details

The `setup_admin.sql` script performs the following operations:

#### 1. **Add Role Column**
```sql
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS Role VARCHAR(20) DEFAULT 'user';
```
- Adds a `Role` column to track user permissions
- Default value: `'user'`
- Admin users have: `'admin'`

#### 2. **Create Admin User**
```sql
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES ('admin', 'System Administrator', 'admin@careerguidance.com', 
        '[BCrypt Hash]', 'admin', NOW())
ON DUPLICATE KEY UPDATE Role = 'admin';
```
- Creates default admin account
- Password: `Admin@123` (BCrypt hashed)
- Updates existing user if email already exists

#### 3. **Create Activity Log Table**
```sql
CREATE TABLE IF NOT EXISTS admin_activity_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_user_id INT NULL,
    description TEXT NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    -- Foreign keys and indexes
);
```
- Tracks all admin actions
- Links to Users table via `admin_id`
- Records action type, target user, and IP address

#### 4. **Create Dashboard View**
```sql
CREATE OR REPLACE VIEW vw_admin_dashboard AS
SELECT 
    total_users,
    active_users_today,
    avg_progress,
    -- ... and more stats
```
- Pre-aggregated statistics for dashboard
- Improves performance for analytics queries

#### 5. **Create Stored Procedure**
```sql
CREATE PROCEDURE LogAdminAction(
    IN p_admin_id INT,
    IN p_action_type VARCHAR(50),
    -- ... other parameters
)
```
- Helper function to log admin activities
- Can be called from application code

### Backend Configuration

The admin module uses your existing backend configuration:

**From `appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=sql.freedb.tech;..."
  },
  "Jwt": {
    "Key": "your-secret-key",
    "Issuer": "MyFirstApi",
    "Audience": "MyFirstApiUsers",
    "ExpireMinutes": 43200
  }
}
```

**Files Modified:**
- ✅ `Models/User.cs` - Added `Role` property
- ✅ `Controllers/AuthController.cs` - Updated login query to include Role
- ✅ `Controllers/AdminController.cs` - Already implemented
- ✅ `wwwroot/admin.html` - Already created

---

## 🖥️ Accessing the Admin Panel

### Local Access

**URL**: `http://localhost:5001/admin.html`

**Default Credentials:**
- Email: `admin@careerguidance.com`
- Password: `Admin@123`

### Network Access (Same LAN)

To access from other devices on your network:

1. Find your computer's IP address:
```powershell
ipconfig | Select-String "IPv4"
# Example output: 192.168.1.100
```

2. Update backend to listen on all interfaces (already configured):
```json
// Properties/launchSettings.json
"applicationUrl": "http://0.0.0.0:5001"
```

3. Access from any device on the network:
```
http://192.168.1.100:5001/admin.html
```

4. **Optional**: Update admin.html if you need to hardcode the API URL:
```javascript
// Line 507 in admin.html
const API_URL = 'http://192.168.1.100:5001/api';
```

---

## ✨ Features & Capabilities

### 1. Dashboard Tab 📊

**Displays:**
- Total Users
- Active Users (Today/This Week)
- Total Careers Selected
- Total Videos Watched
- Total Resumes Created
- Total Chat Sessions
- Average User Progress

**Real-time Updates:**
- Statistics refresh on tab switch
- Data pulled from `vw_admin_dashboard` view

### 2. Users Management Tab 👥

**Features:**
- **List View**: Paginated table of all users (20 per page)
- **Search**: Find users by name, email, or username
- **Sort**: Click column headers to sort
- **Filters**: 
  - Sort by: Created Date, Username, Full Name, Email, Last Active
  - Order: Ascending/Descending

**User Information Displayed:**
- User ID, Username, Full Name, Email
- Selected Career Path
- Overall Progress (%)
- Videos: Completed/Total
- Last Active timestamp

**Actions:**
- **View**: Opens detailed modal with:
  - Basic Information
  - Profile Details
  - Career Progress
  - Video Watch History
  - Resume Data
  - Chat History
- **Delete**: Removes user and all associated data (with confirmation)

### 3. Analytics Tab 📈

**Visualizations:**
- Popular Careers Chart (Top 10)
- User Growth Trends
- Recent User Activities

**Insights:**
- Career selection patterns
- User engagement metrics
- Activity timeline

### 4. Export Tab 📥

**CSV Export Includes:**
- User ID, Username, Full Name, Email
- Creation Date
- Selected Career
- Progress Percentage
- Videos Watched/Completed

**Usage:**
- Click "Export Users" button
- Downloads as `users_export_YYYYMMDD_HHmmss.csv`
- Opens in Excel or any CSV viewer

---

## 📡 API Documentation

### Authentication

All admin endpoints require:
```http
Authorization: Bearer {JWT_TOKEN}
```

The JWT token must include:
- `role` claim with value `"admin"` or `"Admin"`

### Endpoints

#### 1. Get All Users
```http
GET /api/admin/users?page=1&pageSize=20&search=&sortBy=createdAt&sortOrder=desc
```

**Query Parameters:**
| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| page | int | 1 | Page number |
| pageSize | int | 20 | Items per page |
| search | string | "" | Search term |
| sortBy | string | "createdAt" | Sort column |
| sortOrder | string | "desc" | asc/desc |

**Response:**
```json
{
  "users": [
    {
      "userId": 1,
      "username": "john_doe",
      "fullName": "John Doe",
      "email": "john@example.com",
      "createdAt": "2024-01-01T10:00:00",
      "selectedCareer": "Data Science",
      "totalVideosWatched": 25,
      "completedVideos": 18,
      "overallProgress": 72.5,
      "hasResume": true,
      "lastActive": "2024-12-01T15:30:00",
      "totalWatchTimeMinutes": 420
    }
  ],
  "totalUsers": 150,
  "currentPage": 1,
  "pageSize": 20,
  "totalPages": 8
}
```

#### 2. Get User Details
```http
GET /api/admin/users/{userId}
```

**Response:**
```json
{
  "userId": 1,
  "username": "john_doe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "createdAt": "2024-01-01T10:00:00",
  "profile": { /* demographics, education, skills */ },
  "career": { /* progress, courses, dates */ },
  "videoProgress": [ /* watch history array */ ],
  "resume": { /* resume data */ },
  "chatHistory": [ /* chat sessions array */ ]
}
```

#### 3. Get System Statistics
```http
GET /api/admin/stats
```

**Response:**
```json
{
  "totalUsers": 150,
  "activeUsersToday": 25,
  "activeUsersWeek": 89,
  "totalCareersSelected": 120,
  "totalVideosWatched": 3500,
  "totalResumes": 95,
  "totalChatSessions": 450,
  "averageProgress": 65.8,
  "popularCareers": [ /* array of career counts */ ],
  "recentActivities": [ /* array of recent actions */ ]
}
```

#### 4. Delete User
```http
DELETE /api/admin/users/{userId}
```

**Response:**
```json
{
  "message": "User deleted successfully"
}
```

**⚠️ Warning:** This cascades to all related data:
- User profiles
- Career progress
- Video watch history
- Resumes
- Chat history

#### 5. Update User Role
```http
PUT /api/admin/users/{userId}/role
Content-Type: application/json

{
  "role": "admin"
}
```

**Response:**
```json
{
  "message": "User role updated successfully"
}
```

#### 6. Export Users CSV
```http
GET /api/admin/export/users
```

**Response:** CSV file download

#### 7. Get User Growth Analytics
```http
GET /api/admin/analytics/growth?days=30
```

**Response:**
```json
[
  {
    "date": "2024-01-01",
    "count": 5
  },
  {
    "date": "2024-01-02",
    "count": 8
  }
]
```

---

## 🔧 Troubleshooting

### Issue: Can't Login to Admin Panel

**Symptoms:**
- "Invalid credentials" error
- 401 Unauthorized

**Solutions:**

1. **Verify admin user exists:**
```sql
SELECT Id, Username, Email, Role 
FROM Users 
WHERE Email = 'admin@careerguidance.com';
```

Expected: One row with `Role = 'admin'`

2. **Reset admin password:**
```sql
-- Generate new BCrypt hash for "Admin@123"
UPDATE Users 
SET PasswordHash = '$2a$11$xHZf8c8VVJKJKxjW6J5iYO8vZ0xZQGqL5Y7qZ8yZ9yZ0yZ1yZ2yZ3',
    Role = 'admin'
WHERE Email = 'admin@careerguidance.com';
```

3. **Check Role column exists:**
```sql
SHOW COLUMNS FROM Users LIKE 'Role';
```

If missing, run `setup_admin.sql` again.

### Issue: "No data showing" in Dashboard

**Solutions:**

1. **Check if users exist:**
```sql
SELECT COUNT(*) FROM Users;
```

2. **Verify database connection:**
- Check `appsettings.json` connection string
- Test connectivity to MySQL server

3. **Check browser console for errors:**
- Press F12 in browser
- Look for API errors in Console tab
- Check Network tab for failed requests

### Issue: Build Errors

**If you see compilation errors:**

1. **Verify User.cs has Role property:**
```csharp
public class User
{
    // ... other properties
    public string Role { get; set; } = "user";
}
```

2. **Clean and rebuild:**
```powershell
dotnet clean
dotnet build
```

3. **Stop running instance before building:**
```powershell
# Find process
Get-Process | Where-Object {$_.ProcessName -like "*MyFirstApi*"}

# Kill if needed (replace XXXX with process ID)
Stop-Process -Id XXXX -Force

# Then rebuild
dotnet build
```

### Issue: Port Already in Use

**Symptoms:**
```
Failed to bind to address http://0.0.0.0:5001
```

**Solutions:**

1. **Find what's using port 5001:**
```powershell
netstat -ano | findstr :5001
```

2. **Kill the process:**
```powershell
Stop-Process -Id XXXX -Force
```

3. **Change port (if needed):**
Edit `Properties/launchSettings.json`:
```json
"applicationUrl": "http://0.0.0.0:5002"
```

### Issue: CORS Errors

**Symptoms:**
- "Access to fetch has been blocked by CORS policy"

**Solution:**
CORS is already configured in `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

Ensure `app.UseCors("AllowAll");` is called before routing.

---

## 🔒 Security Best Practices

### 1. Change Default Admin Password

**⚠️ IMPORTANT**: Change the default password immediately after first login!

Currently, there's no password change endpoint. To change manually:

```sql
-- Generate new BCrypt hash
-- Use online BCrypt generator or C# code:
-- var hash = BCrypt.Net.BCrypt.HashPassword("YourNewPassword");

UPDATE Users 
SET PasswordHash = '$2a$11$[YOUR_NEW_HASH_HERE]'
WHERE Email = 'admin@careerguidance.com';
```

### 2. Create Additional Admin Users

```sql
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'your_username',
    'Your Full Name',
    'your@email.com',
    '$2a$11$[BCRYPT_HASH_HERE]',
    'admin',
    NOW()
);
```

### 3. Promote Existing User to Admin

**Via API:**
```http
PUT /api/admin/users/123/role
Authorization: Bearer {ADMIN_TOKEN}
Content-Type: application/json

{
  "role": "admin"
}
```

**Via SQL:**
```sql
UPDATE Users 
SET Role = 'admin' 
WHERE Email = 'user@example.com';
```

### 4. JWT Token Security

- Tokens expire after configured time (default: 43200 minutes = 30 days)
- Use refresh tokens for long-lived sessions
- Store tokens securely (localStorage in browser)
- Never share admin tokens

### 5. Database Security

- Use strong MySQL passwords
- Don't commit credentials to version control
- Use environment variables for production
- Regular backups of database

### 6. Network Security

- Use HTTPS in production
- Restrict admin panel to internal network
- Use VPN for remote admin access
- Consider adding IP whitelisting

---

## 📝 Additional Notes

### Creating Admin Users Programmatically

If you want to add admin users from C#:

```csharp
var passwordHash = BCrypt.Net.BCrypt.HashPassword("SecurePassword123");

using var conn = new MySqlConnection(connectionString);
conn.Open();

var query = @"INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
              VALUES (@username, @fullName, @email, @passwordHash, 'admin', NOW())";
              
using var cmd = new MySqlCommand(query, conn);
cmd.Parameters.AddWithValue("@username", "newadmin");
cmd.Parameters.AddWithValue("@fullName", "New Admin");
cmd.Parameters.AddWithValue("@email", "newadmin@example.com");
cmd.Parameters.AddWithValue("@passwordHash", passwordHash);
cmd.ExecuteNonQuery();
```

### Customizing the Admin Dashboard

The admin dashboard is a single HTML file: `wwwroot/admin.html`

**To customize:**
1. Edit colors, logos, text directly in the HTML
2. Modify API_URL if deploying to different server
3. Add custom tabs or features as needed
4. All JavaScript is embedded - no build process needed

### Performance Considerations

- Pagination prevents loading too many users at once
- Dashboard view (`vw_admin_dashboard`) caches statistics
- Index on `Role` column speeds up admin queries
- Consider adding more indexes for large datasets

### Future Enhancements

Potential features to add:
- [ ] Admin activity logging (infrastructure exists)
- [ ] Password change functionality
- [ ] Email notifications for admin actions
- [ ] Advanced filtering and search
- [ ] Bulk operations (delete multiple users)
- [ ] Export individual user data
- [ ] User suspension/activation
- [ ] Audit trail viewer
- [ ] Dashboard customization
- [ ] Multi-language support

---

## 📞 Support

If you encounter issues:

1. Check this guide's [Troubleshooting](#troubleshooting) section
2. Verify all setup steps were completed
3. Check application logs
4. Review browser console for errors
5. Test API endpoints using Postman/Swagger

---

## ✅ Setup Checklist

Use this checklist to ensure complete setup:

- [ ] MySQL database accessible
- [ ] `setup_admin.sql` executed successfully
- [ ] `Role` column added to Users table
- [ ] Default admin user created
- [ ] `admin_activity_log` table created
- [ ] `vw_admin_dashboard` view created
- [ ] `User.cs` model has `Role` property
- [ ] `AuthController.cs` includes Role in JWT
- [ ] `AdminController.cs` exists with all endpoints
- [ ] `admin.html` exists in wwwroot folder
- [ ] Backend builds successfully
- [ ] Backend running on port 5001
- [ ] Can access `http://localhost:5001/admin.html`
- [ ] Can login with admin credentials
- [ ] Dashboard loads with data
- [ ] Can view user details
- [ ] Can search and filter users
- [ ] Export functionality works

---

**🎉 Congratulations! Your admin module is now fully set up and ready to use!**

For questions or support, refer to the documentation files:
- `ADMIN_MODULE_GUIDE.md` - Original guide
- `ADMIN_QUICK_SETUP.md` - Quick reference
- This file - Complete setup guide

---

*Last Updated: January 10, 2026*
