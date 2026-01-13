-- Add transcript column to learning_videos table
-- This will store manually downloaded subtitles/captions for each video

-- Select the database first
USE freedb_career_guidence;

ALTER TABLE learning_videos 
ADD COLUMN transcript MEDIUMTEXT NULL 
COMMENT 'Video transcript/subtitle content for quiz generation (up to 16MB)'
AFTER youtube_video_id;

-- Note: Run this migration on your database
-- Execute: mysql -h sql.freedb.tech -u freedb_shanib -p freedb_career_guidence < add_transcript_column.sql
