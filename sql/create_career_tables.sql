-- Career recommendation system tables
USE my_database;

-- Careers table (predefined career options)
CREATE TABLE IF NOT EXISTS careers (
  id INT PRIMARY KEY AUTO_INCREMENT,
  name VARCHAR(200) NOT NULL,
  description TEXT,
  required_education VARCHAR(200),
  average_salary VARCHAR(100),
  growth_outlook VARCHAR(100),
  key_skills JSON,  -- Array of required skills
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Quiz sessions table
CREATE TABLE IF NOT EXISTS quiz_sessions (
  id INT PRIMARY KEY AUTO_INCREMENT,
  user_id INT NOT NULL,
  questions JSON NOT NULL,
  answers JSON,
  completed BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  completed_at TIMESTAMP NULL,
  
  FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE,
  INDEX idx_user_completed (user_id, completed)
);

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
  
  FOREIGN KEY (user_id) REFERENCES Users(id) ON DELETE CASCADE,
  FOREIGN KEY (career_id) REFERENCES careers(id) ON DELETE CASCADE,
  UNIQUE KEY unique_user_career (user_id, career_id),
  INDEX idx_user_match (user_id, match_percentage DESC)
);

-- Insert sample careers
INSERT IGNORE INTO careers (id, name, description, required_education, average_salary, growth_outlook, key_skills) VALUES
(1, 'Software Engineer', 'Design, develop, and maintain software applications', 'Bachelor in Computer Science', '$80,000 - $150,000', 'Strong growth', '["Programming", "Problem Solving", "Algorithms", "Teamwork"]'),
(2, 'Data Scientist', 'Analyze complex data to help organizations make decisions', 'Bachelor/Master in Data Science or Statistics', '$90,000 - $160,000', 'Very strong growth', '["Statistics", "Python/R", "Machine Learning", "Communication"]'),
(3, 'UX/UI Designer', 'Create user-friendly interfaces and experiences', 'Bachelor in Design or HCI', '$60,000 - $120,000', 'Strong growth', '["Design Tools", "User Research", "Creativity", "Empathy"]'),
(4, 'Product Manager', 'Lead product strategy and development', 'Bachelor in Business or related field', '$100,000 - $180,000', 'Strong growth', '["Leadership", "Communication", "Strategic Thinking", "Technical Knowledge"]'),
(5, 'DevOps Engineer', 'Automate and optimize software deployment', 'Bachelor in Computer Science', '$85,000 - $140,000', 'Very strong growth', '["Linux", "CI/CD", "Cloud Platforms", "Scripting"]'),
(6, 'Cybersecurity Analyst', 'Protect systems and data from threats', 'Bachelor in Cybersecurity or IT', '$70,000 - $130,000', 'Very strong growth', '["Security Protocols", "Risk Analysis", "Ethical Hacking", "Attention to Detail"]'),
(7, 'Marketing Manager', 'Plan and execute marketing strategies', 'Bachelor in Marketing or Business', '$60,000 - $120,000', 'Moderate growth', '["Communication", "Creativity", "Analytics", "Social Media"]'),
(8, 'Financial Analyst', 'Analyze financial data and trends', 'Bachelor in Finance or Economics', '$60,000 - $110,000', 'Moderate growth', '["Excel", "Financial Modeling", "Analysis", "Attention to Detail"]'),
(9, 'Mechanical Engineer', 'Design and develop mechanical systems', 'Bachelor in Mechanical Engineering', '$65,000 - $110,000', 'Moderate growth', '["CAD", "Problem Solving", "Physics", "Teamwork"]'),
(10, 'Teacher/Educator', 'Educate and mentor students', 'Bachelor in Education', '$40,000 - $70,000', 'Stable', '["Communication", "Patience", "Subject Expertise", "Empathy"]');
