-- =============================================================================
-- Agent Checkpoint/Resume Pipeline — SQLite Schema
-- "Game save system" for AI agents with context window management
-- =============================================================================
-- Design principles:
--   1. WAL mode for crash safety (set via PRAGMA at connection time)
--   2. Base state reloaded fresh every session; checkpoints are deltas
--   3. One active task at a time; others suspended (enforced by trigger)
--   4. Everything queryable for post-mortem debugging
--   5. Foreign keys enforced (PRAGMA foreign_keys = ON at connection time)
-- =============================================================================

-- Connection-time PRAGMAs (run these on every db open, not in schema):
--   PRAGMA journal_mode = WAL;
--   PRAGMA foreign_keys = ON;
--   PRAGMA busy_timeout = 5000;
--   PRAGMA synchronous = NORMAL;  -- safe with WAL, faster than FULL

-- ---------------------------------------------------------------------------
-- Schema versioning — tracks migrations so the orchestrator knows what's deployed
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS schema_version (
    version     INTEGER PRIMARY KEY,
    applied_at  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    description TEXT    NOT NULL
);

INSERT INTO schema_version (version, description)
VALUES (1, 'Initial schema — tasks, sessions, checkpoints, decisions, errors, artifacts, conversations');

-- ---------------------------------------------------------------------------
-- Tasks — top-level entity, the "game" being played
-- ---------------------------------------------------------------------------
-- status lifecycle: active -> suspended -> active -> completed -> archived
-- Only ONE task may be 'active' at any time (enforced by unique partial index).
-- session_count increments each time the worker resets and resumes.
CREATE TABLE IF NOT EXISTS tasks (
    task_id         TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    status          TEXT    NOT NULL DEFAULT 'active'
                            CHECK (status IN ('active', 'suspended', 'completed', 'archived')),
    description     TEXT    NOT NULL,
    current_step    TEXT    NOT NULL DEFAULT 'Initializing',
    session_count   INTEGER NOT NULL DEFAULT 0,
    created_at      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    updated_at      TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

-- Hard constraint: at most one active task at any time.
-- SQLite partial unique indexes treat NULLs as distinct, so we use a WHERE clause.
CREATE UNIQUE INDEX IF NOT EXISTS idx_tasks_one_active
    ON tasks (status) WHERE status = 'active';

CREATE INDEX IF NOT EXISTS idx_tasks_status ON tasks (status);

-- Auto-bump updated_at on any change.
CREATE TRIGGER IF NOT EXISTS trg_tasks_updated_at
    AFTER UPDATE ON tasks
    FOR EACH ROW
BEGIN
    UPDATE tasks SET updated_at = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE task_id = NEW.task_id;
END;

-- ---------------------------------------------------------------------------
-- Sessions — one row per reset/resume cycle within a task
-- ---------------------------------------------------------------------------
-- reason_ended captures WHY the session stopped. 'checkpoint' = planned save at
-- context threshold. 'crash' = unclean exit (no checkpoint written). The
-- orchestrator writes 'crash' on startup if the previous session has no ended_at.
CREATE TABLE IF NOT EXISTS sessions (
    session_id            TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id               TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    session_number        INTEGER NOT NULL,
    started_at            TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    ended_at              TEXT,
    reason_ended          TEXT    CHECK (reason_ended IN ('checkpoint', 'complete', 'crash', 'user_exit')),
    start_context_percent REAL    NOT NULL DEFAULT 0.0 CHECK (start_context_percent BETWEEN 0.0 AND 100.0),
    end_context_percent   REAL    CHECK (end_context_percent BETWEEN 0.0 AND 100.0),
    turns_completed       INTEGER NOT NULL DEFAULT 0,
    UNIQUE (task_id, session_number)
);

CREATE INDEX IF NOT EXISTS idx_sessions_task    ON sessions (task_id);
CREATE INDEX IF NOT EXISTS idx_sessions_task_sn ON sessions (task_id, session_number DESC);

-- ---------------------------------------------------------------------------
-- Checkpoints — the "save files"
-- ---------------------------------------------------------------------------
-- Each checkpoint captures the full structured state the model wrote at save time.
-- resume_instruction is the single most important line — what to do next on reload.
-- validated = 0 until the manager/sentinel confirms the checkpoint is coherent.
-- A task may have many checkpoints (one per save); only the latest valid one is
-- used on resume.
CREATE TABLE IF NOT EXISTS checkpoints (
    checkpoint_id        TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id              TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    session_number       INTEGER NOT NULL,
    checkpoint_text      TEXT    NOT NULL,
    resume_instruction   TEXT    NOT NULL,
    context_percent_at_save REAL NOT NULL CHECK (context_percent_at_save BETWEEN 0.0 AND 100.0),
    validated            INTEGER NOT NULL DEFAULT 0 CHECK (validated IN (0, 1)),
    created_at           TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (task_id, session_number) REFERENCES sessions (task_id, session_number)
);

CREATE INDEX IF NOT EXISTS idx_checkpoints_task      ON checkpoints (task_id);
CREATE INDEX IF NOT EXISTS idx_checkpoints_task_latest ON checkpoints (task_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_checkpoints_validated  ON checkpoints (task_id, validated, created_at DESC);

-- ---------------------------------------------------------------------------
-- Decisions — prescriptive constraints carried across sessions
-- ---------------------------------------------------------------------------
-- These are the "rules of the game" that persist through resets.
-- On resume, ALL active decisions for the task are loaded into context as
-- hard constraints the model must follow.
-- checkpoint_id is nullable: some decisions are made at task creation time
-- (e.g., from the Architect), before any checkpoint exists.
CREATE TABLE IF NOT EXISTS decisions (
    decision_id   TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id       TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    checkpoint_id TEXT    REFERENCES checkpoints (checkpoint_id) ON DELETE SET NULL,
    content       TEXT    NOT NULL,
    rationale     TEXT,
    priority      TEXT    NOT NULL DEFAULT 'normal'
                          CHECK (priority IN ('critical', 'normal', 'minor')),
    active        INTEGER NOT NULL DEFAULT 1 CHECK (active IN (0, 1)),
    created_at    TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now'))
);

CREATE INDEX IF NOT EXISTS idx_decisions_task        ON decisions (task_id);
CREATE INDEX IF NOT EXISTS idx_decisions_active       ON decisions (task_id, active) WHERE active = 1;
CREATE INDEX IF NOT EXISTS idx_decisions_priority     ON decisions (task_id, priority) WHERE active = 1;

-- ---------------------------------------------------------------------------
-- Errors — mistakes log with resolution tracking
-- ---------------------------------------------------------------------------
-- lesson is the forward-looking takeaway ("avoid X pattern because Y").
-- Unresolved errors for the active task are included in the resume payload
-- so the model doesn't repeat them.
CREATE TABLE IF NOT EXISTS errors (
    error_id       TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id        TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    session_number INTEGER NOT NULL,
    description    TEXT    NOT NULL,
    resolution     TEXT,
    resolved       INTEGER NOT NULL DEFAULT 0 CHECK (resolved IN (0, 1)),
    lesson         TEXT,
    created_at     TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (task_id, session_number) REFERENCES sessions (task_id, session_number)
);

CREATE INDEX IF NOT EXISTS idx_errors_task       ON errors (task_id);
CREATE INDEX IF NOT EXISTS idx_errors_unresolved ON errors (task_id, resolved) WHERE resolved = 0;

-- ---------------------------------------------------------------------------
-- Artifacts — code files produced by the agent
-- ---------------------------------------------------------------------------
-- file_hash (SHA-256) lets the orchestrator detect external modifications.
-- status tracks intent: 'pending' means planned but not yet written to disk.
-- On archive, this table becomes the bill of materials for the completed task.
CREATE TABLE IF NOT EXISTS artifacts (
    artifact_id TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id     TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    file_path   TEXT    NOT NULL,
    file_hash   TEXT,
    status      TEXT    NOT NULL DEFAULT 'created'
                        CHECK (status IN ('created', 'modified', 'pending', 'deleted')),
    description TEXT,
    created_at  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    updated_at  TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    UNIQUE (task_id, file_path)
);

CREATE INDEX IF NOT EXISTS idx_artifacts_task ON artifacts (task_id);

CREATE TRIGGER IF NOT EXISTS trg_artifacts_updated_at
    AFTER UPDATE ON artifacts
    FOR EACH ROW
BEGIN
    UPDATE artifacts SET updated_at = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
    WHERE artifact_id = NEW.artifact_id;
END;

-- ---------------------------------------------------------------------------
-- Conversations — full message history (backup only, never loaded on resume)
-- ---------------------------------------------------------------------------
-- This is the black box flight recorder. It exists for debugging and
-- training data extraction, NOT for context injection.
-- block_type categorizes content for filtering: you can pull just checkpoints
-- or just tool results without parsing the full conversation.
-- token_count is estimated by the orchestrator at insertion time.
CREATE TABLE IF NOT EXISTS conversations (
    message_id     TEXT    PRIMARY KEY DEFAULT (lower(hex(randomblob(8)))),
    task_id        TEXT    NOT NULL REFERENCES tasks (task_id) ON DELETE CASCADE,
    session_number INTEGER NOT NULL,
    turn_number    INTEGER NOT NULL,
    role           TEXT    NOT NULL CHECK (role IN ('user', 'assistant', 'system')),
    content        TEXT    NOT NULL,
    token_count    INTEGER NOT NULL DEFAULT 0,
    block_type     TEXT    NOT NULL DEFAULT 'reasoning'
                           CHECK (block_type IN (
                               'instruction', 'reasoning', 'code_output',
                               'checkpoint', 'tool_call', 'tool_result', 'status'
                           )),
    created_at     TEXT    NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%fZ', 'now')),
    FOREIGN KEY (task_id, session_number) REFERENCES sessions (task_id, session_number)
);

-- Ordered access for replaying a session.
CREATE INDEX IF NOT EXISTS idx_conversations_replay
    ON conversations (task_id, session_number, turn_number);

-- Filter by block_type for training data extraction.
CREATE INDEX IF NOT EXISTS idx_conversations_block_type
    ON conversations (task_id, block_type);


-- =============================================================================
-- COMMON QUERIES
-- =============================================================================

-- ---------------------------------------------------------------------------
-- 1. Load resume payload for the active task
-- ---------------------------------------------------------------------------
-- This is the query the orchestrator runs on startup. It assembles everything
-- the model needs to resume: the task description, current step, the latest
-- validated checkpoint, all active decisions, and all unresolved errors.
-- The orchestrator formats this into the resume prompt.

-- 1a. Get active task + latest validated checkpoint in one shot
SELECT
    t.task_id,
    t.description,
    t.current_step,
    t.session_count,
    c.checkpoint_id,
    c.checkpoint_text,
    c.resume_instruction,
    c.context_percent_at_save,
    c.created_at AS checkpoint_created_at
FROM tasks t
LEFT JOIN checkpoints c
    ON c.task_id = t.task_id
    AND c.validated = 1
    AND c.created_at = (
        SELECT MAX(c2.created_at)
        FROM checkpoints c2
        WHERE c2.task_id = t.task_id AND c2.validated = 1
    )
WHERE t.status = 'active';

-- 1b. Active decisions for the resume payload (run separately, append to prompt)
SELECT content, rationale, priority
FROM decisions
WHERE task_id = (SELECT task_id FROM tasks WHERE status = 'active')
  AND active = 1
ORDER BY
    CASE priority WHEN 'critical' THEN 0 WHEN 'normal' THEN 1 WHEN 'minor' THEN 2 END,
    created_at;

-- 1c. Unresolved errors for the resume payload (so the model doesn't repeat them)
SELECT description, lesson
FROM errors
WHERE task_id = (SELECT task_id FROM tasks WHERE status = 'active')
  AND resolved = 0
ORDER BY created_at;


-- ---------------------------------------------------------------------------
-- 2. Show all unresolved errors for a specific task
-- ---------------------------------------------------------------------------
SELECT
    e.error_id,
    e.session_number,
    e.description,
    e.resolution,
    e.lesson,
    e.created_at
FROM errors e
WHERE e.task_id = :task_id
  AND e.resolved = 0
ORDER BY e.created_at DESC;


-- ---------------------------------------------------------------------------
-- 3. Get the latest checkpoint for a task (validated or not)
-- ---------------------------------------------------------------------------
SELECT
    c.checkpoint_id,
    c.session_number,
    c.checkpoint_text,
    c.resume_instruction,
    c.context_percent_at_save,
    c.validated,
    c.created_at
FROM checkpoints c
WHERE c.task_id = :task_id
ORDER BY c.created_at DESC
LIMIT 1;


-- ---------------------------------------------------------------------------
-- 4. List all critical decisions still active (across all tasks)
-- ---------------------------------------------------------------------------
-- Useful for the Manager/Architect to review what constraints are in play.
SELECT
    d.decision_id,
    d.task_id,
    t.description AS task_description,
    t.status      AS task_status,
    d.content,
    d.rationale,
    d.created_at
FROM decisions d
JOIN tasks t ON t.task_id = d.task_id
WHERE d.active = 1
  AND d.priority = 'critical'
ORDER BY t.status, d.created_at;


-- ---------------------------------------------------------------------------
-- 5. Archive a completed task
-- ---------------------------------------------------------------------------
-- Two-step: mark completed, then archive. The orchestrator runs KV->MD
-- conversion between these two steps, then deletes KV files from disk.
-- Step 1: mark complete (orchestrator does KV->MD here)
UPDATE tasks SET status = 'completed', current_step = 'Completed' WHERE task_id = :task_id;
-- Step 2: archive (after KV->MD conversion and KV deletion)
UPDATE tasks SET status = 'archived' WHERE task_id = :task_id AND status = 'completed';


-- ---------------------------------------------------------------------------
-- 6. Dashboard query — task status overview with session stats
-- ---------------------------------------------------------------------------
-- One row per task with aggregated metrics. Useful for monitoring.
SELECT
    t.task_id,
    t.status,
    t.description,
    t.current_step,
    t.session_count,
    t.created_at,
    t.updated_at,
    (SELECT COUNT(*)     FROM checkpoints c WHERE c.task_id = t.task_id)                   AS total_checkpoints,
    (SELECT COUNT(*)     FROM checkpoints c WHERE c.task_id = t.task_id AND c.validated = 1) AS validated_checkpoints,
    (SELECT COUNT(*)     FROM decisions d   WHERE d.task_id = t.task_id AND d.active = 1)    AS active_decisions,
    (SELECT COUNT(*)     FROM errors e      WHERE e.task_id = t.task_id AND e.resolved = 0)  AS unresolved_errors,
    (SELECT COUNT(*)     FROM artifacts a   WHERE a.task_id = t.task_id)                     AS total_artifacts,
    (SELECT SUM(s.turns_completed) FROM sessions s WHERE s.task_id = t.task_id)              AS total_turns,
    (SELECT s.end_context_percent FROM sessions s
     WHERE s.task_id = t.task_id ORDER BY s.session_number DESC LIMIT 1)                     AS last_context_percent
FROM tasks t
ORDER BY
    CASE t.status WHEN 'active' THEN 0 WHEN 'suspended' THEN 1 WHEN 'completed' THEN 2 WHEN 'archived' THEN 3 END,
    t.updated_at DESC;
