-- ============================================
-- Performance Indexes for Career Guidance App
-- ============================================
-- Run this file to add critical missing indexes
-- Estimated improvement: 50-70% query speed increase
-- Note: Ignore "Duplicate key name" errors if indexes already exist

-- Select the database
USE freedb_career_guidence;

-- ============================================
-- 1. RECOMMENDATIONS TABLE
-- ============================================
-- Most frequently queried by user_id
CREATE INDEX idx_recommendations_user_id 
ON recommendations(user_id);

-- Compound index for user_id + match_percentage (used in ORDER BY)
CREATE INDEX idx_recommendations_user_match 
ON recommendations(user_id, match_percentage DESC);

-- ============================================
-- 2. QUIZ_SESSIONS TABLE
-- ============================================
-- Frequently queried by user_id and completion status
CREATE INDEX idx_quiz_sessions_user_id 
ON quiz_sessions(user_id);

-- Compound index for user_id + completed (filtering completed quizzes)
CREATE INDEX idx_quiz_sessions_user_completed 
ON quiz_sessions(user_id, completed);

-- Index on quiz_id for quick lookups
CREATE INDEX idx_quiz_sessions_quiz_id 
ON quiz_sessions(quiz_id);

-- ============================================
-- 3. CHATSESSIONS TABLE
-- ============================================
-- User's chat sessions ordered by recent activity
CREATE INDEX idx_chatsessions_user_updated 
ON chatsessions(UserId, UpdatedAt DESC);

-- SessionId lookups
CREATE INDEX idx_chatsessions_session_id 
ON chatsessions(SessionId);

-- ============================================
-- 4. CHATMESSAGES TABLE
-- ============================================
-- Messages for a session ordered by timestamp
CREATE INDEX idx_chatmessages_session_created 
ON chatmessages(SessionId, CreatedAt ASC);

-- User's messages across all sessions
CREATE INDEX idx_chatmessages_user_created 
ON chatmessages(UserId, CreatedAt DESC);

-- ============================================
-- 5. COURSE_PROGRESS TABLE
-- ============================================
-- Compound index for user + career + completion queries
CREATE INDEX idx_course_progress_user_career 
ON course_progress(user_id, career_name);

-- Index for finding incomplete courses
CREATE INDEX idx_course_progress_incomplete 
ON course_progress(user_id, is_completed, last_watched DESC);

-- ============================================
-- 6. USER_CAREER_PROGRESS TABLE
-- ============================================
-- Active career progress for user
CREATE INDEX idx_career_progress_user_active 
ON user_career_progress(user_id, is_active, last_accessed DESC);

-- ============================================
-- 7. LEARNING_PATH_PROGRESS TABLE
-- ============================================
-- Compound index for user + career + skill lookups
CREATE INDEX idx_learning_path_user_career_skill 
ON learning_path_progress(user_id, career_name, skill_name);

-- ============================================
-- 8. VIDEO_WATCH_HISTORY TABLE
-- ============================================
-- User's watch history ordered by recent
CREATE INDEX idx_video_watch_user_recent 
ON video_watch_history(user_id, last_watched DESC);

-- Compound index for user + career + skill
CREATE INDEX idx_video_watch_user_career_skill 
ON video_watch_history(user_id, career_name, skill_name);

-- ============================================
-- 9. USER_RESUMES TABLE
-- ============================================
-- Unique index already exists on user_id (UNIQUE KEY)
-- No additional index needed

-- ============================================
-- 10. CAREERS TABLE (READ-HEAVY)
-- ============================================
-- Add covering index for career list endpoint
CREATE INDEX idx_careers_name_id 
ON careers(career_name, id);

-- ============================================
-- 11. JOB TABLES (NEW - CRITICAL FOR JOB FINDER)
-- ============================================
-- Saved jobs - compound index for user lookups
CREATE INDEX idx_saved_jobs_user_job 
ON saved_jobs(user_id, job_id);

-- Job applications - compound index for user lookups
CREATE INDEX idx_job_applications_user_job 
ON job_applications(user_id, job_id);

-- Saved jobs - index for recent saves query
CREATE INDEX idx_saved_jobs_user_date 
ON saved_jobs(user_id, saved_at DESC);

-- ============================================
-- VERIFY INDEXES CREATED
-- ============================================
-- Run these queries to verify indexes were created:

-- SELECT 
--     TABLE_NAME,
--     INDEX_NAME,
--     COLUMN_NAME,
--     INDEX_TYPE
-- FROM 
--     information_schema.STATISTICS
-- WHERE 
--     TABLE_SCHEMA = 'freedb_career_guidence'
--     AND TABLE_NAME IN (
--         'recommendations', 'quiz_sessions', 'chatsessions', 
--         'chatmessages', 'course_progress', 'user_career_progress'
--     )
-- ORDER BY 
--     TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX;

-- ============================================
-- EXPECTED IMPROVEMENTS
-- ============================================
-- Before: Sequential scans on large tables (200-400ms)
-- After: Index seeks (5-20ms)
-- Overall API improvement: 50-70% faster for data-heavy endpoints

-- ============================================
-- MONITORING
-- ============================================
-- After deploying, monitor query performance:
-- 1. Check slow query log (if available)
-- 2. Use EXPLAIN on problematic queries
-- 3. Monitor API response times via logging
-- 4. Track database CPU and connection count

-- Example EXPLAIN query:
-- EXPLAIN SELECT r.id, r.career_id, c.career_name, r.match_percentage
-- FROM recommendations r
-- JOIN careers c ON r.career_id = c.id
-- WHERE r.user_id = 1
-- ORDER BY r.match_percentage DESC;
-- 
-- Should show "type: ref" and "Using index" in Extra column
