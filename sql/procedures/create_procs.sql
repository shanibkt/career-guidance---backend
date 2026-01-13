-- Stored procedures for Users and UserProfiles
USE my_database;

-- Drop existing procedures
DROP PROCEDURE IF EXISTS sp_create_user;
DROP PROCEDURE IF EXISTS sp_get_user_by_email;
DROP PROCEDURE IF EXISTS sp_get_user_by_id;
DROP PROCEDURE IF EXISTS sp_update_user;
DROP PROCEDURE IF EXISTS sp_delete_user;
DROP PROCEDURE IF EXISTS sp_create_or_update_profile;
DROP PROCEDURE IF EXISTS sp_get_profile_by_userid;

DELIMITER $$

-- Create user (for registration)
CREATE PROCEDURE sp_create_user(
  IN p_username VARCHAR(100),
  IN p_fullName VARCHAR(200),
  IN p_email VARCHAR(255),
  IN p_passwordHash VARCHAR(255)
)
BEGIN
  INSERT INTO Users (Username, FullName, Email, PasswordHash)
  VALUES (p_username, p_fullName, p_email, p_passwordHash);
  SELECT LAST_INSERT_ID() AS UserId;
END$$

-- Get user by email (for login)
CREATE PROCEDURE sp_get_user_by_email(
  IN p_email VARCHAR(255)
)
BEGIN
  SELECT Id, Username, FullName, Email, PasswordHash, CreatedAt, UpdatedAt 
  FROM Users 
  WHERE Email = p_email 
  LIMIT 1;
END$$

-- Get user by ID
CREATE PROCEDURE sp_get_user_by_id(
  IN p_userId INT
)
BEGIN
  SELECT Id, Username, FullName, Email, CreatedAt, UpdatedAt 
  FROM Users 
  WHERE Id = p_userId 
  LIMIT 1;
END$$

-- Update user info
CREATE PROCEDURE sp_update_user(
  IN p_userId INT,
  IN p_username VARCHAR(100),
  IN p_fullName VARCHAR(200),
  IN p_email VARCHAR(255)
)
BEGIN
  UPDATE Users 
  SET Username = p_username, 
      FullName = p_fullName, 
      Email = p_email,
      UpdatedAt = CURRENT_TIMESTAMP
  WHERE Id = p_userId;
  SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Delete user (cascade will delete profile)
CREATE PROCEDURE sp_delete_user(
  IN p_userId INT
)
BEGIN
  DELETE FROM Users WHERE Id = p_userId;
  SELECT ROW_COUNT() AS AffectedRows;
END$$

-- Create or update user profile
CREATE PROCEDURE sp_create_or_update_profile(
  IN p_userId INT,
  IN p_phoneNumber VARCHAR(20),
  IN p_age INT,
  IN p_gender VARCHAR(20),
  IN p_educationLevel VARCHAR(100),
  IN p_fieldOfStudy VARCHAR(200),
  IN p_skills JSON,
  IN p_areasOfInterest TEXT,
  IN p_profileImagePath VARCHAR(500)
)
BEGIN
  -- Check if profile exists
  IF EXISTS(SELECT 1 FROM UserProfiles WHERE UserId = p_userId) THEN
    -- Update existing profile
    UPDATE UserProfiles 
    SET PhoneNumber = p_phoneNumber,
        Age = p_age,
        Gender = p_gender,
        EducationLevel = p_educationLevel,
        FieldOfStudy = p_fieldOfStudy,
        Skills = p_skills,
        AreasOfInterest = p_areasOfInterest,
        ProfileImagePath = COALESCE(p_profileImagePath, ProfileImagePath),
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE UserId = p_userId;
  ELSE
    -- Insert new profile
    INSERT INTO UserProfiles (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, AreasOfInterest, ProfileImagePath)
    VALUES (p_userId, p_phoneNumber, p_age, p_gender, p_educationLevel, p_fieldOfStudy, p_skills, p_areasOfInterest, p_profileImagePath);
  END IF;
  
  SELECT Id, UserId FROM UserProfiles WHERE UserId = p_userId LIMIT 1;
END$$

-- Get profile by user ID
CREATE PROCEDURE sp_get_profile_by_userid(
  IN p_userId INT
)
BEGIN
  SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, AreasOfInterest, ProfileImagePath, CreatedAt, UpdatedAt
  FROM UserProfiles 
  WHERE UserId = p_userId 
  LIMIT 1;
END$$

DELIMITER ;
