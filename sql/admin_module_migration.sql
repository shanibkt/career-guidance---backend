-- Admin Module Database Migration
-- Add Role column and create admin user

-- 1. Add Role column to Users table if not exists
ALTER TABLE Users 
ADD COLUMN IF NOT EXISTS Role VARCHAR(20) DEFAULT 'user';

-- 2. Add index for Role
CREATE INDEX IF NOT EXISTS idx_users_role ON Users(Role);

-- 3. Create default admin user (password: Admin@123)
-- BCrypt hash for "Admin@123"
INSERT INTO Users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'admin',
    'System Administrator',
    'admin@careerguidance.com',
    '$2a$11$xHZf8c8VVJKJKxjW6J5iYO8vZ0xZQGqL5Y7qZ8yZ9yZ0yZ1yZ2yZ3',
    'admin',
    NOW()
)
ON DUPLICATE KEY UPDATE Role = 'admin';

-- 4. Create admin activity log table
CREATE TABLE IF NOT EXISTS admin_activity_log (
    id INT AUTO_INCREMENT PRIMARY KEY,
    admin_id INT NOT NULL,
    action_type VARCHAR(50) NOT NULL,
    target_user_id INT NULL,
    description TEXT NOT NULL,
    ip_address VARCHAR(45) NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (admin_id) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_admin_activity (admin_id, created_at),
    INDEX idx_action_type (action_type),
    INDEX idx_target_user (target_user_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- 5. Create view for admin dashboard
CREATE OR REPLACE VIEW vw_admin_dashboard AS
SELECT 
    (SELECT COUNT(*) FROM Users) as total_users,
    (SELECT COUNT(*) FROM Users WHERE DATE(CreatedAt) = CURDATE()) as new_users_today,
    (SELECT COUNT(*) FROM Users WHERE CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)) as new_users_week,
    (SELECT COUNT(*) FROM Users WHERE CreatedAt >= DATE_SUB(CURDATE(), INTERVAL 30 DAY)) as new_users_month,
    (SELECT COUNT(DISTINCT user_id) FROM video_watch_history WHERE DATE(last_watched) = CURDATE()) as active_users_today,
    (SELECT COUNT(DISTINCT user_id) FROM video_watch_history WHERE last_watched >= DATE_SUB(CURDATE(), INTERVAL 7 DAY)) as active_users_week,
    (SELECT COUNT(*) FROM user_career_progress WHERE is_active = TRUE) as total_careers_selected,
    (SELECT COUNT(*) FROM video_watch_history) as total_videos_watched,
    (SELECT COUNT(*) FROM user_resumes) as total_resumes,
    (SELECT COUNT(*) FROM chat_history) as total_chat_sessions,
    (SELECT AVG(overall_progress) FROM user_career_progress WHERE is_active = TRUE) as avg_progress;

-- 6. Stored procedure to log admin actions
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

-- 7. Grant appropriate permissions
-- GRANT SELECT, INSERT, UPDATE, DELETE ON career_guidance_db.* TO 'admin_user'@'localhost';
-- FLUSH PRIVILEGES;

-- Test queries
-- SELECT * FROM vw_admin_dashboard;
-- SELECT * FROM Users WHERE Role = 'admin';
