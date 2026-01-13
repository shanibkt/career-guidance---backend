# 🔧 Chat Endpoint Troubleshooting Guide

## **I've Added Enhanced Error Logging**

The backend now has detailed console logs that will show exactly what's failing.

---

## **Step 1: Test Groq API Connection**

### **Test Endpoint (No Authentication Required):**
```
GET http://192.168.1.100:5001/api/chat/test
```

**This will:**
- ✅ Test if Groq API key is valid
- ✅ Test if API URL is correct
- ✅ Show detailed error logs in console

**Expected Success Response:**
```json
{
  "success": true,
  "message": "Groq API is working!",
  "response": "Hello! Yes, I'm working perfectly..."
}
```

**If it fails, you'll see:**
```json
{
  "success": false,
  "error": "Exact error message here",
  "type": "HttpRequestException"
}
```

---

## **Step 2: Check Backend Console Logs**

When you call `/api/chat/test` or `/api/chat`, look for these logs:

### **✅ Success Logs:**
```
🔵 GetChatResponse called
🔵 User message: Hello
🔵 Calling Groq API with model: llama-3.1-70b-versatile
🔵 API URL: https://api.groq.com/openai/v1/chat/completions
🔵 Groq API Response Status: 200
🔵 Response (first 200 chars): {"choices":[{"message":{"content":"Hello!...
🟢 AI Response: Hello! Yes, I'm working perfectly...
```

### **❌ Error Logs (401 Unauthorized):**
```
🔵 GetChatResponse called
🔵 Calling Groq API with model: llama-3.1-70b-versatile
🔵 Groq API Response Status: 401
🔴 Groq API Error Response: {"error":{"message":"Invalid API key"}}
🔴 GetChatResponse Exception: HttpRequestException
🔴 Error Message: Groq API returned 401: {"error":...}
```

### **❌ Error Logs (Network Issue):**
```
🔵 GetChatResponse called
🔴 GetChatResponse Exception: HttpRequestException
🔴 Error Message: No such host is known
```

---

## **Step 3: Common Issues & Fixes**

### **Issue 1: 401 Unauthorized**
**Cause:** Invalid API key

**Fix:** Test the API key manually:
```bash
curl https://api.groq.com/openai/v1/chat/completions \
  -H "Authorization: Bearer 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" \
  -H "Content-Type: application/json" \
  -d '{
    "model": "llama-3.1-70b-versatile",
    "messages": [{"role": "user", "content": "Hello"}]
  }'
```

If this returns 401, the API key is **invalid or expired**.

---

### **Issue 2: Model Not Found**
**Cause:** Wrong model name

**Current model:** `llama-3.1-70b-versatile`

**Alternative models to try:**
- `llama-3.1-8b-instant` (faster, less capable)
- `mixtral-8x7b-32768` (alternative provider)

**Change in GroqService.cs:**
```csharp
private readonly string _chatModel = "llama-3.1-8b-instant"; // Try this
```

---

### **Issue 3: Network Timeout**
**Cause:** Slow internet or Groq API is down

**Fix:** Check Groq API status:
```bash
curl https://api.groq.com/openai/v1/models \
  -H "Authorization: Bearer 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"
```

Should return list of available models.

---

### **Issue 4: ChatSessions Table Not Created**
**Cause:** Database tables missing

**Fix:**
```
GET http://192.168.1.100:5001/api/setup/create-chat-tables
```

---

## **Step 4: Temporary Hardcoded Response (Flutter Testing)**

If Groq API keeps failing, temporarily use this for Flutter testing:

**ChatController.cs - Replace SendMessage method:**
```csharp
[HttpPost]
public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
{
    try
    {
        _logger.LogInformation($"📩 Received message: {request.Message}");

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid user token" });
        }

        // TEMPORARY: Hardcoded response for Flutter testing
        var mockResponse = $"This is a test response to: '{request.Message}'. The AI integration is being fixed.";

        return Ok(new ChatResponse
        {
            Response = mockResponse,
            SessionId = request.SessionId ?? Guid.NewGuid()
        });
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error: {ex.Message}");
        return StatusCode(500, new { error = ex.Message });
    }
}
```

This proves the Flutter app works correctly - the issue is only the Groq API.

---

## **Step 5: Debug with Postman**

### **Test 1: Test Groq Connection**
```
GET http://192.168.1.100:5001/api/chat/test
```

### **Test 2: Send Chat Message**
```
POST http://192.168.1.100:5001/api/chat
Headers:
  Authorization: Bearer YOUR_JWT_TOKEN
  Content-Type: application/json
Body:
{
  "message": "Hello",
  "sessionId": null
}
```

Watch the backend console for detailed logs.

---

## **Step 6: Verify GroqService Configuration**

**Check GroqService.cs line 8-11:**
```csharp
private readonly string _apiKey = "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";
private readonly string _model = "llama-3.1-8b-instant";
private readonly string _chatModel = "llama-3.1-70b-versatile";
```

**Check GroqService.cs line 13-17:**
```csharp
public GroqService(HttpClient httpClient)
{
    _httpClient = httpClient;
    _httpClient.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
    _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
}
```

---

## **Step 7: Full Error Details**

The enhanced logging will show:
- ✅ Exact HTTP status code (200, 401, 404, 500)
- ✅ Full error response from Groq API
- ✅ Exception type (HttpRequestException, TaskCanceledException, etc.)
- ✅ Stack trace for debugging

**Watch the terminal output** when Flutter calls the endpoint.

---

## **Expected Results:**

### **✅ If Test Endpoint Works:**
The issue was with chat session/database - now fixed with better error handling.

### **❌ If Test Endpoint Fails:**
1. Check API key validity
2. Try alternative model (`llama-3.1-8b-instant`)
3. Test with curl directly
4. Use hardcoded response temporarily

---

## **Quick Commands:**

### **Restart API:**
```powershell
Stop-Process -Name "MyFirstApi" -Force
cd c:\Users\Dell\Desktop\dotnet\learn\MyFirstApi
dotnet run
```

### **Test Groq Connection:**
```bash
curl http://192.168.1.100:5001/api/chat/test
```

### **Test Chat with Postman:**
```
POST http://192.168.1.100:5001/api/chat
Authorization: Bearer YOUR_TOKEN
Body: {"message": "Hello", "sessionId": null}
```

---

## **Current Status:**

✅ Enhanced logging added to ChatController  
✅ Enhanced logging added to GroqService  
✅ New test endpoint created: `/api/chat/test`  
✅ Better error messages with exception types  
✅ API is running and ready for testing  

**Next:** Run the test endpoint and check console logs! 🔍
