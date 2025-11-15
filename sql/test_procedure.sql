-- Test if stored procedure exists and works
USE my_database;

-- Check if procedure exists
SELECT ROUTINE_NAME, ROUTINE_TYPE 
FROM information_schema.ROUTINES 
WHERE ROUTINE_SCHEMA = 'my_database' 
AND ROUTINE_NAME = 'sp_create_or_update_profile';

-- Test the procedure manually
-- Replace userId=1 with your actual user ID
CALL sp_create_or_update_profile(
    1,                    -- p_userId (change this to your user's ID)
    '8089885747',        -- p_phoneNumber
    21,                  -- p_age
    'Male',              -- p_gender
    'Bachelor',          -- p_educationLevel
    'Computer Science',  -- p_fieldOfStudy
    '["java", "python"]', -- p_skills (JSON string)
    'Software Development', -- p_areasOfInterest
    NULL                 -- p_profileImagePath
);

-- Check if data was saved
SELECT * FROM UserProfiles WHERE UserId = 1;
