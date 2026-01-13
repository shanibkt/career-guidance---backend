-- ============================================
-- Fix Transcript Column Size
-- Change from TEXT (65KB) to MEDIUMTEXT (16MB)
-- ============================================

USE freedb_career_guidence;

-- Modify transcript column to handle larger content
ALTER TABLE learning_videos 
MODIFY COLUMN transcript MEDIUMTEXT NULL 
COMMENT 'Video transcript/subtitle content for quiz generation (up to 16MB)';

-- Verify the change
DESCRIBE learning_videos;

-- Expected: transcript should now be MEDIUMTEXT
