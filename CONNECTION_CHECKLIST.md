# Backend Connection Checklist

## ✅ Working Endpoints

1. **Authentication** - `POST /api/auth/login`
   - Status: ✅ WORKING
   - Returns: JWT token

2. **Profile** - `GET /api/userprofile/13`
   - Status: ✅ WORKING  
   - Returns: Full profile with phone, age, gender, education, skills, careerPath

## ❓ Endpoints to Check in Flutter App

### 3. Career Recommendations
- **Endpoint**: `GET /api/recommendations/careers`
- **Flutter File**: Check how this is called
- **Common Issue**: Missing auth token

### 4. Chatbot
- **Endpoint**: `POST /api/chat`
- **Body**: `{"message": "your message"}`
- **Requirements**: 
  - Auth token
  - Groq API key configured
- **Check**: `appsettings.json` has Groq API key

### 5. Quiz Generation
- **Endpoint**: `POST /api/quiz/generate`
- **Body**: `{"careerName": "...", "skillName": "...", "difficulty": "medium"}`
- **Requirements**: Auth token, Groq API

### 6. Learning Videos
- **Endpoint**: `GET /api/learningvideos`
- **Or**: `GET /api/learningvideos/skills?skills=Python,Flutter`
- **Requirements**: Auth token

### 7. Personalized Jobs
- **Endpoint**: `GET /api/jobs/personalized`
- **Requirements**: 
  - Auth token
  - User must have selected career
  - Profile must have skills

### 8. Resume
- **Endpoint**: `GET /api/resume/13`
- **Endpoint**: `POST /api/resume/save`
- **Requirements**: Auth token

## 🔧 Fixes Applied

1. ✅ Changed `UserProfiles` → `userprofiles` (lowercase)
2. ✅ Added `CareerPath` field to model
3. ✅ Updated stored procedures
4. ✅ Fixed image upload query
5. ✅ Backend running on `http://localhost:5001`
6. ✅ Flutter configured to use `http://10.0.2.2:5001`

## 🐛 Common Issues & Solutions

### Issue: "Can't use chatbot"
**Possible Causes**:
1. No auth token sent
2. Groq API key missing/invalid
3. Flutter not sending proper request body

**Check**:
- Flutter console for error messages
- Backend logs for incoming requests
- Network inspector in Flutter DevTools

### Issue: "Can't generate quiz"
**Possible Causes**:
1. Same as chatbot (uses Groq API)
2. Missing careerName or skillName in request

### Issue: "Career suggestions not showing"
**Possible Causes**:
1. No careers in database
2. Auth token not sent
3. Flutter parsing error

**Solution**: Check if `/api/recommendations/careers` returns data

### Issue: "Learning path not clickable"
**Possible Causes**:
1. No selected career
2. No learning videos in database for that career
3. Flutter navigation error

**Solution**: Must select a career first from recommendations

### Issue: "Personalized jobs not showing"
**Possible Causes**:
1. No career selected
2. No skills in profile
3. External job API not responding

### Issue: "Resume data incomplete"
**Possible Causes**:
1. Profile data not fully filled
2. Resume not created yet (first time users)

**Solution**: Fill all profile fields first

## 📋 Testing Steps

1. **Login** → Should work ✅
2. **Fill Profile** → Edit and save all fields
3. **Select a Career** → Go to recommendations, pick one
4. **Try Chatbot** → Send message, check Flutter console
5. **Try Quiz** → Click generate, check logs
6. **Check Learning Path** → Should show videos
7. **Check Jobs** → Should show personalized results

## 🔍 Debugging Commands

Check if backend is running:
```powershell
Get-NetTCPConnection -LocalPort 5001
```

Test login:
```powershell
curl http://localhost:5001/api/auth/login -Method POST -Headers @{"Content-Type"="application/json"} -Body '{"email":"shanib@gmail.com","password":"shanib"}'
```

## 📝 What to Check Next

1. **Flutter Console Logs**: Look for specific error messages when clicking each feature
2. **Backend Logs**: Check what requests are received
3. **Network Tab**: Use Flutter DevTools to see API calls

Share the specific error messages from Flutter console for each non-working feature!
