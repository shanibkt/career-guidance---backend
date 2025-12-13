-- Populate careers table in freedb_career_guidence database
USE freedb_career_guidence;

-- Insert comprehensive career data
INSERT INTO careers (career_name, description, required_education, key_skills, average_salary, growth_outlook) VALUES
('Flutter Developer', 'Build cross-platform mobile applications using Flutter and Dart framework. Create beautiful, natively compiled applications for mobile, web, and desktop from a single codebase.', 
 'Bachelor''s Degree in Computer Science or related field', 
 '["Flutter", "Dart", "Mobile Development", "UI/UX Design", "State Management", "REST APIs", "Firebase", "Git"]',
 '$60,000 - $120,000', 'High Growth'),

('Full Stack Java Developer', 'Develop enterprise applications using Java and modern web technologies. Work on both frontend and backend systems with frameworks like Spring Boot and React.',
 'Bachelor''s Degree in Computer Science or Software Engineering',
 '["Java", "Spring Boot", "SQL", "JavaScript", "React", "REST API", "Microservices", "Docker", "Git"]',
 '$70,000 - $130,000', 'Very High Growth'),

('Mobile App Developer', 'Create mobile applications for iOS and Android platforms using native or cross-platform technologies. Focus on user experience and performance optimization.',
 'Bachelor''s Degree in Computer Science or Mobile Development',
 '["Flutter", "Dart", "Java", "Kotlin", "Swift", "Mobile Development", "UI/UX", "REST APIs", "Firebase"]',
 '$65,000 - $125,000', 'High Growth'),

('Backend Developer', 'Build and maintain server-side applications, APIs, and databases. Design scalable architectures and ensure system reliability and security.',
 'Bachelor''s Degree in Computer Science or Software Engineering',
 '["Java", "Node.js", "Python", "SQL", "REST API", "Microservices", "Docker", "AWS", "Git"]',
 '$70,000 - $140,000', 'Very High Growth'),

('Frontend Developer', 'Create responsive and interactive user interfaces for web applications. Work with modern JavaScript frameworks and ensure great user experience.',
 'Bachelor''s Degree in Web Development or Computer Science',
 '["JavaScript", "React", "HTML", "CSS", "TypeScript", "UI/UX", "Git", "Responsive Design"]',
 '$60,000 - $110,000', 'High Growth'),

('Data Scientist', 'Analyze complex data sets to drive business decisions. Build machine learning models and create data visualizations to communicate insights.',
 'Master''s Degree in Data Science, Statistics, or Computer Science',
 '["Python", "Machine Learning", "Statistics", "SQL", "Data Visualization", "TensorFlow", "Pandas", "R"]',
 '$80,000 - $150,000', 'Very High Growth'),

('DevOps Engineer', 'Automate and optimize development and deployment processes. Manage cloud infrastructure and ensure system reliability and scalability.',
 'Bachelor''s Degree in Computer Science or IT',
 '["Docker", "Kubernetes", "AWS", "CI/CD", "Linux", "Git", "Python", "Terraform", "Monitoring"]',
 '$75,000 - $145,000', 'Very High Growth'),

('UI/UX Designer', 'Design user-centered interfaces and experiences for digital products. Create wireframes, prototypes, and conduct user research.',
 'Bachelor''s Degree in Design, HCI, or related field',
 '["UI/UX Design", "Figma", "Adobe XD", "User Research", "Prototyping", "Wireframing", "HTML", "CSS"]',
 '$55,000 - $105,000', 'High Growth'),

('Cloud Architect', 'Design and implement cloud-based solutions and infrastructure. Ensure scalability, security, and cost optimization of cloud systems.',
 'Bachelor''s Degree in Computer Science plus cloud certifications',
 '["AWS", "Azure", "Cloud Architecture", "Microservices", "Docker", "Kubernetes", "Security", "Networking"]',
 '$90,000 - $160,000', 'Very High Growth'),

('Cybersecurity Analyst', 'Protect systems and networks from cyber threats. Monitor security incidents, conduct vulnerability assessments, and implement security measures.',
 'Bachelor''s Degree in Cybersecurity or Computer Science',
 '["Network Security", "Ethical Hacking", "Security Tools", "Linux", "Python", "Risk Assessment", "Incident Response"]',
 '$70,000 - $130,000', 'Very High Growth'),

('AI/ML Engineer', 'Develop artificial intelligence and machine learning solutions. Build and deploy ML models for various applications.',
 'Master''s Degree in Computer Science, AI, or related field',
 '["Python", "Machine Learning", "Deep Learning", "TensorFlow", "PyTorch", "Neural Networks", "NLP", "Computer Vision"]',
 '$85,000 - $160,000', 'Extremely High Growth'),

('Software Architect', 'Design high-level software solutions and system architectures. Make critical technical decisions and guide development teams.',
 'Bachelor''s Degree plus 5+ years experience',
 '["System Design", "Microservices", "Cloud Architecture", "Java", "Design Patterns", "Leadership", "Documentation"]',
 '$95,000 - $170,000', 'High Growth'),

('Game Developer', 'Create video games for various platforms. Work on game mechanics, graphics, and user experience in gaming.',
 'Bachelor''s Degree in Computer Science or Game Development',
 '["Unity", "Unreal Engine", "C++", "C#", "3D Graphics", "Game Design", "Physics", "Git"]',
 '$60,000 - $120,000', 'Moderate Growth'),

('Blockchain Developer', 'Build decentralized applications and smart contracts. Work with blockchain technologies and cryptocurrency systems.',
 'Bachelor''s Degree in Computer Science',
 '["Solidity", "Blockchain", "Smart Contracts", "Ethereum", "Web3", "Cryptography", "JavaScript", "Node.js"]',
 '$80,000 - $150,000', 'High Growth'),

('Database Administrator', 'Manage and optimize database systems. Ensure data integrity, security, and performance.',
 'Bachelor''s Degree in Computer Science or Database Management',
 '["SQL", "MySQL", "PostgreSQL", "Database Design", "Performance Tuning", "Backup & Recovery", "Security"]',
 '$65,000 - $120,000', 'Moderate Growth'),

('Quality Assurance Engineer', 'Test software applications to ensure quality and reliability. Create automated tests and identify bugs.',
 'Bachelor''s Degree in Computer Science or Software Testing',
 '["Test Automation", "Selenium", "Java", "Python", "Manual Testing", "Bug Tracking", "CI/CD", "API Testing"]',
 '$55,000 - $100,000', 'High Growth'),

('Product Manager', 'Define product strategy and roadmap. Work with cross-functional teams to deliver successful products.',
 'Bachelor''s Degree in Business, Computer Science, or related field',
 '["Product Strategy", "Agile", "User Research", "Data Analysis", "Communication", "Leadership", "Roadmapping"]',
 '$80,000 - $145,000', 'High Growth'),

('Business Analyst', 'Analyze business processes and requirements. Bridge the gap between business needs and technical solutions.',
 'Bachelor''s Degree in Business, IT, or related field',
 '["Requirements Gathering", "Data Analysis", "SQL", "Communication", "Agile", "Documentation", "Process Modeling"]',
 '$60,000 - $110,000', 'High Growth'),

('IoT Developer', 'Build Internet of Things solutions connecting physical devices to digital systems.',
 'Bachelor''s Degree in Computer Science or Electrical Engineering',
 '["IoT", "Embedded Systems", "Python", "C++", "Networking", "Sensors", "Cloud Integration", "Security"]',
 '$70,000 - $130,000', 'Very High Growth'),

('AR/VR Developer', 'Create augmented and virtual reality experiences and applications.',
 'Bachelor''s Degree in Computer Science or Interactive Media',
 '["Unity", "Unreal Engine", "C#", "3D Graphics", "AR/VR", "Computer Vision", "Mobile Development"]',
 '$75,000 - $135,000', 'High Growth');

-- Show what was inserted
SELECT id, career_name, required_education, average_salary, growth_outlook FROM careers ORDER BY id;
