# AI-Powered Career Suggestion System - API Documentation

## Overview
This system uses Groq AI (Llama 3.1) to generate personalized career quiz questions and provide career match recommendations based on user profiles and quiz responses.

## Setup

### 1. Run Database Migration
Execute the SQL file to create required tables:
```sql
mysql -u root -p my_database < sql/create_career_tables.sql
```

This creates:
- `careers` table with 10 predefined careers
- `quiz_sessions` table to store user quiz attempts
- `recommendations` table to save AI-generated career matches

### 2. Start the API
```bash
dotnet run
```

## API Endpoints

### 1. Generate Quiz Questions
**Endpoint:** `POST /api/quiz/generate`

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body:** None (uses user profile from JWT token)

**Response:**
```json
{
  "sessionId": 123,
  "questions": [
    {
      "id": 1,
      "question": "Which activity energizes you the most?",
      "type": "multiple_choice",
      "options": [
        "Solving complex technical problems",
        "Creating visual designs",
        "Helping people overcome challenges",
        "Analyzing data to find patterns"
      ]
    },
    {
      "id": 2,
      "question": "Describe a project you're proud of and why",
      "type": "open_ended"
    }
  ]
}
```

**How it works:**
- Fetches user profile (education, skills, interests) from database
- Calls Groq AI to generate 10 personalized questions (7 multiple-choice, 3 open-ended)
- Saves quiz session to database
- Returns session ID and questions

---

### 2. Submit Quiz Answers
**Endpoint:** `POST /api/quiz/submit`

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body:**
```json
{
  "sessionId": 123,
  "answers": [
    {
      "questionId": 1,
      "answer": "Solving complex technical problems"
    },
    {
      "questionId": 2,
      "answer": "I built a mobile app that helps students track their study progress..."
    }
  ]
}
```

**Response:**
```json
{
  "status": "completed",
  "message": "Quiz submitted successfully"
}
```

**How it works:**
- Validates session belongs to logged-in user
- Stores answers in database
- Marks quiz as completed

---

### 3. Generate Career Recommendations
**Endpoint:** `POST /api/recommendations/generate`

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json
```

**Request Body:**
```json
{
  "sessionId": 123
}
```

**Response:**
```json
{
  "recommendations": [
    {
      "careerId": 1,
      "careerName": "Software Engineer",
      "matchPercentage": 92.5,
      "reasoning": "Your strong problem-solving skills, technical interests, and experience with coding align perfectly with software engineering...",
      "strengths": [
        "Analytical thinking",
        "Technical aptitude",
        "Detail-oriented"
      ],
      "areasToDevelop": [
        "Team collaboration",
        "Communication skills"
      ]
    },
    {
      "careerId": 2,
      "careerName": "Data Scientist",
      "matchPercentage": 85.3,
      "reasoning": "Your analytical mindset and interest in patterns make you well-suited...",
      "strengths": ["Problem solving", "Mathematics"],
      "areasToDevelop": ["Python programming", "Statistics"]
    }
  ]
}
```

**How it works:**
- Fetches quiz session, user profile, and all careers from database
- Sends data to Groq AI for analysis
- AI returns match percentages (0-100) for each career with reasoning
- Saves recommendations to database
- Returns top 10 career matches ranked by percentage

---

### 4. Get Saved Recommendations
**Endpoint:** `GET /api/recommendations`

**Headers:**
```
Authorization: Bearer <JWT_TOKEN>
```

**Response:**
```json
{
  "recommendations": [
    {
      "id": 45,
      "careerId": 1,
      "careerName": "Software Engineer",
      "matchPercentage": 92.5,
      "reasoning": "...",
      "strengths": ["..."],
      "areasToDevelop": ["..."],
      "createdAt": "2025-11-23T10:30:00Z"
    }
  ]
}
```

**How it works:**
- Fetches all saved recommendations for the logged-in user
- Returns them sorted by match percentage (highest first)

---

## Testing the Flow

### Step 1: Login/Signup
```bash
# Signup
curl -X POST http://localhost:5001/api/auth/signup \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "fullName": "Test User",
    "email": "test@example.com",
    "password": "Test123!",
    "phone": "1234567890",
    "age": 22,
    "gender": "Male"
  }'

# Response includes JWT token
```

### Step 2: Update Profile (Optional)
```bash
curl -X POST http://localhost:5001/api/userprofile \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "userId": 1,
    "educationLevel": "Bachelor",
    "fieldOfStudy": "Computer Science",
    "skills": ["Python", "Java"],
    "areasOfInterest": "Software Development, AI"
  }'
```

### Step 3: Generate Quiz
```bash
curl -X POST http://localhost:5001/api/quiz/generate \
  -H "Authorization: Bearer YOUR_TOKEN"

# Save the sessionId from response
```

### Step 4: Submit Answers
```bash
curl -X POST http://localhost:5001/api/quiz/submit \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "sessionId": 1,
    "answers": [
      {"questionId": 1, "answer": "Solving technical problems"},
      {"questionId": 2, "answer": "I built a web application..."}
    ]
  }'
```

### Step 5: Get Recommendations
```bash
curl -X POST http://localhost:5001/api/recommendations/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"sessionId": 1}'
```

### Step 6: View Saved Recommendations
```bash
curl -X GET http://localhost:5001/api/recommendations \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## AI Configuration

**API Key:** `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`

**Model:** `llama-3.1-8b-instant` (fast, good for real-time responses)

**Alternative Models:**
- `llama-3.1-70b-versatile` - More accurate but slower
- `llama-3.3-70b-versatile` - Latest model

To change the model, edit `Services/GroqService.cs`:
```csharp
private readonly string _model = "llama-3.1-70b-versatile";
```

---

## Database Schema

### careers
- `id` - Career ID
- `name` - Career name
- `description` - Career description
- `required_education` - Education requirements
- `average_salary` - Salary range
- `growth_outlook` - Job market outlook
- `key_skills` - Required skills (JSON array)

### quiz_sessions
- `id` - Session ID
- `user_id` - User who took quiz
- `questions` - Generated questions (JSON)
- `answers` - User responses (JSON)
- `completed` - Boolean
- `completed_at` - Completion timestamp

### recommendations
- `id` - Recommendation ID
- `user_id` - User
- `career_id` - Career
- `match_percentage` - Match score (0-100)
- `reasoning` - AI explanation
- `strengths` - User strengths (JSON array)
- `areas_to_develop` - Skills to improve (JSON array)

---

## Troubleshooting

### AI Not Responding
- Check Groq API key is valid
- Verify internet connection
- Check console logs for API errors

### Quiz Questions Not Personalized
- Ensure user has completed their profile (education, skills, interests)
- Without profile data, questions will be generic

### Low Match Percentages
- User should complete profile fully
- Answer quiz questions thoughtfully
- Try retaking quiz with different answers

---

## Notes

- Quiz sessions are saved, so users can retake quizzes anytime
- Recommendations update when new quiz is taken
- Each user can have multiple quiz sessions
- Recommendations are unique per user-career pair (no duplicates)
