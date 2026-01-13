# Career Guidance Backend API

> A comprehensive ASP.NET Core 9.0 backend for the Career Guidance mobile application, providing career recommendations, AI-powered features, video learning, quiz generation, and job search functionality.

## 📁 Project Structure

```
career-guidance-backend/
├── Controllers/           # API Controllers (14 endpoints)
├── Services/              # Business Logic (6 services)
├── Models/                # Data Models
├── Filters/               # Global Filters
├── sql/                   # Database Scripts
│   ├── migrations/        # Schema changes & updates
│   ├── seeds/             # Sample data & population
│   ├── procedures/        # Stored procedures
│   └── admin/             # Admin setup scripts
├── scripts/               # PowerShell Scripts
│   ├── admin/             # Admin panel setup
│   └── deployment/        # Deployment scripts
├── docs/                  # Documentation
│   ├── guides/            # User & setup guides
│   ├── admin/             # Admin documentation
│   ├── deployment/        # Deployment guides
│   └── implementation/    # Technical implementation docs
├── wwwroot/               # Static Files
│   └── admin.html         # Admin panel UI
├── appsettings.json       # Configuration
└── Program.cs             # Entry point
```

## 🚀 Quick Start

### Prerequisites
- .NET 9.0 SDK
- MySQL Database
- Visual Studio 2022 or VS Code

### 1. Configure Database
Edit `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=freedb_career_guidence;User=your-user;Password=your-password;"
  },
  "Jwt": {
    "Key": "your-secret-key-here-minimum-32-characters",
    "Issuer": "CareerGuidanceAPI",
    "Audience": "CareerGuidanceApp"
  }
}
```

### 2. Run Database Migrations
```bash
cd sql/migrations
mysql -h your-server -u your-user -p your-database < RUN_ALL_MIGRATIONS.sql
```

### 3. Build and Run
```bash
dotnet restore
dotnet build
dotnet run
```

API available at: `http://localhost:5001`

## 📚 Key Features

- 🔐 **JWT Authentication** - Secure token-based auth
- 🎯 **AI Career Guidance** - Personalized recommendations
- 📹 **Video Learning** - Progress tracking & transcripts
- 🧠 **AI Quiz Generation** - From video transcripts
- 💼 **Job Search** - API integration & bookmarking
- 💬 **AI Chatbot** - Groq-powered career counseling
- 📄 **Resume Builder** - Professional templates
- 👨‍💼 **Admin Panel** - Content & user management

## 🔌 API Endpoints

### Authentication
```
POST   /api/auth/register          # Register user
POST   /api/auth/login             # Login
POST   /api/auth/refresh           # Refresh token
```

### Learning Videos
```
GET    /api/learningvideos                     # List all
POST   /api/learningvideos                     # Create (Admin)
PUT    /api/learningvideos/{id}                # Update (Admin)
DELETE /api/learningvideos/{id}                # Delete (Admin)
GET    /api/learningvideos/{id}/transcript     # Get transcript
```

### Quiz
```
POST   /api/quiz/generate-from-video           # Generate from video
POST   /api/quiz/generate-skill-based          # Generate by skill
POST   /api/quiz/save-session                  # Save results
```

### Jobs
```
GET    /api/jobs/search                        # Search jobs
POST   /api/jobs/save                          # Save job
GET    /api/jobs/saved/{userId}                # Get saved
```

### Admin
```
GET    /api/admin/stats                        # Statistics
GET    /api/admin/users                        # List users
DELETE /api/admin/users/{userId}               # Delete user
```

See full API documentation in `docs/guides/`

## 📊 Database Schema

### Core Tables
- `users` - Authentication
- `user_profiles` - Profile data
- `user_career_progress` - Career tracking
- `learning_videos` - Video content with transcripts
- `video_watch_history` - Progress
- `quiz_sessions` - Quiz results
- `chat_history` - AI chat logs
- `saved_jobs` - Job bookmarks
- `user_resumes` - Resume data

See `sql/database_schema_complete.sql`

## 🔧 Configuration

### Key Settings
- **JWT_KEY**: Minimum 32 characters
- **GROQ_API_KEY**: For AI features
- **JOB_API_KEY**: For job search

See `docs/guides/DATABASE_SETUP.md` for detailed setup.

## 🚢 Deployment

- **Azure**: See `docs/deployment/AZURE_DEPLOYMENT_GUIDE.md`
- **FTP**: See `docs/deployment/WINSCP_DEPLOYMENT_GUIDE.md`

## 📖 Documentation

All documentation moved to organized folders:
- **Setup Guides**: `docs/guides/`
- **Admin Guides**: `docs/admin/`
- **Deployment**: `docs/deployment/`
- **Technical**: `docs/implementation/`

## 👨‍💼 Admin Panel

Access admin panel at: `http://localhost:5001/admin.html`

Default credentials:
```
Email: admin@careerguidance.com
Password: Admin@123
```

Setup: Run `scripts/admin/setup-admin-quick.ps1`

## 🛠️ Development

### Folder Organization
All files organized by purpose:
- Source code: `Controllers/`, `Services/`, `Models/`
- Database: `sql/migrations/`, `sql/seeds/`, `sql/procedures/`
- Scripts: `scripts/admin/`, `scripts/deployment/`
- Docs: `docs/guides/`, `docs/admin/`, `docs/deployment/`

### Adding Features
1. Create model in `Models/`
2. Add service in `Services/`
3. Create controller in `Controllers/`
4. Add migration in `sql/migrations/`

## 📝 Recent Updates

### Clean Code Structure (Latest)
- ✅ Organized all documentation into `docs/` with subcategories
- ✅ Moved scripts to `scripts/admin/` and `scripts/deployment/`
- ✅ Organized SQL files: migrations, seeds, procedures, admin
- ✅ Fixed Flutter code issues (unused imports, unnecessary casts)
- ✅ Updated README with new structure

### Features
- ✅ Video transcript management for quiz generation
- ✅ Admin panel with user & video management
- ✅ AI-powered quiz generation from transcripts
- ✅ Job search & bookmarking
- ✅ Chat history persistence
- ✅ Resume builder integration

## 🆘 Quick Help

### Common Commands
```bash
# Run backend
dotnet run

# Check for errors
dotnet build

# Run migrations
cd sql/migrations && mysql -h server -u user -p db < RUN_ALL_MIGRATIONS.sql

# Setup admin user
cd scripts/admin && .\setup-admin-quick.ps1
```

### Troubleshooting
- Check `docs/guides/TESTING_INSTRUCTIONS.md`
- See `docs/implementation/` for technical details
- Review `docs/admin/` for admin panel issues

## 🔗 Related

- **Flutter App**: `../career_guidence/`
- **Admin Panel**: Included in `wwwroot/admin.html`

---

**Built with ❤️ using .NET 9.0 | Organized for Clean Code**
