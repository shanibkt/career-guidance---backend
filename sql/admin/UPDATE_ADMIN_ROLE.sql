-- ============================================
-- FIX ADMIN ROLE CASE
-- Updates admin user role from 'admin' to 'Admin'
-- ============================================

USE freedb_career_guidence;

-- Update admin user role to use proper casing
UPDATE users 
SET Role = 'Admin' 
WHERE Email = 'admin@careerguidance.com' 
   OR Username = 'admin'
   OR Role = 'admin';

-- Verify the update
SELECT Id, Username, Email, Role, CreatedAt 
FROM users 
WHERE Role = 'Admin' OR Role = 'admin';

-- Expected result: Role should be 'Admin' with capital A
