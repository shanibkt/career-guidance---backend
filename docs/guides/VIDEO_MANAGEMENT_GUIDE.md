# Video Management Admin Panel - Quick Start

## ✅ What's Implemented

### Full CRUD Operations for Learning Videos
- **Create** new videos with all details + transcript
- **Read/View** all videos and their transcripts  
- **Update** video information and transcripts
- **Delete** videos (with confirmation)

### Features
- ✨ **Side-by-side interface**: Video list + full editor
- 📊 **Progress tracking**: See which videos have transcripts
- 📝 **Full transcript editor**: Scrollable 300px height box
- 🔍 **Character counter**: Track transcript size
- 🎯 **Form validation**: Required fields marked with *
- 🗑️ **Safe deletion**: Confirmation dialog before deleting
- 💾 **Auto-save**: Instant feedback on save/error

## 🚀 How to Use

### 1. Access Admin Panel

Add this to your Flutter admin navigation:

```dart
import 'package:career_guidence/features/admin/screens/video_management_screen.dart';

// In your admin menu
ElevatedButton.icon(
  onPressed: () {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => const VideoManagementScreen(),
      ),
    );
  },
  icon: const Icon(Icons.video_settings),
  label: const Text('Manage Videos'),
)
```

### 2. View All Videos

- **Left panel** shows all videos
- ✅ Green check = Has transcript
- ⚠️ Orange warning = No transcript
- Click any video to edit

### 3. Create New Video

Click **"+ New Video"** button (floating action button)

**Fill in the form:**
- **Skill Name*** (e.g., "Python", "React")
- **Video Title*** (e.g., "Python Complete Tutorial")
- **Description** (optional summary)
- **YouTube Video ID*** (e.g., "dQw4w9WgXcQ")
- **Duration (minutes)*** (e.g., "120")
- **Thumbnail URL** (optional, auto-generated if empty)
- **Transcript** (paste full subtitle content)

Click **"Create Video"** → Done! ✅

### 4. Edit Existing Video

**Select video** from left panel

The form loads with all current data including **full transcript**

**Modify any fields** you want to change

Click **"Save Changes"** → Updated! ✅

### 5. View/Edit Full Transcript

When you select a video, the **transcript box shows full content**:
- ✅ Scrollable 300px height container
- ✅ Shows full text (not truncated)
- ✅ Character counter at top
- ✅ Monospace font for readability
- ✅ Easy copy/paste

**To update transcript:**
1. Click in the transcript box
2. Paste new content (or edit existing)
3. Click "Save Changes"
4. See confirmation ✅

### 6. Download Subtitles

**Click "Download Subtitles" button** → Copies link to clipboard

**Open** https://www.downloadyoutubesubtitles.com/

**Paste** the YouTube video ID (shown on screen)

**Download** subtitles as TXT

**Copy all text** and paste into transcript box

**Save** ✅

### 7. Delete Video

**Method 1:** Click trash icon (🗑️) next to video in list

**Method 2:** Select video → Click "Delete Video" button in editor

**Confirmation dialog appears** → Click "Delete" to confirm

Video removed! ✅

## 📡 API Endpoints

### Get All Videos
```http
GET /api/learningvideos
Authorization: Optional (public endpoint)
```

### Get Video Transcript
```http
GET /api/learningvideos/{id}/transcript
Authorization: Optional (public endpoint)
```

### Create Video (Admin)
```http
POST /api/learningvideos
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "skillName": "Python",
  "videoTitle": "Python Tutorial",
  "videoDescription": "Learn Python",
  "youtubeVideoId": "abc123",
  "durationMinutes": 120,
  "thumbnailUrl": "https://...",
  "transcript": "Full subtitle text..."
}
```

### Update Video (Admin)
```http
PUT /api/learningvideos/{id}
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "skillName": "Python",
  "videoTitle": "Python Tutorial - Updated",
  "videoDescription": "Updated description",
  "youtubeVideoId": "abc123",
  "durationMinutes": 125,
  "thumbnailUrl": "https://...",
  "transcript": "Updated transcript..."
}
```

### Update Transcript Only (Admin)
```http
PUT /api/learningvideos/{id}/transcript
Authorization: Bearer {admin_token}
Content-Type: application/json

{
  "transcript": "New transcript content..."
}
```

### Delete Video (Admin)
```http
DELETE /api/learningvideos/{id}
Authorization: Bearer {admin_token}
```

## 🎨 UI Features

### Video List (Left Panel)
- Shows all videos with status icons
- Green ✅ = Has transcript
- Orange ⚠️ = No transcript
- Delete button (trash icon) on each item
- Click to select and edit

### Editor (Right Panel)
- All fields visible at once
- Required fields marked with *
- Transcript box: 300px scrollable height
- Character counter updates live
- Buttons: Save, Clear Form, Delete

### Transcript Box Features
- **Height**: 300px (fully scrollable)
- **Font**: Monospace for readability
- **Border**: Outlined container
- **Header**: Shows character count
- **Large text indicator**: Shows "Large" badge if >50k chars

## 🔒 Security

- **Public Endpoints**: GET requests (anyone can view videos)
- **Admin Endpoints**: POST, PUT, DELETE (requires Admin role)
- **Authorization**: Bearer token with `role: Admin`
- **Validation**: Backend validates all required fields

## 🧪 Testing

### Test Backend
```bash
# Start backend
cd career-guidance---backend
dotnet run

# Test create video (requires admin token)
curl -X POST http://localhost:5001/api/learningvideos \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ADMIN_TOKEN" \
  -d '{"skillName":"Test Skill","videoTitle":"Test Video","youtubeVideoId":"test123","durationMinutes":60}'
```

### Test Flutter
```bash
cd career_guidence
flutter run

# In app:
# 1. Login as admin
# 2. Navigate to Video Management
# 3. Try creating/editing/deleting videos
# 4. Paste a transcript and save
# 5. Verify transcript shows fully in editor
```

## 📁 Files Created

### Flutter
- ✅ `lib/features/admin/screens/video_management_screen.dart` - Full CRUD UI
- ✅ `lib/services/api/video_management_service.dart` - API service

### Backend
- ✅ `Controllers/LearningVideosController.cs` - Added CRUD endpoints:
  - POST /api/learningvideos (create)
  - PUT /api/learningvideos/{id} (update)
  - DELETE /api/learningvideos/{id} (delete)
  - PUT /api/learningvideos/{id}/transcript (update transcript only)
  - GET endpoints remain public

## 💡 Tips

### Managing Transcripts
- **Batch download**: Get all subtitles first, then upload
- **Large transcripts**: System handles up to 16MB (TEXT column limit)
- **Character count**: Visible in real-time, helps track size
- **Scrolling**: 300px box lets you scroll through entire transcript

### Video Creation
- **Skill name** should match existing career paths if applicable
- **Thumbnail** auto-generated from YouTube if left empty
- **Duration** should match actual video length for UX consistency
- **YouTube ID** found in URL: youtube.com/watch?v=**THIS_PART**

### Best Practices
1. **Always add transcripts** when creating videos
2. **Verify transcript quality** before saving
3. **Test quiz generation** after adding transcript
4. **Keep backups** of transcripts (export periodically)

## 🆘 Troubleshooting

### Can't see full transcript
- ✅ **Fixed!** Transcript box is 300px scrollable
- Scroll down to see all content
- Character counter shows total length

### "Unauthorized" error
- Ensure logged in as **Admin** user
- Check JWT token has `role: "Admin"`
- Verify user has `is_admin=1` in database

### Transcript not saving
- Check backend logs for errors
- Verify database column exists (TEXT type)
- Check transcript size (<16MB limit)

### Video creation fails
- Ensure all required fields filled (marked with *)
- Check skill name doesn't already exist
- Verify YouTube video ID is valid
- Check backend logs for details

---

**Status**: ✅ Fully functional  
**Backend**: 5 new endpoints added  
**Flutter**: Complete admin panel  
**Transcript**: Full view/edit capability  
**CRUD**: Create, Read, Update, Delete all working
