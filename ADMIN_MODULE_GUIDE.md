# 🔐 Admin Module - Complete Documentation

## Overview

The Admin Module provides comprehensive control and monitoring capabilities for the Career Guidance application. Administrators can view all users, monitor activities, manage data, and export analytics.

---

## 🚀 Quick Setup

### Step 1: Run Database Migration

```bash
mysql -u your_username -p career_guidance_db < MyFirstApi/sql/admin_module_migration.sql
```

This will:
- ✅ Add `Role` column to Users table
- ✅ Create default admin user
- ✅ Create admin activity log table
- ✅ Create admin dashboard view
- ✅ Create stored procedure for logging actions

### Step 2: Restart Backend

```bash
cd MyFirstApi
dotnet run
```

### Step 3: Access Admin Dashboard

Open browser and navigate to:
```
http://localhost:5087/admin.html
```

**Default Admin Credentials:**
- Email: `admin@careerguidance.com`
- Password: `Admin@123`

---

## 📡 API Endpoints

### 1. Get All Users

**Endpoint:** `GET /api/admin/users`

**Query Parameters:**
- `page` (int, default: 1) - Page number
- `pageSize` (int, default: 20) - Items per page
- `search` (string, optional) - Search by name, email, or username
- `sortBy` (string, default: "createdAt") - Sort column
- `sortOrder` (string, default: "desc") - Sort direction (asc/desc)

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

---

### 2. Get User Detail

**Endpoint:** `GET /api/admin/users/{userId}`

**Response:**
```json
{
  "userId": 1,
  "username": "john_doe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "createdAt": "2024-01-01T10:00:00",
  "profile": {
    "age": 25,
    "gender": "Male",
    "educationLevel": "Bachelor's",
    "fieldOfStudy": "Computer Science",
    "skills": "Python, JavaScript, React",
    "phoneNumber": "+1234567890"
  },
  "career": {
    "careerName": "Data Science",
    "overallProgress": 72.5,
    "completedCourses": 5,
    "totalCourses": 8,
    "selectedAt": "2024-01-15T10:00:00",
    "lastAccessed": "2024-12-01T15:30:00"
  },
  "videoProgress": [
    {
      "videoId": "abc123",
      "videoTitle": "Python Basics",
      "skillName": "Python",
      "careerName": "Data Science",
      "watchPercentage": 100.0,
      "isCompleted": true,
      "lastWatched": "2024-12-01T14:20:00"
    }
  ],
  "resume": {
    "fullName": "John Doe",
    "jobTitle": "Data Scientist",
    "email": "john@example.com",
    "phone": "+1234567890",
    "location": "New York, USA",
    "linkedin": "linkedin.com/in/johndoe",
    "professionalSummary": "Experienced data scientist...",
    "createdAt": "2024-11-01T10:00:00",
    "updatedAt": "2024-11-28T16:45:00"
  },
  "chatHistory": [
    {
      "sessionId": "session-123",
      "title": "Career Advice Chat",
      "createdAt": "2024-11-30T10:00:00",
      "updatedAt": "2024-11-30T10:30:00"
    }
  ]
}
```

---

### 3. Get System Statistics

**Endpoint:** `GET /api/admin/stats`

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
    {
      "careerName": "Data Science",
      "count": 45
    },
    {
      "careerName": "Web Development",
      "count": 38
    }
  ],
  "recentActivities": [
    {
      "username": "john_doe",
      "fullName": "John Doe",
      "activityType": "Video Watched",
      "activityDetail": "Python Advanced Techniques",
      "activityTime": "2024-11-30T15:30:00"
    }
  ]
}
```

---

### 4. Delete User

**Endpoint:** `DELETE /api/admin/users/{userId}`

**Response:**
```json
{
  "message": "User deleted successfully"
}
```

**Note:** This will cascade delete all related data (profile, career progress, videos, resume, chat history)

---

### 5. Update User Role

**Endpoint:** `PUT /api/admin/users/{userId}/role`

**Request Body:**
```json
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

---

### 6. Export Users to CSV

**Endpoint:** `GET /api/admin/export/users`

**Response:** CSV file download

**CSV Columns:**
- Id
- Username
- FullName
- Email
- CreatedAt
- Career
- Progress
- VideosWatched
- CompletedVideos

---

### 7. Get User Growth Analytics

**Endpoint:** `GET /api/admin/analytics/growth?days=30`

**Query Parameters:**
- `days` (int, default: 30) - Number of days to analyze

**Response:**
```json
[
  {
    "date": "2024-11-01",
    "count": 5
  },
  {
    "date": "2024-11-02",
    "count": 8
  }
]
```

---

## 🎨 Admin Dashboard Features

### Dashboard Overview

The admin dashboard provides a modern, responsive interface with the following features:

#### 1. **Statistics Cards**
- Total Users
- Active Users Today
- Active Users This Week
- Total Videos Watched
- Total Resumes Created
- Average Progress Percentage

#### 2. **Users Management**
- **Search & Filter**: Search by name, email, or username
- **Pagination**: Navigate through user pages
- **Sort**: Sort by different columns
- **View Details**: Click "View" to see complete user information
- **Delete Users**: Remove users with confirmation

#### 3. **Recent Activities**
- Real-time view of user activities
- Shows latest video watches
- User information and timestamps

#### 4. **Popular Careers**
- Visual representation of most selected career paths
- Shows user count per career
- Progress bars for easy comparison

#### 5. **Data Export**
- Export all user data to CSV
- Includes comprehensive user information
- Timestamped file names

---

## 🔒 Security

### Authentication
- All endpoints require JWT authentication
- Admin role verification on each request
- Token stored securely in localStorage

### Authorization
```csharp
private bool IsAdmin()
{
    var userRole = User.FindFirstValue(ClaimTypes.Role);
    return userRole == "admin" || userRole == "Admin";
}
```

### Password Security
- Admin password is hashed using BCrypt
- Never stored in plain text
- Secure password reset available

---

## 📊 Database Tables

### admin_activity_log
Tracks all admin actions for audit purposes.

```sql
CREATE TABLE admin_activity_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_user_id INT NULL,
    description TEXT NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Action Types:**
- `view_user` - Viewed user details
- `delete_user` - Deleted a user
- `update_role` - Updated user role
- `export_data` - Exported user data
- `login` - Admin login
- `logout` - Admin logout

---

## 💻 Usage Examples

### Example 1: Search for Users

```javascript
const searchUsers = async (searchTerm) => {
  const response = await fetch(
    `http://localhost:5087/api/admin/users?page=1&search=${searchTerm}`,
    {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    }
  );
  
  const data = await response.json();
  return data.users;
};

// Usage
const users = await searchUsers('john');
```

### Example 2: View User Details

```javascript
const getUserDetails = async (userId) => {
  const response = await fetch(
    `http://localhost:5087/api/admin/users/${userId}`,
    {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    }
  );
  
  return await response.json();
};

// Usage
const userDetail = await getUserDetails(123);
console.log(userDetail.profile);
console.log(userDetail.career);
```

### Example 3: Delete User

```javascript
const deleteUser = async (userId) => {
  if (!confirm('Are you sure?')) return;
  
  const response = await fetch(
    `http://localhost:5087/api/admin/users/${userId}`,
    {
      method: 'DELETE',
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    }
  );
  
  return await response.json();
};
```

### Example 4: Export Data

```javascript
const exportUsers = async () => {
  const response = await fetch(
    'http://localhost:5087/api/admin/export/users',
    {
      headers: {
        'Authorization': `Bearer ${authToken}`
      }
    }
  );
  
  const blob = await response.blob();
  const url = window.URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `users_${new Date().toISOString()}.csv`;
  a.click();
};
```

---

## 🎯 Common Tasks

### Task 1: Create New Admin User

```sql
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'newadmin',
    'New Admin',
    'newadmin@example.com',
    '$2a$11$hashedpassword',  -- Use BCrypt to hash
    'admin',
    NOW()
);
```

### Task 2: View All Admin Actions

```sql
SELECT 
    u.Username,
    aal.action_type,
    aal.description,
    aal.created_at
FROM admin_activity_log aal
JOIN Users u ON aal.admin_id = u.Id
ORDER BY aal.created_at DESC
LIMIT 100;
```

### Task 3: Get User Count by Career

```sql
SELECT 
    career_name,
    COUNT(*) as user_count,
    AVG(overall_progress) as avg_progress
FROM user_career_progress
WHERE is_active = TRUE
GROUP BY career_name
ORDER BY user_count DESC;
```

### Task 4: Find Inactive Users

```sql
SELECT 
    u.Id,
    u.Username,
    u.Email,
    u.CreatedAt,
    MAX(vwh.last_watched) as last_activity
FROM Users u
LEFT JOIN video_watch_history vwh ON u.Id = vwh.user_id
GROUP BY u.Id
HAVING last_activity < DATE_SUB(NOW(), INTERVAL 30 DAY)
   OR last_activity IS NULL;
```

---

## 🔧 Configuration

### Update API URL

In `admin.html`, update the API URL if your backend is on a different address:

```javascript
const API_URL = 'http://your-server-ip:5087/api';
```

### Change Default Credentials

Update the admin password in the migration script:

```sql
-- Generate new BCrypt hash for your password
-- Use online tool or C# BCrypt.Net.BCrypt.HashPassword("YourPassword")

UPDATE Users 
SET PasswordHash = '$2a$11$your-new-hash'
WHERE Email = 'admin@careerguidance.com';
```

---

## 📈 Performance Tips

1. **Pagination**: Always use pagination for large datasets
2. **Indexing**: Ensure indexes are created (done in migration)
3. **Caching**: Consider caching statistics for 5-10 minutes
4. **Search**: Use full-text search for large user bases
5. **Export**: Limit export to relevant date ranges

---

## 🐛 Troubleshooting

### Issue: "Forbidden" error
**Solution**: Ensure user has admin role in database
```sql
UPDATE Users SET Role = 'admin' WHERE Email = 'your@email.com';
```

### Issue: Statistics not loading
**Solution**: Check database views are created
```sql
SELECT * FROM vw_admin_dashboard;
```

### Issue: Export not working
**Solution**: Ensure wwwroot folder exists and is accessible

### Issue: Can't login
**Solution**: Verify admin user exists
```sql
SELECT * FROM Users WHERE Role = 'admin';
```

---

## 🚀 Next Steps

1. **Custom Reports**: Add more analytics endpoints
2. **Email Notifications**: Send alerts for important events
3. **Bulk Operations**: Add bulk user management
4. **Advanced Filters**: Add more filtering options
5. **Audit Trail**: Implement comprehensive activity logging

---

## 📞 Admin Support

For admin-specific issues:
1. Check database migrations are complete
2. Verify JWT token is valid
3. Check user has admin role
4. Review server logs for errors
5. Test endpoints with Postman/cURL

---

**Created**: November 2024  
**Status**: ✅ Production Ready  
**Access**: http://localhost:5087/admin.html
