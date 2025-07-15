-- Migration 001: Initial Schema with Feedback Configuration
-- Description: Create initial database tables for ApBox system including feedback configuration

-- Reader configurations table
CREATE TABLE reader_configurations (
    reader_id TEXT PRIMARY KEY,
    reader_name TEXT NOT NULL,
    address INTEGER NOT NULL DEFAULT 1,
    is_enabled INTEGER NOT NULL DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Card events table for logging
CREATE TABLE card_events (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    reader_id TEXT NOT NULL,
    card_number TEXT NOT NULL,
    bit_length INTEGER NOT NULL,
    reader_name TEXT NOT NULL,
    success BOOLEAN NOT NULL,
    message TEXT,
    processed_by_plugin TEXT,
    timestamp DATETIME NOT NULL
);

-- Plugin configurations table
CREATE TABLE plugin_configurations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    plugin_name TEXT NOT NULL,
    configuration_key TEXT NOT NULL,
    configuration_value TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(plugin_name, configuration_key)
);

-- Feedback configurations table for default feedback patterns
CREATE TABLE feedback_configurations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    configuration_type TEXT NOT NULL,
    led_color TEXT,
    led_duration_seconds INTEGER,
    beep_count INTEGER,
    display_message TEXT,
    permanent_led_color TEXT,
    heartbeat_flash_color TEXT,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL,
    UNIQUE(configuration_type)
);

-- System logs table
CREATE TABLE system_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    timestamp DATETIME NOT NULL,
    level TEXT NOT NULL,
    category TEXT NOT NULL,
    message TEXT NOT NULL,
    exception TEXT
);

-- Create indexes for better performance
CREATE INDEX idx_card_events_reader_id ON card_events(reader_id);
CREATE INDEX idx_card_events_timestamp ON card_events(timestamp);
CREATE INDEX idx_feedback_configurations_type ON feedback_configurations(configuration_type);
CREATE INDEX idx_system_logs_timestamp ON system_logs(timestamp);
CREATE INDEX idx_system_logs_level ON system_logs(level);

-- Insert default feedback configurations
INSERT INTO feedback_configurations 
(configuration_type, led_color, led_duration_seconds, beep_count, display_message, permanent_led_color, heartbeat_flash_color, created_at, updated_at)
VALUES 
-- Success feedback: Green LED, 1 second, 1 beep, "ACCESS GRANTED"
('success', 'Green', 1, 1, 'ACCESS GRANTED', NULL, NULL, datetime('now'), datetime('now')),

-- Failure feedback: Red LED, 2 seconds, 3 beeps, "ACCESS DENIED"  
('failure', 'Red', 2, 3, 'ACCESS DENIED', NULL, NULL, datetime('now'), datetime('now')),

-- Idle state: Blue permanent LED with Green heartbeat flash
('idle', NULL, NULL, NULL, NULL, 'Blue', 'Green', datetime('now'), datetime('now'));