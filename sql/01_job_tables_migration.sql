-- Job-related tables for Career Guidance Platform

-- Table: saved_jobs
-- Stores jobs that users have saved/bookmarked
CREATE TABLE IF NOT EXISTS saved_jobs (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    job_id VARCHAR(255) NOT NULL UNIQUE,
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
    INDEX idx_user_id (user_id),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: job_applications
-- Tracks job applications submitted by users
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
    UNIQUE KEY unique_user_job (user_id, job_id),
    INDEX idx_user_id (user_id),
    INDEX idx_status (application_status),
    FOREIGN KEY (user_id) REFERENCES Users(Id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Table: job_search_history
-- Optional: Tracks job searches for analytics and recommendations
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

-- Table: job_recommendations
-- Stores AI-generated personalized job recommendations
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

-- Add columns to UserProfiles if not exist (for storing user skills)
-- These might already exist, but including for completeness
ALTER TABLE UserProfiles 
ADD COLUMN IF NOT EXISTS skills JSON COMMENT 'Array of user skills' AFTER AreasOfInterest;

-- Add index for better query performance on user-related lookups
CREATE INDEX IF NOT EXISTS idx_saved_jobs_user_job ON saved_jobs(user_id, job_id);
CREATE INDEX IF NOT EXISTS idx_job_applications_user_job ON job_applications(user_id, job_id);
