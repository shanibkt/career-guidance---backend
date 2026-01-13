-- Fix Firebase video with a valid one that has captions
-- Current video 'fgdpvwEWJ9M' is unavailable

-- Option 1: Firebase Full Course by freeCodeCamp (has captions, 7 hours)
UPDATE learning_videos 
SET 
    youtube_video_id = 'zucCZe5Keyg',
    video_title = 'Firebase Complete Course',
    video_description = 'Complete Firebase tutorial covering all features',
    duration_minutes = 420
WHERE skill_name = 'Firebase';

-- Option 2: Firebase Tutorial by Fireship (shorter, has captions, 100 lessons)
-- UPDATE learning_videos 
-- SET 
--     youtube_video_id = 'vAoB4VbhRzM',
--     video_title = 'Firebase Tutorial',
--     video_description = 'Firebase fundamentals and best practices',
--     duration_minutes = 240
-- WHERE skill_name = 'Firebase';

-- Option 3: If Firebase doesn't exist in table, insert it
INSERT INTO learning_videos (skill_name, video_title, video_description, youtube_video_id, duration_minutes) 
VALUES ('Firebase', 'Firebase Complete Course', 'Complete Firebase tutorial covering all features', 'zucCZe5Keyg', 420)
ON DUPLICATE KEY UPDATE 
    youtube_video_id = 'zucCZe5Keyg',
    video_title = 'Firebase Complete Course',
    duration_minutes = 420;

-- Verify the update
SELECT skill_name, video_title, youtube_video_id, duration_minutes 
FROM learning_videos 
WHERE skill_name = 'Firebase';
