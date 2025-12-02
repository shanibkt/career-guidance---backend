USE my_database;

-- Drop and recreate sp_get_profile_by_userid WITHOUT AreasOfInterest
DROP PROCEDURE IF EXISTS sp_get_profile_by_userid;

DELIMITER $$
CREATE PROCEDURE sp_get_profile_by_userid(
  IN p_userId INT
)
BEGIN
  SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy,
         Skills, ProfileImagePath, CreatedAt, UpdatedAt
  FROM UserProfiles
  WHERE UserId = p_userId
  LIMIT 1;
END$$
DELIMITER ;

-- Drop and recreate sp_create_or_update_profile WITHOUT AreasOfInterest
DROP PROCEDURE IF EXISTS sp_create_or_update_profile;

DELIMITER $$
CREATE PROCEDURE sp_create_or_update_profile(
  IN p_userId INT,
  IN p_phoneNumber VARCHAR(20),
  IN p_age INT,
  IN p_gender VARCHAR(20),
  IN p_educationLevel VARCHAR(100),
  IN p_fieldOfStudy VARCHAR(200),
  IN p_skills JSON,
  IN p_profileImagePath VARCHAR(500)
)
BEGIN
  -- Check if profile exists
  DECLARE profile_exists INT DEFAULT 0;
  
  SELECT COUNT(*) INTO profile_exists
  FROM UserProfiles
  WHERE UserId = p_userId;
  
  IF profile_exists > 0 THEN
    -- Update existing profile
    UPDATE UserProfiles
    SET PhoneNumber = p_phoneNumber,
        Age = p_age,
        Gender = p_gender,
        EducationLevel = p_educationLevel,
        FieldOfStudy = p_fieldOfStudy,
        Skills = p_skills,
        ProfileImagePath = COALESCE(p_profileImagePath, ProfileImagePath),
        UpdatedAt = CURRENT_TIMESTAMP
    WHERE UserId = p_userId;
  ELSE
    -- Insert new profile
    INSERT INTO UserProfiles (UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy, Skills, ProfileImagePath)
    VALUES (p_userId, p_phoneNumber, p_age, p_gender, p_educationLevel, p_fieldOfStudy, p_skills, p_profileImagePath);
  END IF;
  
  -- Return the updated/created profile
  SELECT Id, UserId, PhoneNumber, Age, Gender, EducationLevel, FieldOfStudy,
         Skills, ProfileImagePath, CreatedAt, UpdatedAt
  FROM UserProfiles
  WHERE UserId = p_userId
  LIMIT 1;
END$$
DELIMITER ;

SELECT 'Stored procedures updated successfully!' AS Status;
