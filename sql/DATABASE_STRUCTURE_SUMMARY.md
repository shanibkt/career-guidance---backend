# Database Structure Summary

## Complete Table List (19 Tables)

### 1. Core User Tables (3 tables)
- **Users** - Main user authentication
  - Columns: Id, Username, FullName, Email, PasswordHash, Role, CreatedAt, UpdatedAt
- **UserProfiles** - Extended user profile data
  - Columns: Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills (JSON), AreasOfInterest, career_path, ProfileImagePath, CreatedAt, UpdatedAt
- **refresh_tokens** - JWT refresh tokens
  - Columns: id, user_id, token, expires_at, created_at, revoked

### 2. Career & Quiz Tables (3 tables)
- **careers** - Available career options
  - Columns: id, name, description, required_education, average_salary, growth_outlook, key_skills (JSON), created_at, updated_at
- **quiz_sessions** - User quiz attempts
  - Columns: id, user_id, questions (JSON), answers (JSON), completed, created_at, completed_at
- **recommendations** - Career recommendations based on quiz
  - Columns: id, user_id, career_id, match_percentage, reasoning, strengths (JSON), areas_to_develop (JSON), created_at, updated_at

### 3. Chat Tables (2 tables)
- **ChatSessions** - AI chatbot sessions
  - Columns: Id (UUID), UserId, Title, CreatedAt, UpdatedAt
- **ChatMessages** - Chat message history
  - Columns: Id, SessionId, Role, Content, CreatedAt

### 4. Learning & Progress Tables (5 tables)
- **learning_videos** - Available learning video content
  - Columns: id, skill_name, video_title, video_description, youtube_video_id, duration_minutes, thumbnail_url, created_at, updated_at
- **user_career_progress** - Overall career selection and progress
  - Columns: id, user_id, career_id, career_name, required_skills (JSON), selected_at, is_active, overall_progress, completed_courses, total_courses, last_accessed
- **course_progress** - Individual video/course progress
  - Columns: id, user_id, career_name, course_id, skill_name, video_title, youtube_video_id, watched_percentage, watch_time_seconds, total_duration_seconds, is_completed, started_at, completed_at, last_watched
- **learning_path_progress** - Skill-level progress tracking
  - Columns: id, user_id, career_name, skill_name, total_videos, completed_videos, total_duration_seconds, watched_duration_seconds, progress_percentage, started_at, last_accessed
- **video_watch_history** - Detailed video watch tracking
  - Columns: id, user_id, career_name, skill_name, video_id, video_title, current_position_seconds, duration_seconds, watch_percentage, is_completed, first_watched, last_watched, watch_count

### 5. Job Search Tables (4 tables)
- **saved_jobs** - User-saved job listings
  - Columns: id, user_id, job_id, title, company, location, url, description, job_type, salary_min, salary_max, salary_currency, experience_level, required_skills (JSON), posted_date, saved_at
- **job_applications** - Jobs applied to by user
  - Columns: id, user_id, job_id, title, company, location, cover_letter, application_status, applied_at, updated_at, notes
- **job_search_history** - Search history for analytics
  - Columns: id, user_id, search_query, location, job_type, experience_level, searched_at
- **job_recommendations** - AI-generated job recommendations
  - Columns: id, user_id, career_id, job_id, title, company, location, match_percentage, recommendation_reason, viewed_at, dismissed_at, created_at

### 6. Resume Tables (2 tables)
- **user_resumes** - User resume data
  - Columns: id, user_id, full_name, job_title, email, phone, location, linkedin, professional_summary, skills (JSON), experiences (JSON), education (JSON), created_at, updated_at
- **resume_export_history** - Resume export tracking
  - Columns: id, user_id, resume_id, export_format, file_path, exported_at

## Stored Procedures (9 procedures)

1. **sp_create_user** - Create new user account
2. **sp_get_user_by_email** - Retrieve user by email (login)
3. **sp_get_user_by_id** - Retrieve user by ID
4. **sp_update_user** - Update user information
5. **sp_delete_user** - Delete user account
6. **sp_create_or_update_profile** - Create/update user profile
7. **sp_get_profile_by_userid** - Get user profile data
8. **GetUserLearningAnalytics** - Get complete learning progress
9. **UpdateLearningPathProgress** - Update skill progress automatically

## Database Views (2 views)

1. **vw_user_learning_dashboard** - Aggregated learning dashboard data
2. **vw_resume_completion** - Resume completion percentage

## Triggers (2 triggers)

1. **trg_update_learning_path_after_video** - Auto-update progress on video update
2. **trg_insert_learning_path_after_video** - Auto-update progress on new video

## Key Relationships

```
Users (1) ----< (Many) UserProfiles
Users (1) ----< (Many) refresh_tokens
Users (1) ----< (Many) quiz_sessions
Users (1) ----< (Many) recommendations
Users (1) ----< (Many) ChatSessions
Users (1) ----< (Many) user_career_progress
Users (1) ----< (Many) course_progress
Users (1) ----< (Many) saved_jobs
Users (1) ----< (Many) job_applications
Users (1) ----< (Many) user_resumes

ChatSessions (1) ----< (Many) ChatMessages
careers (1) ----< (Many) recommendations
careers (1) ----< (Many) job_recommendations
user_resumes (1) ----< (Many) resume_export_history
```

## Initial Data Included

- **10 Career Options** - Software Engineer, Data Scientist, UX/UI Designer, Product Manager, DevOps Engineer, Cybersecurity Analyst, Marketing Manager, Financial Analyst, Mechanical Engineer, Teacher/Educator
- **30+ Learning Videos** - Sample videos for popular skills (Python, Java, JavaScript, React, SQL, Docker, AWS, Machine Learning, etc.)

## How to Use

1. **Create your new database:**
   ```sql
   CREATE DATABASE your_database_name;
   USE your_database_name;
   ```

2. **Run the complete recreation script:**
   ```sql
   SOURCE COMPLETE_DATABASE_RECREATION.sql;
   ```

3. **Verify tables were created:**
   ```sql
   SHOW TABLES;
   ```

4. **Check sample data:**
   ```sql
   SELECT COUNT(*) FROM careers;
   SELECT COUNT(*) FROM learning_videos;
   ```

## Important Notes

- All tables use `InnoDB` engine with `utf8mb4_unicode_ci` collation
- Foreign keys are set with `ON DELETE CASCADE` for proper cleanup
- Indexes are optimized for common queries
- JSON columns are used for flexible array/object storage
- Timestamps use MySQL CURRENT_TIMESTAMP with auto-update
- All procedures are recreated (DROP IF EXISTS)
