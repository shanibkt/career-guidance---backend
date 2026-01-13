-- Fix missing salary_currency column in saved_jobs table

-- Check if column exists and add it if it doesn't
ALTER TABLE saved_jobs 
ADD COLUMN IF NOT EXISTS salary_currency VARCHAR(10) DEFAULT 'USD' AFTER salary_max;

-- Show the updated table structure
DESCRIBE saved_jobs;
