# 🔄 Token Refresh System - Flutter Integration Guide

## ✅ Problem Solved!

**Before:** Users had to logout and login again when token expired (every 24 hours)
**Now:** Flutter app automatically refreshes expired tokens in the background!

---

## 🗄️ Step 1: Create Database Table (Backend - Do This First!)

Run this SQL in your MySQL database:

```sql
-- Run this file:
SOURCE c:/Users/Dell/Desktop/dotnet/learn/MyFirstApi/sql/create_refresh_tokens.sql;

-- Or copy-paste this:
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at DATETIME NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    revoked BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_token (token),
    INDEX idx_expires (expires_at)
);
```

---

## 🔌 Step 2: Updated API Responses

### Login Response (Now includes refresh token)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "xK9mP3qR8sT5vW2yZ...",
  "tokenExpiration": "2025-11-25T05:44:00Z",
  "user": {
    "id": 123,
    "username": "john",
    "fullName": "John Doe",
    "email": "john@example.com"
  }
}
```

### Signup Response (Also includes refresh token)
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "aB1cD2eF3gH4iJ5k...",
  "tokenExpiration": "2025-11-25T05:44:00Z",
  "user": {
    "id": 456,
    "username": "jane",
    "fullName": "Jane Smith",
    "email": "jane@example.com"
  }
}
```

---

## 🆕 Step 3: New Refresh Endpoint

### POST /api/auth/refresh

**Request:**
```json
{
  "token": "old_expired_token",
  "refreshToken": "stored_refresh_token"
}
```

**Response (200 OK):**
```json
{
  "token": "new_fresh_token",
  "refreshToken": "new_refresh_token",
  "tokenExpiration": "2025-11-25T06:00:00Z"
}
```

**Error Responses:**
- `401`: Refresh token invalid/expired/revoked
- `400`: Missing refresh token

---

## 📱 Step 4: Flutter Implementation

### Update Auth Models

```dart
class LoginResponse {
  final String token;
  final String refreshToken;
  final DateTime tokenExpiration;
  final User user;

  LoginResponse({
    required this.token,
    required this.refreshToken,
    required this.tokenExpiration,
    required this.user,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) {
    return LoginResponse(
      token: json['token'] as String,
      refreshToken: json['refreshToken'] as String,
      tokenExpiration: DateTime.parse(json['tokenExpiration']),
      user: User.fromJson(json['user']),
    );
  }
}
```

### Update Storage to Save Both Tokens

```dart
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class AuthStorage {
  final _storage = const FlutterSecureStorage();

  // Save tokens after login/signup
  Future<void> saveTokens({
    required String accessToken,
    required String refreshToken,
    required DateTime expiration,
  }) async {
    await _storage.write(key: 'access_token', value: accessToken);
    await _storage.write(key: 'refresh_token', value: refreshToken);
    await _storage.write(key: 'token_expiration', value: expiration.toIso8601String());
  }

  // Get access token
  Future<String?> getAccessToken() async {
    return await _storage.read(key: 'access_token');
  }

  // Get refresh token
  Future<String?> getRefreshToken() async {
    return await _storage.read(key: 'refresh_token');
  }

  // Get expiration time
  Future<DateTime?> getTokenExpiration() async {
    final expirationStr = await _storage.read(key: 'token_expiration');
    return expirationStr != null ? DateTime.parse(expirationStr) : null;
  }

  // Check if token is expired
  Future<bool> isTokenExpired() async {
    final expiration = await getTokenExpiration();
    if (expiration == null) return true;
    return DateTime.now().isAfter(expiration);
  }

  // Clear all tokens (logout)
  Future<void> clearTokens() async {
    await _storage.delete(key: 'access_token');
    await _storage.delete(key: 'refresh_token');
    await _storage.delete(key: 'token_expiration');
  }
}
```

### Create Token Refresh Service

```dart
import 'dart:convert';
import 'package:http/http.dart' as http;

class TokenRefreshService {
  final String baseUrl = 'http://192.168.1.100:5001';
  final AuthStorage _storage = AuthStorage();

  // Refresh the access token
  Future<bool> refreshToken() async {
    try {
      final accessToken = await _storage.getAccessToken();
      final refreshToken = await _storage.getRefreshToken();

      if (refreshToken == null) {
        print('No refresh token available');
        return false;
      }

      final response = await http.post(
        Uri.parse('$baseUrl/api/auth/refresh'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({
          'token': accessToken ?? '',
          'refreshToken': refreshToken,
        }),
      ).timeout(const Duration(seconds: 10));

      if (response.statusCode == 200) {
        final data = jsonDecode(response.body);
        
        // Save new tokens
        await _storage.saveTokens(
          accessToken: data['token'],
          refreshToken: data['refreshToken'],
          expiration: DateTime.parse(data['tokenExpiration']),
        );

        print('✅ Token refreshed successfully');
        return true;
      } else {
        print('❌ Token refresh failed: ${response.statusCode}');
        return false;
      }
    } catch (e) {
      print('❌ Token refresh error: $e');
      return false;
    }
  }
}
```

### Update API Service with Auto-Refresh

```dart
class ApiService {
  final String baseUrl = 'http://192.168.1.100:5001';
  final AuthStorage _storage = AuthStorage();
  final TokenRefreshService _tokenRefresh = TokenRefreshService();

  // Make authenticated request with auto-refresh
  Future<http.Response> authenticatedRequest({
    required String method,
    required String endpoint,
    Map<String, String>? headers,
    dynamic body,
  }) async {
    // Check if token is expired
    if (await _storage.isTokenExpired()) {
      print('🔄 Token expired, attempting refresh...');
      final refreshed = await _tokenRefresh.refreshToken();
      
      if (!refreshed) {
        // Refresh failed - redirect to login
        throw Exception('Session expired. Please login again.');
      }
    }

    // Get fresh token
    final token = await _storage.getAccessToken();
    final requestHeaders = {
      'Content-Type': 'application/json',
      'Authorization': 'Bearer $token',
      ...?headers,
    };

    // Make request
    http.Response response;
    final uri = Uri.parse('$baseUrl$endpoint');

    switch (method.toUpperCase()) {
      case 'GET':
        response = await http.get(uri, headers: requestHeaders);
        break;
      case 'POST':
        response = await http.post(uri, headers: requestHeaders, body: body);
        break;
      case 'PUT':
        response = await http.put(uri, headers: requestHeaders, body: body);
        break;
      case 'DELETE':
        response = await http.delete(uri, headers: requestHeaders);
        break;
      default:
        throw Exception('Unsupported HTTP method: $method');
    }

    // If still 401 after refresh, session is invalid
    if (response.statusCode == 401) {
      await _storage.clearTokens();
      throw Exception('Session expired. Please login again.');
    }

    return response;
  }

  // Usage examples:
  Future<QuizResponse> generateQuiz() async {
    final response = await authenticatedRequest(
      method: 'POST',
      endpoint: '/api/quiz/generate',
    );

    if (response.statusCode == 200) {
      return QuizResponse.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to generate quiz');
  }

  Future<RecommendationsResponse> getRecommendations() async {
    final response = await authenticatedRequest(
      method: 'GET',
      endpoint: '/api/recommendations',
    );

    if (response.statusCode == 200) {
      return RecommendationsResponse.fromJson(jsonDecode(response.body));
    }
    throw Exception('Failed to get recommendations');
  }
}
```

### Update Login/Signup to Save Refresh Token

```dart
class AuthService {
  final String baseUrl = 'http://192.168.1.100:5001';
  final AuthStorage _storage = AuthStorage();

  Future<LoginResponse> login(String email, String password) async {
    final response = await http.post(
      Uri.parse('$baseUrl/api/auth/login'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'email': email,
        'password': password,
      }),
    );

    if (response.statusCode == 200) {
      final loginResponse = LoginResponse.fromJson(jsonDecode(response.body));
      
      // Save both tokens
      await _storage.saveTokens(
        accessToken: loginResponse.token,
        refreshToken: loginResponse.refreshToken,
        expiration: loginResponse.tokenExpiration,
      );

      return loginResponse;
    }
    throw Exception('Login failed');
  }

  Future<void> logout() async {
    await _storage.clearTokens();
  }
}
```

---

## 🎯 How It Works

1. **User logs in** → Backend returns:
   - Access token (valid 24 hours)
   - Refresh token (valid 7 days)
   - Expiration timestamp

2. **Before each API call**, Flutter checks:
   - Is access token expired?
   - **Yes** → Automatically call `/api/auth/refresh` with refresh token
   - **No** → Use existing access token

3. **Refresh token expired (after 7 days)?**
   - Redirect to login
   - User must login again

---

## ✅ Benefits

- ✅ **No more "token expired" errors** during normal usage
- ✅ Users stay logged in for **7 days** (vs 24 hours)
- ✅ **Automatic background refresh** - user doesn't notice
- ✅ **Security**: Access tokens are short-lived (24h)
- ✅ **Better UX**: No forced logouts during quiz/browsing

---

## 🔒 Security Features

1. **Refresh tokens are single-use**
   - Old refresh token revoked when new one issued
   - Prevents token reuse attacks

2. **Refresh tokens expire after 7 days**
   - Forces re-authentication for security

3. **All old refresh tokens revoked on new login**
   - Only one active session per user

4. **Database tracking**
   - Can see all active sessions
   - Can revoke tokens remotely

---

## 🧪 Testing

### Test Flow:
1. Login → Save both tokens
2. Wait 24 hours (or manually expire token in DB)
3. Try to generate quiz → Should auto-refresh
4. Quiz works without re-login ✅

### Manual Test:
```dart
// In your Flutter app
void testTokenRefresh() async {
  final service = TokenRefreshService();
  final success = await service.refreshToken();
  print(success ? '✅ Refresh works' : '❌ Refresh failed');
}
```

---

## 📊 Backend Logs

When refresh happens, you'll see:
```
Token validated for user: 123
Quiz generate endpoint called
User ID from token: 123
```

No more "token expired" errors! 🎉

---

## 🚀 Deployment Checklist

- [ ] Run `create_refresh_tokens.sql` in MySQL
- [ ] Update Flutter models to include `refreshToken` and `tokenExpiration`
- [ ] Implement `AuthStorage` with secure storage
- [ ] Create `TokenRefreshService`
- [ ] Update all API calls to use `authenticatedRequest()`
- [ ] Test login → wait → auto-refresh flow
- [ ] Deploy backend with refresh endpoint

---

**Now users won't have to logout/login anymore!** The app handles token refresh automatically in the background. 🎯
