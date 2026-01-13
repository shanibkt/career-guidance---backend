# Admin Module - Complete Setup & Usage Guide

## 🎯 Overview

The Admin Module is a comprehensive management system for the Career Guidance application that allows administrators to monitor users, analyze system statistics, and manage the platform effectively.

---

## 📋 Table of Contents

1. [Quick Start](#quick-start)
2. [Architecture](#architecture)
3. [Database Setup](#database-setup)
4. [Testing](#testing)
5. [Available Endpoints](#available-endpoints)
6. [Features](#features)
7. [Troubleshooting](#troubleshooting)

---

## 🚀 Quick Start

### Prerequisites

- .NET 9.0 SDK installed
- MySQL Server running
- Database `career_guidance_db` exists

### Setup Steps

**1. Run Database Migration**

```powershell
# From career-guidance---backend directory
.\setup-admin-db.ps1
```

Enter your MySQL credentials when prompted. This will:
- Add `Role` column to Users table
- Create default admin user
- Create admin_activity_log table
- Create admin dashboard views

**2. Build and Run Backend**

```powershell
dotnet build
dotnet run
```

**3. Access Admin Dashboard**

Open browser to: `http://localhost:5001/admin.html`

**Default Credentials:**
- Email: `admin@careerguidance.com`
- Password: `Admin@123`

⚠️ **IMPORTANT:** Change the default password after first login!

---

## 🏗️ Architecture

### Backend Components

#### 1. **AdminController.cs** (648 lines)
Main controller handling all admin operations:

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/api/admin/users` | GET | List all users with pagination |
| `/api/admin/users/{id}` | GET | Get detailed user info |
| `/api/admin/users/{id}` | DELETE | Delete user |
| `/api/admin/users/{id}/role` | PUT | Update user role |
| `/api/admin/stats` | GET | System statistics |
| `/api/admin/export/users` | GET | Export users as CSV |
| `/api/admin/analytics/growth` | GET | User growth data |

#### 2. **AuthController.cs**
Handles authentication with role-based JWT tokens:
- Login endpoint returns JWT with role claims
- Role stored in `ClaimTypes.Role`
- Default role: `"user"`, Admin role: `"admin"`

#### 3. **User Model** (`Models/User.cs`)
```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "user";  // ← Admin role field
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
```

### Frontend Component

#### **admin.html** (862 lines)
Modern, responsive admin dashboard with:
- Login screen
- Statistics dashboard
- User management interface
- Analytics charts
- Export functionality

---

## 💾 Database Setup

### Tables Created/Modified

**1. Users Table - Modified**
```sql
ALTER TABLE Users 
ADD COLUMN Role VARCHAR(20) DEFAULT 'user';
CREATE INDEX idx_users_role ON Users(Role);
```

**2. admin_activity_log - New Table**
```sql
CREATE TABLE admin_activity_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_user_id INT NULL,
    description TEXT NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (admin_id) REFERENCES Users(Id) ON DELETE CASCADE
);
```

**3. vw_admin_dashboard - View**
Aggregated statistics view for quick dashboard queries.

**4. LogAdminAction - Stored Procedure**
Procedure for logging admin actions (not currently used by controller).

### Default Admin User

**Created by Migration:**
- Username: `admin`
- Full Name: `System Administrator`
- Email: `admin@careerguidance.com`
- Password: `Admin@123` (BCrypt hashed)
- Role: `admin`

---

## 🧪 Testing

### Test Admin Endpoints

```powershell
# Make sure backend is running first
dotnet run

# In a new terminal:
.\test-admin-endpoints.ps1
```

This will test:
- ✅ Admin login
- ✅ Get system statistics
- ✅ List users
- ✅ Get user details
- ✅ Growth analytics

### Manual Testing with Postman/Thunder Client

**1. Login to Get Token**
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "admin@careerguidance.com",
  "password": "Admin@123"
}
```

**Response:**
```json
{
  "token": "eyJhbGc...",
  "refreshToken": "...",
  "tokenExpiration": "2026-01-11T...",
  "user": { ... }
}
```

**2. Use Token for Admin Endpoints**
```http
GET http://localhost:5001/api/admin/stats
Authorization: Bearer <your_token_here>
```

---

## 📊 Available Endpoints

### 1. Get All Users

```http
GET /api/admin/users?page=1&pageSize=20&search=john&sortBy=createdAt&sortOrder=desc
Authorization: Bearer <token>
```

**Query Parameters:**
- `page` (int, default: 1)
- `pageSize` (int, default: 20)
- `search` (string, optional) - Search by name, email, username
- `sortBy` (string, default: "createdAt")
- `sortOrder` (string, default: "desc")

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

### 2. Get User Details

```http
GET /api/admin/users/{userId}
Authorization: Bearer <token>
```

**Response:** Complete user profile with career progress, videos, resume, and chat history.

### 3. Get System Statistics

```http
GET /api/admin/stats
Authorization: Bearer <token>
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
  "popularCareers": [
    { "careerName": "Data Science", "count": 45 }
  ],
  "recentActivities": [
    {
      "username": "john_doe",
      "fullName": "John Doe",
      "activityType": "Video Watched",
      "activityDetail": "Python Advanced",
      "activityTime": "2024-11-30T15:30:00"
    }
  ]
}
```

### 4. Delete User

```http
DELETE /api/admin/users/{userId}
Authorization: Bearer <token>
```

**Note:** Cascades to all related data.

### 5. Update User Role

```http
PUT /api/admin/users/{userId}/role
Authorization: Bearer <token>
Content-Type: application/json

{
  "role": "admin"
}
```

### 6. Export Users

```http
GET /api/admin/export/users
Authorization: Bearer <token>
```

Downloads CSV file with all user data.

### 7. User Growth Analytics

```http
GET /api/admin/analytics/growth?days=30
Authorization: Bearer <token>
```

---

## ✨ Features

### Dashboard Features

**📊 Statistics Cards:**
- Total users count
- Active users (today/week)
- Total videos watched
- Total resumes created
- Average user progress

**👥 User Management:**
- Paginated user list
- Search functionality
- Sort by multiple columns
- View detailed user profiles
- Delete users (with confirmation)
- Promote users to admin

**📈 Analytics:**
- Popular career paths chart
- Recent user activities feed
- User growth trends

**📥 Export:**
- Download all user data as CSV
- Includes progress and activity metrics

### Security Features

**Authorization:**
- JWT-based authentication
- Role-based access control
- Token expiration and refresh
- Every admin endpoint checks `IsAdmin()`

**Password Security:**
- BCrypt hashing
- No plain text storage
- Secure password transmission

---

## 🔧 Troubleshooting

### Issue: "401 Unauthorized"

**Cause:** Token expired or user not admin.

**Solution:**
1. Check token is included in Authorization header
2. Verify user has admin role:
   ```sql
   SELECT Id, Username, Email, Role FROM Users WHERE Email = 'admin@careerguidance.com';
   ```
3. Role should be `'admin'`

### Issue: "Can't connect to MySQL"

**Solution:**
```powershell
# Test MySQL connection
mysql -u root -p -e "SELECT 1"

# Check database exists
mysql -u root -p -e "SHOW DATABASES LIKE 'career_guidance_db'"
```

### Issue: "Role column doesn't exist"

**Solution:** Run migration again:
```powershell
.\setup-admin-db.ps1
```

### Issue: "Admin page not loading"

**Solution:**
1. Check backend is running: `dotnet run`
2. Verify port is 5001 (check console output)
3. Ensure `wwwroot/admin.html` exists
4. Check browser console for errors

### Issue: "No data in dashboard"

**Solution:** Ensure you have users in database:
```sql
SELECT COUNT(*) FROM Users;
```

---

## 📱 Mobile/Remote Access

To access from another device on the same network:

**1. Find your computer's IP:**
```powershell
ipconfig
# Look for IPv4 Address, e.g., 192.168.1.100
```

**2. Update `appsettings.json`:**
```json
"Kestrel": {
  "Endpoints": {
    "Http": {
      "Url": "http://0.0.0.0:5001"
    }
  }
}
```

**3. Access from mobile:**
```
http://192.168.1.100:5001/admin.html
```

---

## 🔐 Security Best Practices

1. **Change Default Password Immediately**
2. **Use Strong Admin Passwords**
3. **Limit Admin Access to Trusted IPs** (configure firewall)
4. **Enable HTTPS in Production**
5. **Rotate Admin Passwords Regularly**
6. **Monitor Admin Activity Logs**
7. **Use Environment Variables for Sensitive Data**

---

## 📚 Additional Resources

- [ADMIN_MODULE_GUIDE.md](ADMIN_MODULE_GUIDE.md) - Detailed API documentation
- [ADMIN_QUICK_SETUP.md](ADMIN_QUICK_SETUP.md) - Quick setup instructions
- [DATABASE_SETUP.md](DATABASE_SETUP.md) - Database configuration

---

## 🎓 Summary

Your admin module is now fully set up with:
- ✅ Role-based authentication
- ✅ Complete user management
- ✅ System analytics and monitoring
- ✅ CSV export functionality
- ✅ Modern web dashboard
- ✅ Secure JWT authentication
- ✅ Database migrations

**Access Dashboard:** http://localhost:5001/admin.html  
**Default Login:** admin@careerguidance.com / Admin@123

🎉 **Admin module is ready to use!**
