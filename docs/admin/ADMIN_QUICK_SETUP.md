# Admin Module Quick Setup Guide

## 🚀 Quick Start (5 Minutes)

### Step 1: Run Database Migration (1 min)

Open your terminal/command prompt and run:

```bash
# Navigate to project directory
cd "C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi"

# Run MySQL migration (update with your credentials)
mysql -u root -p career_guidance_db < sql\admin_module_migration.sql
```

When prompted, enter your MySQL password.

---

### Step 2: Start Backend Server (1 min)

```bash
# In the same MyFirstApi directory
dotnet run
```

You should see:
```
Now listening on: http://localhost:5087
```

---

### Step 3: Access Admin Dashboard (30 sec)

Open your web browser and go to:
```
http://localhost:5087/admin.html
```

---

### Step 4: Login (30 sec)

Use the default credentials:
- **Email**: `admin@careerguidance.com`
- **Password**: `Admin@123`

---

## ✅ What You'll See

After logging in, you'll have access to:

### 📊 Dashboard
- Total Users
- Active Users (Today/Week)
- Total Videos Watched
- Total Resumes Created
- Average User Progress

### 👥 Users Management
- **View All Users**: Complete list with pagination
- **Search**: Find users by name, email, or username
- **User Details**: Click "View" to see full user profile including:
  - Personal information
  - Career progress
  - Video watch history
  - Resume data
  - Chat history
- **Delete Users**: Remove users with confirmation

### 📈 Analytics
- **Recent Activities**: See what users are doing in real-time
- **Popular Careers**: Visual chart of most selected career paths
- **User Growth**: Track user registration trends

### 📥 Export
- Download all user data as CSV
- Perfect for analytics and reporting

---

## 🎯 Quick Actions

### Create Another Admin User

```sql
-- Connect to MySQL
mysql -u root -p career_guidance_db

-- Run this query (replace with actual details)
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'youradmin',
    'Your Name',
    'your@email.com',
    '$2a$11$hashedpasswordhere',  -- Generate with BCrypt
    'admin',
    NOW()
);
```

To generate password hash in C#:
```csharp
var hash = BCrypt.Net.BCrypt.HashPassword("YourPassword");
Console.WriteLine(hash);
```

---

### Make Existing User an Admin

```sql
UPDATE Users 
SET Role = 'admin' 
WHERE Email = 'user@example.com';
```

---

### Reset Admin Password

```sql
UPDATE Users 
SET PasswordHash = '$2a$11$newhashhere'
WHERE Email = 'admin@careerguidance.com';
```

---

## 🔧 Troubleshooting

### Problem: "Can't connect to MySQL"
**Solution**: Make sure MySQL is running
```bash
# Check MySQL status
mysql -u root -p -e "SELECT 1"
```

### Problem: "Migration failed"
**Solution**: Check database exists
```bash
mysql -u root -p -e "CREATE DATABASE IF NOT EXISTS career_guidance_db"
```

### Problem: "401 Unauthorized"
**Solution**: Make sure user has admin role
```sql
SELECT Id, Username, Email, Role FROM Users WHERE Email = 'admin@careerguidance.com';
```
Should show Role = 'admin'

### Problem: "Admin page not loading"
**Solution**: Check backend is running on port 5087
```bash
# Should see this when you run: dotnet run
Now listening on: http://localhost:5087
```

### Problem: "No data showing"
**Solution**: Make sure you have users in database
```sql
SELECT COUNT(*) FROM Users;
```

---

## 📱 Mobile Access

To access from mobile device on same network:

1. Find your computer's IP address:
```bash
# Windows
ipconfig

# Look for IPv4 Address, e.g., 192.168.1.100
```

2. Update `admin.html` (line 364):
```javascript
const API_URL = 'http://192.168.1.100:5087/api';
```

3. Access from mobile:
```
http://192.168.1.100:5087/admin.html
```

---

## 🎨 Customization

### Change Admin Dashboard Title

Edit `wwwroot/admin.html` line 352:
```html
<h2>📊 Your Company Name - Admin</h2>
```

### Change Theme Colors

Edit `admin.html` CSS (lines 15-18):
```css
background: linear-gradient(135deg, #your-color1 0%, #your-color2 100%);
```

### Add Custom Statistics

Edit `AdminController.cs` in the `GetSystemStats` method to add more metrics.

---

## 📊 Sample Queries for Analytics

### Most Active Users (Last 7 Days)
```sql
SELECT 
    u.Username,
    u.FullName,
    COUNT(DISTINCT vwh.video_id) as videos_watched,
    SUM(vwh.current_position_seconds)/60 as minutes_watched
FROM Users u
JOIN video_watch_history vwh ON u.Id = vwh.user_id
WHERE vwh.last_watched >= DATE_SUB(NOW(), INTERVAL 7 DAY)
GROUP BY u.Id
ORDER BY videos_watched DESC
LIMIT 10;
```

### Career Completion Rates
```sql
SELECT 
    career_name,
    COUNT(*) as total_users,
    AVG(overall_progress) as avg_progress,
    SUM(CASE WHEN overall_progress >= 90 THEN 1 ELSE 0 END) as completed_users
FROM user_career_progress
WHERE is_active = TRUE
GROUP BY career_name
ORDER BY total_users DESC;
```

### User Engagement Trends
```sql
SELECT 
    DATE(last_watched) as date,
    COUNT(DISTINCT user_id) as active_users,
    COUNT(*) as total_videos_watched
FROM video_watch_history
WHERE last_watched >= DATE_SUB(NOW(), INTERVAL 30 DAY)
GROUP BY DATE(last_watched)
ORDER BY date DESC;
```

---

## 🔒 Security Best Practices

1. **Change Default Password** immediately after first login
2. **Use Strong Passwords**: Minimum 12 characters with numbers, symbols
3. **Limit Admin Access**: Only give admin role to trusted users
4. **Enable HTTPS** in production (requires SSL certificate)
5. **Regular Backups**: Backup database daily
6. **Monitor Activity**: Check admin_activity_log regularly

---

## 📈 Performance Optimization

For large user bases (1000+ users):

1. **Enable Caching**:
```csharp
// In AdminController, add caching for stats
[ResponseCache(Duration = 300)] // 5 minutes
public async Task<IActionResult> GetSystemStats()
```

2. **Add Database Indexes** (already done in migration):
```sql
CREATE INDEX idx_users_role ON Users(Role);
CREATE INDEX idx_video_history_user_date ON video_watch_history(user_id, last_watched);
```

3. **Optimize Queries**: Use pagination always, never load all data at once

4. **Monitor Performance**:
```sql
-- Check slow queries
SHOW PROCESSLIST;

-- Check table sizes
SELECT 
    table_name,
    ROUND(((data_length + index_length) / 1024 / 1024), 2) AS "Size (MB)"
FROM information_schema.TABLES
WHERE table_schema = "career_guidance_db"
ORDER BY (data_length + index_length) DESC;
```

---

## ✨ Features Checklist

After setup, verify these features work:

- [ ] Login with admin credentials
- [ ] Dashboard shows correct statistics
- [ ] Can view list of users
- [ ] Search functionality works
- [ ] Can view individual user details
- [ ] Recent activities display correctly
- [ ] Popular careers chart shows data
- [ ] Export users to CSV works
- [ ] Delete user function works (test with dummy user)
- [ ] Pagination works for large user lists
- [ ] Logout works correctly
- [ ] Session persists on page refresh

---

## 🆘 Support

**Database Issues**: Check `sql/admin_module_migration.sql` ran successfully  
**API Issues**: Check backend logs in terminal  
**UI Issues**: Press F12 in browser and check Console tab  
**Permission Issues**: Verify admin role in database  

---

## 📚 Next Steps

1. ✅ Setup admin module (you're here!)
2. Test all features with sample data
3. Create additional admin users
4. Setup regular database backups
5. Configure production environment
6. Enable HTTPS/SSL
7. Add custom analytics as needed
8. Train admin users on dashboard usage

---

**Estimated Total Setup Time**: 5-10 minutes  
**Status**: ✅ Ready to use  
**Access**: http://localhost:5087/admin.html

**Default Login**: admin@careerguidance.com / Admin@123

**⚠️ IMPORTANT**: Change the default password immediately after first login!
