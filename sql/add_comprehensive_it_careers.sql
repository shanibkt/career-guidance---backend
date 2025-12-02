-- Add comprehensive IT career options
USE my_database;

-- Clear existing careers
DELETE FROM recommendations;
DELETE FROM careers;

-- Insert comprehensive IT career options (50+ careers)
INSERT INTO careers (career_name, description, required_skills, skill_weights, min_score_percentage) VALUES

-- Software Development & Engineering
('Frontend Developer', 'Create responsive and interactive user interfaces for web applications using modern frameworks', 
 '["HTML", "CSS", "JavaScript", "React", "Vue.js", "Angular", "TypeScript", "Responsive Design"]',
 '{"HTML": 85, "CSS": 85, "JavaScript": 95, "React": 90, "Vue.js": 80, "Angular": 80, "TypeScript": 85, "Responsive Design": 80}', 60),

('Backend Developer', 'Build server-side applications, APIs, and database management systems',
 '["Java", "Python", "Node.js", "SQL", "REST API", "GraphQL", "Microservices", "Database Design"]',
 '{"Java": 85, "Python": 85, "Node.js": 85, "SQL": 90, "REST API": 90, "GraphQL": 75, "Microservices": 80, "Database Design": 85}', 60),

('Full Stack Developer', 'Develop both frontend and backend components of web applications',
 '["JavaScript", "React", "Node.js", "SQL", "HTML", "CSS", "REST API", "Git"]',
 '{"JavaScript": 90, "React": 85, "Node.js": 85, "SQL": 80, "HTML": 80, "CSS": 80, "REST API": 85, "Git": 75}', 55),

('Mobile App Developer', 'Create native and cross-platform mobile applications for iOS and Android',
 '["Flutter", "Dart", "React Native", "Swift", "Kotlin", "Mobile UI/UX", "API Integration"]',
 '{"Flutter": 85, "Dart": 85, "React Native": 80, "Swift": 75, "Kotlin": 75, "Mobile UI/UX": 80, "API Integration": 85}', 55),

('Flutter Developer', 'Build beautiful cross-platform mobile applications using Flutter framework',
 '["Flutter", "Dart", "Mobile Development", "UI/UX", "State Management", "API Integration", "Firebase"]',
 '{"Flutter": 95, "Dart": 90, "Mobile Development": 85, "UI/UX": 80, "State Management": 85, "API Integration": 85, "Firebase": 75}', 60),

('iOS Developer', 'Develop native applications for Apple platforms using Swift and iOS SDK',
 '["Swift", "SwiftUI", "UIKit", "Xcode", "iOS SDK", "Mobile Development", "App Store"]',
 '{"Swift": 95, "SwiftUI": 85, "UIKit": 85, "Xcode": 80, "iOS SDK": 90, "Mobile Development": 85, "App Store": 70}', 60),

('Android Developer', 'Create native Android applications using Kotlin and Android Studio',
 '["Kotlin", "Java", "Android SDK", "Jetpack Compose", "Material Design", "Android Studio", "Google Play"]',
 '{"Kotlin": 90, "Java": 85, "Android SDK": 90, "Jetpack Compose": 80, "Material Design": 75, "Android Studio": 80, "Google Play": 70}', 60),

('Software Engineer', 'Design, develop, and maintain software systems using best practices',
 '["Programming", "Data Structures", "Algorithms", "OOP", "Design Patterns", "Testing", "Git"]',
 '{"Programming": 90, "Data Structures": 85, "Algorithms": 85, "OOP": 85, "Design Patterns": 80, "Testing": 80, "Git": 75}', 60),

('Java Developer', 'Build enterprise applications using Java and Spring ecosystem',
 '["Java", "Spring Boot", "Hibernate", "SQL", "REST API", "Maven", "JUnit"]',
 '{"Java": 95, "Spring Boot": 90, "Hibernate": 85, "SQL": 85, "REST API": 85, "Maven": 75, "JUnit": 80}', 60),

('Python Developer', 'Develop applications using Python for web, automation, and data processing',
 '["Python", "Django", "Flask", "REST API", "SQL", "Git", "Testing"]',
 '{"Python": 95, "Django": 85, "Flask": 80, "REST API": 85, "SQL": 80, "Git": 75, "Testing": 80}', 60),

('.NET Developer', 'Build Windows and web applications using .NET framework and C#',
 '["C#", ".NET Core", "ASP.NET", "SQL Server", "Entity Framework", "Azure", "REST API"]',
 '{"C#": 95, ".NET Core": 90, "ASP.NET": 85, "SQL Server": 85, "Entity Framework": 85, "Azure": 75, "REST API": 85}', 60),

('PHP Developer', 'Create dynamic web applications using PHP and related frameworks',
 '["PHP", "Laravel", "MySQL", "HTML", "CSS", "JavaScript", "REST API"]',
 '{"PHP": 95, "Laravel": 85, "MySQL": 85, "HTML": 75, "CSS": 75, "JavaScript": 80, "REST API": 85}', 55),

('Ruby Developer', 'Build web applications using Ruby on Rails framework',
 '["Ruby", "Ruby on Rails", "PostgreSQL", "REST API", "Testing", "Git", "HTML/CSS"]',
 '{"Ruby": 95, "Ruby on Rails": 90, "PostgreSQL": 80, "REST API": 85, "Testing": 80, "Git": 75, "HTML/CSS": 70}', 60),

('Go Developer', 'Develop high-performance backend services using Go programming language',
 '["Go", "Microservices", "REST API", "SQL", "Docker", "Kubernetes", "Cloud"]',
 '{"Go": 95, "Microservices": 85, "REST API": 85, "SQL": 80, "Docker": 80, "Kubernetes": 75, "Cloud": 75}', 60),

-- DevOps & Cloud
('DevOps Engineer', 'Automate software development and deployment processes using CI/CD pipelines',
 '["Linux", "Docker", "Kubernetes", "CI/CD", "Jenkins", "Git", "Scripting", "Cloud"]',
 '{"Linux": 90, "Docker": 90, "Kubernetes": 85, "CI/CD": 90, "Jenkins": 80, "Git": 85, "Scripting": 85, "Cloud": 85}', 60),

('Cloud Engineer', 'Design and manage cloud infrastructure on AWS, Azure, or Google Cloud',
 '["AWS", "Azure", "Cloud Architecture", "Terraform", "Docker", "Kubernetes", "Networking"]',
 '{"AWS": 85, "Azure": 85, "Cloud Architecture": 90, "Terraform": 80, "Docker": 80, "Kubernetes": 80, "Networking": 80}', 60),

('Site Reliability Engineer (SRE)', 'Ensure system reliability, scalability, and performance',
 '["Linux", "Monitoring", "Automation", "Python", "Kubernetes", "Cloud", "Incident Management"]',
 '{"Linux": 90, "Monitoring": 90, "Automation": 85, "Python": 80, "Kubernetes": 85, "Cloud": 85, "Incident Management": 80}', 60),

('Cloud Architect', 'Design scalable and secure cloud-based solutions',
 '["Cloud Architecture", "AWS", "Azure", "Security", "Microservices", "Networking", "DevOps"]',
 '{"Cloud Architecture": 95, "AWS": 85, "Azure": 85, "Security": 85, "Microservices": 85, "Networking": 85, "DevOps": 80}', 65),

-- Data & Analytics
('Data Scientist', 'Analyze complex data to extract insights using statistical methods and machine learning',
 '["Python", "R", "Machine Learning", "Statistics", "SQL", "Data Visualization", "Pandas", "NumPy"]',
 '{"Python": 90, "R": 80, "Machine Learning": 90, "Statistics": 90, "SQL": 85, "Data Visualization": 80, "Pandas": 85, "NumPy": 85}', 60),

('Data Analyst', 'Interpret data and create reports to support business decisions',
 '["SQL", "Excel", "Data Visualization", "Tableau", "Power BI", "Python", "Statistics"]',
 '{"SQL": 90, "Excel": 85, "Data Visualization": 85, "Tableau": 80, "Power BI": 80, "Python": 75, "Statistics": 80}', 55),

('Data Engineer', 'Build and maintain data pipelines and infrastructure for analytics',
 '["SQL", "Python", "ETL", "Data Warehousing", "Apache Spark", "Hadoop", "Cloud"]',
 '{"SQL": 95, "Python": 85, "ETL": 90, "Data Warehousing": 85, "Apache Spark": 80, "Hadoop": 75, "Cloud": 80}', 60),

('Machine Learning Engineer', 'Develop and deploy machine learning models into production',
 '["Python", "Machine Learning", "Deep Learning", "TensorFlow", "PyTorch", "MLOps", "Cloud"]',
 '{"Python": 95, "Machine Learning": 95, "Deep Learning": 90, "TensorFlow": 85, "PyTorch": 85, "MLOps": 80, "Cloud": 80}', 65),

('AI Engineer', 'Build artificial intelligence systems and neural networks',
 '["Python", "Deep Learning", "NLP", "Computer Vision", "TensorFlow", "PyTorch", "Mathematics"]',
 '{"Python": 90, "Deep Learning": 95, "NLP": 85, "Computer Vision": 85, "TensorFlow": 85, "PyTorch": 85, "Mathematics": 85}', 65),

('Business Intelligence Analyst', 'Transform data into actionable business insights using BI tools',
 '["SQL", "Power BI", "Tableau", "Data Modeling", "DAX", "Excel", "Data Analysis"]',
 '{"SQL": 90, "Power BI": 90, "Tableau": 85, "Data Modeling": 85, "DAX": 80, "Excel": 80, "Data Analysis": 85}', 60),

-- Security
('Cybersecurity Analyst', 'Protect systems and networks from cyber threats and attacks',
 '["Security", "Networking", "Ethical Hacking", "SIEM", "Incident Response", "Linux", "Firewalls"]',
 '{"Security": 95, "Networking": 85, "Ethical Hacking": 85, "SIEM": 80, "Incident Response": 85, "Linux": 80, "Firewalls": 80}', 60),

('Security Engineer', 'Design and implement security solutions for applications and infrastructure',
 '["Security", "Cryptography", "Penetration Testing", "Cloud Security", "Networking", "Compliance"]',
 '{"Security": 95, "Cryptography": 85, "Penetration Testing": 85, "Cloud Security": 85, "Networking": 85, "Compliance": 75}', 60),

('Penetration Tester', 'Test systems for vulnerabilities by simulating cyber attacks',
 '["Ethical Hacking", "Penetration Testing", "Security Tools", "Scripting", "Networking", "Linux"]',
 '{"Ethical Hacking": 95, "Penetration Testing": 95, "Security Tools": 90, "Scripting": 80, "Networking": 85, "Linux": 85}', 65),

('Security Architect', 'Design comprehensive security frameworks and strategies',
 '["Security Architecture", "Risk Assessment", "Compliance", "Cloud Security", "Networking", "Cryptography"]',
 '{"Security Architecture": 95, "Risk Assessment": 90, "Compliance": 85, "Cloud Security": 85, "Networking": 85, "Cryptography": 85}', 65),

-- Design & UX
('UI/UX Designer', 'Create user-centered designs for digital products',
 '["UI Design", "UX Design", "Figma", "Adobe XD", "Prototyping", "User Research", "Wireframing"]',
 '{"UI Design": 90, "UX Design": 95, "Figma": 90, "Adobe XD": 85, "Prototyping": 85, "User Research": 85, "Wireframing": 85}', 60),

('Product Designer', 'Design end-to-end product experiences from research to final design',
 '["UX Design", "UI Design", "Figma", "User Research", "Prototyping", "Design Thinking", "Usability Testing"]',
 '{"UX Design": 95, "UI Design": 90, "Figma": 90, "User Research": 90, "Prototyping": 85, "Design Thinking": 85, "Usability Testing": 85}', 60),

('UX Researcher', 'Conduct user research to inform product design decisions',
 '["User Research", "Usability Testing", "Data Analysis", "Survey Design", "Interviewing", "Personas", "Analytics"]',
 '{"User Research": 95, "Usability Testing": 90, "Data Analysis": 85, "Survey Design": 85, "Interviewing": 85, "Personas": 80, "Analytics": 80}', 60),

-- Database & Systems
('Database Administrator', 'Manage and optimize database systems for performance and reliability',
 '["SQL", "Database Design", "Performance Tuning", "Backup & Recovery", "PostgreSQL", "MySQL", "MongoDB"]',
 '{"SQL": 95, "Database Design": 90, "Performance Tuning": 90, "Backup & Recovery": 85, "PostgreSQL": 85, "MySQL": 85, "MongoDB": 75}', 60),

('System Administrator', 'Manage and maintain IT infrastructure and servers',
 '["Linux", "Windows Server", "Networking", "Scripting", "Virtualization", "Security", "Monitoring"]',
 '{"Linux": 90, "Windows Server": 85, "Networking": 85, "Scripting": 80, "Virtualization": 80, "Security": 80, "Monitoring": 80}', 55),

('Network Engineer', 'Design, implement, and manage computer networks',
 '["Networking", "Cisco", "TCP/IP", "Routing", "Switching", "Firewalls", "VPN"]',
 '{"Networking": 95, "Cisco": 85, "TCP/IP": 90, "Routing": 85, "Switching": 85, "Firewalls": 80, "VPN": 80}', 60),

-- QA & Testing
('QA Engineer', 'Ensure software quality through testing and automation',
 '["Testing", "Test Automation", "Selenium", "API Testing", "Bug Tracking", "Test Planning", "CI/CD"]',
 '{"Testing": 95, "Test Automation": 90, "Selenium": 85, "API Testing": 85, "Bug Tracking": 80, "Test Planning": 85, "CI/CD": 75}', 60),

('Test Automation Engineer', 'Develop automated testing frameworks and scripts',
 '["Test Automation", "Selenium", "Python", "Java", "CI/CD", "API Testing", "Performance Testing"]',
 '{"Test Automation": 95, "Selenium": 90, "Python": 85, "Java": 85, "CI/CD": 85, "API Testing": 85, "Performance Testing": 80}', 60),

('Performance Tester', 'Test and optimize application performance and scalability',
 '["Performance Testing", "JMeter", "LoadRunner", "Monitoring", "Scripting", "Analysis", "SQL"]',
 '{"Performance Testing": 95, "JMeter": 90, "LoadRunner": 80, "Monitoring": 85, "Scripting": 80, "Analysis": 85, "SQL": 75}', 60),

-- Management & Leadership
('Technical Project Manager', 'Lead technical projects from planning to delivery',
 '["Project Management", "Agile", "Scrum", "JIRA", "Communication", "Technical Knowledge", "Risk Management"]',
 '{"Project Management": 95, "Agile": 90, "Scrum": 90, "JIRA": 80, "Communication": 90, "Technical Knowledge": 80, "Risk Management": 85}', 60),

('Scrum Master', 'Facilitate agile development processes and remove team impediments',
 '["Scrum", "Agile", "Facilitation", "JIRA", "Communication", "Coaching", "Conflict Resolution"]',
 '{"Scrum": 95, "Agile": 95, "Facilitation": 90, "JIRA": 80, "Communication": 90, "Coaching": 85, "Conflict Resolution": 85}', 60),

('Product Manager', 'Define product strategy and roadmap based on market research',
 '["Product Management", "Market Research", "User Stories", "Roadmapping", "Communication", "Analytics", "Agile"]',
 '{"Product Management": 95, "Market Research": 85, "User Stories": 85, "Roadmapping": 85, "Communication": 90, "Analytics": 80, "Agile": 80}', 60),

('Engineering Manager', 'Lead and mentor engineering teams while driving technical excellence',
 '["Leadership", "Team Management", "Technical Knowledge", "Agile", "Communication", "Mentoring", "Strategic Planning"]',
 '{"Leadership": 95, "Team Management": 90, "Technical Knowledge": 85, "Agile": 85, "Communication": 90, "Mentoring": 85, "Strategic Planning": 85}', 65),

-- Emerging Technologies
('Blockchain Developer', 'Build decentralized applications using blockchain technology',
 '["Blockchain", "Solidity", "Smart Contracts", "Web3", "Cryptography", "JavaScript", "Ethereum"]',
 '{"Blockchain": 95, "Solidity": 90, "Smart Contracts": 90, "Web3": 85, "Cryptography": 80, "JavaScript": 80, "Ethereum": 85}', 65),

('IoT Developer', 'Develop applications for Internet of Things devices',
 '["IoT", "Embedded Systems", "C/C++", "Python", "Sensors", "MQTT", "Cloud"]',
 '{"IoT": 95, "Embedded Systems": 90, "C/C++": 85, "Python": 80, "Sensors": 85, "MQTT": 80, "Cloud": 75}', 60),

('AR/VR Developer', 'Create augmented and virtual reality experiences',
 '["Unity", "C#", "3D Modeling", "AR/VR", "Game Development", "Graphics", "UI/UX"]',
 '{"Unity": 90, "C#": 85, "3D Modeling": 85, "AR/VR": 95, "Game Development": 85, "Graphics": 80, "UI/UX": 75}', 65),

('Game Developer', 'Design and develop video games for various platforms',
 '["Game Development", "Unity", "C#", "C++", "3D Modeling", "Game Design", "Physics"]',
 '{"Game Development": 95, "Unity": 90, "C#": 85, "C++": 85, "3D Modeling": 80, "Game Design": 85, "Physics": 75}', 60),

-- Specialized Roles
('Solutions Architect', 'Design complex technical solutions to meet business requirements',
 '["System Design", "Architecture", "Cloud", "Microservices", "Integration", "Communication", "Technical Leadership"]',
 '{"System Design": 95, "Architecture": 95, "Cloud": 85, "Microservices": 85, "Integration": 85, "Communication": 85, "Technical Leadership": 85}', 65),

('IT Consultant', 'Advise organizations on technology strategy and implementation',
 '["IT Strategy", "Business Analysis", "Communication", "Problem Solving", "Technical Knowledge", "Project Management"]',
 '{"IT Strategy": 90, "Business Analysis": 90, "Communication": 95, "Problem Solving": 90, "Technical Knowledge": 85, "Project Management": 80}', 60),

('Technical Writer', 'Create documentation for technical products and processes',
 '["Technical Writing", "Documentation", "Communication", "Technical Knowledge", "Tools", "API Documentation"]',
 '{"Technical Writing": 95, "Documentation": 95, "Communication": 90, "Technical Knowledge": 80, "Tools": 75, "API Documentation": 85}', 55),

('Support Engineer', 'Provide technical support and troubleshooting for software products',
 '["Troubleshooting", "Customer Service", "Technical Knowledge", "Communication", "Documentation", "Ticketing Systems"]',
 '{"Troubleshooting": 90, "Customer Service": 90, "Technical Knowledge": 85, "Communication": 90, "Documentation": 80, "Ticketing Systems": 75}', 55),

('Sales Engineer', 'Bridge technical and sales teams to close deals',
 '["Technical Knowledge", "Communication", "Presentations", "Problem Solving", "Sales", "Product Demos"]',
 '{"Technical Knowledge": 85, "Communication": 95, "Presentations": 90, "Problem Solving": 85, "Sales": 85, "Product Demos": 85}', 55),

('Embedded Systems Engineer', 'Develop software for embedded hardware devices',
 '["C/C++", "Embedded Systems", "Microcontrollers", "RTOS", "Hardware", "Debugging", "Protocols"]',
 '{"C/C++": 95, "Embedded Systems": 95, "Microcontrollers": 90, "RTOS": 85, "Hardware": 80, "Debugging": 85, "Protocols": 80}', 60);

-- Show results
SELECT COUNT(*) as total_careers FROM careers;
SELECT career_name FROM careers ORDER BY career_name;
