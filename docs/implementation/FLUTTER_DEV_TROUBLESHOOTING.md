# 🚨 Flutter Developer - Quiz Integration Troubleshooting Guide

## Backend Status: ✅ WORKING PERFECTLY

Your .NET backend is **100% functional** and generating quizzes successfully. The issue is on the Flutter side.

---

## ⚠️ Common Issues & Fixes

### 1. **NETWORK CONNECTION ISSUE** (Most Likely)

#### ❌ WRONG - Will NOT work:
```dart
const String baseUrl = "http://localhost:5001/api";
const String baseUrl = "http://127.0.0.1:5001/api";
```
**Why?** Localhost refers to the device/emulator itself, not your PC.

#### ✅ CORRECT - Use your PC's IP address:

**Step 1:** Get your PC's IP address
- Windows: Open CMD → Type `ipconfig`
- Look for **"IPv4 Address"** (e.g., `192.168.1.100` or `10.0.0.5`)

**Step 2:** Update Flutter code:
```dart
// Replace with YOUR actual IP address
const String baseUrl = "http://192.168.1.100:5001/api";
```

**Step 3:** Make sure your phone/emulator is on the **same WiFi network** as your PC

---

### 2. **ANDROID CLEARTEXT TRAFFIC** (Required for HTTP)

Android blocks HTTP traffic by default. You MUST enable it.

**File:** `android/app/src/main/AndroidManifest.xml`

```xml
<manifest xmlns:android="http://schemas.android.com/apk/res/android">
    
    <!-- Add this permission -->
    <uses-permission android:name="android.permission.INTERNET"/>
    
    <application
        android:label="your_app_name"
        android:usesCleartextTraffic="true"  <!-- ADD THIS LINE -->
        android:icon="@mipmap/ic_launcher">
        
        <!-- rest of your config -->
    </application>
</manifest>
```

---

### 3. **JSON PARSING ERROR** (Field Name Mismatch)

The API returns `snake_case` but your model might use `camelCase`.

#### ✅ CORRECT Model:
```dart
class QuizQuestion {
  final int id;
  final String question;
  final String type;
  final String skillCategory;
  final String correctAnswer;
  final List<String> options;

  QuizQuestion({
    required this.id,
    required this.question,
    required this.type,
    required this.skillCategory,
    required this.correctAnswer,
    required this.options,
  });

  factory QuizQuestion.fromJson(Map<String, dynamic> json) {
    return QuizQuestion(
      id: json['id'],
      question: json['question'],
      type: json['type'],
      skillCategory: json['skill_category'],    // ⚠️ MUST BE snake_case
      correctAnswer: json['correct_answer'],    // ⚠️ MUST BE snake_case
      options: List<String>.from(json['options']),
    );
  }
}

class QuizResponse {
  final String quizId;
  final List<QuizQuestion> questions;

  QuizResponse({
    required this.quizId,
    required this.questions,
  });

  factory QuizResponse.fromJson(Map<String, dynamic> json) {
    return QuizResponse(
      quizId: json['quizId'],    // ⚠️ camelCase here
      questions: (json['questions'] as List)
          .map((q) => QuizQuestion.fromJson(q))
          .toList(),
    );
  }
}
```

---

### 4. **MISSING/INVALID JWT TOKEN**

Make sure you're sending the JWT token correctly.

```dart
Future<QuizResponse> generateQuiz() async {
  final prefs = await SharedPreferences.getInstance();
  final token = prefs.getString('jwt_token') ?? '';
  
  if (token.isEmpty) {
    throw Exception('No authentication token found. Please login again.');
  }

  final response = await http.post(
    Uri.parse('$baseUrl/quiz/generate'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',  // ⚠️ Don't forget "Bearer "
    },
  );

  if (response.statusCode == 401) {
    throw Exception('Token expired. Please login again.');
  }

  if (response.statusCode == 200) {
    return QuizResponse.fromJson(jsonDecode(response.body));
  } else {
    throw Exception('Failed: ${response.body}');
  }
}
```

---

### 5. **USER HAS NO SKILLS IN PROFILE**

The backend requires the user to have skills before generating a quiz.

**Error:** `"No skills found in profile"`

**Fix:** Ensure the user has completed their profile with skills:
```dart
// User profile must have skills array like:
{
  "skills": ["Flutter", "Java", "SQL"]
}
```

---

## 🔍 DEBUG YOUR ISSUE

Add this logging to see EXACTLY what's wrong:

```dart
class QuizApi {
  static const String baseUrl = "http://YOUR_IP:5001/api";  // ⚠️ CHANGE THIS
  
  static Future<QuizResponse> generateQuiz() async {
    try {
      final prefs = await SharedPreferences.getInstance();
      final token = prefs.getString('jwt_token') ?? '';
      
      print('🔍 Base URL: $baseUrl/quiz/generate');
      print('🔑 Token: ${token.substring(0, 20)}...');
      
      final response = await http.post(
        Uri.parse('$baseUrl/quiz/generate'),
        headers: {
          'Content-Type': 'application/json',
          'Authorization': 'Bearer $token',
        },
      );
      
      print('📡 Status Code: ${response.statusCode}');
      print('📄 Response Body: ${response.body}');
      
      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        print('✅ Quiz ID: ${data['quizId']}');
        print('✅ Questions: ${data['questions'].length}');
        return QuizResponse.fromJson(data);
      } else {
        print('❌ Error: ${response.body}');
        throw Exception(response.body);
      }
    } catch (e) {
      print('💥 Exception: $e');
      rethrow;
    }
  }
}
```

**Run this and check your console.** Share the output if you still have issues.

---

## 📱 EXPECTED SUCCESSFUL OUTPUT

When working correctly, you should see:
```
🔍 Base URL: http://192.168.1.100:5001/api/quiz/generate
🔑 Token: eyJhbGciOiJIUzI1NiIs...
📡 Status Code: 200
📄 Response Body: {"quizId":"024d9f26-724e-4cbc-ac6d-fd01cf8bdca6","questions":[...]}
✅ Quiz ID: 024d9f26-724e-4cbc-ac6d-fd01cf8bdca6
✅ Questions: 10
```

---

## 🧪 TEST WITH POSTMAN/CURL FIRST

Before testing in Flutter, verify the API works:

### Using Postman:
```
POST http://YOUR_IP:5001/api/quiz/generate
Headers:
  Content-Type: application/json
  Authorization: Bearer YOUR_JWT_TOKEN
```

### Using curl:
```bash
curl -X POST http://YOUR_IP:5001/api/quiz/generate \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

If this works, the backend is fine. Fix Flutter network settings.

---

## ✅ COMPLETE WORKING CODE

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class QuizApi {
  static const String baseUrl = "http://192.168.1.100:5001/api";  // ⚠️ CHANGE THIS
  
  static Future<String> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('jwt_token') ?? '';
  }

  static Future<QuizResponse> generateQuiz() async {
    final token = await _getToken();
    
    final response = await http.post(
      Uri.parse('$baseUrl/quiz/generate'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
    );

    if (response.statusCode == 200) {
      return QuizResponse.fromJson(jsonDecode(response.body));
    } else if (response.statusCode == 400) {
      final error = jsonDecode(response.body);
      throw Exception(error['error'] ?? 'No skills in profile');
    } else if (response.statusCode == 401) {
      throw Exception('Please login again');
    } else {
      throw Exception('Server error: ${response.statusCode}');
    }
  }

  static Future<QuizResult> submitQuiz(String quizId, List<QuizAnswer> answers) async {
    final token = await _getToken();
    
    final response = await http.post(
      Uri.parse('$baseUrl/quiz/submit'),
      headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer $token',
      },
      body: jsonEncode({
        'quizId': quizId,
        'answers': answers.map((a) => a.toJson()).toList(),
      }),
    );

    if (response.statusCode == 200) {
      return QuizResult.fromJson(jsonDecode(response.body));
    } else {
      throw Exception('Failed to submit: ${response.body}');
    }
  }
}

// MODELS
class QuizResponse {
  final String quizId;
  final List<QuizQuestion> questions;

  QuizResponse({required this.quizId, required this.questions});

  factory QuizResponse.fromJson(Map<String, dynamic> json) {
    return QuizResponse(
      quizId: json['quizId'],
      questions: (json['questions'] as List)
          .map((q) => QuizQuestion.fromJson(q))
          .toList(),
    );
  }
}

class QuizQuestion {
  final int id;
  final String question;
  final String type;
  final String skillCategory;
  final String correctAnswer;
  final List<String> options;

  QuizQuestion({
    required this.id,
    required this.question,
    required this.type,
    required this.skillCategory,
    required this.correctAnswer,
    required this.options,
  });

  factory QuizQuestion.fromJson(Map<String, dynamic> json) {
    return QuizQuestion(
      id: json['id'],
      question: json['question'],
      type: json['type'],
      skillCategory: json['skill_category'],
      correctAnswer: json['correct_answer'],
      options: List<String>.from(json['options']),
    );
  }
}

class QuizAnswer {
  final int questionId;
  final String answer;

  QuizAnswer({required this.questionId, required this.answer});

  Map<String, dynamic> toJson() => {
    'questionId': questionId,
    'answer': answer,
  };
}

class QuizResult {
  final int totalScore;
  final int totalQuestions;
  final double percentage;
  final List<SkillScore> skillBreakdown;
  final List<CareerMatch> careerMatches;

  QuizResult({
    required this.totalScore,
    required this.totalQuestions,
    required this.percentage,
    required this.skillBreakdown,
    required this.careerMatches,
  });

  factory QuizResult.fromJson(Map<String, dynamic> json) {
    return QuizResult(
      totalScore: json['totalScore'],
      totalQuestions: json['totalQuestions'],
      percentage: (json['percentage'] as num).toDouble(),
      skillBreakdown: (json['skillBreakdown'] as List)
          .map((s) => SkillScore.fromJson(s))
          .toList(),
      careerMatches: (json['careerMatches'] as List)
          .map((c) => CareerMatch.fromJson(c))
          .toList(),
    );
  }
}

class SkillScore {
  final String skill;
  final int correct;
  final int total;
  final double percentage;

  SkillScore({
    required this.skill,
    required this.correct,
    required this.total,
    required this.percentage,
  });

  factory SkillScore.fromJson(Map<String, dynamic> json) {
    return SkillScore(
      skill: json['skill'],
      correct: json['correct'],
      total: json['total'],
      percentage: (json['percentage'] as num).toDouble(),
    );
  }
}

class CareerMatch {
  final int careerId;
  final String careerName;
  final double matchPercentage;
  final List<String> matchingSkills;
  final List<String> missingSkills;
  final String? salaryRange;

  CareerMatch({
    required this.careerId,
    required this.careerName,
    required this.matchPercentage,
    required this.matchingSkills,
    required this.missingSkills,
    this.salaryRange,
  });

  factory CareerMatch.fromJson(Map<String, dynamic> json) {
    return CareerMatch(
      careerId: json['careerId'],
      careerName: json['careerName'],
      matchPercentage: (json['matchPercentage'] as num).toDouble(),
      matchingSkills: List<String>.from(json['matchingSkills']),
      missingSkills: List<String>.from(json['missingSkills']),
      salaryRange: json['salaryRange'],
    );
  }
}
```

---

## 🎯 CHECKLIST - DO THIS IN ORDER:

1. ✅ Get your PC's IP address (`ipconfig` on Windows)
2. ✅ Update `baseUrl` in Flutter to use your IP
3. ✅ Add `android:usesCleartextTraffic="true"` to AndroidManifest.xml
4. ✅ Ensure phone/emulator on same WiFi as PC
5. ✅ Add debug print statements to see errors
6. ✅ Verify user has skills in their profile
7. ✅ Test with Postman first if still failing

---

## 🔥 ACTUAL BACKEND RESPONSE (VERIFIED)

Your backend is returning this EXACT format:

```json
{
  "quizId": "024d9f26-724e-4cbc-ac6d-fd01cf8bdca6",
  "questions": [
    {
      "id": 1,
      "question": "In Java, how do you declare a method that returns an array of integers?",
      "type": "multiple_choice",
      "skill_category": "Java",
      "correct_answer": "A",
      "options": [
        "A) public int[] getArray()",
        "B) public void getArray(int[] array)",
        "C) public int getArray(int[] array)",
        "D) public int getArray()"
      ]
    }
    // ... 9 more questions
  ]
}
```

Match your Flutter models to this EXACTLY.

---

## 🆘 STILL NOT WORKING?

Share these in your error report:
1. Console output from the debug logging above
2. Your `baseUrl` value
3. Testing on: Real device / Emulator?
4. Same WiFi? (Yes/No)
5. AndroidManifest.xml has `usesCleartextTraffic="true"`? (Yes/No)

**Backend is 100% working. It's a Flutter network/parsing issue.** 🎯
