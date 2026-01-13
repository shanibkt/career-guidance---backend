# Video Transcript Management - Setup Guide

## Overview
Store video transcripts/subtitles in the database for AI quiz generation. This eliminates YouTube API blocking issues.

## ✅ What's Implemented

### 1. Database Changes
- **New Column**: `transcript` (TEXT) added to `learning_videos` table
- Stores manually downloaded subtitle content
- Located after `youtube_video_id` column

### 2. Backend APIs (C#)
- **GET** `/api/learningvideos/{id}/transcript` - Get transcript for a video
- **PUT** `/api/learningvideos/{id}/transcript` - Update transcript (Admin only)
- **Modified** `/api/quiz/generate-from-video` - Now reads from database instead of YouTube

### 3. Flutter Admin Panel
- **New Screen**: `TranscriptManagementScreen`
- Visual progress tracker (X/Y videos completed)
- Side-by-side video list and editor
- Easy copy-paste workflow
- Auto-save with character count

### 4. Flutter Service Methods
- `getAllVideosWithTranscripts()` - Fetch all videos with transcript status
- `getVideoTranscript(videoId)` - Get transcript content
- `updateVideoTranscript(videoId, transcript)` - Upload transcript

## 🚀 Setup Steps

### Step 1: Run Database Migration
```bash
cd "career-guidance---backend"

# Option A: Using MySQL CLI
mysql -h sql.freedb.tech -u freedb_shanib -p freedb_career_guidence < sql/add_transcript_column.sql

# Option B: Using MySQL Workbench
# 1. Connect to sql.freedb.tech
# 2. Open sql/add_transcript_column.sql
# 3. Execute
```

### Step 2: Restart Backend Server
```bash
cd "career-guidance---backend"
dotnet restore
dotnet run
```

Backend will now:
- ✅ Read transcripts from database
- ✅ Fall back to skill-based quiz if no transcript
- ✅ Accept admin uploads via API

### Step 3: Add Admin Screen to Flutter App
Navigate to your admin section and add:

```dart
// In your admin menu/navigation
ElevatedButton.icon(
  onPressed: () {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const TranscriptManagementScreen(),
      ),
    );
  },
  icon: const Icon(Icons.subtitles),
  label: const Text('Manage Transcripts'),
)
```

## 📝 How to Use

### For Admins: Adding Transcripts

1. **Open Admin Panel**
   - Navigate to "Manage Transcripts" screen
   - See list of all videos with status indicators:
     - ✅ Green: Has transcript
     - ⚠️ Orange: No transcript

2. **Download Subtitles**
   - Click "Download Subtitles" button (copies link)
   - Open https://www.downloadyoutubesubtitles.com/ in browser
   - Paste the video ID (shown on screen)
   - Download subtitles as TXT or SRT
   - Copy all the text content

3. **Upload to Database**
   - Paste subtitle content in the editor
   - Click "Save Transcript"
   - See confirmation with character count

4. **Repeat for All Videos**
   - Progress bar shows completion percentage
   - Work through orange videos until all are green

### For Users: Taking Quizzes

**Nothing changes!** Quizzes work exactly the same:
1. Watch a video
2. Click "Take Quiz"
3. If transcript exists → Questions based on video content ✨
4. If no transcript → Questions based on skill topic (like before)

## 🎯 Benefits

### Before (YouTube API)
❌ YouTube blocks caption extraction  
❌ All quizzes fall back to skill-based  
❌ Can't use actual video content  
❌ XmlParserException errors  

### After (Database Storage)
✅ 100% reliable transcript access  
✅ Quiz questions match video content  
✅ No API blocking issues  
✅ Works offline (once transcripts loaded)  
✅ Admin controls quality  

## 📊 API Reference

### Get Video Transcript
```http
GET /api/learningvideos/{id}/transcript
Authorization: Bearer {token}
```

**Response:**
```json
{
  "videoId": "c9Wg6Cb_YlU",
  "videoTitle": "UI/UX Design Complete",
  "hasTranscript": true,
  "transcript": "Welcome to this UI/UX tutorial...",
  "transcriptLength": 15420
}
```

### Update Video Transcript
```http
PUT /api/learningvideos/{id}/transcript
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "transcript": "Full subtitle content here..."
}
```

**Response:**
```json
{
  "success": true,
  "message": "Transcript updated successfully",
  "transcriptLength": 15420
}
```

### Generate Quiz from Video
```http
POST /api/quiz/generate-from-video
Authorization: Bearer {token}
Content-Type: application/json

{
  "video_id": "c9Wg6Cb_YlU",
  "skill_name": "UI/UX Design",
  "video_title": "UI/UX Design Complete"
}
```

**Response:**
```json
{
  "quiz_id": "uuid",
  "questions": [...],
  "transcript_available": true,
  "message": "Quiz generated from video transcript"
}
```

## 🔍 Testing

### Test Backend
```bash
# Terminal 1: Start backend
cd career-guidance---backend
dotnet run

# Terminal 2: Test transcript endpoint
curl http://localhost:5001/api/learningvideos/1/transcript \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Test Flutter
```bash
cd career_guidence
flutter run

# In app:
# 1. Login as admin
# 2. Navigate to "Manage Transcripts"
# 3. Try uploading a transcript
# 4. Watch video → Take Quiz
# 5. Check logs for "transcript_available: true"
```

## 📁 Files Created/Modified

### Backend
- ✅ `sql/add_transcript_column.sql` - Database migration
- ✅ `Controllers/LearningVideosController.cs` - Added 2 new endpoints
- ✅ `Controllers/QuizController.cs` - Modified to read from DB

### Flutter
- ✅ `lib/features/admin/screens/transcript_management_screen.dart` - Admin UI
- ✅ `lib/services/api/career_quiz_service.dart` - Added 3 new methods

## 🎓 Next Steps

1. ✅ Run database migration
2. ✅ Restart backend server
3. ✅ Test transcript upload with one video
4. ✅ Download all 27 video subtitles
5. ✅ Upload all transcripts via admin panel
6. ✅ Test quiz generation with transcript
7. ✅ Celebrate! 🎉

## 🆘 Troubleshooting

### "Unauthorized" error when uploading
- Ensure you're logged in as Admin
- Check `role` in JWT token is "Admin"
- Admin users must have `is_admin=1` in database

### Transcript not saving
- Check backend logs for SQL errors
- Verify database column exists: `DESCRIBE learning_videos;`
- Check transcript size (should be < 16MB for TEXT column)

### Quiz still says "captions unavailable"
- Verify transcript was saved: Check `/api/learningvideos/{id}/transcript`
- Check backend logs for "Transcript found in database"
- Restart backend after database migration

### Can't access admin screen
- Add navigation route in your admin section
- Import: `import 'package:career_guidence/features/admin/screens/transcript_management_screen.dart';`
- Ensure user has admin role

## 💡 Tips

- **Batch Upload**: Download all subtitles first, then upload systematically
- **Quality Check**: Review auto-generated subtitles for accuracy
- **Character Limit**: TEXT column supports up to 16MB (~16 million characters)
- **Performance**: Transcripts are cached in memory during quiz generation
- **Backup**: Consider exporting transcripts periodically for backup

---

**Status**: ✅ Ready to use  
**Files Created**: 2 new, 3 modified  
**Database Migration**: Required (run once)  
**User Impact**: Significantly improved quiz quality!
