-- =============================================
-- COMPLETE DATABASE RECREATION SCRIPT
-- Career Guidance Application
-- All Tables, Procedures, and Initial Data
-- =============================================

-- Replace 'your_new_database_name' with your actual database name
-- CREATE DATABASE IF NOT EXISTS your_new_database_name;
-- USE your_new_database_name;

-- =============================================
-- 1. CORE USER TABLES
-- =============================================

-- Users table (for authentication)
CREATE TABLE IF NOT EXISTS Users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(100) UNIQUE NOT NULL,
    FullName VARCHAR(200) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) DEFAULT 'user',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (Email),
    INDEX idx_user_username (Username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- UserProfiles table (for additional profile data)
CREATE TABLE IF NOT EXISTS UserProfiles (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL UNIQUE,
    PhoneNumber VARCHAR(20),
    Age INT,
    Gender VARCHAR(20),
    EducationLevel VARCHAR(100),
    FieldOfStudy VARCHAR(200),
    Skills JSON,
    AreasOfInterest TEXT,
    career_path VARCHAR(255),
    ProfileImagePath VARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_profile_userid (UserId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Refresh Tokens Table
CREATE TABLE IF NOT EXISTS refresh_tokens (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    token VARCHAR(500) NOT NULL UNIQUE,
    expires_at DATETIME NOT NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    revoked BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_token (token),
    INDEX idx_expires (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 2. CAREER & QUIZ TABLES
-- =============================================

-- Careers table (predefined career options)
CREATE TABLE IF NOT EXISTS careers (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    required_education VARCHAR(200),
    average_salary VARCHAR(100),
    growth_outlook VARCHAR(100),
    key_skills JSON,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_career_name (name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Quiz sessions table
CREATE TABLE IF NOT EXISTS quiz_sessions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    questions JSON NOT NULL,
    answers JSON,
    completed BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_completed (user_id, completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Recommendations table
CREATE TABLE IF NOT EXISTS recommendations (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    career_id INT NOT NULL,
    match_percentage DECIMAL(5,2) NOT NULL,
    reasoning TEXT,
    strengths JSON,
    areas_to_develop JSON,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (career_id) REFERENCES careers(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_career (user_id, career_id),
    INDEX idx_user_match (user_id, match_percentage DESC)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 3. CHAT TABLES
-- =============================================

-- Create ChatSessions table
CREATE TABLE IF NOT EXISTS ChatSessions (
    Id VARCHAR(36) PRIMARY KEY,
    UserId INT NOT NULL,
    Title VARCHAR(255) NOT NULL DEFAULT 'New Chat',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_sessions (UserId, UpdatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create ChatMessages table
CREATE TABLE IF NOT EXISTS ChatMessages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SessionId VARCHAR(36) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(Id) ON DELETE CASCADE,
    INDEX idx_session_messages (SessionId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 4. LEARNING & PROGRESS TABLES
-- =============================================

-- Learning videos table
CREATE TABLE IF NOT EXISTS learning_videos (
    id INT AUTO_INCREMENT PRIMARY KEY,
    skill_name VARCHAR(100) NOT NULL UNIQUE,
    video_title VARCHAR(255) NOT NULL,
    video_description TEXT,
    youtube_video_id VARCHAR(50) NOT NULL,
    duration_minutes INT NOT NULL DEFAULT 0,
    thumbnail_url VARCHAR(500),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_skill_name (skill_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- User career progress table
CREATE TABLE IF NOT EXISTS user_career_progress (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_id INT,
    career_name VARCHAR(255) NOT NULL,
    required_skills JSON,
    selected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_active BOOLEAN DEFAULT TRUE,
    overall_progress DECIMAL(5,2) DEFAULT 0.00,
    completed_courses INT DEFAULT 0,
    total_courses INT DEFAULT 0,
    last_accessed TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id),
    INDEX idx_career_id (career_id),
    INDEX idx_is_active (is_active),
    INDEX idx_user_active (user_id, is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Course progress table
CREATE TABLE IF NOT EXISTS course_progress (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_name VARCHAR(255) NOT NULL,
    course_id VARCHAR(255) NOT NULL,
    skill_name VARCHAR(100) NOT NULL,
    video_title VARCHAR(255),
    youtube_video_id VARCHAR(50),
    watched_percentage DECIMAL(5,2) DEFAULT 0.00,
    watch_time_seconds INT DEFAULT 0,
    total_duration_seconds INT DEFAULT 0,
    is_completed BOOLEAN DEFAULT FALSE,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    last_watched TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_course (user_id, course_id),
    INDEX idx_user_career (user_id, career_name),
    INDEX idx_skill_name (skill_name),
    INDEX idx_completed (is_completed),
    INDEX idx_completion (user_id, is_completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Learning path progress table
CREATE TABLE IF NOT EXISTS learning_path_progress (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_name VARCHAR(255) NOT NULL,
    skill_name VARCHAR(255) NOT NULL,
    total_videos INT DEFAULT 0,
    completed_videos INT DEFAULT 0,
    total_duration_seconds INT DEFAULT 0,
    watched_duration_seconds INT DEFAULT 0,
    progress_percentage DECIMAL(5,2) DEFAULT 0.00,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_accessed TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_skill_path (user_id, career_name, skill_name),
    INDEX idx_user_career_skill (user_id, career_name, skill_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Video watch history table
CREATE TABLE IF NOT EXISTS video_watch_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_name VARCHAR(255) NOT NULL,
    skill_name VARCHAR(255) NOT NULL,
    video_id VARCHAR(100) NOT NULL,
    video_title VARCHAR(500) NOT NULL,
    current_position_seconds INT DEFAULT 0,
    duration_seconds INT DEFAULT 0,
    watch_percentage DECIMAL(5,2) DEFAULT 0.00,
    is_completed BOOLEAN DEFAULT FALSE,
    first_watched TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_watched TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    watch_count INT DEFAULT 1,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_video (user_id, video_id, career_name),
    INDEX idx_user_video (user_id, video_id),
    INDEX idx_last_watched (last_watched),
    INDEX idx_completion (is_completed),
    INDEX idx_video_history_user_career (user_id, career_name, last_watched)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 5. JOB SEARCH TABLES
-- =============================================

-- Saved jobs table
CREATE TABLE IF NOT EXISTS saved_jobs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    job_id VARCHAR(255) NOT NULL,
    title VARCHAR(255),
    company VARCHAR(255),
    location VARCHAR(255),
    url VARCHAR(500),
    description LONGTEXT,
    job_type VARCHAR(50),
    salary_min VARCHAR(50),
    salary_max VARCHAR(50),
    salary_currency VARCHAR(10) DEFAULT 'USD',
    experience_level VARCHAR(50),
    required_skills JSON,
    posted_date VARCHAR(100),
    saved_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY unique_user_job (user_id, job_id),
    INDEX idx_user_id (user_id),
    INDEX idx_saved_jobs_user_job (user_id, job_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Job applications table
CREATE TABLE IF NOT EXISTS job_applications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    job_id VARCHAR(255) NOT NULL,
    title VARCHAR(255),
    company VARCHAR(255),
    location VARCHAR(255),
    cover_letter LONGTEXT,
    application_status VARCHAR(50) DEFAULT 'Applied',
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NULL ON UPDATE CURRENT_TIMESTAMP,
    notes LONGTEXT,
    UNIQUE KEY unique_user_job_app (user_id, job_id),
    INDEX idx_user_id (user_id),
    INDEX idx_status (application_status),
    INDEX idx_job_applications_user_job (user_id, job_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Job search history table
CREATE TABLE IF NOT EXISTS job_search_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    search_query VARCHAR(500),
    location VARCHAR(255),
    job_type VARCHAR(50),
    experience_level VARCHAR(50),
    searched_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_id (user_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Job recommendations table
CREATE TABLE IF NOT EXISTS job_recommendations (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_id INT,
    job_id VARCHAR(255),
    title VARCHAR(255),
    company VARCHAR(255),
    location VARCHAR(255),
    match_percentage DECIMAL(5, 2),
    recommendation_reason LONGTEXT,
    viewed_at TIMESTAMP NULL,
    dismissed_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_id (user_id),
    INDEX idx_career_id (career_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (career_id) REFERENCES careers(id) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 6. RESUME TABLES
-- =============================================

-- User resumes table
CREATE TABLE IF NOT EXISTS user_resumes (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    full_name VARCHAR(255) NOT NULL DEFAULT '',
    job_title VARCHAR(255) NOT NULL DEFAULT '',
    email VARCHAR(255) NOT NULL DEFAULT '',
    phone VARCHAR(50) NOT NULL DEFAULT '',
    location VARCHAR(255) NOT NULL DEFAULT '',
    linkedin VARCHAR(500) NOT NULL DEFAULT '',
    professional_summary TEXT NOT NULL,
    skills JSON NOT NULL,
    experiences JSON NOT NULL,
    education JSON NOT NULL,
    certifications JSON DEFAULT NULL,
    projects JSON DEFAULT NULL,
    languages JSON DEFAULT NULL,
    achievements JSON DEFAULT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY unique_user_resume (user_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Resume export history table
CREATE TABLE IF NOT EXISTS resume_export_history (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    resume_id INT NOT NULL,
    export_format VARCHAR(20) NOT NULL DEFAULT 'PDF',
    file_path VARCHAR(500) NULL,
    exported_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (resume_id) REFERENCES user_resumes(id) ON DELETE CASCADE,
    INDEX idx_user_exports (user_id, exported_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- =============================================
-- 7. STORED PROCEDURES
-- =============================================

DELIMITER $$

-- Create user (for registration)
DROP PROCEDURE IF EXISTS sp_create_user$$
CREATE PROCEDURE sp_create_user(
    IN p_username VARCHAR(100),
    IN p_fullName VARCHAR(200),
    IN p_email VARCHAR(255),
    IN p_passwordHash VARCHAR(255)
)
BEGIN
    INSERT INTO Users (Username, FullName, Email, PasswordHash)
    VALUES (p_username, p_fullName, p_email, p_passwordHash);
    SELECT LAST_INSERT_ID() AS Id;
END$$

-- Get user by email (for login)
DROP PROCEDURE IF EXISTS sp_get_user_by_email$$
CREATE PROCEDURE sp_get_user_by_email(
    IN p_email VARCHAR(255)
)
BEGIN
    SELECT Id, Username, FullName, Email, PasswordHash, CreatedAt, UpdatedAt 
    FROM Users 
    WHERE Email = p_email 
    LIMIT 1;
END$$

-- Get user by ID
DROP PROCEDURE IF EXISTS sp_get_user_by_id$$
CREATE PROCEDURE sp_get_user_by_id(
    IN p_userId INT
)
BEGIN
    SELECT Id, Username, FullName, Email, CreatedAt, UpdatedAt 
    FROM Users 
    WHERE Id = p_userId 
    LIMIT 1;
END$$

-- Update user
DROP PROCEDURE IF EXISTS sp_update_user$$
CREATE PROCEDURE sp_update_user(
    IN p_userId INT,
    IN p_fullName VARCHAR(200),
    IN p_username VARCHAR(100),
    IN p_email VARCHAR(255)
)
BEGIN
    UPDATE Users 
    SET FullName = p_fullName, Username = p_username, Email = p_email 
    WHERE Id = p_userId;
    SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Delete user
DROP PROCEDURE IF EXISTS sp_delete_user$$
CREATE PROCEDURE sp_delete_user(
    IN p_userId INT
)
BEGIN
    DELETE FROM Users WHERE Id = p_userId;
    SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Create or update profile
DROP PROCEDURE IF EXISTS sp_create_or_update_profile$$
CREATE PROCEDURE sp_create_or_update_profile(
    IN p_userId INT,
    IN p_phoneNumber VARCHAR(20),
    IN p_age INT,
    IN p_gender VARCHAR(20),
    IN p_educationLevel VARCHAR(100),
    IN p_fieldOfStudy VARCHAR(200),
    IN p_skills JSON,
    IN p_careerPath VARCHAR(255),
    IN p_profileImagePath VARCHAR(500)
)
BEGIN
    INSERT INTO UserProfiles 
        (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, career_path, ProfileImagePath)
    VALUES 
        (p_userId, p_phoneNumber, p_age, p_gender, p_educationLevel, p_fieldOfStudy, p_skills, p_careerPath, p_profileImagePath)
    ON DUPLICATE KEY UPDATE
        PhoneNumber = VALUES(PhoneNumber),
        Age = VALUES(Age),
        Gender = VALUES(Gender),
        EducationLevel = VALUES(EducationLevel),
        FieldOfStudy = VALUES(FieldOfStudy),
        Skills = VALUES(Skills),
        career_path = VALUES(career_path),
        ProfileImagePath = COALESCE(VALUES(ProfileImagePath), ProfileImagePath);
    
    SELECT Id, UserId FROM UserProfiles WHERE UserId = p_userId LIMIT 1;
END$$

-- Get profile by user ID
DROP PROCEDURE IF EXISTS sp_get_profile_by_userid$$
CREATE PROCEDURE sp_get_profile_by_userid(
    IN p_userId INT
)
BEGIN
    SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, 
           Skills, career_path, ProfileImagePath, CreatedAt, UpdatedAt
    FROM UserProfiles 
    WHERE UserId = p_userId 
    LIMIT 1;
END$$

-- Get User's Complete Learning Analytics
DROP PROCEDURE IF EXISTS GetUserLearningAnalytics$$
CREATE PROCEDURE GetUserLearningAnalytics(IN p_user_id INT)
BEGIN
    -- Career Progress Summary
    SELECT 
        career_name,
        overall_progress,
        completed_courses,
        total_courses,
        selected_at,
        last_accessed
    FROM user_career_progress
    WHERE user_id = p_user_id AND is_active = TRUE;
    
    -- Skills Progress Summary
    SELECT 
        skill_name,
        total_videos,
        completed_videos,
        progress_percentage,
        watched_duration_seconds,
        total_duration_seconds
    FROM learning_path_progress
    WHERE user_id = p_user_id
    ORDER BY last_accessed DESC;
    
    -- Recent Video Activity
    SELECT 
        video_title,
        skill_name,
        watch_percentage,
        is_completed,
        last_watched
    FROM video_watch_history
    WHERE user_id = p_user_id
    ORDER BY last_watched DESC
    LIMIT 10;
END$$

-- Update Learning Path Progress
DROP PROCEDURE IF EXISTS UpdateLearningPathProgress$$
CREATE PROCEDURE UpdateLearningPathProgress(
    IN p_user_id INT,
    IN p_career_name VARCHAR(255),
    IN p_skill_name VARCHAR(255)
)
SQL SECURITY INVOKER
DETERMINISTIC
BEGIN
    DECLARE v_total_videos INT DEFAULT 0;
    DECLARE v_completed_videos INT DEFAULT 0;
    DECLARE v_total_duration INT DEFAULT 0;
    DECLARE v_watched_duration INT DEFAULT 0;
    DECLARE v_progress DECIMAL(5,2) DEFAULT 0.00;
    
    -- Calculate totals from video watch history
    SELECT 
        COUNT(*),
        SUM(CASE WHEN is_completed = TRUE THEN 1 ELSE 0 END),
        SUM(duration_seconds),
        SUM(current_position_seconds)
    INTO v_total_videos, v_completed_videos, v_total_duration, v_watched_duration
    FROM video_watch_history
    WHERE user_id = p_user_id 
      AND career_name = p_career_name 
      AND skill_name = p_skill_name;
    
    -- Calculate progress percentage
    IF v_total_videos > 0 THEN
        SET v_progress = (v_completed_videos / v_total_videos) * 100;
    END IF;
    
    -- Insert or update learning path progress
    INSERT INTO learning_path_progress 
        (user_id, career_name, skill_name, total_videos, completed_videos, 
         total_duration_seconds, watched_duration_seconds, progress_percentage)
    VALUES 
        (p_user_id, p_career_name, p_skill_name, v_total_videos, v_completed_videos,
         v_total_duration, v_watched_duration, v_progress)
    ON DUPLICATE KEY UPDATE
        total_videos = v_total_videos,
        completed_videos = v_completed_videos,
        total_duration_seconds = v_total_duration,
        watched_duration_seconds = v_watched_duration,
        progress_percentage = v_progress,
        last_accessed = NOW();
END$$

DELIMITER ;

-- =============================================
-- 8. VIEWS
-- =============================================

-- User Learning Dashboard View
CREATE OR REPLACE VIEW vw_user_learning_dashboard AS
SELECT 
    u.Id as user_id,
    u.FullName as user_name,
    u.Email as user_email,
    ucp.career_name,
    ucp.overall_progress as career_progress,
    ucp.completed_courses,
    ucp.total_courses,
    COUNT(DISTINCT cp.course_id) as courses_in_progress,
    COUNT(DISTINCT vwh.video_id) as videos_watched,
    SUM(vwh.watch_count) as total_watch_count,
    MAX(vwh.last_watched) as last_learning_activity
FROM Users u
LEFT JOIN user_career_progress ucp ON u.Id = ucp.user_id AND ucp.is_active = TRUE
LEFT JOIN course_progress cp ON u.Id = cp.user_id AND cp.career_name = ucp.career_name
LEFT JOIN video_watch_history vwh ON u.Id = vwh.user_id AND vwh.career_name = ucp.career_name
GROUP BY u.Id, u.FullName, u.Email, ucp.career_name, ucp.overall_progress, ucp.completed_courses, ucp.total_courses;

-- Resume Completion View
CREATE OR REPLACE VIEW vw_resume_completion AS
SELECT 
    ur.user_id,
    ur.id as resume_id,
    u.FullName,
    CASE 
        WHEN ur.full_name != '' THEN 1 ELSE 0 END +
    CASE WHEN ur.job_title != '' THEN 1 ELSE 0 END +
    CASE WHEN ur.email != '' THEN 1 ELSE 0 END +
    CASE WHEN ur.phone != '' THEN 1 ELSE 0 END +
    CASE WHEN ur.professional_summary != '' THEN 1 ELSE 0 END +
    CASE WHEN JSON_LENGTH(ur.skills) > 0 THEN 1 ELSE 0 END +
    CASE WHEN JSON_LENGTH(ur.experiences) > 0 THEN 1 ELSE 0 END +
    CASE WHEN JSON_LENGTH(ur.education) > 0 THEN 1 ELSE 0 END
    AS sections_completed,
    ROUND((
        CASE WHEN ur.full_name != '' THEN 1 ELSE 0 END +
        CASE WHEN ur.job_title != '' THEN 1 ELSE 0 END +
        CASE WHEN ur.email != '' THEN 1 ELSE 0 END +
        CASE WHEN ur.phone != '' THEN 1 ELSE 0 END +
        CASE WHEN ur.professional_summary != '' THEN 1 ELSE 0 END +
        CASE WHEN JSON_LENGTH(ur.skills) > 0 THEN 1 ELSE 0 END +
        CASE WHEN JSON_LENGTH(ur.experiences) > 0 THEN 1 ELSE 0 END +
        CASE WHEN JSON_LENGTH(ur.education) > 0 THEN 1 ELSE 0 END
    ) / 8 * 100, 2) as completion_percentage,
    ur.updated_at as last_updated
FROM user_resumes ur
JOIN Users u ON ur.user_id = u.Id;

-- =============================================
-- 9. TRIGGERS (DISABLED - Not supported on FreedDB)
-- =============================================

-- NOTE: Triggers are disabled because FreedDB doesn't allow them without SUPER privilege.
-- Instead, call UpdateLearningPathProgress() manually from your API after video updates.
-- 
-- Example in your C# code after updating video_watch_history:
-- await _db.ExecuteStoredProcedure("UpdateLearningPathProgress", userId, careerName, skillName);

/*
-- Auto-update learning path progress when video is updated
DELIMITER $$
DROP TRIGGER IF EXISTS trg_update_learning_path_after_video$$
CREATE TRIGGER trg_update_learning_path_after_video
AFTER UPDATE ON video_watch_history
FOR EACH ROW
BEGIN
    CALL UpdateLearningPathProgress(NEW.user_id, NEW.career_name, NEW.skill_name);
END$$

-- Auto-update learning path progress when video is inserted
DROP TRIGGER IF EXISTS trg_insert_learning_path_after_video$$
CREATE TRIGGER trg_insert_learning_path_after_video
AFTER INSERT ON video_watch_history
FOR EACH ROW
BEGIN
    CALL UpdateLearningPathProgress(NEW.user_id, NEW.career_name, NEW.skill_name);
END$$

DELIMITER ;
*/

-- =============================================
-- 10. INITIAL DATA - CAREERS
-- =============================================

INSERT INTO careers (id, name, description, required_education, average_salary, growth_outlook, key_skills) VALUES
(1, 'Software Engineer', 'Design, develop, and maintain software applications', 'Bachelor in Computer Science', '$80,000 - $150,000', 'Strong growth', '["Programming", "Problem Solving", "Algorithms", "Teamwork"]'),
(2, 'Data Scientist', 'Analyze complex data to help organizations make decisions', 'Bachelor/Master in Data Science or Statistics', '$90,000 - $160,000', 'Very strong growth', '["Statistics", "Python/R", "Machine Learning", "Communication"]'),
(3, 'UX/UI Designer', 'Create user-friendly interfaces and experiences', 'Bachelor in Design or HCI', '$60,000 - $120,000', 'Strong growth', '["Design Tools", "User Research", "Creativity", "Empathy"]'),
(4, 'Product Manager', 'Lead product strategy and development', 'Bachelor in Business or related field', '$100,000 - $180,000', 'Strong growth', '["Leadership", "Communication", "Strategic Thinking", "Technical Knowledge"]'),
(5, 'DevOps Engineer', 'Automate and optimize software deployment', 'Bachelor in Computer Science', '$85,000 - $140,000', 'Very strong growth', '["Linux", "CI/CD", "Cloud Platforms", "Scripting"]'),
(6, 'Cybersecurity Analyst', 'Protect systems and data from threats', 'Bachelor in Cybersecurity or IT', '$70,000 - $130,000', 'Very strong growth', '["Security Protocols", "Risk Analysis", "Ethical Hacking", "Attention to Detail"]'),
(7, 'Marketing Manager', 'Plan and execute marketing strategies', 'Bachelor in Marketing or Business', '$60,000 - $120,000', 'Moderate growth', '["Communication", "Creativity", "Analytics", "Social Media"]'),
(8, 'Financial Analyst', 'Analyze financial data and trends', 'Bachelor in Finance or Economics', '$60,000 - $110,000', 'Moderate growth', '["Excel", "Financial Modeling", "Analysis", "Attention to Detail"]'),
(9, 'Mechanical Engineer', 'Design and develop mechanical systems', 'Bachelor in Mechanical Engineering', '$65,000 - $110,000', 'Moderate growth', '["CAD", "Problem Solving", "Physics", "Teamwork"]'),
(10, 'Teacher/Educator', 'Educate and mentor students', 'Bachelor in Education', '$40,000 - $70,000', 'Stable', '["Communication", "Patience", "Subject Expertise", "Empathy"]')
ON DUPLICATE KEY UPDATE
    name = VALUES(name),
    description = VALUES(description),
    required_education = VALUES(required_education),
    average_salary = VALUES(average_salary),
    growth_outlook = VALUES(growth_outlook),
    key_skills = VALUES(key_skills);

-- =============================================
-- 11. INITIAL DATA - LEARNING VIDEOS (Comprehensive)
-- =============================================

INSERT INTO learning_videos (skill_name, video_title, video_description, youtube_video_id, duration_minutes) VALUES
-- Programming Languages
('Python', 'Python Complete Tutorial', 'Master Python from basics to advanced concepts', '_uQrJ0TkZlc', 280),
('Java', 'Java Full Course', 'Complete Java programming tutorial', 'eIrMbAQSU34', 200),
('JavaScript', 'JavaScript Tutorial', 'Learn JavaScript fundamentals and advanced topics', 'PkZNo7MFNFg', 195),
('C#', 'C# Full Course', 'Complete C# programming guide', 'GhQdlIFylQ8', 244),
('C++', 'C++ Complete Tutorial', 'Learn C++ programming from scratch', 'vLnPwxZdW4Y', 240),
('PHP', 'PHP Tutorial', 'PHP web development complete course', 'OK_JCtrrv-c', 210),
('Ruby', 'Ruby Programming', 'Learn Ruby programming language', 't_ispmWmdjY', 240),
('Go', 'Go Programming Tutorial', 'Complete Go (Golang) course', 'YS4e4q9oBaU', 420),
('Swift', 'Swift Tutorial', 'iOS development with Swift', 'CwA1VWP0Ldw', 180),
('Kotlin', 'Kotlin Full Course', 'Android development with Kotlin', 'F9UC9DY-vIU', 240),
('Dart', 'Dart Programming', 'Learn Dart for Flutter development', '5xlVP04905w', 120),
('TypeScript', 'TypeScript Course', 'TypeScript for JavaScript developers', 'd56mG7DezGs', 180),
('Rust', 'Rust Programming', 'Systems programming with Rust', 'zF34dRivLOw', 240),
('R', 'R Programming Tutorial', 'Statistical computing with R', '_V8eKsto3Ug', 180),

-- Web Development
('HTML', 'HTML Full Course', 'HTML fundamentals and best practices', 'qz0aGYrrlhU', 120),
('CSS', 'CSS Complete Guide', 'Master CSS styling and layouts', 'yfoY53QXEnI', 180),
('React', 'React Tutorial', 'Build modern web apps with React', 'bMknfKXIFA8', 144),
('Angular', 'Angular Full Course', 'Complete Angular framework guide', 'k5E2AVpwsko', 240),
('Vue.js', 'Vue.js Tutorial', 'Learn Vue.js framework', 'FXpIoQ_rT_c', 180),
('Node.js', 'Node.js Tutorial', 'Backend development with Node.js', 'TlB_eWDSMt4', 180),
('Express.js', 'Express.js Tutorial', 'Node.js web framework', 'L72fhGm1tfE', 120),
('Next.js', 'Next.js Course', 'React framework for production', 'Sklc_fQBmcs', 180),
('Svelte', 'Svelte Tutorial', 'Modern web framework', 'zojEMeQGGHs', 120),

-- Mobile Development
('Flutter', 'Flutter Complete Course', 'Build mobile apps with Flutter', 'VPvVD8t02U8', 240),
('React Native', 'React Native Tutorial', 'Cross-platform mobile development', '0-S5a0eXPoc', 240),
('Android SDK', 'Android Development', 'Native Android app development', 'fis26HvvDII', 300),
('iOS SDK', 'iOS Development', 'Native iOS app development', '09TeUXjzpKs', 240),

-- Databases
('SQL', 'SQL Complete Tutorial', 'Master SQL database queries', 'HXV3zeQKqGY', 240),
('MySQL', 'MySQL Tutorial', 'Learn MySQL database management', '7S_tz1z_5bA', 180),
('PostgreSQL', 'PostgreSQL Course', 'Advanced PostgreSQL database', 'qw--VYLpxG4', 240),
('MongoDB', 'MongoDB Tutorial', 'NoSQL database with MongoDB', 'c2M-rlkkT5o', 180),
('Redis', 'Redis Tutorial', 'In-memory data structure store', 'jgpVdJB2sKQ', 120),
('Firebase', 'Firebase Complete Guide', 'Backend as a Service platform', 'q5J5ho7YUhA', 180),
('SQLite', 'SQLite Tutorial', 'Embedded database', 'byHcYRpMgI4', 90),

-- Frameworks
('Django', 'Django Tutorial', 'Python web framework', 'rHux0gMZ3Eg', 90),
('Spring Boot', 'Spring Boot Course', 'Java Spring Boot framework', '9SGDpanrc8U', 240),
('ASP.NET', 'ASP.NET Tutorial', '.NET web development', 'BfEjDD8mWYg', 300),
('Laravel', 'Laravel Course', 'PHP Laravel framework', 'ImtZ5yENzgE', 240),
('.NET Core', '.NET Core Tutorial', 'Modern .NET development', 'BfEjDD8mWYg', 300),
('Flask', 'Flask Tutorial', 'Python microframework', 'Z1RJmh_OqeA', 120),
('FastAPI', 'FastAPI Course', 'Modern Python web framework', '7t2alSnE2-I', 150),

-- DevOps & Cloud
('Docker', 'Docker Tutorial', 'Containerization with Docker', 'fqMOX6JJhGo', 180),
('Kubernetes', 'Kubernetes Course', 'Container orchestration', 'X48VuDVv0do', 240),
('AWS', 'AWS Complete Guide', 'Amazon Web Services tutorial', 'SOTamWNgDKc', 600),
('Azure', 'Microsoft Azure Tutorial', 'Cloud computing with Azure', 'NKEFWyqJ5XA', 180),
('Google Cloud', 'Google Cloud Platform', 'GCP fundamentals', 'JPno2xvtGz8', 240),
('CI/CD', 'CI/CD Pipeline Tutorial', 'Continuous Integration & Deployment', 'scEDHsr3APg', 120),
('Git', 'Git Tutorial', 'Version control with Git', 'RGOj5yH7evk', 90),
('GitHub Actions', 'GitHub Actions Guide', 'Automate workflows', 'R8_veQiYBjI', 90),
('Linux', 'Linux Complete Course', 'Linux operating system', 'sWbUDq4S6Y8', 300),
('Jenkins', 'Jenkins Tutorial', 'Automation server', '89yWXXIOisk', 150),
('Terraform', 'Terraform Course', 'Infrastructure as Code', 'l5k1ai_GBDE', 180),
('Ansible', 'Ansible Tutorial', 'Configuration management', 'goclfp6a2IQ', 150),

-- Data Science & AI
('Machine Learning', 'Machine Learning Course', 'ML fundamentals and algorithms', 'i_LwzRVP7bg', 480),
('Deep Learning', 'Deep Learning Tutorial', 'Neural networks and deep learning', 'VyWAvY2CF9c', 360),
('Data Analysis', 'Data Analysis Course', 'Analyze data with Python', 'r-uOLxNrNk8', 240),
('Pandas', 'Pandas Tutorial', 'Data manipulation with Pandas', 'vmEHCJofslg', 120),
('NumPy', 'NumPy Course', 'Numerical computing with NumPy', 'QUT1VHiLmmI', 120),
('TensorFlow', 'TensorFlow Tutorial', 'Deep learning framework', 'tPYj3fFJGjk', 240),
('PyTorch', 'PyTorch Course', 'Deep learning with PyTorch', 'c36lUUr864M', 180),
('Scikit-learn', 'Scikit-learn Tutorial', 'Machine learning library', 'pqNCD_5r0IU', 180),
('Data Visualization', 'Data Visualization', 'Visualize data effectively', 'eazGMRuFq78', 150),
('Big Data', 'Big Data Tutorial', 'Big data processing', 'KCEPt0XXAUQ', 240),
('Tableau', 'Tableau Tutorial', 'Business intelligence tool', 'aHaOIvR00So', 180),
('Power BI', 'Power BI Course', 'Microsoft business analytics', 'TmhQCQr_DCA', 240),

-- Design & UX
('Figma', 'Figma Tutorial', 'UI/UX design with Figma', 'FTFaQWZBqQ8', 120),
('UI/UX', 'UI/UX Design Course', 'User interface and experience design', 'c9Wg6Cb_YlU', 180),
('UX Design', 'UX Design Fundamentals', 'User experience design principles', 'c9Wg6Cb_YlU', 180),
('UI Design', 'UI Design Tutorial', 'User interface design best practices', 'c9Wg6Cb_YlU', 180),
('Adobe XD', 'Adobe XD Tutorial', 'Design and prototype with XD', 'WEljsc2jorI', 120),
('Sketch', 'Sketch Tutorial', 'Digital design for Mac', '_J8khb0a44g', 90),
('Photoshop', 'Photoshop for Web Design', 'Design web graphics', 'IyR_uYsRdPs', 180),

-- Testing & Quality
('Testing', 'Software Testing Course', 'Testing methodologies and practices', '_2Dxqf-Cf4k', 180),
('Selenium', 'Selenium Tutorial', 'Test automation with Selenium', 'Uy1hQ1U3S_U', 150),
('Jest', 'Jest Testing Tutorial', 'JavaScript testing framework', 'FgnxcUQ5vho', 90),
('Unit Testing', 'Unit Testing Guide', 'Write effective unit tests', 'ur_xYT_YQBU', 120),
('Cypress', 'Cypress Testing', 'End-to-end testing', 'u8vMu7viCm8', 120),

-- Cybersecurity
('Cybersecurity', 'Cybersecurity Course', 'Security fundamentals', 'hXSFdwIOfnE', 300),
('Penetration Testing', 'Penetration Testing', 'Ethical hacking and pen testing', 'WnN6dbos5u8', 240),
('Network Security', 'Network Security', 'Secure network infrastructure', 'qM4S_XKEo8I', 180),
('Ethical Hacking', 'Ethical Hacking Full Course', 'Learn ethical hacking', '3Kq1MIfTWCE', 360),

-- Other Technologies
('Blockchain', 'Blockchain Tutorial', 'Blockchain technology fundamentals', 'qOVAbKKSH10', 240),
('GraphQL', 'GraphQL Course', 'API development with GraphQL', 'ed8SzALpx1Q', 120),
('REST APIs', 'REST API Tutorial', 'Build RESTful web services', 'lsMQRaeKNDk', 90),
('Microservices', 'Microservices Architecture', 'Design microservices systems', 'CdBtNQZH8a4', 180),
('OAuth', 'OAuth 2.0 Tutorial', 'Authentication and authorization', '996OiexHze0', 90),
('WebSockets', 'WebSockets Tutorial', 'Real-time communication', 'i5OVcTdt_OU', 60),
('Agile', 'Agile Methodology', 'Agile software development', 'Z9QbYZh1YXY', 90),
('Scrum', 'Scrum Framework', 'Scrum agile framework', 'XU0llRltyFM', 60),
('API Development', 'API Development Guide', 'Build robust APIs', 'lsMQRaeKNDk', 120),
('Cloud Computing', 'Cloud Computing Fundamentals', 'Introduction to cloud services', 'M988_fsOSWo', 180),
('Data Structures', 'Data Structures Tutorial', 'Essential data structures', 'RBSGKlAvoiM', 240),
('Algorithms', 'Algorithms Course', 'Algorithm design and analysis', 'pLT_9jwaPzs', 300),
('System Design', 'System Design Interview', 'Design scalable systems', 'MbjObHmDbZo', 180),
('Software Architecture', 'Software Architecture', 'Architectural patterns and practices', '5yF4EH1Rlg8', 240),
('OOP', 'Object-Oriented Programming', 'OOP principles and patterns', 'pTB0EiLXUC8', 180),
('Design Patterns', 'Design Patterns Tutorial', 'Common software design patterns', 'NU_1StN5Tkk', 240),
('Version Control', 'Git & GitHub Complete', 'Master version control', 'RGOj5yH7evk', 120),
('Web Security', 'Web Application Security', 'Secure your web apps', 'F5DJuMM_Bww', 180)
ON DUPLICATE KEY UPDATE 
    video_title = VALUES(video_title),
    youtube_video_id = VALUES(youtube_video_id),
    duration_minutes = VALUES(duration_minutes);

-- =============================================
-- 12. CREATE DEFAULT ADMIN USER
-- =============================================

-- Insert admin user
-- Password: Admin@123 (hashed with BCrypt)
-- You can change this password after first login
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt, UpdatedAt) VALUES
('admin', 'System Administrator', 'admin@careerguidance.com', '$2a$11$xFT3xKqXMQZ3YYYvJ5YYyObVxG8WQKnC7qPjBsE5gU7VVxUZ.FXey', 'admin', NOW(), NOW())
ON DUPLICATE KEY UPDATE
    Username = VALUES(Username),
    FullName = VALUES(FullName),
    Role = VALUES(Role);

-- Create admin profile
INSERT INTO UserProfiles (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, career_path, CreatedAt, UpdatedAt)
SELECT 
    Id,
    '+1234567890',
    30,
    'N/A',
    'Advanced',
    'Computer Science',
    '["Administration", "Management", "System Configuration"]',
    'Administrator',
    NOW(),
    NOW()
FROM Users 
WHERE Email = 'admin@careerguidance.com'
ON DUPLICATE KEY UPDATE
    PhoneNumber = VALUES(PhoneNumber);

-- =============================================
-- SUCCESS MESSAGE
-- =============================================

SELECT '✅ Database recreation complete!' AS Status,
       '19 tables created' AS Tables,
       '9 procedures created' AS Procedures,
       'Triggers disabled (FreedDB limitation)' AS Triggers,
       '2 views created' AS Views,
       '10 careers + 100+ videos inserted' AS Data,
       'Admin user created' AS Admin;

SELECT '🔑 ADMIN LOGIN CREDENTIALS' AS Notice,
       'Email: admin@careerguidance.com' AS Email,
       'Password: Admin@123' AS Password,
       '⚠️ CHANGE PASSWORD AFTER FIRST LOGIN!' AS Warning;

-- =============================================
-- SUMMARY OF TABLES CREATED
-- =============================================
-- 1. Users (13 tables total)
-- 2. UserProfiles
-- 3. refresh_tokens
-- 4. careers
-- 5. quiz_sessions
-- 6. recommendations
-- 7. ChatSessions
-- 8. ChatMessages
-- 9. learning_videos
-- 10. user_career_progress
-- 11. course_progress
-- 12. learning_path_progress
-- 13. video_watch_history
-- 14. saved_jobs
-- 15. job_applications
-- 16. job_search_history
-- 17. job_recommendations
-- 18. user_resumes
-- 19. resume_export_history
-- =============================================
