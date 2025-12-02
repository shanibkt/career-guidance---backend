# AI Career Chatbot - Implementation Complete ✅

## **Overview**
Conversational AI chatbot for career guidance using Groq API (llama-3.1-70b-versatile model).

---

## **Setup Instructions**

### **1. Create Database Tables**

**Run this endpoint first to create the chat tables:**
```
GET http://localhost:5001/api/setup/create-chat-tables
```

**Response:**
```json
{
  "success": true,
  "message": "Chat tables (ChatSessions and ChatMessages) created successfully!",
  "tables": ["ChatSessions", "ChatMessages"]
}
```

**Verify tables created:**
```
GET http://localhost:5001/api/setup/verify-tables
```

---

## **API Endpoints**

### **1. Send Chat Message**

**POST /api/chat**

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json
```

**Request Body:**
```json
{
  "message": "What career paths are available in tech for someone with Flutter skills?",
  "sessionId": null
}
```

**Response:**
```json
{
  "response": "With Flutter skills, you have excellent career opportunities in mobile development! Here are some paths:\n\n1. **Mobile App Developer** - Build cross-platform apps for iOS/Android\n2. **UI/UX Developer** - Focus on creating beautiful user interfaces\n3. **Full-Stack Developer** - Combine Flutter with backend technologies\n4. **Freelance Developer** - Build apps for clients worldwide\n5. **Startup Founder** - Create your own mobile products\n\nWhich area interests you most? I can provide more specific guidance!",
  "sessionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479"
}
```

**Features:**
- Creates new session automatically if sessionId is null
- Returns sessionId for continuing conversation
- Maintains conversation context (last 6 messages)
- Uses user profile data for personalized responses
- 30-second timeout protection
- Saves all messages to database

---

### **2. Get Chat History**

**GET /api/chat/history?sessionId=YOUR_SESSION_ID**

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "messages": [
    {
      "id": 1,
      "sessionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "role": "user",
      "message": "What career paths are available in tech?",
      "timestamp": "2025-11-24T10:30:00"
    },
    {
      "id": 2,
      "sessionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "role": "assistant",
      "message": "With Flutter skills, you have excellent career opportunities...",
      "timestamp": "2025-11-24T10:30:05"
    }
  ]
}
```

**Get all user's messages (across all sessions):**
```
GET /api/chat/history
```

---

### **3. Get User's Chat Sessions**

**GET /api/chat/sessions**

**Headers:**
```
Authorization: Bearer YOUR_JWT_TOKEN
```

**Response:**
```json
{
  "sessions": [
    {
      "sessionId": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "createdAt": "2025-11-24T10:30:00"
    },
    {
      "sessionId": "a12bc34d-56ef-7890-gh12-ij3456kl7890",
      "createdAt": "2025-11-23T15:20:00"
    }
  ]
}
```

---

## **Database Schema**

### **ChatSessions Table**
```sql
Id (INT, PK, AUTO_INCREMENT)
UserId (INT, FK -> users.id)
SessionId (VARCHAR(36), UNIQUE)
CreatedAt (DATETIME)
```

### **ChatMessages Table**
```sql
Id (INT, PK, AUTO_INCREMENT)
SessionId (VARCHAR(36), FK -> ChatSessions.SessionId)
Role (VARCHAR(20)) -- 'user' or 'assistant'
Message (TEXT)
Timestamp (DATETIME)
```

---

## **AI Configuration**

**Model:** `llama-3.1-70b-versatile` (More creative and versatile than 8b-instant)
**Temperature:** `0.7` (Balanced creativity and consistency)
**Max Tokens:** `500` (Keeps responses concise ~150 words)
**Timeout:** `30 seconds`

**System Prompt Features:**
- Expert career guidance assistant
- Personalized advice based on user profile
- Actionable recommendations
- Concise responses (<150 words)
- Asks clarifying questions
- Supportive and professional tone

---

## **Testing Guide**

### **Step 1: Create Tables**
```bash
curl http://localhost:5001/api/setup/create-chat-tables
```

### **Step 2: Login and Get Token**
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password123"}'
```

### **Step 3: Start Chat Conversation**
```bash
curl -X POST http://localhost:5001/api/chat \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"I want to become a software engineer. What skills should I learn?"}'
```

### **Step 4: Continue Conversation (Use returned sessionId)**
```bash
curl -X POST http://localhost:5001/api/chat \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"message":"I already know Flutter, what else?","sessionId":"SESSION_ID_FROM_STEP_3"}'
```

### **Step 5: View History**
```bash
curl http://localhost:5001/api/chat/history?sessionId=YOUR_SESSION_ID \
  -H "Authorization: Bearer YOUR_TOKEN"
```

---

## **Flutter Integration Example**

```dart
// Chat Service
class ChatService {
  final String baseUrl = 'http://YOUR_IP:5001/api/chat';
  String? sessionId;
  
  Future<String> sendMessage(String message) async {
    final response = await http.post(
      Uri.parse(baseUrl),
      headers: {
        'Authorization': 'Bearer $token',
        'Content-Type': 'application/json',
      },
      body: jsonEncode({
        'message': message,
        'sessionId': sessionId,
      }),
    );
    
    if (response.statusCode == 200) {
      final data = jsonDecode(response.body);
      sessionId = data['sessionId']; // Save for next message
      return data['response'];
    }
    throw Exception('Failed to get response');
  }
}

// Usage in Widget
class ChatScreen extends StatefulWidget {
  @override
  _ChatScreenState createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  final ChatService _chatService = ChatService();
  final List<Message> _messages = [];
  final TextEditingController _controller = TextEditingController();
  bool _isLoading = false;
  
  void _sendMessage() async {
    if (_controller.text.isEmpty) return;
    
    final userMessage = _controller.text;
    setState(() {
      _messages.add(Message(text: userMessage, isUser: true));
      _isLoading = true;
    });
    _controller.clear();
    
    try {
      final aiResponse = await _chatService.sendMessage(userMessage);
      setState(() {
        _messages.add(Message(text: aiResponse, isUser: false));
        _isLoading = false;
      });
    } catch (e) {
      setState(() {
        _messages.add(Message(text: 'Error: $e', isUser: false));
        _isLoading = false;
      });
    }
  }
  
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Career Assistant')),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              itemCount: _messages.length,
              itemBuilder: (context, index) {
                final message = _messages[index];
                return ChatBubble(
                  message: message.text,
                  isUser: message.isUser,
                );
              },
            ),
          ),
          if (_isLoading) CircularProgressIndicator(),
          Padding(
            padding: EdgeInsets.all(8.0),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _controller,
                    decoration: InputDecoration(
                      hintText: 'Ask about your career...',
                    ),
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.send),
                  onPressed: _sendMessage,
                ),
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

## **Key Features**

✅ **Conversation Memory** - AI remembers last 6 messages for context
✅ **User Profile Context** - Uses education, skills, interests for personalization
✅ **Session Management** - Multiple conversation sessions per user
✅ **Chat History** - All messages saved to database
✅ **Timeout Protection** - 30-second timeout prevents hanging
✅ **Error Handling** - Graceful failure with user-friendly messages
✅ **JWT Authentication** - Secure user-specific conversations
✅ **Concise Responses** - Limited to ~150 words for better UX

---

## **Sample Conversation**

**User:** "I'm interested in AI and machine learning. What career options do I have?"

**AI:** "Great choice! AI/ML offers exciting career paths:

1. **Machine Learning Engineer** - Build and deploy ML models
2. **Data Scientist** - Analyze data and create predictive models
3. **AI Research Scientist** - Advance AI technology through research
4. **MLOps Engineer** - Manage ML infrastructure and pipelines
5. **Computer Vision Engineer** - Work on image/video AI applications

Given your Flutter background, you could combine mobile development with AI by building intelligent mobile apps. What aspect interests you most - the research side or practical applications?"

**User:** "Practical applications sound good. What skills do I need?"

**AI:** "Perfect! For AI application development, focus on:

**Core Skills:**
- Python (TensorFlow, PyTorch)
- Machine Learning fundamentals
- API integration (REST, GraphQL)
- Cloud platforms (AWS, GCP, Azure)

**Your Flutter Advantage:**
- Build AI-powered mobile apps
- Integrate ML models into apps
- Create on-device AI features

**Next Steps:**
1. Learn Python & TensorFlow basics
2. Explore Flutter ML packages (tflite_flutter)
3. Build a simple AI app project

Would you like specific learning resources or project ideas?"

---

## **Status Codes**

- **200** - Success with AI response
- **400** - Bad request (missing message)
- **401** - Unauthorized (invalid/expired token)
- **500** - Server error / AI API failure / Timeout

---

## **Implementation Complete! 🎉**

All endpoints are live and ready for Flutter integration.
