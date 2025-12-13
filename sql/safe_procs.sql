-- Safe stored procedures creation
-- Replaces procedures without dropping tables
-- Fixed for case-sensitive table names (lowercase)

USE freedb_career_guidence;

DELIMITER $$

-- Create user (for registration)
DROP PROCEDURE IF EXISTS sp_create_user$$
CREATE PROCEDURE sp_create_user(
  IN p_username VARCHAR(100),
  IN p_fullName VARCHAR(200),
  IN p_email VARCHAR(255),
  IN p_passwordHash VARCHAR(255)
)
BEGIN
  INSERT INTO users (Username, FullName, Email, PasswordHash)
  VALUES (p_username, p_fullName, p_email, p_passwordHash);
  SELECT LAST_INSERT_ID() AS Id;
END$$

-- Get user by email (for login)
DROP PROCEDURE IF EXISTS sp_get_user_by_email$$
CREATE PROCEDURE sp_get_user_by_email(
  IN p_email VARCHAR(255)
)
BEGIN
  SELECT Id, Username, FullName, Email, PasswordHash, CreatedAt, UpdatedAt 
  FROM users 
  WHERE Email = p_email 
  LIMIT 1;
END$$

-- Get user by ID
DROP PROCEDURE IF EXISTS sp_get_user_by_id$$
CREATE PROCEDURE sp_get_user_by_id(
  IN p_userId INT
)
BEGIN
  SELECT Id, Username, FullName, Email, CreatedAt, UpdatedAt 
  FROM users 
  WHERE Id = p_userId 
  LIMIT 1;
END$$

-- Update user
DROP PROCEDURE IF EXISTS sp_update_user$$
CREATE PROCEDURE sp_update_user(
  IN p_userId INT,
  IN p_fullName VARCHAR(200),
  IN p_username VARCHAR(100),
  IN p_email VARCHAR(255)
)
BEGIN
  UPDATE users 
  SET FullName = p_fullName, Username = p_username, Email = p_email 
  WHERE Id = p_userId;
  SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Delete user
DROP PROCEDURE IF EXISTS sp_delete_user$$
CREATE PROCEDURE sp_delete_user(
  IN p_userId INT
)
BEGIN
  DELETE FROM users WHERE Id = p_userId;
  SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Create or update profile
DROP PROCEDURE IF EXISTS sp_create_or_update_profile$$
CREATE PROCEDURE sp_create_or_update_profile(
  IN p_userId INT,
  IN p_phoneNumber VARCHAR(20),
  IN p_age INT,
  IN p_gender VARCHAR(20),
  IN p_educationLevel VARCHAR(100),
  IN p_fieldOfStudy VARCHAR(200),
  IN p_skills JSON,
  IN p_careerPath VARCHAR(255),
  IN p_profileImagePath VARCHAR(500)
)
BEGIN
  INSERT INTO userprofiles 
    (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, career_path, ProfileImagePath)
  VALUES 
    (p_userId, p_phoneNumber, p_age, p_gender, p_educationLevel, p_fieldOfStudy, p_skills, p_careerPath, p_profileImagePath)
  ON DUPLICATE KEY UPDATE
    PhoneNumber = VALUES(PhoneNumber),
    Age = VALUES(Age),
    Gender = VALUES(Gender),
    EducationLevel = VALUES(EducationLevel),
    FieldOfStudy = VALUES(FieldOfStudy),
    Skills = VALUES(Skills),
    career_path = VALUES(career_path),
    ProfileImagePath = COALESCE(VALUES(ProfileImagePath), ProfileImagePath);
  
  SELECT Id, UserId FROM userprofiles WHERE UserId = p_userId LIMIT 1;
END$$

-- Get profile by user ID
DROP PROCEDURE IF EXISTS sp_get_profile_by_userid$$
CREATE PROCEDURE sp_get_profile_by_userid(
  IN p_userId INT
)
BEGIN
  SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, 
         Skills, career_path, ProfileImagePath, CreatedAt, UpdatedAt
  FROM userprofiles 
  WHERE UserId = p_userId 
  LIMIT 1;
END$$

DELIMITER ;
