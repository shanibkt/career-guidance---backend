# 🎯 Complete Flutter Integration Guide for Career Quiz API

## 📱 For Your Flutter Developer

Your .NET backend is **LIVE** at `http://192.168.1.100:5001` with 4 working endpoints for the AI-powered career quiz feature.

---

## 🚀 Quick Start Checklist

- [ ] Update API base URL to `http://192.168.1.100:5001`
- [ ] Ensure you're sending JWT token in Authorization header
- [ ] Handle loading states (AI takes 5-15 seconds)
- [ ] Test all 4 endpoints in sequence
- [ ] Display error messages from API

---

## 🔧 API Configuration

### Update Your API Service

```dart
class ApiConfig {
  // CHANGE THIS - Use your computer's IP address
  static const String baseUrl = 'http://192.168.1.100:5001';
  
  // Endpoints
  static const String login = '/api/auth/login';
  static const String signup = '/api/auth/signup';
  static const String profile = '/api/profile';
  static const String quizGenerate = '/api/quiz/generate';
  static const String quizSubmit = '/api/quiz/submit';
  static const String recommendationsGenerate = '/api/recommendations/generate';
  static const String recommendationsGet = '/api/recommendations';
}
```

---

## 📦 Required Models (Copy These Exactly)

### 1. Quiz Models

```dart
class QuizResponse {
  final int sessionId;
  final List<QuizQuestion> questions;

  QuizResponse({
    required this.sessionId,
    required this.questions,
  });

  factory QuizResponse.fromJson(Map<String, dynamic> json) {
    return QuizResponse(
      sessionId: json['sessionId'] as int,
      questions: (json['questions'] as List)
          .map((q) => QuizQuestion.fromJson(q))
          .toList(),
    );
  }
}

class QuizQuestion {
  final int id;
  final String question;
  final String type; // "multiple_choice" or "open_ended"
  final List<String>? options;

  QuizQuestion({
    required this.id,
    required this.question,
    required this.type,
    this.options,
  });

  factory QuizQuestion.fromJson(Map<String, dynamic> json) {
    return QuizQuestion(
      id: json['id'] as int,
      question: json['question'] as String,
      type: json['type'] as String,
      options: json['options'] != null
          ? List<String>.from(json['options'])
          : null,
    );
  }

  bool get isMultipleChoice => type == 'multiple_choice';
  bool get isOpenEnded => type == 'open_ended';
}

class QuizAnswer {
  final int questionId;
  final String answer;

  QuizAnswer({
    required this.questionId,
    required this.answer,
  });

  Map<String, dynamic> toJson() {
    return {
      'questionId': questionId,
      'answer': answer,
    };
  }
}
```

### 2. Recommendation Models

```dart
class RecommendationsResponse {
  final List<CareerRecommendation> recommendations;

  RecommendationsResponse({required this.recommendations});

  factory RecommendationsResponse.fromJson(Map<String, dynamic> json) {
    return RecommendationsResponse(
      recommendations: (json['recommendations'] as List)
          .map((r) => CareerRecommendation.fromJson(r))
          .toList(),
    );
  }
}

class CareerRecommendation {
  final int? id;
  final int careerId;
  final String careerName;
  final double matchPercentage;
  final String? reasoning;
  final List<String>? strengths;
  final List<String>? areasToDevelop;
  final DateTime? createdAt;

  CareerRecommendation({
    this.id,
    required this.careerId,
    required this.careerName,
    required this.matchPercentage,
    this.reasoning,
    this.strengths,
    this.areasToDevelop,
    this.createdAt,
  });

  factory CareerRecommendation.fromJson(Map<String, dynamic> json) {
    return CareerRecommendation(
      id: json['id'] as int?,
      careerId: json['careerId'] as int,
      careerName: json['careerName'] as String,
      matchPercentage: (json['matchPercentage'] as num).toDouble(),
      reasoning: json['reasoning'] as String?,
      strengths: json['strengths'] != null
          ? List<String>.from(json['strengths'])
          : null,
      areasToDevelop: json['areasToDevelop'] != null
          ? List<String>.from(json['areasToDevelop'])
          : null,
      createdAt: json['createdAt'] != null
          ? DateTime.parse(json['createdAt'])
          : null,
    );
  }
}
```

---

## 🔌 API Service Layer (Complete Implementation)

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class CareerQuizService {
  final String baseUrl = ApiConfig.baseUrl;
  String? _token;

  void setToken(String token) {
    _token = token;
  }

  Map<String, String> get _headers {
    final headers = {
      'Content-Type': 'application/json',
    };
    if (_token != null) {
      headers['Authorization'] = 'Bearer $_token';
    }
    return headers;
  }

  // 1. Generate Quiz Questions
  Future<QuizResponse> generateQuiz() async {
    try {
      final response = await http.post(
        Uri.parse('$baseUrl${ApiConfig.quizGenerate}'),
        headers: _headers,
      ).timeout(const Duration(seconds: 30));

      print('Generate Quiz Status: ${response.statusCode}');
      print('Generate Quiz Response: ${response.body}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return QuizResponse.fromJson(data);
      } else if (response.statusCode == 401) {
        throw Exception('Unauthorized - Please login again');
      } else {
        final error = jsonDecode(response.body);
        throw Exception(error['error'] ?? error['message'] ?? 'Failed to generate quiz');
      }
    } catch (e) {
      print('Generate Quiz Error: $e');
      if (e.toString().contains('SocketException') || 
          e.toString().contains('TimeoutException')) {
        throw Exception('Cannot connect to server. Check your network and API URL.');
      }
      rethrow;
    }
  }

  // 2. Submit Quiz Answers
  Future<Map<String, dynamic>> submitQuiz({
    required int sessionId,
    required List<QuizAnswer> answers,
  }) async {
    try {
      final body = jsonEncode({
        'sessionId': sessionId,
        'answers': answers.map((a) => a.toJson()).toList(),
      });

      print('Submitting quiz with body: $body');

      final response = await http.post(
        Uri.parse('$baseUrl${ApiConfig.quizSubmit}'),
        headers: _headers,
        body: body,
      ).timeout(const Duration(seconds: 15));

      print('Submit Quiz Status: ${response.statusCode}');
      print('Submit Quiz Response: ${response.body}');

      if (response.statusCode == 200) {
        return jsonDecode(response.body);
      } else if (response.statusCode == 401) {
        throw Exception('Unauthorized - Please login again');
      } else {
        final error = jsonDecode(response.body);
        throw Exception(error['error'] ?? error['message'] ?? 'Failed to submit quiz');
      }
    } catch (e) {
      print('Submit Quiz Error: $e');
      rethrow;
    }
  }

  // 3. Generate Career Recommendations (AI Processing)
  Future<RecommendationsResponse> generateRecommendations({
    required int sessionId,
  }) async {
    try {
      final body = jsonEncode({'sessionId': sessionId});

      print('Generating recommendations for session: $sessionId');

      final response = await http.post(
        Uri.parse('$baseUrl${ApiConfig.recommendationsGenerate}'),
        headers: _headers,
        body: body,
      ).timeout(const Duration(seconds: 60)); // AI takes longer

      print('Generate Recommendations Status: ${response.statusCode}');
      print('Generate Recommendations Response: ${response.body}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return RecommendationsResponse.fromJson(data);
      } else if (response.statusCode == 401) {
        throw Exception('Unauthorized - Please login again');
      } else {
        final error = jsonDecode(response.body);
        throw Exception(error['error'] ?? error['message'] ?? 'Failed to generate recommendations');
      }
    } catch (e) {
      print('Generate Recommendations Error: $e');
      rethrow;
    }
  }

  // 4. Get Saved Recommendations
  Future<RecommendationsResponse> getRecommendations() async {
    try {
      final response = await http.get(
        Uri.parse('$baseUrl${ApiConfig.recommendationsGet}'),
        headers: _headers,
      ).timeout(const Duration(seconds: 15));

      print('Get Recommendations Status: ${response.statusCode}');
      print('Get Recommendations Response: ${response.body}');

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        return RecommendationsResponse.fromJson(data);
      } else if (response.statusCode == 401) {
        throw Exception('Unauthorized - Please login again');
      } else {
        final error = jsonDecode(response.body);
        throw Exception(error['error'] ?? error['message'] ?? 'Failed to get recommendations');
      }
    } catch (e) {
      print('Get Recommendations Error: $e');
      rethrow;
    }
  }
}
```

---

## 🎨 Example UI Implementation

### Quiz Screen (Complete Example)

```dart
import 'package:flutter/material.dart';

class QuizScreen extends StatefulWidget {
  const QuizScreen({Key? key}) : super(key: key);

  @override
  State<QuizScreen> createState() => _QuizScreenState();
}

class _QuizScreenState extends State<QuizScreen> {
  final CareerQuizService _quizService = CareerQuizService();
  
  bool _isLoading = false;
  bool _isSubmitting = false;
  QuizResponse? _quizData;
  Map<int, String> _answers = {}; // questionId -> answer
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadQuiz();
  }

  Future<void> _loadQuiz() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      // Make sure token is set (get from your auth storage)
      final token = await getStoredToken(); // Your method
      _quizService.setToken(token);

      final quiz = await _quizService.generateQuiz();
      
      setState(() {
        _quizData = quiz;
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString().replaceAll('Exception: ', '');
        _isLoading = false;
      });
      
      _showErrorDialog(_error!);
    }
  }

  Future<void> _submitQuiz() async {
    // Validate all questions answered
    if (_answers.length != _quizData!.questions.length) {
      _showErrorDialog('Please answer all questions');
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      // Convert answers to QuizAnswer list
      final answersList = _answers.entries
          .map((e) => QuizAnswer(questionId: e.key, answer: e.value))
          .toList();

      // Submit quiz
      await _quizService.submitQuiz(
        sessionId: _quizData!.sessionId,
        answers: answersList,
      );

      // Generate recommendations (AI processing)
      final recommendations = await _quizService.generateRecommendations(
        sessionId: _quizData!.sessionId,
      );

      setState(() => _isSubmitting = false);

      // Navigate to results
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (context) => RecommendationsScreen(
            recommendations: recommendations,
          ),
        ),
      );
    } catch (e) {
      setState(() => _isSubmitting = false);
      _showErrorDialog(e.toString().replaceAll('Exception: ', ''));
    }
  }

  void _showErrorDialog(String message) {
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Error'),
        content: Text(message),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('OK'),
          ),
          if (message.contains('network') || message.contains('connect'))
            TextButton(
              onPressed: () {
                Navigator.pop(context);
                _loadQuiz();
              },
              child: const Text('Retry'),
            ),
        ],
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_isLoading) {
      return Scaffold(
        appBar: AppBar(title: const Text('Career Assessment')),
        body: const Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              CircularProgressIndicator(),
              SizedBox(height: 16),
              Text('AI is generating personalized questions...'),
              SizedBox(height: 8),
              Text(
                'This may take 5-10 seconds',
                style: TextStyle(color: Colors.grey),
              ),
            ],
          ),
        ),
      );
    }

    if (_error != null && _quizData == null) {
      return Scaffold(
        appBar: AppBar(title: const Text('Career Assessment')),
        body: Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.error_outline, size: 64, color: Colors.red),
              const SizedBox(height: 16),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 24),
              ElevatedButton.icon(
                onPressed: _loadQuiz,
                icon: const Icon(Icons.refresh),
                label: const Text('Retry'),
              ),
            ],
          ),
        ),
      );
    }

    return Scaffold(
      appBar: AppBar(
        title: const Text('Career Assessment Quiz'),
        actions: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Center(
              child: Text(
                '${_answers.length}/${_quizData!.questions.length}',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
            ),
          ),
        ],
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: _quizData!.questions.length,
        itemBuilder: (context, index) {
          final question = _quizData!.questions[index];
          return _buildQuestionCard(question, index);
        },
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(16.0),
          child: ElevatedButton(
            onPressed: _isSubmitting ? null : _submitQuiz,
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
              backgroundColor: Theme.of(context).primaryColor,
            ),
            child: _isSubmitting
                ? const Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(color: Colors.white),
                      ),
                      SizedBox(width: 12),
                      Text('Analyzing your responses...'),
                    ],
                  )
                : const Text(
                    'Submit & Get Career Recommendations',
                    style: TextStyle(fontSize: 16, color: Colors.white),
                  ),
          ),
        ),
      ),
    );
  }

  Widget _buildQuestionCard(QuizQuestion question, int index) {
    final isAnswered = _answers.containsKey(question.id);

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      elevation: isAnswered ? 2 : 1,
      color: isAnswered ? Colors.green.shade50 : null,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  radius: 16,
                  backgroundColor: isAnswered 
                      ? Colors.green 
                      : Colors.grey.shade300,
                  child: Text(
                    '${index + 1}',
                    style: TextStyle(
                      color: isAnswered ? Colors.white : Colors.black,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    question.question,
                    style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            if (question.isMultipleChoice)
              ...question.options!.map((option) => RadioListTile<String>(
                    title: Text(option),
                    value: option,
                    groupValue: _answers[question.id],
                    onChanged: (value) {
                      setState(() {
                        _answers[question.id] = value!;
                      });
                    },
                  ))
            else
              TextField(
                decoration: InputDecoration(
                  hintText: 'Type your answer here...',
                  border: const OutlineInputBorder(),
                  filled: true,
                  fillColor: Colors.white,
                ),
                maxLines: 3,
                onChanged: (value) {
                  setState(() {
                    _answers[question.id] = value;
                  });
                },
              ),
          ],
        ),
      ),
    );
  }
}
```

### Recommendations Screen

```dart
class RecommendationsScreen extends StatelessWidget {
  final RecommendationsResponse recommendations;

  const RecommendationsScreen({
    Key? key,
    required this.recommendations,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    // Sort by match percentage (highest first)
    final sorted = List<CareerRecommendation>.from(recommendations.recommendations)
      ..sort((a, b) => b.matchPercentage.compareTo(a.matchPercentage));

    return Scaffold(
      appBar: AppBar(
        title: const Text('Your Career Matches'),
      ),
      body: ListView.builder(
        padding: const EdgeInsets.all(16),
        itemCount: sorted.length,
        itemBuilder: (context, index) {
          final career = sorted[index];
          return _buildCareerCard(context, career, index);
        },
      ),
    );
  }

  Widget _buildCareerCard(BuildContext context, CareerRecommendation career, int index) {
    final matchColor = career.matchPercentage >= 80
        ? Colors.green
        : career.matchPercentage >= 60
            ? Colors.orange
            : Colors.grey;

    return Card(
      margin: const EdgeInsets.only(bottom: 16),
      child: ExpansionTile(
        leading: CircleAvatar(
          backgroundColor: matchColor,
          child: Text(
            '#${index + 1}',
            style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
          ),
        ),
        title: Text(
          career.careerName,
          style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const SizedBox(height: 8),
            Row(
              children: [
                Text(
                  '${career.matchPercentage.toStringAsFixed(1)}% Match',
                  style: TextStyle(
                    color: matchColor,
                    fontWeight: FontWeight.bold,
                    fontSize: 16,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            LinearProgressIndicator(
              value: career.matchPercentage / 100,
              backgroundColor: Colors.grey.shade200,
              color: matchColor,
            ),
          ],
        ),
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (career.reasoning != null) ...[
                  const Text(
                    'Why this career fits you:',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  Text(career.reasoning!),
                  const SizedBox(height: 16),
                ],
                if (career.strengths != null && career.strengths!.isNotEmpty) ...[
                  const Text(
                    '✅ Your Strengths:',
                    style: TextStyle(fontWeight: FontWeight.bold, color: Colors.green),
                  ),
                  const SizedBox(height: 8),
                  ...career.strengths!.map((s) => Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Row(
                          children: [
                            const Icon(Icons.check_circle, size: 16, color: Colors.green),
                            const SizedBox(width: 8),
                            Expanded(child: Text(s)),
                          ],
                        ),
                      )),
                  const SizedBox(height: 16),
                ],
                if (career.areasToDevelop != null && career.areasToDevelop!.isNotEmpty) ...[
                  const Text(
                    '📈 Areas to Develop:',
                    style: TextStyle(fontWeight: FontWeight.bold, color: Colors.orange),
                  ),
                  const SizedBox(height: 8),
                  ...career.areasToDevelop!.map((a) => Padding(
                        padding: const EdgeInsets.only(bottom: 4),
                        child: Row(
                          children: [
                            const Icon(Icons.trending_up, size: 16, color: Colors.orange),
                            const SizedBox(width: 8),
                            Expanded(child: Text(a)),
                          ],
                        ),
                      )),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
```

---

## ⚠️ Common Issues & Solutions

### Issue 1: "Failed to load quiz" / Connection Error

**Possible Causes:**
1. Wrong API URL
2. Backend not running
3. Network connectivity

**Fix:**
```dart
// 1. Verify API URL is correct
print('API URL: ${ApiConfig.baseUrl}');

// 2. Test connection
try {
  final response = await http.get(Uri.parse('${ApiConfig.baseUrl}/api/auth/test'));
  print('API reachable: ${response.statusCode}');
} catch (e) {
  print('API NOT reachable: $e');
}

// 3. Check your computer's IP
// Windows: ipconfig
// The API should show: "Now listening on: http://0.0.0.0:5001"
```

### Issue 2: "Unauthorized" (401)

**Fix:**
```dart
// Make sure JWT token is set before API calls
final token = await storage.read(key: 'jwt_token');
if (token == null) {
  // Redirect to login
  Navigator.pushReplacementNamed(context, '/login');
  return;
}
_quizService.setToken(token);
```

### Issue 3: Timeout on Quiz Generation

**Fix:**
```dart
// AI processing takes time - increase timeout
final response = await http.post(
  Uri.parse('$baseUrl/api/quiz/generate'),
  headers: _headers,
).timeout(const Duration(seconds: 30)); // Increase from default 10s
```

### Issue 4: JSON Parsing Errors

**Debug:**
```dart
try {
  final data = jsonDecode(response.body);
  print('Parsed JSON: $data');
  return QuizResponse.fromJson(data);
} catch (e) {
  print('Raw response: ${response.body}');
  print('Parse error: $e');
  throw Exception('Invalid response format');
}
```

---

## 🧪 Testing Steps

### 1. Test Login First
```dart
// Make sure you can login and get a token
final loginResponse = await http.post(
  Uri.parse('http://192.168.1.100:5001/api/auth/login'),
  headers: {'Content-Type': 'application/json'},
  body: jsonEncode({
    'email': 'test@example.com',
    'password': 'Test123!'
  }),
);
print('Login Status: ${loginResponse.statusCode}');
print('Token: ${jsonDecode(loginResponse.body)['token']}');
```

### 2. Test Quiz Generation
```dart
final token = 'your_jwt_token_here';
final quizResponse = await http.post(
  Uri.parse('http://192.168.1.100:5001/api/quiz/generate'),
  headers: {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $token',
  },
);
print('Quiz Status: ${quizResponse.statusCode}');
print('Quiz Data: ${quizResponse.body}');
```

### 3. Complete Flow Test
```dart
void testCompleteFlow() async {
  try {
    // 1. Generate quiz
    final quiz = await _quizService.generateQuiz();
    print('✅ Quiz generated: ${quiz.questions.length} questions');
    
    // 2. Create dummy answers
    final answers = quiz.questions.map((q) => QuizAnswer(
      questionId: q.id,
      answer: q.isMultipleChoice ? q.options!.first : 'Test answer',
    )).toList();
    
    // 3. Submit quiz
    await _quizService.submitQuiz(sessionId: quiz.sessionId, answers: answers);
    print('✅ Quiz submitted');
    
    // 4. Generate recommendations
    final recs = await _quizService.generateRecommendations(sessionId: quiz.sessionId);
    print('✅ Got ${recs.recommendations.length} recommendations');
    
    // 5. Verify saved recommendations
    final saved = await _quizService.getRecommendations();
    print('✅ Saved recommendations: ${saved.recommendations.length}');
    
  } catch (e) {
    print('❌ Test failed: $e');
  }
}
```

---

## 📊 Expected Response Formats

### Quiz Generate Response
```json
{
  "sessionId": 1,
  "questions": [
    {
      "id": 1,
      "question": "Which activity energizes you the most?",
      "type": "multiple_choice",
      "options": [
        "Solving technical problems",
        "Creating visual designs",
        "Helping others succeed",
        "Analyzing data patterns"
      ]
    },
    {
      "id": 2,
      "question": "Describe a challenging project you completed and what you learned from it",
      "type": "open_ended"
    }
  ]
}
```

### Recommendations Response
```json
{
  "recommendations": [
    {
      "id": 1,
      "careerId": 1,
      "careerName": "Software Engineer",
      "matchPercentage": 92.5,
      "reasoning": "Your strong problem-solving skills and technical background align perfectly with software engineering...",
      "strengths": [
        "Analytical thinking",
        "Technical aptitude",
        "Problem-solving"
      ],
      "areasToDevelop": [
        "Team collaboration",
        "Public speaking"
      ],
      "createdAt": "2025-11-23T10:30:00"
    }
  ]
}
```

---

## 🎯 Deployment Checklist

Before production:

- [ ] Update API URL to production domain
- [ ] Enable HTTPS (currently HTTP only)
- [ ] Store JWT token securely (flutter_secure_storage)
- [ ] Implement token refresh logic
- [ ] Add proper error handling for all network calls
- [ ] Test on both Android and iOS
- [ ] Test with slow network (simulate 3G)
- [ ] Add analytics for quiz completion rate
- [ ] Implement retry logic for failed AI calls
- [ ] Cache recommendations locally

---

## 🆘 Need Help?

### Backend Logs
Check the console where `dotnet run` is running. You'll see:
- "Calling Groq API to generate quiz questions..."
- "Calling Groq API to generate recommendations..."
- Error messages with details

### Flutter Debug
Add extensive logging:
```dart
class DebugInterceptor {
  static void log(String endpoint, int status, String response) {
    print('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
    print('🌐 $endpoint');
    print('📊 Status: $status');
    print('📦 Response: $response');
    print('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  }
}
```

### Contact Backend Developer
If you see errors, send:
1. API endpoint you're calling
2. Request headers
3. Request body
4. Response status code
5. Response body
6. Flutter error message

---

## ✅ Summary

**All 4 endpoints are ready:**
1. ✅ POST /api/quiz/generate - Generates personalized quiz
2. ✅ POST /api/quiz/submit - Saves answers
3. ✅ POST /api/recommendations/generate - AI career matching
4. ✅ GET /api/recommendations - Retrieves saved results

**Backend is running at:** `http://192.168.1.100:5001`

**Just copy the models and service code above - it will work!** 🚀
