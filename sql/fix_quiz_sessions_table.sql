-- Fix quiz_sessions table - Add missing 'completed' column
-- This column is required by the quiz submission endpoint

USE freedb_career_guidence;

-- Add completed column if it doesn't exist
ALTER TABLE quiz_sessions 
ADD COLUMN IF NOT EXISTS completed BOOLEAN DEFAULT FALSE;

-- Verify the change
DESCRIBE quiz_sessions;

-- Update existing records to be consistent
UPDATE quiz_sessions 
SET completed = TRUE 
WHERE completed_at IS NOT NULL;

SELECT 'Quiz sessions table fixed successfully!' AS status;
