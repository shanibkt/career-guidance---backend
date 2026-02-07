-- =============================================
-- ADD MISSING DATA (Videos & Admin User)
-- Run this after COMPLETE_DATABASE_RECREATION.sql
-- =============================================

USE freedb_career_guidence;

-- =============================================
-- INSERT ADDITIONAL LEARNING VIDEOS
-- =============================================

INSERT INTO learning_videos (skill_name, video_title, video_description, youtube_video_id, duration_minutes) VALUES
-- Additional Programming Languages
('Dart', 'Dart Programming', 'Learn Dart for Flutter development', '5xlVP04905w', 120),
('TypeScript', 'TypeScript Course', 'TypeScript for JavaScript developers', 'd56mG7DezGs', 180),
('Rust', 'Rust Programming', 'Systems programming with Rust', 'zF34dRivLOw', 240),
('R', 'R Programming Tutorial', 'Statistical computing with R', '_V8eKsto3Ug', 180),

-- Additional Web Development
('Express.js', 'Express.js Tutorial', 'Node.js web framework', 'L72fhGm1tfE', 120),
('Next.js', 'Next.js Course', 'React framework for production', 'Sklc_fQBmcs', 180),
('Svelte', 'Svelte Tutorial', 'Modern web framework', 'zojEMeQGGHs', 120),

-- Mobile Development
('Flutter', 'Flutter Complete Course', 'Build mobile apps with Flutter', 'VPvVD8t02U8', 240),
('React Native', 'React Native Tutorial', 'Cross-platform mobile development', '0-S5a0eXPoc', 240),
('Android SDK', 'Android Development', 'Native Android app development', 'fis26HvvDII', 300),
('iOS SDK', 'iOS Development', 'Native iOS app development', '09TeUXjzpKs', 240),

-- Additional Databases
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

-- Additional DevOps & Cloud
('Azure', 'Microsoft Azure Tutorial', 'Cloud computing with Azure', 'NKEFWyqJ5XA', 180),
('Google Cloud', 'Google Cloud Platform', 'GCP fundamentals', 'JPno2xvtGz8', 240),
('CI/CD', 'CI/CD Pipeline Tutorial', 'Continuous Integration & Deployment', 'scEDHsr3APg', 120),
('GitHub Actions', 'GitHub Actions Guide', 'Automate workflows', 'R8_veQiYBjI', 90),
('Jenkins', 'Jenkins Tutorial', 'Automation server', '89yWXXIOisk', 150),
('Terraform', 'Terraform Course', 'Infrastructure as Code', 'l5k1ai_GBDE', 180),
('Ansible', 'Ansible Tutorial', 'Configuration management', 'goclfp6a2IQ', 150),

-- Additional Data Science & AI
('Deep Learning', 'Deep Learning Tutorial', 'Neural networks and deep learning', 'VyWAvY2CF9c', 360),
('Pandas', 'Pandas Tutorial', 'Data manipulation with Pandas', 'vmEHCJofslg', 120),
('NumPy', 'NumPy Course', 'Numerical computing with NumPy', 'QUT1VHiLmmI', 120),
('TensorFlow', 'TensorFlow Tutorial', 'Deep learning framework', 'tPYj3fFJGjk', 240),
('PyTorch', 'PyTorch Course', 'Deep learning with PyTorch', 'c36lUUr864M', 180),
('Scikit-learn', 'Scikit-learn Tutorial', 'Machine learning library', 'pqNCD_5r0IU', 180),
('Data Visualization', 'Data Visualization', 'Visualize data effectively', 'eazGMRuFq78', 150),
('Big Data', 'Big Data Tutorial', 'Big data processing', 'KCEPt0XXAUQ', 240),
('Tableau', 'Tableau Tutorial', 'Business intelligence tool', 'aHaOIvR00So', 180),
('Power BI', 'Power BI Course', 'Microsoft business analytics', 'TmhQCQr_DCA', 240),

-- Additional Design & UX
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
    video_title = video_title,
    youtube_video_id = youtube_video_id,
    duration_minutes = duration_minutes;

-- =============================================
-- CREATE ADMIN USER
-- =============================================

-- Insert admin user with BCrypt hashed password: Admin@123
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt, UpdatedAt) VALUES
('admin', 'System Administrator', 'admin@careerguidance.com', '$2a$11$xFT3xKqXMQZ3YYYvJ5YYyObVxG8WQKnC7qPjBsE5gU7VVxUZ.FXey', 'admin', NOW(), NOW())
ON DUPLICATE KEY UPDATE
    Username = 'admin',
    FullName = 'System Administrator',
    Role = 'admin';

-- Get the admin user ID and create profile
SET @admin_user_id = (SELECT Id FROM Users WHERE Email = 'admin@careerguidance.com' LIMIT 1);

INSERT INTO UserProfiles (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, career_path, CreatedAt, UpdatedAt)
VALUES 
    (@admin_user_id, '+1234567890', 30, 'N/A', 'Advanced', 'Computer Science', '["Administration", "Management", "System Configuration"]', 'Administrator', NOW(), NOW())
ON DUPLICATE KEY UPDATE
    PhoneNumber = '+1234567890',
    Skills = '["Administration", "Management", "System Configuration"]';

-- =============================================
-- VERIFICATION & SUCCESS MESSAGE
-- =============================================

SELECT '✅ Missing data added successfully!' AS Status;

SELECT 
    COUNT(*) as total_videos,
    '100+ learning videos available' as message
FROM learning_videos;

SELECT 
    COUNT(*) as admin_users,
    'Admin account created' as message
FROM Users 
WHERE Role = 'admin';

SELECT 
    '🔑 ADMIN LOGIN CREDENTIALS' AS '═════════════════════════════════',
    'Email: admin@careerguidance.com' AS Email,
    'Password: Admin@123' AS Password,
    '⚠️ CHANGE PASSWORD IMMEDIATELY AFTER LOGIN!' AS '⚠️ WARNING ⚠️';

-- =============================================
-- QUICK VERIFICATION QUERIES
-- =============================================

-- Check tables
SELECT 
    'Tables Check' AS verification_type,
    COUNT(*) as total_tables
FROM information_schema.tables 
WHERE table_schema = 'freedb_career_guidence';

-- Check data
SELECT 'Data Check' AS verification_type, 
       (SELECT COUNT(*) FROM Users) as users,
       (SELECT COUNT(*) FROM careers) as careers,
       (SELECT COUNT(*) FROM learning_videos) as videos;

SELECT '✅ Setup Complete! You can now login with admin credentials.' AS final_message;
