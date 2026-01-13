-- Update ChatSessions table to add new columns for chat history feature
USE my_database;

-- Add new columns to ChatSessions
ALTER TABLE ChatSessions 
ADD COLUMN Title VARCHAR(200) DEFAULT 'New Conversation',
ADD COLUMN LastMessage VARCHAR(500),
ADD COLUMN UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
ADD COLUMN IsDeleted TINYINT(1) DEFAULT 0;

-- Add index for UpdatedAt for sorting
CREATE INDEX idx_sessions_updated ON ChatSessions(UserId, UpdatedAt DESC);
CREATE INDEX idx_sessions_deleted ON ChatSessions(UserId, IsDeleted);

-- Show updated table structure
DESCRIBE ChatSessions;
