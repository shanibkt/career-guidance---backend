# Database Setup Instructions

## Quick Setup (Recommended)

I've created a consolidated migration file: **`RUN_ALL_MIGRATIONS.sql`** that contains all the necessary tables for your Career Guidance Platform.

### Option 1: Using MySQL Workbench (Easiest)
1. Open MySQL Workbench
2. Connect to your MySQL server
3. Click: **File → Open SQL Script**
4. Navigate to: `sql\RUN_ALL_MIGRATIONS.sql`
5. Click the **⚡ Execute** button (lightning bolt icon)

### Option 2: Using MySQL Command Line
If you have MySQL in your PATH:
```bash
mysql -u root -p my_database < sql\RUN_ALL_MIGRATIONS.sql
```

### Option 3: Copy-Paste in MySQL CLI
```bash
# Open MySQL
mysql -u root -p

# Switch to database
USE my_database;

# Copy all content from RUN_ALL_MIGRATIONS.sql and paste it
```

## What Gets Created

### 📋 Core Tables (3)
- **Users** - User authentication
- **UserProfiles** - Extended user data
- **RefreshTokens** - JWT tokens

### 🎯 Career & Learning (2)
- **careers** - Career paths
- **learning_videos** - Learning resources

### 📊 Progress Tracking (3)
- **user_career_progress** - Overall career progress
- **course_progress** - Individual course progress
- **video_watch_history** - Video watching history

### 💬 Chat System (2)
- **chat_history** - AI chat messages
- **chat_sessions** - Chat sessions

### 📝 Quiz System (2)
- **quiz_questions** - Quiz questions
- **quiz_results** - Quiz scores

### 💼 Resume & Jobs (3)
- **user_resumes** - User resumes
- **saved_jobs** - Saved job listings
- **job_applications** - Job applications tracking

### 👨‍💼 Admin (1)
- **admin_users** - Admin privileges

**Total: 16 Tables**

## Verification

After running the script, verify the setup:

```sql
-- Check all tables
SHOW TABLES;

-- Check table count (should be 16+)
SELECT COUNT(*) FROM information_schema.tables 
WHERE table_schema = 'my_database';

-- Check specific tables
DESCRIBE Users;
DESCRIBE learning_videos;
DESCRIBE chat_history;
```

## Database Connection

Your app is configured to connect with:
- **Server**: localhost
- **Database**: my_database
- **User**: root
- **Password**: 1234

(from `appsettings.json`)

## Next Steps

After running the migration:
1. ✅ Start your backend: `dotnet run`
2. ✅ Backend will be available at: `http://localhost:5001`
3. ✅ Test endpoints using the Flutter app

## Troubleshooting

### Error: Table already exists
This is normal - the script uses `CREATE TABLE IF NOT EXISTS`, so it won't recreate existing tables.

### Error: Cannot add foreign key constraint
Make sure to run the script in order. The script creates tables in the correct dependency order.

### Error: Access denied
Make sure your MySQL password in `appsettings.json` matches your actual MySQL root password.
