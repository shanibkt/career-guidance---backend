-- Create user_career_progress table to store selected career and learning progress
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
    INDEX idx_is_active (is_active)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create course_progress table to track individual video/course progress
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
    INDEX idx_completed (is_completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Add career_path column to UserProfiles table if it doesn't exist
ALTER TABLE UserProfiles 
ADD COLUMN career_path VARCHAR(255) NULL AFTER Skills;
