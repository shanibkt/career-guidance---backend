-- Create Chat Sessions Table
CREATE TABLE IF NOT EXISTS ChatSessions (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    UserId INT NOT NULL,
    SessionId VARCHAR(36) NOT NULL UNIQUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES users(id) ON DELETE CASCADE,
    INDEX idx_user_sessions (UserId, CreatedAt),
    INDEX idx_session_id (SessionId)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Create Chat Messages Table
CREATE TABLE IF NOT EXISTS ChatMessages (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    SessionId VARCHAR(36) NOT NULL,
    Role VARCHAR(20) NOT NULL,
    Message TEXT NOT NULL,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SessionId) REFERENCES ChatSessions(SessionId) ON DELETE CASCADE,
    INDEX idx_session_messages (SessionId, Timestamp),
    INDEX idx_timestamp (Timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
