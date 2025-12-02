# 🤖 AI Career Chatbot - Flutter Developer Guide

## **For Your Flutter Developer**

Your .NET backend now has an **AI Career Chatbot** using Groq's `llama-3.1-70b-versatile` model!

**Base URL:** `http://192.168.1.100:5001/api`

---

## **🚀 Quick Setup**

### **Step 1: Create Chat Tables (Run Once)**
```bash
GET http://192.168.1.100:5001/api/setup/create-chat-tables
```

### **Step 2: Add to pubspec.yaml**
```yaml
dependencies:
  http: ^1.1.0
```

### **Step 3: Copy the Code Below**

---

## **📱 Flutter Implementation**

### **1. Chat Models (lib/models/chat_models.dart)**

```dart
class ChatRequest {
  final String message;
  final String? sessionId;

  ChatRequest({required this.message, this.sessionId});

  Map<String, dynamic> toJson() => {
    'message': message,
    'sessionId': sessionId,
  };
}

class ChatResponse {
  final String response;
  final String sessionId;

  ChatResponse({required this.response, required this.sessionId});

  factory ChatResponse.fromJson(Map<String, dynamic> json) => ChatResponse(
    response: json['response'],
    sessionId: json['sessionId'],
  );
}

class Message {
  final String text;
  final bool isUser;
  final DateTime time;

  Message({required this.text, required this.isUser, DateTime? time})
      : time = time ?? DateTime.now();
}
```

---

### **2. Chat Service (lib/services/chat_service.dart)**

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import '../models/chat_models.dart';

class ChatService {
  static const String baseUrl = 'http://192.168.1.100:5001/api/chat';
  String? _sessionId;
  final String token;

  ChatService({required this.token});

  Future<ChatResponse> sendMessage(String message) async {
    try {
      final response = await http
          .post(
            Uri.parse(baseUrl),
            headers: {
              'Authorization': 'Bearer $token',
              'Content-Type': 'application/json',
            },
            body: jsonEncode({
              'message': message,
              'sessionId': _sessionId,
            }),
          )
          .timeout(Duration(seconds: 90));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        final chatResponse = ChatResponse.fromJson(data);
        _sessionId = chatResponse.sessionId;
        return chatResponse;
      } else if (response.statusCode == 401) {
        throw Exception('Please login again');
      } else {
        throw Exception('Failed to get response');
      }
    } catch (e) {
      rethrow;
    }
  }

  void startNewChat() {
    _sessionId = null;
  }
}
```

---

### **3. Chat Screen (lib/screens/chat_screen.dart)**

```dart
import 'package:flutter/material.dart';
import '../services/chat_service.dart';
import '../models/chat_models.dart';

class ChatScreen extends StatefulWidget {
  final String token;

  const ChatScreen({Key? key, required this.token}) : super(key: key);

  @override
  State<ChatScreen> createState() => _ChatScreenState();
}

class _ChatScreenState extends State<ChatScreen> {
  late ChatService _chatService;
  final List<Message> _messages = [];
  final TextEditingController _controller = TextEditingController();
  final ScrollController _scrollController = ScrollController();
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _chatService = ChatService(token: widget.token);
    
    // Welcome message
    _messages.add(Message(
      text: "👋 Hi! I'm your AI Career Assistant.\n\nI can help you with:\n• Career path exploration\n• Skills recommendations\n• Job market insights\n• Interview preparation\n\nWhat would you like to know?",
      isUser: false,
    ));
  }

  void _sendMessage() async {
    if (_controller.text.trim().isEmpty) return;

    final userMessage = _controller.text.trim();
    _controller.clear();

    setState(() {
      _messages.add(Message(text: userMessage, isUser: true));
      _isLoading = true;
    });

    _scrollToBottom();

    try {
      final response = await _chatService.sendMessage(userMessage);

      setState(() {
        _messages.add(Message(text: response.response, isUser: false));
        _isLoading = false;
      });

      _scrollToBottom();
    } catch (e) {
      setState(() {
        _messages.add(Message(
          text: "Sorry, something went wrong. Please try again.",
          isUser: false,
        ));
        _isLoading = false;
      });
    }
  }

  void _scrollToBottom() {
    Future.delayed(Duration(milliseconds: 100), () {
      if (_scrollController.hasClients) {
        _scrollController.animateTo(
          _scrollController.position.maxScrollExtent,
          duration: Duration(milliseconds: 300),
          curve: Curves.easeOut,
        );
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('Career Assistant'),
        actions: [
          IconButton(
            icon: Icon(Icons.refresh),
            onPressed: () {
              setState(() {
                _messages.clear();
                _messages.add(Message(
                  text: "New conversation started! How can I help?",
                  isUser: false,
                ));
              });
              _chatService.startNewChat();
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              controller: _scrollController,
              padding: EdgeInsets.all(16),
              itemCount: _messages.length,
              itemBuilder: (context, index) {
                final message = _messages[index];
                return _buildMessageBubble(message);
              },
            ),
          ),
          if (_isLoading)
            Padding(
              padding: EdgeInsets.all(8),
              child: Row(
                children: [
                  SizedBox(width: 16),
                  SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  ),
                  SizedBox(width: 12),
                  Text('AI is thinking...', style: TextStyle(color: Colors.grey)),
                ],
              ),
            ),
          _buildInputArea(),
        ],
      ),
    );
  }

  Widget _buildMessageBubble(Message message) {
    return Padding(
      padding: EdgeInsets.only(bottom: 12),
      child: Row(
        mainAxisAlignment: message.isUser ? MainAxisAlignment.end : MainAxisAlignment.start,
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          if (!message.isUser) ...[
            CircleAvatar(
              backgroundColor: Colors.blue.shade100,
              child: Icon(Icons.support_agent, color: Colors.blue),
            ),
            SizedBox(width: 8),
          ],
          Flexible(
            child: Container(
              padding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: message.isUser ? Colors.blue.shade500 : Colors.grey.shade200,
                borderRadius: BorderRadius.circular(16),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    message.text,
                    style: TextStyle(
                      color: message.isUser ? Colors.white : Colors.black87,
                      fontSize: 15,
                    ),
                  ),
                  SizedBox(height: 4),
                  Text(
                    '${message.time.hour}:${message.time.minute.toString().padLeft(2, '0')}',
                    style: TextStyle(
                      color: message.isUser ? Colors.white70 : Colors.black54,
                      fontSize: 11,
                    ),
                  ),
                ],
              ),
            ),
          ),
          if (message.isUser) ...[
            SizedBox(width: 8),
            CircleAvatar(
              backgroundColor: Colors.green.shade100,
              child: Icon(Icons.person, color: Colors.green),
            ),
          ],
        ],
      ),
    );
  }

  Widget _buildInputArea() {
    return Container(
      decoration: BoxDecoration(
        color: Colors.white,
        boxShadow: [
          BoxShadow(color: Colors.black12, blurRadius: 4, offset: Offset(0, -2)),
        ],
      ),
      padding: EdgeInsets.all(8),
      child: SafeArea(
        child: Row(
          children: [
            Expanded(
              child: TextField(
                controller: _controller,
                decoration: InputDecoration(
                  hintText: 'Ask about your career...',
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(24)),
                  contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                ),
                maxLines: null,
                textInputAction: TextInputAction.send,
                onSubmitted: (_) => _sendMessage(),
              ),
            ),
            SizedBox(width: 8),
            CircleAvatar(
              backgroundColor: Theme.of(context).primaryColor,
              child: IconButton(
                icon: Icon(Icons.send, color: Colors.white),
                onPressed: _sendMessage,
              ),
            ),
          ],
        ),
      ),
    );
  }

  @override
  void dispose() {
    _controller.dispose();
    _scrollController.dispose();
    super.dispose();
  }
}
```

---

### **4. Navigate to Chat**

```dart
// Add this button somewhere in your app:
ElevatedButton(
  onPressed: () {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (context) => ChatScreen(token: yourJwtToken),
      ),
    );
  },
  child: Text('Ask AI Career Assistant'),
)
```

---

## **🧪 Testing**

### **Test with Postman First:**
```
POST http://192.168.1.100:5001/api/chat
Headers:
  Authorization: Bearer YOUR_TOKEN
  Content-Type: application/json
Body:
{
  "message": "What skills do I need for Flutter development?",
  "sessionId": null
}
```

### **Expected Response:**
```json
{
  "response": "For Flutter development, you'll need...",
  "sessionId": "some-guid-here"
}
```

---

## **💡 Key Features**

✅ **Conversation Memory** - AI remembers last 6 messages (pass sessionId)  
✅ **Personalized** - Uses your user profile (education, skills, interests)  
✅ **Fast** - Responses in 2-5 seconds  
✅ **Smart** - Uses advanced llama-3.1-70b model  
✅ **New Chat** - Click refresh to start fresh conversation  

---

## **🔥 Example Conversations**

**User:** "I know Flutter. What career options do I have?"  
**AI:** "With Flutter skills, you have great options: Mobile App Developer, UI/UX Developer, Full-Stack Developer, Freelance Developer, or Startup Founder. Which interests you?"

**User:** "Mobile app development sounds good"  
**AI:** "Perfect! For mobile app development with Flutter, focus on: State management (Provider/Riverpod), Firebase, REST APIs, Git, and UI/UX principles. Start building portfolio projects!"

**User:** "How do I build a portfolio?"  
**AI:** "Build 3-5 real projects: 1) Todo app with Firebase, 2) Weather app with API, 3) E-commerce app, 4) Chat app, 5) Something unique to you. Deploy to Play Store/App Store. GitHub + live demos = strong portfolio!"

---

## **📋 API Reference**

### **POST /api/chat**
- **Headers:** `Authorization: Bearer TOKEN`
- **Body:** `{ "message": "string", "sessionId": "guid or null" }`
- **Response:** `{ "response": "string", "sessionId": "guid" }`

### **GET /api/chat/history?sessionId=GUID**
- Get message history for a session

### **GET /api/chat/sessions**
- Get all user's chat sessions

---

## **🎯 Done!**

Copy the 3 code files above, update the IP address, and you have a working AI chatbot! 🚀
