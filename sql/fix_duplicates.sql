-- Fix duplicate UserProfile rows
USE my_database;

-- Step 1: Show current duplicates
SELECT UserId, COUNT(*) as ProfileCount 
FROM UserProfiles 
GROUP BY UserId 
HAVING COUNT(*) > 1;

-- Step 2: Delete duplicates (keep only the most recent one)
DELETE p1 FROM UserProfiles p1
INNER JOIN UserProfiles p2 
WHERE p1.UserId = p2.UserId 
  AND p1.Id < p2.Id;

-- Step 3: Add unique constraint to prevent future duplicates
ALTER TABLE UserProfiles 
ADD CONSTRAINT UQ_UserProfiles_UserId UNIQUE (UserId);

-- Step 4: Verify - should show 0 rows
SELECT UserId, COUNT(*) as ProfileCount 
FROM UserProfiles 
GROUP BY UserId 
HAVING COUNT(*) > 1;
