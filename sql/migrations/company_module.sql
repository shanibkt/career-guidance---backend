-- ================================================
-- Company Module Migration
-- Career Guidance Platform
-- ================================================

-- 1. Companies table
CREATE TABLE IF NOT EXISTS companies (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    industry VARCHAR(100),
    logo_url VARCHAR(500),
    website VARCHAR(500),
    location VARCHAR(255),
    contact_email VARCHAR(255),
    is_approved BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 2. Company users mapping (links user accounts to companies)
CREATE TABLE IF NOT EXISTS company_users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    company_id INT NOT NULL,
    role VARCHAR(50) DEFAULT 'owner',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_company (user_id, company_id)
);

-- 3. Hiring notifications posted by companies
CREATE TABLE IF NOT EXISTS hiring_notifications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    company_id INT NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    position VARCHAR(255) NOT NULL,
    location VARCHAR(255),
    salary_range VARCHAR(100),
    requirements TEXT,
    target_career_ids TEXT,
    application_deadline DATE,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE
);

-- 4. Per-student notification delivery tracking
CREATE TABLE IF NOT EXISTS student_notifications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    hiring_notification_id INT NOT NULL,
    is_read BOOLEAN DEFAULT FALSE,
    read_at TIMESTAMP NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (hiring_notification_id) REFERENCES hiring_notifications(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_notification (user_id, hiring_notification_id)
);

-- 5. Job applications from students
CREATE TABLE IF NOT EXISTS job_applications (
    id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    hiring_notification_id INT NOT NULL,
    company_id INT NOT NULL,
    cover_message TEXT,
    resume_data TEXT,
    status VARCHAR(50) DEFAULT 'pending',
    applied_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (hiring_notification_id) REFERENCES hiring_notifications(id) ON DELETE CASCADE,
    FOREIGN KEY (company_id) REFERENCES companies(id) ON DELETE CASCADE,
    UNIQUE KEY unique_user_application (user_id, hiring_notification_id)
);

-- Indexes for performance
CREATE INDEX idx_company_users_user ON company_users(user_id);
CREATE INDEX idx_company_users_company ON company_users(company_id);
CREATE INDEX idx_hiring_notifications_company ON hiring_notifications(company_id);
CREATE INDEX idx_hiring_notifications_active ON hiring_notifications(is_active);
CREATE INDEX idx_student_notifications_user ON student_notifications(user_id);
CREATE INDEX idx_student_notifications_read ON student_notifications(user_id, is_read);
CREATE INDEX idx_job_applications_user ON job_applications(user_id);
CREATE INDEX idx_job_applications_company ON job_applications(company_id);
CREATE INDEX idx_job_applications_notification ON job_applications(hiring_notification_id);
