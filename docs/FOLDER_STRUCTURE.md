# 📁 Project Folder Structure

## Overview
This document describes the clean, organized folder structure of the Career Guidance Backend API.

## Root Structure

```
career-guidance-backend/
├── Controllers/              # HTTP API Controllers
├── Services/                 # Business logic & data access
├── Models/                   # Data transfer objects & entities
├── Filters/                  # Global exception filters
├── Properties/               # Project properties
├── sql/                      # Database scripts (organized)
├── scripts/                  # PowerShell automation scripts
├── docs/                     # Documentation (organized)
├── wwwroot/                  # Static web files
├── bin/                      # Build output (git ignored)
├── obj/                      # Intermediate build files (git ignored)
├── publish/                  # Published output (git ignored)
├── .vscode/                  # VS Code settings
├── .git/                     # Git repository
├── .gitignore                # Git ignore rules
├── Program.cs                # Application entry point
├── MyFirstApi.csproj         # Project file
├── MyFirstApi.http           # HTTP request examples
├── appsettings.json          # Configuration (no secrets)
├── appsettings.Development.json  # Dev config (git ignored)
├── README.md                 # Main documentation
└── README.old.md            # Previous README (backup)
```

## 📂 Detailed Structure

### `/Controllers/` - API Endpoints
Contains all HTTP controllers that handle API requests.

**Files (14 controllers):**
- `AdminController.cs` - Admin panel operations
- `AuthController.cs` - Authentication & authorization
- `CareerProgressController.cs` - Career tracking
- `ChatController.cs` - AI chatbot
- `ChatHistoryController.cs` - Chat persistence
- `JobsController.cs` - Job search & saved jobs
- `LearningVideosController.cs` - Video CRUD
- `LogsController.cs` - Logging & diagnostics
- `ProfileController.cs` - User profiles
- `QuizController.cs` - AI quiz generation
- `RecommendationsController.cs` - Career recommendations
- `ResumeController.cs` - Resume builder
- `SetupController.cs` - Initial setup
- `VideoProgressController.cs` - Video watch tracking

### `/Services/` - Business Logic
Contains service classes that implement business logic.

**Files (6 services):**
- `CareerProgressService.cs` - Career progress tracking
- `DatabaseService.cs` - Database operations
- `GroqService.cs` - AI integration (Groq API)
- `JobApiService.cs` - Job API integration
- `JobDatabaseService.cs` - Job data persistence
- `LocalCrashReportingService.cs` - Error reporting

### `/Models/` - Data Models
Data transfer objects and entity models.

**Files:**
- `AuthModels.cs` - Authentication DTOs
- `CareerModels.cs` - Career-related models
- `ChatModels.cs` - Chat DTOs
- `JobModels.cs` - Job-related models
- `User.cs` - User entity

### `/Filters/` - Global Filters
ASP.NET Core filters for cross-cutting concerns.

**Files:**
- `GlobalExceptionFilter.cs` - Global exception handling

### `/sql/` - Database Scripts (Organized)

#### `/sql/migrations/` - Schema Changes
Database schema migrations and updates.

**Purpose:** Version-controlled database changes
**Files:**
- `01_job_tables_migration.sql`
- `add_performance_indexes.sql`
- `add_transcript_column.sql`
- `admin_module_migration.sql`
- `fix_duplicates.sql`
- `fix_firebase_video.sql`
- `fix_profile_procedures.sql`
- `fix_quiz_sessions_table.sql`
- `fix_saved_jobs_currency.sql`
- `update_careers_table.sql`
- `update_chat_tables.sql`
- `update_quiz_system.sql`
- `safe_migration.sql`
- `RUN_ALL_MIGRATIONS.sql` - Master migration script

#### `/sql/seeds/` - Sample Data
Population scripts for initial data.

**Purpose:** Seed database with sample content
**Files:**
- `populate_careers_freedb.sql`
- `populate_learning_videos_freedb.sql`
- `add_comprehensive_it_careers.sql`

#### `/sql/procedures/` - Stored Procedures
MySQL stored procedures for complex operations.

**Purpose:** Database-level business logic
**Files:**
- `create_procs.sql`
- `safe_procs.sql`
- `QUICK_FIX_PROCEDURES.sql`
- `test_procedure.sql`

#### `/sql/admin/` - Admin Setup
Scripts for admin user and panel setup.

**Purpose:** Admin panel initialization
**Files:**
- `setup_admin.sql`
- `check_admin_user.sql`

#### `/sql/` (Root) - Core Schema
Base table creation scripts.

**Files:**
- `create_career_tables.sql`
- `create_chat_tables.sql`
- `create_learning_videos_table.sql`
- `create_progress_tables.sql`
- `create_refresh_tokens.sql`
- `create_users_and_profiles.sql`
- `database_schema_complete.sql` - Full schema
- `SETUP_ALL_TABLES.sql` - Master setup script
- `SETUP_GUIDE.md` - SQL setup documentation

### `/scripts/` - Automation Scripts

#### `/scripts/admin/` - Admin Panel Setup
PowerShell scripts for admin operations.

**Purpose:** Automate admin panel setup and testing
**Files:**
- `create-admin-user.ps1` - Create admin user via API
- `setup-admin-db.ps1` - Database setup
- `setup-admin-module.ps1` - Full admin setup
- `setup-admin-quick.ps1` - Quick setup
- `setup-admin.ps1` - Standard setup
- `setup-admin.bat` - Batch wrapper
- `test-admin-api.ps1` - Comprehensive API tests
- `test-admin-endpoints.ps1` - Endpoint testing
- `test-admin.ps1` - Admin panel tests
- `test-endpoints.ps1` - General endpoint tests
- `test-simple.ps1` - Simple API tests

#### `/scripts/deployment/` - Deployment
Scripts for deploying to various platforms.

**Purpose:** Automated deployment
**Files:**
- `deploy-to-azure-ftp.ps1` - Azure FTP deployment

### `/docs/` - Documentation (Organized)

#### `/docs/guides/` - User Guides
General guides and setup instructions.

**Purpose:** User-facing documentation
**Topics:**
- Database setup
- Video management
- Transcript management
- Web admin guide
- Token refresh
- Testing instructions
- Backend review
- Skill-based quiz
- Debugging
- General setup guides

#### `/docs/admin/` - Admin Documentation
Admin panel specific documentation.

**Purpose:** Admin user documentation
**Topics:**
- Complete admin guide
- Quick reference
- Quick setup
- Processing summary
- Setup completion

#### `/docs/deployment/` - Deployment Guides
Deployment instructions for various platforms.

**Purpose:** Production deployment
**Topics:**
- Azure deployment
- WinSCP deployment

#### `/docs/implementation/` - Technical Docs
Implementation notes and technical details.

**Purpose:** Developer technical reference
**Topics:**
- AI chatbot implementation
- Flutter integration
- Chat history implementation
- Crashlytics implementation
- Code reviews
- Connection fixes
- Groq API fixes
- Flutter dev troubleshooting
- Implementation completion notes

### `/wwwroot/` - Static Web Files
Public web files served by the application.

**Files:**
- `admin.html` - Admin panel single-page application
- `admin/` - (if exists) Admin assets

### `/Properties/` - Project Properties
Visual Studio/MSBuild project properties.

**Files:**
- `launchSettings.json` - Development launch settings

## 🎯 Organization Principles

### 1. Separation of Concerns
- **Controllers**: HTTP layer only
- **Services**: Business logic
- **Models**: Data structures
- **SQL**: Database layer

### 2. Documentation Organization
- **guides/**: End-user documentation
- **admin/**: Admin-specific docs
- **deployment/**: Operations docs
- **implementation/**: Developer docs

### 3. Script Organization
- **admin/**: Admin panel automation
- **deployment/**: Production deployment

### 4. SQL Organization
- **migrations/**: Schema changes (versioned)
- **seeds/**: Initial data
- **procedures/**: Stored procedures
- **admin/**: Admin setup
- **root**: Core schema definitions

## 📝 File Naming Conventions

### Controllers
- Pattern: `{Feature}Controller.cs`
- Example: `AuthController.cs`, `JobsController.cs`

### Services
- Pattern: `{Feature}Service.cs`
- Example: `DatabaseService.cs`, `GroqService.cs`

### Models
- Pattern: `{Feature}Models.cs` or `{Entity}.cs`
- Example: `AuthModels.cs`, `User.cs`

### SQL Migrations
- Pattern: `{action}_{description}.sql`
- Examples: 
  - `add_transcript_column.sql`
  - `fix_duplicates.sql`
  - `update_careers_table.sql`

### SQL Seeds
- Pattern: `populate_{table}_freedb.sql`
- Example: `populate_careers_freedb.sql`

### Scripts
- Pattern: `{action}-{feature}.ps1`
- Examples:
  - `setup-admin-quick.ps1`
  - `test-admin-api.ps1`
  - `deploy-to-azure-ftp.ps1`

### Documentation
- Pattern: `{FEATURE}_{TYPE}.md`
- Examples:
  - `ADMIN_MODULE_GUIDE.md`
  - `AZURE_DEPLOYMENT_GUIDE.md`
  - `DATABASE_SETUP.md`

## 🔍 Finding Files

### By Purpose

**Need to:** → **Look in:**
- Add API endpoint → `Controllers/`
- Add business logic → `Services/`
- Change database schema → `sql/migrations/`
- Add sample data → `sql/seeds/`
- Setup admin panel → `scripts/admin/`
- Deploy application → `scripts/deployment/`
- Read user guide → `docs/guides/`
- Read admin docs → `docs/admin/`
- Check deployment → `docs/deployment/`
- Technical details → `docs/implementation/`

### By Feature

**Feature** → **Related Files:**
- **Authentication**: `AuthController.cs`, `AuthModels.cs`, `sql/create_users_and_profiles.sql`
- **Admin Panel**: `AdminController.cs`, `wwwroot/admin.html`, `sql/admin/`, `scripts/admin/`, `docs/admin/`
- **Videos**: `LearningVideosController.cs`, `sql/create_learning_videos_table.sql`, `docs/guides/VIDEO_MANAGEMENT_GUIDE.md`
- **Quiz**: `QuizController.cs`, `GroqService.cs`, `sql/migrations/update_quiz_system.sql`
- **Jobs**: `JobsController.cs`, `JobApiService.cs`, `JobDatabaseService.cs`, `sql/migrations/01_job_tables_migration.sql`
- **Chat**: `ChatController.cs`, `ChatHistoryController.cs`, `sql/create_chat_tables.sql`

## 🚫 Ignored Files (.gitignore)

The following are excluded from version control:
- `bin/`, `obj/`, `publish/` - Build outputs
- `.vs/`, `.vscode/` (except extensions) - IDE settings
- `appsettings.Development.json` - Local secrets
- `*.user`, `*.suo` - User-specific files
- `*.log` - Log files
- `*.bak`, `*.old` - Backup files

## 📊 Folder Statistics

- **Controllers**: 14 files
- **Services**: 6 files
- **Models**: 5 files
- **SQL Migrations**: 15+ files
- **SQL Seeds**: 3 files
- **SQL Procedures**: 4 files
- **Admin Scripts**: 11 files
- **Deployment Scripts**: 1 file
- **Documentation**: 80+ files (organized into 4 categories)

## ✨ Benefits of This Structure

1. **Easy Navigation**: Find files quickly by purpose
2. **Clean Separation**: Code, data, scripts, and docs separated
3. **Version Control**: Logical organization for Git
4. **Team Collaboration**: Clear ownership of folders
5. **Maintainability**: Easy to add new features
6. **Documentation**: Always know where to look
7. **Deployment**: Scripts organized by target
8. **Testing**: Test scripts in dedicated location

## 🔄 Maintenance

### Adding New Features
1. Create controller in `Controllers/`
2. Add service in `Services/`
3. Create model in `Models/`
4. Add migration in `sql/migrations/`
5. Document in `docs/guides/`

### Updating Database
1. Create migration in `sql/migrations/`
2. Update `RUN_ALL_MIGRATIONS.sql`
3. Test with test database
4. Document in `docs/guides/DATABASE_SETUP.md`

### Adding Documentation
1. User guides → `docs/guides/`
2. Admin docs → `docs/admin/`
3. Deployment → `docs/deployment/`
4. Technical → `docs/implementation/`

---

**Last Updated**: January 13, 2026
**Structure Version**: 2.0 (Clean Code Organization)
