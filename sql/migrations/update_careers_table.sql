-- Update existing careers table for skill-based quiz system
USE my_database;

-- Check and add required_skills column
SET @dbname = DATABASE();
SET @tablename = 'careers';

-- Add required_skills if not exists
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = 'required_skills') = 0,
  'ALTER TABLE careers ADD COLUMN required_skills JSON',
  'SELECT 1'));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- Add skill_weights if not exists
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = 'skill_weights') = 0,
  'ALTER TABLE careers ADD COLUMN skill_weights JSON',
  'SELECT 1'));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- Add min_score_percentage if not exists
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = 'min_score_percentage') = 0,
  'ALTER TABLE careers ADD COLUMN min_score_percentage INT DEFAULT 60',
  'SELECT 1'));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- Rename name to career_name if it exists
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = 'name') > 0,
  'ALTER TABLE careers CHANGE COLUMN name career_name VARCHAR(200) NOT NULL',
  'SELECT 1'));
PREPARE renameIfExists FROM @preparedStatement;
EXECUTE renameIfExists;
DEALLOCATE PREPARE renameIfExists;

-- Add salary_range if not exists
SET @preparedStatement = (SELECT IF(
  (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
   WHERE TABLE_SCHEMA = @dbname AND TABLE_NAME = @tablename AND COLUMN_NAME = 'salary_range') = 0,
  'ALTER TABLE careers ADD COLUMN salary_range VARCHAR(100)',
  'SELECT 1'));
PREPARE alterIfExists FROM @preparedStatement;
EXECUTE alterIfExists;
DEALLOCATE PREPARE alterIfExists;

-- Update existing careers with skill data
-- Clear old data first
DELETE FROM recommendations;
DELETE FROM careers;

-- Insert sample careers with skill requirements
INSERT INTO careers (career_name, description, required_skills, skill_weights, min_score_percentage, salary_range, growth_outlook) VALUES
('Flutter Developer', 'Build cross-platform mobile applications using Flutter and Dart', 
 '["Flutter", "Dart", "Mobile Development", "UI/UX"]', 
 '{"Flutter": 90, "Dart": 85, "Mobile Development": 80, "UI/UX": 70}',
 60, '$60,000 - $120,000', 'High'),
 
('Full Stack Java Developer', 'Develop enterprise applications using Java and web technologies',
 '["Java", "Spring Boot", "SQL", "JavaScript", "REST API"]',
 '{"Java": 90, "Spring Boot": 85, "SQL": 75, "JavaScript": 70, "REST API": 80}',
 60, '$70,000 - $130,000', 'High'),
 
('Mobile App Developer', 'Create mobile applications for iOS and Android platforms',
 '["Flutter", "Dart", "Java", "Kotlin", "Mobile Development"]',
 '{"Flutter": 80, "Dart": 75, "Java": 75, "Kotlin": 70, "Mobile Development": 85}',
 55, '$65,000 - $125,000', 'High'),
 
('Backend Developer', 'Build server-side applications and APIs',
 '["Java", "Node.js", "Python", "SQL", "REST API", "Microservices"]',
 '{"Java": 80, "Node.js": 75, "Python": 75, "SQL": 85, "REST API": 80, "Microservices": 70}',
 55, '$70,000 - $140,000', 'Very High'),
 
('Frontend Developer', 'Create user interfaces for web applications',
 '["JavaScript", "React", "HTML", "CSS", "UI/UX"]',
 '{"JavaScript": 90, "React": 85, "HTML": 80, "CSS": 80, "UI/UX": 75}',
 60, '$60,000 - $110,000', 'High');

-- Show results
SELECT id, career_name, required_skills, skill_weights, min_score_percentage, salary_range FROM careers;
