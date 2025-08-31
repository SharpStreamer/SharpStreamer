CREATE SCHEMA IF NOT EXISTS sharp_streamer;


CREATE TABLE IF NOT EXISTS sharp_streamer.received_events
(
    id UUID PRIMARY KEY,
    event_body JSONB NOT NULL,
    event_headers JSONB NOT NULL,
    event_key VARCHAR(200) NOT NULL,
    try_count INTEGER NOT NULL DEFAULT 0,
    sent_at TIMESTAMPTZ NOT NULL,
    flags INTEGER NOT NULL DEFAULT 0,
    timestamp TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ DEFAULT NULL,
    partition VARCHAR(200) NOT NULL,
    consumer_group VARCHAR(200) NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_failed_events ON sharp_streamer.received_events(event_key) WHERE flags & flags = 2;