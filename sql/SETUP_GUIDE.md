# Quick Setup: Learning Videos Database

## Step 1: Run SQL Script

### Option A: Command Line (Recommended)
```bash
# Navigate to sql directory
cd "C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi\sql"

# Run the SQL script
mysql -u root -p career_guidance_db < create_learning_videos_table.sql

# Enter your MySQL root password when prompted
```

### Option B: MySQL Workbench
1. Open MySQL Workbench
2. Connect to your database server
3. Select database: `USE career_guidance_db;`
4. Open file: `File → Open SQL Script`
5. Navigate to: `MyFirstApi/sql/create_learning_videos_table.sql`
6. Execute: Click the lightning bolt icon ⚡

### Option C: Direct MySQL CLI
```bash
mysql -u root -p

# In MySQL prompt:
USE career_guidance_db;
SOURCE C:/Users/Dell/Desktop/dotnet/learn/MyFirstApi/sql/create_learning_videos_table.sql;
```

## Step 2: Verify Installation

### Check Table Created
```sql
SHOW TABLES LIKE 'learning_videos';
```

### Check Video Count
```sql
SELECT COUNT(*) as total_videos FROM learning_videos;
-- Expected: 80+ videos
```

### View Sample Videos
```sql
SELECT skill_name, video_title, duration_minutes 
FROM learning_videos 
LIMIT 10;
```

### Check Skills Coverage
```sql
SELECT skill_name FROM learning_videos ORDER BY skill_name;
```

## Step 3: Test API Endpoints

### Start Backend Server
```bash
cd "C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi"
dotnet run
```

### Test Endpoints (in new terminal)

**Test 1: Get All Videos**
```bash
curl http://localhost:5001/api/learningvideos
```

**Test 2: Get Videos for Specific Skills**
```bash
curl "http://localhost:5001/api/learningvideos/skills?skills=[\"Python\",\"JavaScript\",\"React\"]"
```

**Test 3: Get Single Video**
```bash
curl http://localhost:5001/api/learningvideos/Python
```

## Step 4: Test in Flutter App

1. Ensure backend is running on `http://192.168.1.102:5001`
2. Run Flutter app: `flutter run`
3. Navigate to Career Suggestions
4. Select any career (e.g., Frontend Developer)
5. Confirm career selection
6. Verify videos load in Learning Path screen

## Expected Results

### Database
- ✅ `learning_videos` table created
- ✅ 80+ video records inserted
- ✅ All skills have corresponding videos

### API
- ✅ `/api/learningvideos` returns all videos
- ✅ `/api/learningvideos/skills` filters by skills
- ✅ `/api/learningvideos/{skill}` returns single video

### Flutter App
- ✅ Learning path displays videos from database
- ✅ Videos match selected career's required skills
- ✅ Course modules show correct titles and durations
- ✅ YouTube player opens when clicking video

## Troubleshooting

### Error: Table already exists
```sql
-- Drop and recreate
DROP TABLE IF EXISTS learning_videos;
-- Then run the SQL script again
```

### Error: Access denied
```bash
# Use correct MySQL user
mysql -u your_username -p career_guidance_db < create_learning_videos_table.sql
```

### Error: Database not found
```sql
-- Create database first
CREATE DATABASE IF NOT EXISTS career_guidance_db;
USE career_guidance_db;
-- Then run the script
```

### API Returns Empty Array
- Check skills parameter is URL-encoded
- Verify skill names match database (case-sensitive)
- Check database records: `SELECT * FROM learning_videos WHERE skill_name = 'Python'`

### Flutter Shows Empty Learning Path
- Verify backend API is accessible from device/emulator
- Check `ApiConstants.baseUrl` in Flutter app
- Review Flutter console for error logs
- Test API endpoint in browser: `http://192.168.1.102:5001/api/learningvideos`

## Success Indicators

When everything is working:
- ✅ SQL script executes without errors
- ✅ 80+ videos in database
- ✅ API endpoints return JSON responses
- ✅ Flutter learning path shows dynamic videos
- ✅ No hardcoded video data in Flutter code
- ✅ Videos load based on selected career skills

## Next Steps

After successful setup:
1. Test different career paths to verify skill-video mapping
2. Add more videos to database as needed
3. Customize video titles and descriptions
4. Consider adding video difficulty levels
5. Track user progress and completion rates
