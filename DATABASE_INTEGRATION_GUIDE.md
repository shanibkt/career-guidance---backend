# Database Integration Guide
## Complete Database Setup for Career Guidance App

This guide covers the comprehensive database integration for resume builder, learning path tracking, and video progress monitoring.

---

## 📋 Table of Contents
1. [Database Schema](#database-schema)
2. [Backend API Endpoints](#backend-api-endpoints)
3. [Flutter Services](#flutter-services)
4. [Usage Examples](#usage-examples)
5. [Migration Steps](#migration-steps)

---

## 🗄️ Database Schema

### Tables Created

#### 1. **user_resumes**
Stores complete resume data for each user.

```sql
Columns:
- id (INT, PRIMARY KEY)
- user_id (INT, FOREIGN KEY)
- full_name (VARCHAR 255)
- job_title (VARCHAR 255)
- email (VARCHAR 255)
- phone (VARCHAR 50)
- location (VARCHAR 255)
- linkedin (VARCHAR 500)
- professional_summary (TEXT)
- skills (JSON)
- experiences (JSON)
- education (JSON)
- created_at (TIMESTAMP)
- updated_at (TIMESTAMP)
```

#### 2. **video_watch_history**
Tracks video watching progress with resume position.

```sql
Columns:
- id (INT, PRIMARY KEY)
- user_id (INT, FOREIGN KEY)
- career_name (VARCHAR 255)
- skill_name (VARCHAR 255)
- video_id (VARCHAR 100)
- video_title (VARCHAR 500)
- current_position_seconds (INT)
- duration_seconds (INT)
- watch_percentage (DECIMAL)
- is_completed (BOOLEAN)
- first_watched (TIMESTAMP)
- last_watched (TIMESTAMP)
- watch_count (INT)
```

#### 3. **learning_path_progress**
Aggregates progress for each skill within a career path.

```sql
Columns:
- id (INT, PRIMARY KEY)
- user_id (INT, FOREIGN KEY)
- career_name (VARCHAR 255)
- skill_name (VARCHAR 255)
- total_videos (INT)
- completed_videos (INT)
- total_duration_seconds (INT)
- watched_duration_seconds (INT)
- progress_percentage (DECIMAL)
- started_at (TIMESTAMP)
- last_accessed (TIMESTAMP)
```

#### 4. **resume_export_history**
Tracks resume exports (PDF, DOCX).

```sql
Columns:
- id (INT, PRIMARY KEY)
- user_id (INT, FOREIGN KEY)
- resume_id (INT, FOREIGN KEY)
- export_format (VARCHAR 20)
- file_path (VARCHAR 500)
- exported_at (TIMESTAMP)
```

### Database Views

#### **vw_user_learning_dashboard**
Complete learning analytics for users.

```sql
SELECT * FROM vw_user_learning_dashboard WHERE user_id = 1;
```

#### **vw_resume_completion**
Resume completion percentage calculation.

```sql
SELECT * FROM vw_resume_completion WHERE user_id = 1;
```

### Stored Procedures

#### **GetUserLearningAnalytics**
Get comprehensive learning analytics.

```sql
CALL GetUserLearningAnalytics(1);
```

#### **UpdateLearningPathProgress**
Auto-update learning path progress based on video history.

```sql
CALL UpdateLearningPathProgress(1, 'Data Science', 'Python');
```

---

## 🔌 Backend API Endpoints

### Resume Controller (`/api/resume`)

#### **POST /api/resume/save**
Save or update user resume.

**Request Body:**
```json
{
  "fullName": "John Doe",
  "jobTitle": "Software Engineer",
  "email": "john@example.com",
  "phone": "+1234567890",
  "location": "New York, USA",
  "linkedin": "linkedin.com/in/johndoe",
  "professionalSummary": "Experienced software engineer...",
  "skills": ["Python", "JavaScript", "React"],
  "experiences": [
    {
      "role": "Senior Developer",
      "company": "Tech Corp",
      "period": "2020-2023",
      "description": "Led team of 5 developers..."
    }
  ],
  "education": [
    {
      "degree": "B.S. Computer Science",
      "institution": "MIT",
      "year": "2020"
    }
  ]
}
```

**Response:**
```json
{
  "message": "Resume saved successfully"
}
```

#### **GET /api/resume**
Get user's resume data.

**Response:**
```json
{
  "fullName": "John Doe",
  "jobTitle": "Software Engineer",
  ...
}
```

#### **DELETE /api/resume**
Delete user's resume.

**Response:**
```json
{
  "message": "Resume deleted successfully"
}
```

---

### Video Progress Controller (`/api/videoprogress`)

#### **POST /api/videoprogress/save**
Save video watch progress.

**Request Body:**
```json
{
  "videoId": "dQw4w9WgXcQ",
  "videoTitle": "Introduction to Python",
  "skillName": "Python Basics",
  "careerName": "Data Science",
  "currentPositionSeconds": 180,
  "durationSeconds": 600,
  "watchPercentage": 30.0,
  "isCompleted": false
}
```

#### **GET /api/videoprogress/{videoId}?careerName={career}**
Get progress for specific video.

**Response:**
```json
{
  "videoId": "dQw4w9WgXcQ",
  "currentPositionSeconds": 180,
  "watchPercentage": 30.0,
  "isCompleted": false
}
```

#### **GET /api/videoprogress/career/{careerName}**
Get all video progress for a career.

**Response:**
```json
[
  {
    "videoId": "abc123",
    "videoTitle": "Python Basics",
    "watchPercentage": 100.0,
    "isCompleted": true
  }
]
```

#### **GET /api/videoprogress/summary?careerName={career}&skillName={skill}**
Get learning path summary.

**Response:**
```json
{
  "careerName": "Data Science",
  "skillName": "Python",
  "totalVideos": 10,
  "completedVideos": 7,
  "totalDurationSeconds": 3600,
  "watchedDurationSeconds": 2800,
  "progressPercentage": 77.78
}
```

#### **GET /api/videoprogress/career-summary/{careerName}**
Get all learning paths summary for a career.

#### **GET /api/videoprogress/recent?limit=10**
Get recently watched videos.

#### **DELETE /api/videoprogress/{videoId}?careerName={career}**
Delete video progress.

#### **DELETE /api/videoprogress/career/{careerName}/reset**
Reset all progress for a career.

---

## 📱 Flutter Services

### ResumeService (`lib/services/resume_service.dart`)

```dart
import 'package:your_app/services/resume_service.dart';

final resumeService = ResumeService();

// Save resume
final resumeData = ResumeService.ResumeData(
  fullName: 'John Doe',
  jobTitle: 'Software Engineer',
  email: 'john@example.com',
  // ... other fields
);

final result = await resumeService.saveResume(resumeData);
if (result['success']) {
  print('Resume saved!');
}

// Get resume
final getResult = await resumeService.getResume();
if (getResult['success']) {
  final resume = getResult['data'] as ResumeService.ResumeData;
  print(resume.fullName);
}

// Get ATS score
final scoreResult = await resumeService.getATSScore(resumeData);
print('ATS Score: ${scoreResult['score']}/100');
print('Grade: ${scoreResult['grade']}');
```

### VideoProgressService (`lib/services/video_progress_service.dart`)

```dart
import 'package:your_app/services/video_progress_service.dart';

final videoService = VideoProgressService();

// Auto-save progress (call every 10 seconds during playback)
await videoService.autoSaveProgress(
  videoId: 'dQw4w9WgXcQ',
  videoTitle: 'Python Introduction',
  skillName: 'Python Basics',
  careerName: 'Data Science',
  currentPosition: 180,
  duration: 600,
);

// Get resume position
final position = await videoService.getResumePosition(
  'dQw4w9WgXcQ',
  'Data Science',
);
print('Resume from: ${position}s');

// Mark as completed
await videoService.markVideoCompleted(
  videoId: 'dQw4w9WgXcQ',
  videoTitle: 'Python Introduction',
  skillName: 'Python Basics',
  careerName: 'Data Science',
  duration: 600,
);

// Get overall career progress
final progress = await videoService.getOverallCareerProgress('Data Science');
print('${progress['completedVideos']}/${progress['totalVideos']} videos completed');
print('Overall: ${progress['overallPercentage'].toStringAsFixed(1)}%');
```

---

## 💡 Usage Examples

### Example 1: Resume Builder Auto-Save

```dart
class ResumeBuilderScreen extends StatefulWidget {
  @override
  _ResumeBuilderScreenState createState() => _ResumeBuilderScreenState();
}

class _ResumeBuilderScreenState extends State<ResumeBuilderScreen> {
  final resumeService = ResumeService();
  Timer? _autoSaveTimer;
  
  @override
  void initState() {
    super.initState();
    _loadResume();
    _startAutoSave();
  }
  
  void _startAutoSave() {
    _autoSaveTimer = Timer.periodic(Duration(seconds: 30), (timer) {
      _saveResume();
    });
  }
  
  Future<void> _loadResume() async {
    final result = await resumeService.getResume();
    if (result['success'] && result['data'] != null) {
      // Populate form fields
      final resume = result['data'] as ResumeService.ResumeData;
      setState(() {
        // Update UI with resume data
      });
    }
  }
  
  Future<void> _saveResume() async {
    final resumeData = _buildResumeData();
    await resumeService.saveResume(resumeData);
  }
  
  @override
  void dispose() {
    _autoSaveTimer?.cancel();
    super.dispose();
  }
}
```

### Example 2: Video Player with Progress Tracking

```dart
class VideoPlayerScreen extends StatefulWidget {
  final String videoId;
  final String videoTitle;
  final String skillName;
  final String careerName;
  
  @override
  _VideoPlayerScreenState createState() => _VideoPlayerScreenState();
}

class _VideoPlayerScreenState extends State<VideoPlayerScreen> {
  final videoService = VideoProgressService();
  Timer? _progressTimer;
  VideoPlayerController? _controller;
  
  @override
  void initState() {
    super.initState();
    _initializePlayer();
  }
  
  Future<void> _initializePlayer() async {
    // Get resume position
    final resumePosition = await videoService.getResumePosition(
      widget.videoId,
      widget.careerName,
    );
    
    // Initialize player and seek to resume position
    _controller = VideoPlayerController.network(videoUrl);
    await _controller!.initialize();
    await _controller!.seekTo(Duration(seconds: resumePosition));
    await _controller!.play();
    
    // Start auto-save timer (every 10 seconds)
    _progressTimer = Timer.periodic(Duration(seconds: 10), (timer) {
      _saveProgress();
    });
  }
  
  Future<void> _saveProgress() async {
    if (_controller != null && _controller!.value.isInitialized) {
      await videoService.autoSaveProgress(
        videoId: widget.videoId,
        videoTitle: widget.videoTitle,
        skillName: widget.skillName,
        careerName: widget.careerName,
        currentPosition: _controller!.value.position.inSeconds,
        duration: _controller!.value.duration.inSeconds,
      );
    }
  }
  
  @override
  void dispose() {
    _progressTimer?.cancel();
    _saveProgress(); // Final save
    _controller?.dispose();
    super.dispose();
  }
}
```

### Example 3: Learning Dashboard

```dart
class LearningDashboard extends StatefulWidget {
  final String careerName;
  
  @override
  _LearningDashboardState createState() => _LearningDashboardState();
}

class _LearningDashboardState extends State<LearningDashboard> {
  final videoService = VideoProgressService();
  List<VideoProgressService.LearningPathSummary> summaries = [];
  Map<String, dynamic>? overallProgress;
  
  @override
  void initState() {
    super.initState();
    _loadProgress();
  }
  
  Future<void> _loadProgress() async {
    // Get all learning paths summary
    final summariesResult = await videoService.getAllLearningPathsSummary(
      widget.careerName,
    );
    
    // Get overall career progress
    final progressResult = await videoService.getOverallCareerProgress(
      widget.careerName,
    );
    
    setState(() {
      if (summariesResult['success']) {
        summaries = summariesResult['data'];
      }
      overallProgress = progressResult;
    });
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('${widget.careerName} Progress')),
      body: Column(
        children: [
          // Overall Progress Card
          Card(
            child: Padding(
              padding: EdgeInsets.all(16),
              child: Column(
                children: [
                  Text('Overall Progress',
                    style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold)),
                  SizedBox(height: 16),
                  LinearProgressIndicator(
                    value: (overallProgress?['overallPercentage'] ?? 0) / 100,
                  ),
                  SizedBox(height: 8),
                  Text('${overallProgress?['completedVideos'] ?? 0}/'
                       '${overallProgress?['totalVideos'] ?? 0} videos completed'),
                  Text('Total watch time: ${overallProgress?['formattedWatchTime'] ?? '0:00'}'),
                ],
              ),
            ),
          ),
          
          // Skill Progress List
          Expanded(
            child: ListView.builder(
              itemCount: summaries.length,
              itemBuilder: (context, index) {
                final summary = summaries[index];
                return ListTile(
                  title: Text(summary.skillName),
                  subtitle: Text('${summary.completedVideos}/${summary.totalVideos} videos'),
                  trailing: CircularProgressIndicator(
                    value: summary.progressPercentage / 100,
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}
```

---

## 🚀 Migration Steps

### Step 1: Run Database Migration

```bash
# Connect to your MySQL database
mysql -u your_username -p your_database_name

# Run the migration script
source MyFirstApi/sql/database_schema_complete.sql
```

Or using MySQL Workbench:
1. Open MySQL Workbench
2. Connect to your database
3. File → Run SQL Script
4. Select `database_schema_complete.sql`
5. Execute

### Step 2: Verify Tables Created

```sql
SHOW TABLES;

-- Should show:
-- user_resumes
-- video_watch_history
-- learning_path_progress
-- resume_export_history
-- (plus existing tables)
```

### Step 3: Test Backend API

```bash
# Start the backend server
cd MyFirstApi
dotnet run

# Test resume endpoint
curl -X GET http://localhost:5087/api/resume \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"

# Test video progress endpoint
curl -X GET http://localhost:5087/api/videoprogress/recent \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

### Step 4: Update Flutter App

1. Add services to your app:
```dart
// In your main.dart or service locator
final resumeService = ResumeService();
final videoService = VideoProgressService();
```

2. Update resume builder to use ResumeService
3. Update video player to use VideoProgressService
4. Test data persistence

---

## 📊 Performance Optimization

### Indexes Created
```sql
-- Video history indexes
CREATE INDEX idx_video_history_user_career 
  ON video_watch_history(user_id, career_name, last_watched);

-- Course progress indexes  
CREATE INDEX idx_course_progress_user_career 
  ON course_progress(user_id, career_name, is_completed);

-- Learning path indexes
CREATE INDEX idx_learning_path_progress_user 
  ON learning_path_progress(user_id, progress_percentage);
```

### Database Triggers
- Auto-update learning path progress when video progress changes
- Maintain data integrity across related tables

---

## 🔒 Security Notes

1. **JWT Authentication**: All endpoints require valid JWT token
2. **User Isolation**: Queries filter by authenticated user ID
3. **SQL Injection Prevention**: Parameterized queries used throughout
4. **Data Validation**: Frontend and backend validation

---

## 📝 Testing Checklist

- [ ] Resume save/load works correctly
- [ ] Resume auto-save triggers every 30 seconds
- [ ] Video progress saves every 10 seconds
- [ ] Video resumes from last position
- [ ] Progress percentage calculates correctly
- [ ] Learning path summary updates automatically
- [ ] ATS score calculation works
- [ ] Recently watched videos display correctly
- [ ] Database triggers fire properly
- [ ] All API endpoints return correct data

---

## 🆘 Troubleshooting

### Issue: Connection timeout
**Solution**: Check backend URL in services (update IP address if needed)

### Issue: 401 Unauthorized
**Solution**: Ensure JWT token is valid and not expired

### Issue: Resume not loading
**Solution**: Check if user_resumes table has data for the user

### Issue: Video progress not saving
**Solution**: Verify video_watch_history table exists and has proper indexes

---

## 📚 Additional Resources

- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core)
- [Flutter HTTP Package](https://pub.dev/packages/http)
- [MySQL JSON Functions](https://dev.mysql.com/doc/refman/8.0/en/json-functions.html)

---

**Last Updated**: December 2024  
**Version**: 1.0.0
