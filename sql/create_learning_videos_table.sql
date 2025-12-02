-- Create learning_videos table to store video content for skills
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

-- Insert video data for all skills
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

-- Web Development
('HTML', 'HTML Full Course', 'HTML fundamentals and best practices', 'qz0aGYrrlhU', 120),
('CSS', 'CSS Complete Guide', 'Master CSS styling and layouts', 'yfoY53QXEnI', 180),
('React', 'React Tutorial', 'Build modern web apps with React', 'bMknfKXIFA8', 144),
('Angular', 'Angular Full Course', 'Complete Angular framework guide', 'k5E2AVpwsko', 240),
('Vue.js', 'Vue.js Tutorial', 'Learn Vue.js framework', 'FXpIoQ_rT_c', 180),
('TypeScript', 'TypeScript Course', 'TypeScript for JavaScript developers', 'd56mG7DezGs', 180),
('Node.js', 'Node.js Tutorial', 'Backend development with Node.js', 'TlB_eWDSMt4', 180),

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

-- Frameworks
('Django', 'Django Tutorial', 'Python web framework', 'rHux0gMZ3Eg', 90),
('Spring Boot', 'Spring Boot Course', 'Java Spring Boot framework', '9SGDpanrc8U', 240),
('ASP.NET', 'ASP.NET Tutorial', '.NET web development', 'BfEjDD8mWYg', 300),
('Laravel', 'Laravel Course', 'PHP Laravel framework', 'ImtZ5yENzgE', 240),
('.NET Core', '.NET Core Tutorial', 'Modern .NET development', 'BfEjDD8mWYg', 300),
('Express.js', 'Express.js Tutorial', 'Node.js web framework', 'L72fhGm1tfE', 120),

-- DevOps & Cloud
('Docker', 'Docker Tutorial', 'Containerization with Docker', 'fqMOX6JJhGo', 180),
('Kubernetes', 'Kubernetes Course', 'Container orchestration', 'X48VuDVv0do', 240),
('AWS', 'AWS Complete Guide', 'Amazon Web Services tutorial', 'SOTamWNgDKc', 600),
('Azure', 'Microsoft Azure Tutorial', 'Cloud computing with Azure', 'NKEFWyqJ5XA', 180),
('Google Cloud', 'Google Cloud Platform', 'GCP fundamentals', 'JPno2xvtGz8', 240),
('CI/CD', 'CI/CD Pipeline Tutorial', 'Continuous Integration & Deployment', 'scEDHsr3APg', 120),
('Git', 'Git Tutorial', 'Version control with Git', 'RGOj5yH7evk', 90),
('Linux', 'Linux Complete Course', 'Linux operating system', 'sWbUDq4S6Y8', 300),
('Jenkins', 'Jenkins Tutorial', 'Automation server', '89yWXXIOisk', 150),
('Terraform', 'Terraform Course', 'Infrastructure as Code', 'l5k1ai_GBDE', 180),

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

-- Design & UX
('Figma', 'Figma Tutorial', 'UI/UX design with Figma', 'FTFaQWZBqQ8', 120),
('UI/UX', 'UI/UX Design Course', 'User interface and experience design', 'c9Wg6Cb_YlU', 180),
('UX Design', 'UX Design Fundamentals', 'User experience design principles', 'c9Wg6Cb_YlU', 180),
('UI Design', 'UI Design Tutorial', 'User interface design best practices', 'c9Wg6Cb_YlU', 180),
('Adobe XD', 'Adobe XD Tutorial', 'Design and prototype with XD', 'WEljsc2jorI', 120),

-- Testing & Quality
('Testing', 'Software Testing Course', 'Testing methodologies and practices', '_2Dxqf-Cf4k', 180),
('Selenium', 'Selenium Tutorial', 'Test automation with Selenium', 'Uy1hQ1U3S_U', 150),
('Jest', 'Jest Testing Tutorial', 'JavaScript testing framework', 'FgnxcUQ5vho', 90),
('Unit Testing', 'Unit Testing Guide', 'Write effective unit tests', 'ur_xYT_YQBU', 120),

-- Cybersecurity
('Cybersecurity', 'Cybersecurity Course', 'Security fundamentals', 'hXSFdwIOfnE', 300),
('Penetration Testing', 'Penetration Testing', 'Ethical hacking and pen testing', 'WnN6dbos5u8', 240),
('Network Security', 'Network Security', 'Secure network infrastructure', 'qM4S_XKEo8I', 180),

-- Other Technologies
('Blockchain', 'Blockchain Tutorial', 'Blockchain technology fundamentals', 'qOVAbKKSH10', 240),
('GraphQL', 'GraphQL Course', 'API development with GraphQL', 'ed8SzALpx1Q', 120),
('REST APIs', 'REST API Tutorial', 'Build RESTful web services', 'lsMQRaeKNDk', 90),
('Microservices', 'Microservices Architecture', 'Design microservices systems', 'CdBtNQZH8a4', 180),
('OAuth', 'OAuth 2.0 Tutorial', 'Authentication and authorization', '996OiexHze0', 90),
('WebSockets', 'WebSockets Tutorial', 'Real-time communication', 'i5OVcTdt_OU', 60),
('Agile', 'Agile Methodology', 'Agile software development', 'Z9QbYZh1YXY', 90),
('Scrum', 'Scrum Framework', 'Scrum agile framework', 'XU0llRltyFM', 60);

-- Add more comprehensive coverage
INSERT INTO learning_videos (skill_name, video_title, video_description, youtube_video_id, duration_minutes) VALUES
('API Development', 'API Development Guide', 'Build robust APIs', 'lsMQRaeKNDk', 120),
('Cloud Computing', 'Cloud Computing Fundamentals', 'Introduction to cloud services', 'M988_fsOSWo', 180),
('Data Structures', 'Data Structures Tutorial', 'Essential data structures', 'RBSGKlAvoiM', 240),
('Algorithms', 'Algorithms Course', 'Algorithm design and analysis', 'pLT_9jwaPzs', 300),
('System Design', 'System Design Interview', 'Design scalable systems', 'MbjObHmDbZo', 180),
('Software Architecture', 'Software Architecture', 'Architectural patterns and practices', '5yF4EH1Rlg8', 240),
('OOP', 'Object-Oriented Programming', 'OOP principles and patterns', 'pTB0EiLXUC8', 180),
('Design Patterns', 'Design Patterns Tutorial', 'Common software design patterns', 'NU_1StN5Tkk', 240)
ON DUPLICATE KEY UPDATE 
    video_title = VALUES(video_title),
    youtube_video_id = VALUES(youtube_video_id),
    duration_minutes = VALUES(duration_minutes);
