-- =============================================
-- Create Chat Tables for AI Chatbot
-- Fixed for case-sensitive table names
-- =============================================

USE freedb_career_guidence;

-- Create ChatSessions table
CREATE TABLE IF NOT EXISTS ChatSessions (
    Id VARCHAR(36) PRIMARY KEY,
    UserId INT NOT NULL,
    Title VARCHAR(255) NOT NULL DEFAULT 'New Chat',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user_sessions (UserId, UpdatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create ChatMessages table
CREATE TABLE IF NOT EXISTS ChatMessages (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    SessionId VARCHAR(36) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    Content TEXT NOT NULL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(Id) ON DELETE CASCADE,
    INDEX idx_session_messages (SessionId, CreatedAt)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Verify tables were created
SELECT 'Chat tables created successfully' AS Status;
SHOW TABLES LIKE 'Chat%';
