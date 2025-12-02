# Flutter Quiz Integration Guide - Skill-Based Quiz System

## API Base URL
```dart
const String baseUrl = "http://YOUR_SERVER_IP:5001/api";
```

## Authentication
All quiz endpoints require JWT token in Authorization header:
```dart
final headers = {
  'Content-Type': 'application/json',
  'Authorization': 'Bearer $jwtToken',
};
```

---

## 1. Generate Quiz Endpoint

### Request
```dart
POST /api/quiz/generate
Headers: Authorization: Bearer {token}
```

### Flutter Implementation
```dart
Future<QuizResponse> generateQuiz() async {
  final response = await http.post(
    Uri.parse('$baseUrl/quiz/generate'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $jwtToken',
    },
  );

  if (response.statusCode == 200) {
    return QuizResponse.fromJson(jsonDecode(response.body));
  } else if (response.statusCode == 400) {
    // No skills in profile
    throw Exception('Please add skills to your profile first');
  } else {
    throw Exception('Failed to generate quiz');
  }
}
```

### Response Model
```dart
class QuizResponse {
  final String quizId;
  final List<QuizQuestion> questions;

  QuizResponse({
    required this.quizId,
    required this.questions,
  });

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
  final String type; // "multiple_choice"
  final String skillCategory;
  final String correctAnswer; // "A", "B", "C", or "D"
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
```

### Response Example
```json
{
  "quizId": "550e8400-e29b-41d4-a716-446655440000",
  "questions": [
    {
      "id": 1,
      "question": "In Flutter, which widget is used for creating scrollable lists?",
      "type": "multiple_choice",
      "skill_category": "Flutter",
      "correct_answer": "B",
      "options": [
        "A) Container",
        "B) ListView",
        "C) Column",
        "D) Stack"
      ]
    },
    {
      "id": 2,
      "question": "What is the purpose of setState() in Flutter?",
      "type": "multiple_choice",
      "skill_category": "Flutter",
      "correct_answer": "A",
      "options": [
        "A) Rebuild the widget with updated state",
        "B) Initialize widget state",
        "C) Dispose widget resources",
        "D) Create new widget instance"
      ]
    }
    // ... 8 more questions
  ]
}
```

---

## 2. Submit Quiz Endpoint

### Request
```dart
POST /api/quiz/submit
Headers: Authorization: Bearer {token}
Content-Type: application/json

Body:
{
  "quizId": "550e8400-e29b-41d4-a716-446655440000",
  "answers": [
    { "questionId": 1, "answer": "B" },
    { "questionId": 2, "answer": "A" },
    { "questionId": 3, "answer": "C" }
  ]
}
```

### Flutter Implementation
```dart
class QuizAnswer {
  final int questionId;
  final String answer;

  QuizAnswer({required this.questionId, required this.answer});

  Map<String, dynamic> toJson() => {
    'questionId': questionId,
    'answer': answer,
  };
}

class SubmitQuizRequest {
  final String quizId;
  final List<QuizAnswer> answers;

  SubmitQuizRequest({
    required this.quizId,
    required this.answers,
  });

  Map<String, dynamic> toJson() => {
    'quizId': quizId,
    'answers': answers.map((a) => a.toJson()).toList(),
  };
}

Future<QuizResult> submitQuiz(SubmitQuizRequest request) async {
  final response = await http.post(
    Uri.parse('$baseUrl/quiz/submit'),
    headers: {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $jwtToken',
    },
    body: jsonEncode(request.toJson()),
  );

  if (response.statusCode == 200) {
    return QuizResult.fromJson(jsonDecode(response.body));
  } else {
    throw Exception('Failed to submit quiz');
  }
}
```

### Response Model
```dart
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
      percentage: json['percentage'].toDouble(),
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
      percentage: json['percentage'].toDouble(),
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
      matchPercentage: json['matchPercentage'].toDouble(),
      matchingSkills: List<String>.from(json['matchingSkills']),
      missingSkills: List<String>.from(json['missingSkills']),
      salaryRange: json['salaryRange'],
    );
  }
}
```

### Response Example
```json
{
  "totalScore": 7,
  "totalQuestions": 10,
  "percentage": 70.00,
  "skillBreakdown": [
    {
      "skill": "Flutter",
      "correct": 4,
      "total": 5,
      "percentage": 80.00
    },
    {
      "skill": "Java",
      "correct": 3,
      "total": 5,
      "percentage": 60.00
    }
  ],
  "careerMatches": [
    {
      "careerId": 11,
      "careerName": "Flutter Developer",
      "matchPercentage": 95.50,
      "matchingSkills": ["Flutter", "Dart", "Mobile Development"],
      "missingSkills": ["UI/UX"],
      "salaryRange": "$60,000 - $120,000"
    },
    {
      "careerId": 13,
      "careerName": "Mobile App Developer",
      "matchPercentage": 78.25,
      "matchingSkills": ["Flutter", "Dart"],
      "missingSkills": ["Java", "Kotlin"],
      "salaryRange": "$65,000 - $125,000"
    }
  ]
}
```

---

## 3. Complete Quiz Flow Implementation

### State Management (using Provider/Riverpod)
```dart
class QuizState extends ChangeNotifier {
  String? quizId;
  List<QuizQuestion> questions = [];
  Map<int, String> userAnswers = {}; // questionId -> answer
  QuizResult? result;
  bool isLoading = false;
  String? error;

  Future<void> generateQuiz() async {
    isLoading = true;
    error = null;
    notifyListeners();

    try {
      final response = await QuizApi.generateQuiz();
      quizId = response.quizId;
      questions = response.questions;
      userAnswers.clear();
      result = null;
    } catch (e) {
      error = e.toString();
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  void setAnswer(int questionId, String answer) {
    userAnswers[questionId] = answer;
    notifyListeners();
  }

  Future<void> submitQuiz() async {
    if (quizId == null) return;

    isLoading = true;
    error = null;
    notifyListeners();

    try {
      final answers = userAnswers.entries
          .map((e) => QuizAnswer(questionId: e.key, answer: e.value))
          .toList();

      final request = SubmitQuizRequest(
        quizId: quizId!,
        answers: answers,
      );

      result = await QuizApi.submitQuiz(request);
    } catch (e) {
      error = e.toString();
    } finally {
      isLoading = false;
      notifyListeners();
    }
  }

  bool get isQuizComplete => userAnswers.length == questions.length;
  double get progress => questions.isEmpty ? 0 : userAnswers.length / questions.length;
}
```

### UI Implementation

#### Quiz Screen
```dart
class QuizScreen extends StatefulWidget {
  @override
  _QuizScreenState createState() => _QuizScreenState();
}

class _QuizScreenState extends State<QuizScreen> {
  int currentQuestionIndex = 0;
  final PageController _pageController = PageController();

  @override
  Widget build(BuildContext context) {
    return Consumer<QuizState>(
      builder: (context, quizState, child) {
        if (quizState.isLoading) {
          return Center(child: CircularProgressIndicator());
        }

        if (quizState.questions.isEmpty) {
          return Center(
            child: ElevatedButton(
              onPressed: () => quizState.generateQuiz(),
              child: Text('Start Quiz'),
            ),
          );
        }

        return Scaffold(
          appBar: AppBar(
            title: Text('Career Quiz'),
            actions: [
              Center(
                child: Padding(
                  padding: EdgeInsets.symmetric(horizontal: 16),
                  child: Text(
                    '${currentQuestionIndex + 1}/${quizState.questions.length}',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ],
          ),
          body: Column(
            children: [
              LinearProgressIndicator(value: quizState.progress),
              Expanded(
                child: PageView.builder(
                  controller: _pageController,
                  physics: NeverScrollableScrollPhysics(),
                  itemCount: quizState.questions.length,
                  onPageChanged: (index) {
                    setState(() => currentQuestionIndex = index);
                  },
                  itemBuilder: (context, index) {
                    return QuestionCard(
                      question: quizState.questions[index],
                      selectedAnswer: quizState.userAnswers[quizState.questions[index].id],
                      onAnswerSelected: (answer) {
                        quizState.setAnswer(quizState.questions[index].id, answer);
                      },
                    );
                  },
                ),
              ),
              Padding(
                padding: EdgeInsets.all(16),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    if (currentQuestionIndex > 0)
                      ElevatedButton(
                        onPressed: () {
                          _pageController.previousPage(
                            duration: Duration(milliseconds: 300),
                            curve: Curves.easeInOut,
                          );
                        },
                        child: Text('Previous'),
                      )
                    else
                      SizedBox.shrink(),
                    
                    ElevatedButton(
                      onPressed: quizState.userAnswers[quizState.questions[currentQuestionIndex].id] == null
                          ? null
                          : () {
                              if (currentQuestionIndex < quizState.questions.length - 1) {
                                _pageController.nextPage(
                                  duration: Duration(milliseconds: 300),
                                  curve: Curves.easeInOut,
                                );
                              } else if (quizState.isQuizComplete) {
                                _submitQuiz(context, quizState);
                              }
                            },
                      child: Text(
                        currentQuestionIndex < quizState.questions.length - 1
                            ? 'Next'
                            : 'Submit Quiz',
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),
        );
      },
    );
  }

  Future<void> _submitQuiz(BuildContext context, QuizState quizState) async {
    await quizState.submitQuiz();
    if (quizState.result != null) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => ResultsScreen(result: quizState.result!),
        ),
      );
    }
  }
}
```

#### Question Card Widget
```dart
class QuestionCard extends StatelessWidget {
  final QuizQuestion question;
  final String? selectedAnswer;
  final Function(String) onAnswerSelected;

  const QuestionCard({
    required this.question,
    required this.selectedAnswer,
    required this.onAnswerSelected,
  });

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: Colors.blue.shade50,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(
              children: [
                Icon(Icons.code, color: Colors.blue),
                SizedBox(width: 8),
                Text(
                  question.skillCategory,
                  style: TextStyle(
                    fontSize: 14,
                    fontWeight: FontWeight.bold,
                    color: Colors.blue.shade700,
                  ),
                ),
              ],
            ),
          ),
          SizedBox(height: 24),
          Text(
            question.question,
            style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
          ),
          SizedBox(height: 24),
          ...question.options.map((option) {
            final letter = option.substring(0, 1); // Extract "A", "B", etc.
            final isSelected = selectedAnswer == letter;
            
            return GestureDetector(
              onTap: () => onAnswerSelected(letter),
              child: Container(
                margin: EdgeInsets.only(bottom: 12),
                padding: EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: isSelected ? Colors.blue.shade100 : Colors.white,
                  border: Border.all(
                    color: isSelected ? Colors.blue : Colors.grey.shade300,
                    width: 2,
                  ),
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(
                  children: [
                    Container(
                      width: 32,
                      height: 32,
                      decoration: BoxDecoration(
                        shape: BoxShape.circle,
                        color: isSelected ? Colors.blue : Colors.grey.shade200,
                      ),
                      child: Center(
                        child: Text(
                          letter,
                          style: TextStyle(
                            color: isSelected ? Colors.white : Colors.black87,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                    SizedBox(width: 16),
                    Expanded(
                      child: Text(
                        option.substring(3), // Remove "A) " prefix
                        style: TextStyle(fontSize: 16),
                      ),
                    ),
                  ],
                ),
              ),
            );
          }).toList(),
        ],
      ),
    );
  }
}
```

#### Results Screen
```dart
class ResultsScreen extends StatelessWidget {
  final QuizResult result;

  const ResultsScreen({required this.result});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Quiz Results')),
      body: SingleChildScrollView(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Overall Score Card
            Card(
              color: Colors.blue.shade50,
              child: Padding(
                padding: EdgeInsets.all(24),
                child: Column(
                  children: [
                    Text(
                      '${result.percentage.toStringAsFixed(1)}%',
                      style: TextStyle(
                        fontSize: 48,
                        fontWeight: FontWeight.bold,
                        color: Colors.blue.shade700,
                      ),
                    ),
                    SizedBox(height: 8),
                    Text(
                      '${result.totalScore} / ${result.totalQuestions} Correct',
                      style: TextStyle(fontSize: 18),
                    ),
                  ],
                ),
              ),
            ),
            SizedBox(height: 24),

            // Skill Breakdown
            Text(
              'Skill Breakdown',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 12),
            ...result.skillBreakdown.map((skill) => SkillScoreCard(skill: skill)),
            SizedBox(height: 24),

            // Career Matches
            Text(
              'Career Matches',
              style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
            ),
            SizedBox(height: 12),
            ...result.careerMatches.map((career) => CareerMatchCard(career: career)),
          ],
        ),
      ),
    );
  }
}

class SkillScoreCard extends StatelessWidget {
  final SkillScore skill;

  const SkillScoreCard({required this.skill});

  @override
  Widget build(BuildContext context) {
    final color = skill.percentage >= 70
        ? Colors.green
        : skill.percentage >= 50
            ? Colors.orange
            : Colors.red;

    return Card(
      margin: EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(
                  skill.skill,
                  style: TextStyle(fontSize: 16, fontWeight: FontWeight.w600),
                ),
                Text(
                  '${skill.correct}/${skill.total}',
                  style: TextStyle(color: Colors.grey.shade600),
                ),
              ],
            ),
            SizedBox(height: 8),
            LinearProgressIndicator(
              value: skill.percentage / 100,
              backgroundColor: Colors.grey.shade200,
              color: color,
            ),
            SizedBox(height: 4),
            Text(
              '${skill.percentage.toStringAsFixed(1)}%',
              style: TextStyle(
                fontSize: 14,
                color: color,
                fontWeight: FontWeight.bold,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class CareerMatchCard extends StatelessWidget {
  final CareerMatch career;

  const CareerMatchCard({required this.career});

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: EdgeInsets.only(bottom: 16),
      child: Padding(
        padding: EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                    career.careerName,
                    style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
                  ),
                ),
                Container(
                  padding: EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  decoration: BoxDecoration(
                    color: Colors.green.shade100,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    '${career.matchPercentage.toStringAsFixed(1)}%',
                    style: TextStyle(
                      color: Colors.green.shade700,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
              ],
            ),
            if (career.salaryRange != null) ...[
              SizedBox(height: 8),
              Row(
                children: [
                  Icon(Icons.attach_money, size: 16, color: Colors.grey),
                  SizedBox(width: 4),
                  Text(
                    career.salaryRange!,
                    style: TextStyle(color: Colors.grey.shade700),
                  ),
                ],
              ),
            ],
            SizedBox(height: 12),
            if (career.matchingSkills.isNotEmpty) ...[
              Text(
                'Matching Skills:',
                style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
              ),
              SizedBox(height: 6),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: career.matchingSkills
                    .map((skill) => Chip(
                          label: Text(skill, style: TextStyle(fontSize: 12)),
                          backgroundColor: Colors.green.shade100,
                          padding: EdgeInsets.zero,
                        ))
                    .toList(),
              ),
            ],
            if (career.missingSkills.isNotEmpty) ...[
              SizedBox(height: 12),
              Text(
                'Skills to Develop:',
                style: TextStyle(fontWeight: FontWeight.w600, fontSize: 14),
              ),
              SizedBox(height: 6),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: career.missingSkills
                    .map((skill) => Chip(
                          label: Text(skill, style: TextStyle(fontSize: 12)),
                          backgroundColor: Colors.orange.shade100,
                          padding: EdgeInsets.zero,
                        ))
                    .toList(),
              ),
            ],
          ],
        ),
      ),
    );
  }
}
```

---

## Error Handling

```dart
try {
  final result = await quizApi.submitQuiz(request);
  // Success
} catch (e) {
  if (e.toString().contains('400')) {
    // Bad request - invalid quiz_id or missing data
    showError('Invalid quiz data');
  } else if (e.toString().contains('401')) {
    // Unauthorized - token expired
    navigateToLogin();
  } else if (e.toString().contains('500')) {
    // Server error
    showError('Server error, please try again');
  } else {
    // Network error
    showError('Network error, check your connection');
  }
}
```

---

## Dependencies
```yaml
dependencies:
  flutter:
    sdk: flutter
  http: ^1.1.0
  provider: ^6.0.5  # or riverpod
  shared_preferences: ^2.2.0  # for token storage
```

---

## Complete API Service Class

```dart
class QuizApi {
  static const String baseUrl = "http://YOUR_IP:5001/api";
  
  static Future<String> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('jwt_token') ?? '';
  }

  static Map<String, String> _getHeaders(String token) => {
    'Content-Type': 'application/json',
    'Authorization': 'Bearer $token',
  };

  static Future<QuizResponse> generateQuiz() async {
    final token = await _getToken();
    final response = await http.post(
      Uri.parse('$baseUrl/quiz/generate'),
      headers: _getHeaders(token),
    );

    if (response.statusCode == 200) {
      return QuizResponse.fromJson(jsonDecode(response.body));
    } else {
      throw Exception(jsonDecode(response.body)['error'] ?? 'Failed to generate quiz');
    }
  }

  static Future<QuizResult> submitQuiz(SubmitQuizRequest request) async {
    final token = await _getToken();
    final response = await http.post(
      Uri.parse('$baseUrl/quiz/submit'),
      headers: _getHeaders(token),
      body: jsonEncode(request.toJson()),
    );

    if (response.statusCode == 200) {
      return QuizResult.fromJson(jsonDecode(response.body));
    } else {
      throw Exception(jsonDecode(response.body)['error'] ?? 'Failed to submit quiz');
    }
  }
}
```

---

## Testing Checklist

✅ User must have skills in profile before generating quiz  
✅ Quiz generates 10 multiple-choice questions  
✅ Each question shows skill category tag  
✅ User can navigate between questions  
✅ Progress indicator shows completion status  
✅ Submit button only enabled when all questions answered  
✅ Results show overall score and percentage  
✅ Skill breakdown displays with colored progress bars  
✅ Career matches sorted by match percentage  
✅ Shows matching vs missing skills for each career  
✅ Displays salary range for careers  

---

## Notes

- Questions are skill-specific (not random/generic)
- Correct answers are validated server-side
- Career matches use weighted scoring algorithm
- Only careers meeting minimum threshold are shown
- Same quiz_id can be used for review (future feature)
