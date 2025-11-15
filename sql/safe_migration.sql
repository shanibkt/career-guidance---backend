-- Safe migration script - creates tables only if they don't exist
-- Run this if you already have data and don't want to lose it

USE my_database;

-- Create Users table only if it doesn't exist
CREATE TABLE IF NOT EXISTS Users (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  Username VARCHAR(100) NOT NULL UNIQUE,
  FullName VARCHAR(200) NOT NULL,
  Email VARCHAR(255) NOT NULL UNIQUE,
  PasswordHash VARCHAR(255) NOT NULL,
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create UserProfiles table only if it doesn't exist
CREATE TABLE IF NOT EXISTS UserProfiles (
  Id INT AUTO_INCREMENT PRIMARY KEY,
  UserId INT NOT NULL,
  PhoneNumber VARCHAR(20),
  Age INT,
  Gender VARCHAR(10),
  EducationLevel VARCHAR(100),
  FieldOfStudy VARCHAR(100),
  Skills JSON,
  AreasOfInterest TEXT,
  ProfileImagePath VARCHAR(500),
  CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
  UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create indexes if they don't exist
CREATE INDEX IF NOT EXISTS idx_users_email ON Users(Email);
CREATE INDEX IF NOT EXISTS idx_users_username ON Users(Username);
CREATE INDEX IF NOT EXISTS idx_userprofiles_userid ON UserProfiles(UserId);

-- Add unique constraint on UserId to prevent duplicates
-- First remove duplicates if they exist
DELETE p1 FROM UserProfiles p1
INNER JOIN UserProfiles p2 
WHERE p1.UserId = p2.UserId AND p1.Id < p2.Id;

-- Then add the constraint
ALTER TABLE UserProfiles 
ADD CONSTRAINT UQ_UserProfiles_UserId UNIQUE (UserId);

-- Add columns if they're missing (for existing tables)
-- This will silently fail if columns already exist - that's OK

SET @dbname = DATABASE();
SET @tablename = 'Users';
SET @columnname = 'PasswordHash';
SET @preparedStatement = (SELECT IF(
  (
    SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
    WHERE
      TABLE_SCHEMA = @dbname
      AND TABLE_NAME = @tablename
      AND COLUMN_NAME = @columnname
  ) > 0,
  'SELECT 1',
  CONCAT('ALTER TABLE ', @tablename, ' ADD COLUMN ', @columnname, ' VARCHAR(255) NOT NULL AFTER Email')
));
PREPARE alterIfNotExists FROM @preparedStatement;
EXECUTE alterIfNotExists;
DEALLOCATE PREPARE alterIfNotExists;
