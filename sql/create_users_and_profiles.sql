-- Create database if it doesn't exist
CREATE DATABASE IF NOT EXISTS my_database;
USE my_database;

-- Drop existing tables if they exist (in correct order due to foreign keys)
DROP TABLE IF EXISTS UserProfiles;
DROP TABLE IF EXISTS Users;

-- Users table (for authentication)
CREATE TABLE Users (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Username VARCHAR(100) UNIQUE NOT NULL,
    FullName VARCHAR(200) NOT NULL,
    Email VARCHAR(255) UNIQUE NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- UserProfiles table (for additional profile data)
CREATE TABLE UserProfiles (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL UNIQUE,  -- UNIQUE to prevent duplicates
    PhoneNumber VARCHAR(20),
    Age INT,
    Gender VARCHAR(20),
    EducationLevel VARCHAR(100),
    FieldOfStudy VARCHAR(200),
    Skills JSON,
    AreasOfInterest TEXT,
    ProfileImagePath VARCHAR(500),
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
);

-- Create indexes for better performance
CREATE INDEX idx_user_email ON Users(Email);
CREATE INDEX idx_user_username ON Users(Username);
CREATE INDEX idx_profile_userid ON UserProfiles(UserId);
