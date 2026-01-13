# MyFirstApi - Setup Guide

## ✅ Completed Implementation

### Database Schema
Two tables with proper relationships:
- **Users** - Authentication data (Id, Username, FullName, Email, PasswordHash, CreatedAt, UpdatedAt)
- **UserProfiles** - Extended profile data (PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills JSON, AreasOfInterest, ProfileImagePath)

### API Endpoints

#### Authentication (No auth required)
- ✅ **POST /api/auth/register** - Create new user account
- ✅ **POST /api/auth/login** - Login and get JWT token

#### User Profile Management (JWT Required)
- ✅ **GET /api/profile/{userId}** - Get user basic info
- ✅ **PUT /api/profile/{userId}** - Update user (name, username, email)
- ✅ **DELETE /api/profile/{userId}** - Delete user account

#### User Profile Data (JWT Required)
- ✅ **GET /api/userprofile/{userId}** - Get full profile
- ✅ **POST /api/userprofile?userId={id}** - Create/Update profile
- ✅ **POST /api/userprofile/upload-image?userId={id}** - Upload profile picture

---

## 🗄️ Database Setup

### Step 1: Create Tables
Run this in MySQL CLI or Workbench:
```bash
mysql -u root -p my_database < "C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi\sql\create_users_and_profiles.sql"
```

### Step 2: Create Stored Procedures
```bash
mysql -u root -p my_database < "C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi\sql\create_procs.sql"
```

### Step 3: Verify
```sql
-- Check tables
SHOW TABLES;

-- Check procedures
SHOW PROCEDURE STATUS WHERE Db='my_database';

-- Check table structure
DESCRIBE Users;
DESCRIBE UserProfiles;
```

---

## 🚀 Running the API

### Start the server
```powershell
cd 'C:\Users\Dell\Desktop\dotnet\learn\MyFirstApi'
dotnet run
```

The API will be available at:
- HTTP: http://localhost:5001
- Swagger UI: http://localhost:5001/swagger

---

## 📝 API Usage Examples

### 1. Register a new user
```http
POST http://localhost:5001/api/auth/register
Content-Type: application/json

{
  "username": "johndoe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "SecureP@ss123"
}
```

**Response:**
```json
{
  "id": 1,
  "username": "johndoe",
  "fullName": "John Doe",
  "email": "john@example.com",
  "createdAt": "2025-10-25T10:30:00"
}
```

### 2. Login
```http
POST http://localhost:5001/api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "SecureP@ss123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": 1,
    "username": "johndoe",
    "fullName": "John Doe",
    "email": "john@example.com",
    "createdAt": "2025-10-25T10:30:00"
  }
}
```

### 3. Get User Profile (requires JWT)
```http
GET http://localhost:5001/api/profile/1
Authorization: Bearer {your-jwt-token}
```

### 4. Update User Info (requires JWT)
```http
PUT http://localhost:5001/api/profile/1
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "fullName": "John Smith",
  "username": "johnsmith",
  "email": "john@example.com"
}
```

### 5. Create/Update Profile Data (requires JWT)
```http
POST http://localhost:5001/api/userprofile?userId=1
Authorization: Bearer {your-jwt-token}
Content-Type: application/json

{
  "phoneNumber": "+1234567890",
  "age": 25,
  "gender": "Male",
  "educationLevel": "Bachelor's Degree",
  "fieldOfStudy": "Computer Science",
  "skills": ["C#", ".NET", "MySQL", "React"],
  "areasOfInterest": "Web Development, AI, Cloud Computing"
}
```

### 6. Get Full Profile (requires JWT)
```http
GET http://localhost:5001/api/userprofile/1
Authorization: Bearer {your-jwt-token}
```

### 7. Upload Profile Picture (requires JWT)
```http
POST http://localhost:5001/api/userprofile/upload-image?userId=1
Authorization: Bearer {your-jwt-token}
Content-Type: multipart/form-data

(Select a .jpg, .jpeg, .png, or .gif file)
```

### 8. Delete Account (requires JWT)
```http
DELETE http://localhost:5001/api/profile/1
Authorization: Bearer {your-jwt-token}
```

---

## 🔐 JWT Configuration

Update `appsettings.json` with a strong secret key for production:
```json
{
  "Jwt": {
    "Key": "your-very-long-secret-key-min-32-chars-for-production",
    "Issuer": "MyFirstApi",
    "Audience": "MyFirstApiUsers",
    "ExpireMinutes": 60
  }
}
```

---

## 📂 Project Structure

```
MyFirstApi/
├── Controllers/
│   ├── AuthController.cs         (register, login)
│   └── ProfileController.cs      (user & profile management)
├── Models/
│   ├── User.cs                   (User, UserProfile classes)
│   └── AuthModels.cs             (DTOs: RegisterRequest, LoginRequest, etc.)
├── sql/
│   ├── create_users_and_profiles.sql   (table schema)
│   └── create_procs.sql                (stored procedures)
├── wwwroot/
│   └── uploads/profiles/         (profile images stored here)
└── appsettings.json
```

---

## ✨ What Changed

### Removed
- ❌ Employee table and controller
- ❌ Old single users table with all fields
- ❌ Signup endpoint (replaced with /register)
- ❌ Phone/Age/Gender in Users table (moved to UserProfiles)

### Added
- ✅ Separate Users and UserProfiles tables
- ✅ Foreign key relationship (UserProfiles → Users)
- ✅ JSON support for Skills field
- ✅ Profile image upload functionality
- ✅ Full CRUD for user management
- ✅ Stored procedures for all operations
- ✅ JWT authorization on profile endpoints

---

## 🔧 Troubleshooting

### Stored procedures not found
Run the create scripts as shown in Database Setup above.

### JWT validation fails
Ensure:
1. You're sending the token in the Authorization header: `Bearer {token}`
2. The Jwt:Key in appsettings.json matches on server
3. Token hasn't expired (default: 60 minutes)

### Image upload fails
- Create the directory: `mkdir wwwroot\uploads\profiles`
- Ensure file extensions are: .jpg, .jpeg, .png, .gif
- Check file size limits in your hosting environment

---

## 📊 Database Fields Summary

**Users table (9 fields):**
1. Id
2. Username
3. FullName
4. Email
5. PasswordHash
6. CreatedAt
7. UpdatedAt

**UserProfiles table (12 fields):**
1. Id
2. UserId (FK)
3. PhoneNumber
4. Age
5. Gender
6. EducationLevel
7. FieldOfStudy
8. Skills (JSON array)
9. AreasOfInterest
10. ProfileImagePath
11. CreatedAt
12. UpdatedAt

---

## 🎯 Next Steps

To integrate with your Flutter app:
1. Use the register/login endpoints
2. Store the JWT token securely (flutter_secure_storage)
3. Include token in Authorization header for all profile requests
4. Handle 401 Unauthorized responses (token expired → re-login)
5. Use multipart/form-data for image uploads

Happy coding! 🚀
