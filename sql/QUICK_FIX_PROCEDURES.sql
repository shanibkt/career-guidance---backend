-- Quick fix for missing stored procedures on freedb_career_guidence database
-- Run this script to create all required stored procedures

USE freedb_career_guidence;

DELIMITER $$

-- Get user by email (for login)
DROP PROCEDURE IF EXISTS sp_get_user_by_email$$
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
DROP PROCEDURE IF EXISTS sp_get_user_by_id$$
CREATE PROCEDURE sp_get_user_by_id(
  IN p_userId INT
)
BEGIN
  SELECT Id, Username, FullName, Email, CreatedAt, UpdatedAt 
  FROM Users 
  WHERE Id = p_userId 
  LIMIT 1;
END$$

-- Create user (for registration)
DROP PROCEDURE IF EXISTS sp_create_user$$
CREATE PROCEDURE sp_create_user(
  IN p_username VARCHAR(100),
  IN p_fullName VARCHAR(200),
  IN p_email VARCHAR(255),
  IN p_passwordHash VARCHAR(255)
)
BEGIN
  INSERT INTO Users (Username, FullName, Email, PasswordHash)
  VALUES (p_username, p_fullName, p_email, p_passwordHash);
  SELECT LAST_INSERT_ID() AS Id;
END$$

DELIMITER ;

SELECT 'Stored procedures created successfully!' AS Status;
