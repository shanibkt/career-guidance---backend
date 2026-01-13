-- Admin User Setup for Career Guidance System
-- Run this in MySQL Workbench connected to: sql.freedb.tech / freedb_career_guidence

-- STEP 1: Check if admin user exists
SELECT Id, Username, FullName, Email, Role, is_admin 
FROM users 
WHERE Email = 'admin@careerguidance.com';

-- STEP 2: If user exists (from API registration), update to admin role
UPDATE users 
SET Role = 'Admin', 
    is_admin = 1 
WHERE Email = 'admin@careerguidance.com';

-- STEP 3: Verify admin user is set up correctly
SELECT Id, Username, FullName, Email, Role, is_admin, CreatedAt
FROM users 
WHERE Email = 'admin@careerguidance.com';

-- EXPECTED RESULT:
-- Id | Username | FullName              | Email                        | Role  | is_admin | CreatedAt
-- 20 | admin    | System Administrator  | admin@careerguidance.com     | Admin | 1        | 2026-01-13...

-- STEP 4: List all admin users
SELECT Id, Username, FullName, Email, Role, is_admin 
FROM users 
WHERE Role = 'Admin' OR is_admin = 1;

-- =====================================================
-- ALTERNATIVE: If you want to make an existing user admin
-- Replace 'YOUR_EMAIL@example.com' with your actual email
-- =====================================================
-- UPDATE users 
-- SET Role = 'Admin', is_admin = 1 
-- WHERE Email = 'YOUR_EMAIL@example.com';

-- =====================================================
-- ALTERNATIVE: Create admin from scratch (if not created via API)
-- Note: Password hash for "Admin@123"
-- =====================================================
-- INSERT INTO users (Username, FullName, Email, PasswordHash, Role, is_admin, CreatedAt, UpdatedAt)
-- SELECT * FROM (SELECT 
--     'admin' as Username,
--     'System Administrator' as FullName,
--     'admin@careerguidance.com' as Email,
--     '$2a$11$vB5GqPQQzJW6YHXjGjxGauVtW9PpO3gqZJH4xR5YGxYsQqH5QqH5O' as PasswordHash,
--     'Admin' as Role,
--     1 as is_admin,
--     NOW() as CreatedAt,
--     NOW() as UpdatedAt
-- ) AS tmp
-- WHERE NOT EXISTS (
--     SELECT Email FROM users WHERE Email = 'admin@careerguidance.com'
-- );

-- =====================================================
-- After running above commands, you can login at:
-- http://localhost:5001/admin.html
-- Email: admin@careerguidance.com
-- Password: Admin@123
-- =====================================================

