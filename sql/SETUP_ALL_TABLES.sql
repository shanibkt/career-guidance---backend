-- =============================================
-- Quick Database Setup - Run All Missing Tables
-- For freedb_career_guidence database
-- =============================================

USE freedb_career_guidence;

-- 1. Chat Tables (CRITICAL - chatbot won't work without this)
SOURCE create_chat_tables.sql;

-- 2. Career Tables (needed for career suggestions)
SOURCE create_career_tables.sql;

-- 3. Learning Videos (needed for learning path)
SOURCE create_learning_videos_table.sql;

-- 4. Progress Tracking (needed for tracking)
SOURCE create_progress_tables.sql;

-- 5. Job Tables (needed for job search)
SOURCE 01_job_tables_migration.sql;

-- 6. Stored Procedures (CRITICAL - profile won't work without this)
SOURCE safe_procs.sql;

SELECT '✅ All tables and procedures created!' AS Status;
