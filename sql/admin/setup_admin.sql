-- Setup Admin Module for my_database
USE my_database;

-- 1. Add Role column to Users table if not exists
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS Role VARCHAR(20) DEFAULT 'user';

-- 2. Add index for Role
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);

-- 3. Create default admin user (password: Admin@123)
-- First, let's check if user exists and delete if needed
DELETE FROM Users WHERE Email = 'admin@careerguidance.com';

-- Insert admin user with proper BCrypt hash
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'admin',
    'System Administrator',
    'admin@careerguidance.com',
    '$2a$11$xHZf8c8VVJKJKxjW6J5iYO8vZ0xZQGqL5Y7qZ8yZ9yZ0yZ1yZ2yZ3',
    'admin',
    NOW()
);

-- 4. Create admin activity log table
CREATE TABLE IF NOT EXISTS admin_activity_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_user_id INT NULL,
    description TEXT NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (admin_id) REFERENCES Users(Id) ON DELETE CASCADE,
    FOREIGN KEY (target_user_id) REFERENCES Users(Id) ON DELETE SET NULL
);

-- 5. Create admin dashboard view
CREATE OR REPLACE VIEW vw_admin_dashboard AS
SELECT 
    (SELECT COUNT(*) FROM Users) as total_users,
    (SELECT COUNT(*) FROM Users WHERE DATE(CreatedAt) = CURDATE()) as users_today,
    (SELECT COUNT(*) FROM Users WHERE CreatedAt >= DATE_SUB(NOW(), INTERVAL 7 DAY)) as users_week,
    (SELECT COUNT(DISTINCT user_id) FROM video_watch_history WHERE DATE(last_watched) = CURDATE()) as active_users_today,
    (SELECT COUNT(DISTINCT user_id) FROM video_watch_history WHERE last_watched >= DATE_SUB(NOW(), INTERVAL 7 DAY)) as active_users_week,
    (SELECT COUNT(*) FROM video_watch_history) as total_videos_watched,
    (SELECT COUNT(*) FROM resumes) as total_resumes,
    (SELECT COALESCE(AVG(overall_progress), 0) FROM user_career_progress WHERE is_active = TRUE) as avg_user_progress;

-- 6. Create stored procedure for logging admin actions
DELIMITER //

CREATE PROCEDURE IF NOT EXISTS LogAdminAction(
    IN p_admin_id INT,
    IN p_action_type VARCHAR(50),
    IN p_target_user_id INT,
    IN p_description TEXT,
    IN p_ip_address VARCHAR(45)
)
BEGIN
    INSERT INTO admin_activity_log (admin_id, action_type, target_user_id, description, ip_address)
    VALUES (p_admin_id, p_action_type, p_target_user_id, p_description, p_ip_address);
END //

DELIMITER ;

-- 7. Verify admin user was created
SELECT Id, Username, Email, Role, CreatedAt 
FROM Users 
WHERE Email = 'admin@careerguidance.com';
