-- =============================================
-- Career Guidance App - Complete Database Schema
-- =============================================

-- 1. User Resumes Table
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
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY unique_user_resume (user_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_id (user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 2. User Career Progress Table (if not exists)
CREATE TABLE IF NOT EXISTS user_career_progress (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_id INT NULL,
    career_name VARCHAR(255) NOT NULL,
    required_skills JSON NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    overall_progress DECIMAL(5,2) DEFAULT 0.00,
    completed_courses INT DEFAULT 0,
    total_courses INT DEFAULT 0,
    selected_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_accessed TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_active (user_id, is_active),
    INDEX idx_career_name (career_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Course Progress Table (if not exists)
CREATE TABLE IF NOT EXISTS course_progress (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    career_name VARCHAR(255) NOT NULL,
    course_id VARCHAR(255) NOT NULL,
    skill_name VARCHAR(255) NOT NULL,
    video_title VARCHAR(500) NULL,
    youtube_video_id VARCHAR(100) NULL,
    watched_percentage DECIMAL(5,2) DEFAULT 0.00,
    watch_time_seconds INT DEFAULT 0,
    total_duration_seconds INT DEFAULT 0,
    is_completed BOOLEAN DEFAULT FALSE,
    started_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    completed_at TIMESTAMP NULL,
    last_watched TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_course (user_id, course_id, career_name),
    INDEX idx_user_career (user_id, career_name),
    INDEX idx_completion (user_id, is_completed),
    INDEX idx_last_watched (last_watched)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Learning Path Progress Table
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

-- 5. Video Watch History Table
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
    INDEX idx_completion (is_completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 6. Resume Export History Table
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
-- Stored Procedures for Analytics
-- =============================================

-- Get User's Complete Learning Analytics
DELIMITER //
CREATE PROCEDURE IF NOT EXISTS GetUserLearningAnalytics(IN p_user_id INT)
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
END //
DELIMITER ;

-- Update Learning Path Progress
DELIMITER //
CREATE PROCEDURE IF NOT EXISTS UpdateLearningPathProgress(
    IN p_user_id INT,
    IN p_career_name VARCHAR(255),
    IN p_skill_name VARCHAR(255)
)
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
END //
DELIMITER ;

-- =============================================
-- Views for Quick Data Access
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
-- Sample Queries for Testing
-- =============================================

-- Get complete user progress
-- SELECT * FROM vw_user_learning_dashboard WHERE user_id = 1;

-- Get resume completion status
-- SELECT * FROM vw_resume_completion WHERE user_id = 1;

-- Get user's learning analytics
-- CALL GetUserLearningAnalytics(1);

-- =============================================
-- Indexes for Performance
-- =============================================

-- Additional composite indexes for common queries
CREATE INDEX IF NOT EXISTS idx_video_history_user_career ON video_watch_history(user_id, career_name, last_watched);
CREATE INDEX IF NOT EXISTS idx_course_progress_user_career ON course_progress(user_id, career_name, is_completed);
CREATE INDEX IF NOT EXISTS idx_learning_path_progress_user ON learning_path_progress(user_id, progress_percentage);

-- =============================================
-- Data Integrity Triggers
-- =============================================

-- Auto-update learning path progress when video is updated
DELIMITER //
CREATE TRIGGER IF NOT EXISTS trg_update_learning_path_after_video
AFTER UPDATE ON video_watch_history
FOR EACH ROW
BEGIN
    CALL UpdateLearningPathProgress(NEW.user_id, NEW.career_name, NEW.skill_name);
END //
DELIMITER ;

-- Auto-update learning path progress when video is inserted
DELIMITER //
CREATE TRIGGER IF NOT EXISTS trg_insert_learning_path_after_video
AFTER INSERT ON video_watch_history
FOR EACH ROW
BEGIN
    CALL UpdateLearningPathProgress(NEW.user_id, NEW.career_name, NEW.skill_name);
END //
DELIMITER ;

-- =============================================
-- Grant Permissions (adjust as needed)
-- =============================================
-- GRANT SELECT, INSERT, UPDATE, DELETE ON career_guidance_db.* TO 'your_app_user'@'localhost';
-- FLUSH PRIVILEGES;
