-- Migration 001: Initial Schema
-- Description: Create initial database tables for ApBox system

-- Reader configurations table
CREATE TABLE reader_configurations (
    reader_id TEXT PRIMARY KEY,
    reader_name TEXT NOT NULL,
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
CREATE INDEX idx_system_logs_timestamp ON system_logs(timestamp);
CREATE INDEX idx_system_logs_level ON system_logs(level);