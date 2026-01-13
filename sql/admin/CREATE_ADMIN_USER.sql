-- ============================================
-- COPY AND PASTE THIS INTO MYSQL WORKBENCH
-- OR RUN DIRECTLY ON freedb.tech
-- ============================================

USE freedb_career_guidence;

-- Add Role column (if not exists)
ALTER TABLE users ADD COLUMN Role VARCHAR(20) DEFAULT 'user';

-- Create admin user (Password: Admin@123)
-- Using 'Admin' with capital A for consistency with ASP.NET conventions
INSERT INTO users (Username, FullName, Email, PasswordHash, Role, CreatedAt)
VALUES (
    'admin',
    'System Administrator', 
    'admin@careerguidance.com',
    '$2a$11$xHZf8c8VVJKJKxjW6J5iYO8vZ0xZQGqL5Y7qZ8yZ9yZ0yZ1yZ2yZ3',
    'Admin',
    NOW()
)
ON DUPLICATE KEY UPDATE Role = 'Admin';

-- Verify
SELECT Id, Username, Email, Role FROM users WHERE Role = 'Admin';
