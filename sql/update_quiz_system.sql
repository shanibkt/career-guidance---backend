-- Update Quiz System Schema
USE my_database;

-- 1. Drop old quiz_sessions table to recreate with new structure
DROP TABLE IF EXISTS quiz_sessions;

-- 2. Create new quiz_sessions table with skill-based scoring
CREATE TABLE quiz_sessions (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    quiz_id VARCHAR(36) NOT NULL UNIQUE,
    questions JSON NOT NULL,
    answers JSON,
    skill_scores JSON,
    total_score INT DEFAULT 0,
    total_questions INT DEFAULT 0,
    percentage DECIMAL(5,2) DEFAULT 0,
    completed_at DATETIME,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_quiz (user_id, quiz_id),
    INDEX idx_quiz_id (quiz_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 3. Create careers table
CREATE TABLE IF NOT EXISTS careers (
    id INT PRIMARY KEY AUTO_INCREMENT,
    career_name VARCHAR(200) NOT NULL,
    description TEXT,
    required_skills JSON NOT NULL,
    skill_weights JSON,
    min_score_percentage INT DEFAULT 60,
    salary_range VARCHAR(100),
    growth_outlook VARCHAR(50),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_career_name (career_name)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 4. Remove AreasOfInterest column from UserProfiles (if exists)
SET @dbname = DATABASE();
SET @tablename = 'UserProfiles';
SET @columnname = 'AreasOfInterest';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      TABLE_SCHEMA = @dbname
      AND TABLE_NAME = @tablename
      AND COLUMN_NAME = @columnname
  ) > 0,
  CONCAT('ALTER TABLE ', @tablename, ' DROP COLUMN ', @columnname),
  'SELECT 1'
));

PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- 5. Insert sample careers
INSERT INTO careers (career_name, description, required_skills, skill_weights) VALUES
('Flutter Developer', 'Build cross-platform mobile applications using Flutter and Dart', 
 '["Flutter", "Dart", "Mobile Development", "UI/UX"]', 
 '{"Flutter": 90, "Dart": 85, "Mobile Development": 80, "UI/UX": 70}'),
 
('Full Stack Java Developer', 'Develop enterprise applications using Java and web technologies',
 '["Java", "Spring Boot", "SQL", "JavaScript", "REST API"]',
 '{"Java": 90, "Spring Boot": 85, "SQL": 75, "JavaScript": 70, "REST API": 80}'),
 
('Mobile App Developer', 'Create mobile applications for iOS and Android platforms',
 '["Flutter", "Dart", "Java", "Kotlin", "Mobile Development"]',
 '{"Flutter": 80, "Dart": 75, "Java": 75, "Kotlin": 70, "Mobile Development": 85}'),
 
('Backend Developer', 'Build server-side applications and APIs',
 '["Java", "Node.js", "Python", "SQL", "REST API", "Microservices"]',
 '{"Java": 80, "Node.js": 75, "Python": 75, "SQL": 85, "REST API": 80, "Microservices": 70}'),
 
('Frontend Developer', 'Create user interfaces for web applications',
 '["JavaScript", "React", "HTML", "CSS", "UI/UX"]',
 '{"JavaScript": 90, "React": 85, "HTML": 80, "CSS": 80, "UI/UX": 75}');

-- Show updated tables
SHOW TABLES;
DESCRIBE quiz_sessions;
DESCRIBE careers;
SELECT * FROM careers;
