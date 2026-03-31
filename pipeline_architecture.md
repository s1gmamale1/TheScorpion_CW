# GyattMaxxer5000 Checkpoint/Resume Pipeline
## Visual Architecture Document
> The "Game Save System" for AI Agents
> March 2026

---

## Table of Contents

1. [System Overview](#system-overview)
2. [The 6 Pipeline Scripts](#the-6-pipeline-scripts)
3. [SQLite Database Schema](#sqlite-database-schema-gyattdb)
4. [Full Lifecycle Flow](#full-lifecycle-flow)
5. [Resume Payload Assembly](#resume-payload-assembly)
6. [What Gets Loaded vs What Doesn't](#what-gets-loaded-vs-what-doesnt)
7. [The Game Save Analogy](#the-game-save-analogy)
8. [Hardware Layout](#hardware-layout)

---

## System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                     GYATTMAXXER5000 CHECKPOINT/RESUME PIPELINE             │
│                                                                             │
│   "A game save system for AI agents — save state, reset clean, resume."    │
│                                                                             │
│   Problem:  Context windows fill up. Quality degrades. Resets lose work.   │
│   Solution: Checkpoint the quest log, not the entire game world.           │
│   Result:   ~426 tokens on resume. 98.7% of 32K window stays free.        │
└─────────────────────────────────────────────────────────────────────────────┘

┌──────────────┐    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│   SESSION 1  │    │   SESSION 2  │    │   SESSION 3  │    │   SESSION N  │
│              │    │              │    │              │    │              │
│  Work work   │    │  Work work   │    │  Work work   │    │  Work work   │
│  work work   │    │  work work   │    │  work work   │    │  work work   │
│  ...         │    │  ...         │    │  ...         │    │  ...         │
│  80% full!   │    │  80% full!   │    │  DONE!       │    │              │
│  CHECKPOINT  │    │  CHECKPOINT  │    │  COMPLETE    │    │              │
│  ══════════  │    │  ══════════  │    │  ═════════   │    │              │
│  RESET       │    │  RESET       │    │  ARCHIVE     │    │              │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘    └──────────────┘
       │                   │                   │
       │  ~426 tokens      │  ~426 tokens      │  Markdown
       │  resume payload   │  resume payload   │  archive
       ▼                   ▼                   ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          gyatt.db (SQLite)                                  │
│                                                                             │
│  checkpoints │ decisions │ errors │ artifacts │ conversations │ sessions   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## The 6 Pipeline Scripts

Located on Mac Mini at `~/Desktop/GyattMaxxer5000/pipeline/`

```
~/Desktop/GyattMaxxer5000/pipeline/
├── db_init.py              1. Creates the database
├── task_registry.py        2. Task lifecycle management
├── checkpoint_writer.py    3. Parses and saves checkpoints
├── checkpoint_loader.py    4. Assembles resume payloads
├── session_manager.py      5. Session tracking + crash detection
├── archiver.py             6. Generates markdown archives
└── gyatt.db                   The SQLite database (created by db_init.py)
```

### Script Dependency Map

```
                        ┌────────────────┐
                        │  db_init.py    │
                        │  Creates all   │
                        │  7 tables      │
                        └───────┬────────┘
                                │
                    ┌───────────┼───────────┐
                    │           │           │
                    ▼           ▼           ▼
         ┌──────────────┐ ┌──────────┐ ┌───────────────┐
         │task_registry │ │ session  │ │  checkpoint   │
         │    .py       │ │ _manager │ │  _writer.py   │
         │              │ │   .py    │ │               │
         │ create()     │ │ start()  │ │ parse()       │
         │ suspend()    │ │ end()    │ │ validate()    │
         │ complete()   │ │ crash    │ │ save()        │
         │ archive()    │ │ detect() │ │               │
         └──────┬───────┘ └────┬─────┘ └───────┬───────┘
                │              │               │
                │              │               │
                ▼              ▼               ▼
         ┌──────────────┐ ┌──────────────────────────┐
         │ archiver.py  │ │  checkpoint_loader.py     │
         │              │ │                            │
         │ Generates MD │ │  Queries DB, assembles     │
         │ archive with │ │  resume payload (~426 tok) │
         │ achievements │ │                            │
         └──────────────┘ └────────────────────────────┘
```

### Script-by-Script Breakdown

```
┌─────────────────────────────────────────────────────────────────────┐
│  1. db_init.py                                                      │
│  ─────────────                                                      │
│  Purpose:  Creates gyatt.db with all 7 tables                       │
│  Run:      Once on first setup, or to reset the database            │
│  Input:    Nothing                                                  │
│  Output:   gyatt.db with empty tables + indexes                     │
│                                                                     │
│  Key detail: Creates a PARTIAL UNIQUE INDEX on tasks table          │
│  to enforce "only one active task at a time" at the DB level.       │
│                                                                     │
│  CREATE UNIQUE INDEX idx_one_active_task                             │
│    ON tasks(status) WHERE status = 'active';                        │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  2. task_registry.py                                                │
│  ────────────────────                                               │
│  Purpose:  Manages full task lifecycle                               │
│  Methods:  create(name, description) -> task_id                     │
│            suspend(task_id)  -- pauses, keeps state                 │
│            complete(task_id) -- marks done, triggers archiver       │
│            archive(task_id)  -- moves to archived status            │
│                                                                     │
│  Task states:   active -> suspended -> active -> completed          │
│                                   └──────────────> archived         │
│                                                                     │
│  Enforces:  Only ONE active task at a time (DB constraint)          │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  3. checkpoint_writer.py                                            │
│  ────────────────────────                                           │
│  Purpose:  Parses model's CHECKPOINT block, saves to DB             │
│  Input:    Raw model output containing ## CHECKPOINT block          │
│  Process:                                                           │
│    1. Regex-parse the 6 required checkpoint fields                  │
│    2. Validate all fields present and non-empty                     │
│    3. Extract decisions -> save to decisions table                  │
│    4. Extract errors -> save to errors table                        │
│    5. Save checkpoint record with validated=1 if complete           │
│                                                                     │
│  Checkpoint fields parsed:                                          │
│    - What I was doing                                               │
│    - What I completed                                               │
│    - What I was about to do                                         │
│    - Exact mid-task state                                           │
│    - Context when interrupted                                       │
│    - Resume instruction                                             │
│                                                                     │
│  validated = 1 if ALL 6 fields present, 0 if any missing           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  4. checkpoint_loader.py                                            │
│  ────────────────────────                                           │
│  Purpose:  Queries DB, builds the resume payload                    │
│  Output:   Single text block (~426 tokens) ready to inject          │
│                                                                     │
│  Assembly order:                                                    │
│    1. Task description + current step                               │
│    2. Latest validated checkpoint text                              │
│    3. All active decisions (CRITICAL first, then normal, minor)     │
│    4. Unresolved errors + their lessons                             │
│    5. Artifact manifest (filenames + hashes)                        │
│    6. CRITICAL CONSTRAINTS repeated (recency bias exploit)          │
│    7. Resume instruction from checkpoint                            │
│                                                                     │
│  Design choice: Critical constraints appear TWICE in the payload    │
│  -- once in natural order and once at the very end. This exploits   │
│  the transformer's recency bias to ensure they get high attention.  │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  5. session_manager.py                                              │
│  ──────────────────────                                             │
│  Purpose:  Tracks session lifecycle, detects crashes                │
│  Methods:  start_session(task_id) -> session_number                 │
│            end_session(reason)    -- checkpoint/complete/crash       │
│            record_turn(message)   -- saves to conversations table   │
│                                                                     │
│  Crash detection:                                                   │
│    On start_session(), checks if previous session has               │
│    ended_at = NULL. If so, marks it as reason='crashed'.            │
│    Crashed sessions still have their conversation log.              │
│                                                                     │
│  Session reasons:  checkpoint | complete | crashed | manual_stop    │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  6. archiver.py                                                     │
│  ───────────────                                                    │
│  Purpose:  Generates human-readable markdown on task completion     │
│  Input:    task_id (pulls everything from DB)                       │
│  Output:   completed/{task_id}.md                                   │
│                                                                     │
│  Archive contains:                                                  │
│    - Task metadata (created, completed, duration)                   │
│    - Full checkpoint history (every save point)                     │
│    - All decisions made and their rationale                         │
│    - All errors encountered and resolutions                         │
│    - Artifact manifest with file hashes                             │
│    - "Achievements" section:                                        │
│        - Sessions survived (reset count)                            │
│        - Errors encountered and fixed                               │
│        - Total turns across all sessions                            │
│        - Crash recoveries                                           │
│                                                                     │
│  Conversation logs:  NOT included in archive.                       │
│  Retained in DB for 30 days, then cleaned.                          │
└─────────────────────────────────────────────────────────────────────┘
```

---

## SQLite Database Schema (gyatt.db)

### Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              gyatt.db                                       │
│                           7 Tables Total                                    │
└─────────────────────────────────────────────────────────────────────────────┘

                              ┌──────────────────┐
                              │      tasks       │
                              │──────────────────│
                              │ task_id     (PK) │
                              │ name             │
                              │ description      │
                              │ status           │
                              │ created_at       │
                              │ completed_at     │
                              │──────────────────│
                              │ PARTIAL UNIQUE   │
                              │ INDEX on status  │
                              │ WHERE = 'active' │
                              │ (one active max) │
                              └────────┬─────────┘
                                       │
            ┌──────────────┬───────────┼───────────┬──────────────┬──────────────┐
            │              │           │           │              │              │
            │ 1:many       │ 1:many    │ 1:many    │ 1:many       │ 1:many       │
            ▼              ▼           ▼           ▼              ▼              ▼
┌────────────────┐ ┌──────────────┐ ┌──────────────┐ ┌────────────────┐ ┌──────────────────┐
│   sessions     │ │ checkpoints  │ │  decisions   │ │    errors      │ │   artifacts      │
│────────────────│ │──────────────│ │──────────────│ │────────────────│ │──────────────────│
│ session_id (PK)│ │ cp_id   (PK) │ │ dec_id  (PK) │ │ error_id  (PK) │ │ artifact_id (PK) │
│ task_id   (FK) │ │ task_id (FK) │ │ task_id (FK) │ │ task_id   (FK) │ │ task_id     (FK) │
│ session_number │ │ session_num  │ │ session_num  │ │ session_num    │ │ session_num      │
│ started_at     │ │ checkpoint   │ │ decision     │ │ error_text     │ │ filename         │
│ ended_at       │ │ _text        │ │ _text        │ │ lesson         │ │ sha256_hash      │
│ end_reason     │ │ validated    │ │ priority     │ │ resolved       │ │ created_at       │
│ turn_count     │ │ created_at   │ │ status       │ │ resolution     │ │                  │
│ peak_context%  │ │              │ │ created_at   │ │ created_at     │ │                  │
└────────┬───────┘ └──────────────┘ └──────────────┘ └────────────────┘ └──────────────────┘
         │
         │ 1:many
         ▼
┌──────────────────┐
│  conversations   │
│──────────────────│
│ conv_id     (PK) │
│ task_id     (FK) │
│ session_number   │
│ turn_number      │
│ role             │
│ content          │
│ timestamp        │
│──────────────────│
│ FLIGHT RECORDER  │
│ Never loaded     │
│ on resume.       │
│ Cleaned after    │
│ 30 days.         │
└──────────────────┘
```

### Table Details

```
┌─────────────────────────────────────────────────────────────────────┐
│  tasks — The top-level entity                                       │
├─────────────────────────────────────────────────────────────────────┤
│  Column        │ Type     │ Notes                                   │
│  ──────────────┼──────────┼─────────────────────────────────────── │
│  task_id       │ TEXT PK  │ Unique identifier (e.g. "jwt_auth")    │
│  name          │ TEXT     │ Human-readable name                     │
│  description   │ TEXT     │ Full task description                   │
│  status        │ TEXT     │ active | suspended | completed |archived│
│  created_at    │ DATETIME │ When task was created                   │
│  completed_at  │ DATETIME │ NULL until done                         │
│                                                                     │
│  Constraint: Only ONE row can have status='active' at a time.       │
│  Enforced by partial unique index, not application logic.           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  sessions — One per reset/resume cycle                              │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  session_id      │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ 1, 2, 3... per task                  │
│  started_at      │ DATETIME │ Session start time                    │
│  ended_at        │ DATETIME │ NULL if crashed (crash detection!)    │
│  end_reason      │ TEXT     │ checkpoint | complete | crashed       │
│  turn_count      │ INT      │ How many turns this session           │
│  peak_context_pct│ REAL     │ Highest context % reached             │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  checkpoints — The "save files"                                     │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  cp_id           │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ Which session wrote this              │
│  checkpoint_text │ TEXT     │ Full CHECKPOINT block content         │
│  validated       │ INT      │ 1 = all 6 fields present, 0 = partial│
│  created_at      │ DATETIME │ When checkpoint was written           │
│                                                                     │
│  Only the LATEST validated checkpoint is loaded on resume.          │
│  Older checkpoints kept for history/debugging.                      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  decisions — Prescriptive constraints carried across sessions       │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  dec_id          │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ Which session made this decision      │
│  decision_text   │ TEXT     │ The decision/constraint               │
│  rationale       │ TEXT     │ Why this decision was made            │
│  priority        │ TEXT     │ critical | normal | minor             │
│  status          │ TEXT     │ active | revoked | superseded         │
│  created_at      │ DATETIME │ When decision was recorded            │
│                                                                     │
│  On resume: ALL active decisions loaded, CRITICAL first.            │
│  Critical decisions are REPEATED at end of payload (recency bias).  │
│  Revoked/superseded decisions are NOT loaded.                       │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  errors — Mistake log with lessons learned                          │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  error_id        │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ Which session hit this error          │
│  error_text      │ TEXT     │ What went wrong                       │
│  lesson          │ TEXT     │ What was learned (don't repeat this)  │
│  resolved        │ INT      │ 0 = unresolved, 1 = fixed            │
│  resolution      │ TEXT     │ How it was fixed (NULL if unresolved) │
│  created_at      │ DATETIME │ When error occurred                   │
│                                                                     │
│  On resume: Only UNRESOLVED errors loaded (with their lessons).     │
│  Resolved errors stay in DB for archive but don't cost tokens.      │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  artifacts — Code files produced, tracked by content hash           │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  artifact_id     │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ Which session created/modified it     │
│  filename        │ TEXT     │ Relative path to the file             │
│  sha256_hash     │ TEXT     │ Content hash for integrity            │
│  created_at      │ DATETIME │ When artifact was recorded            │
│                                                                     │
│  On resume: Manifest loaded (filename + hash list).                 │
│  Model can verify files haven't changed between sessions.           │
│  Full file contents are NOT stored in DB.                           │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│  conversations — Full message history (flight recorder)             │
├─────────────────────────────────────────────────────────────────────┤
│  Column          │ Type     │ Notes                                 │
│  ────────────────┼──────────┼───────────────────────────────────── │
│  conv_id         │ INT PK   │ Auto-increment                       │
│  task_id         │ TEXT FK  │ References tasks                      │
│  session_number  │ INT      │ Which session                         │
│  turn_number     │ INT      │ Turn within session                   │
│  role            │ TEXT     │ system | user | assistant | tool      │
│  content         │ TEXT     │ Full message content                  │
│  timestamp       │ DATETIME │ When message was sent/received        │
│                                                                     │
│  NEVER loaded on resume. This is the flight recorder.               │
│  Exists for debugging, auditing, and post-mortem analysis.          │
│  Cleaned automatically 30 days after task completion.               │
└─────────────────────────────────────────────────────────────────────┘
```

### Cross-Table Relationships

```
tasks.task_id ──┬──< sessions.task_id
                │       └── sessions.(task_id, session_number) ──┐
                │                                                 │
                ├──< checkpoints.task_id                         │
                │       └── checkpoints.session_number ──────────┤
                │                                                 │
                ├──< decisions.task_id                            │  Linked by
                │       └── decisions.session_number ─────────────┤  composite
                │                                                 │  (task_id,
                ├──< errors.task_id                               │  session_number)
                │       └── errors.session_number ────────────────┤
                │                                                 │
                ├──< artifacts.task_id                            │
                │       └── artifacts.session_number ─────────────┤
                │                                                 │
                └──< conversations.task_id                        │
                        └── conversations.session_number ─────────┘
```

---

## Full Lifecycle Flow

### Phase 1: New Task

```
┌─────────────────────────────────────────────────────────────────────┐
│                         NEW TASK BEGINS                              │
└─────────────────────────────────────────────────────────────────────┘

  User: "Build the JWT refresh endpoint"
    │
    ▼
┌──────────────────────────────────────┐
│  task_registry.create()              │
│  ├── INSERT INTO tasks               │
│  │   (task_id='jwt_refresh',         │
│  │    status='active')               │
│  └── Partial unique index enforces   │
│      only one active task            │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  session_manager.start_session()     │
│  ├── Crash detection check:          │
│  │   "Does previous session have     │
│  │    ended_at = NULL?"              │
│  │   └── If yes: mark as crashed     │
│  ├── INSERT INTO sessions            │
│  │   (session_number=1)              │
│  └── Return session handle           │
└──────────────────┬───────────────────┘
                   │
                   ▼
┌──────────────────────────────────────┐
│  Model receives initial payload:     │
│  ┌────────────────────────────────┐  │
│  │ System prompt                  │  │
│  │ + project_goal.md    ~100 tok  │  │
│  │ + role_instructions.md ~200 tok│  │
│  │ + architecture.md    ~200 tok  │  │
│  │ + data_contracts.md  ~200 tok  │  │
│  │ ──────────────────────────── │  │
│  │ Total stable refs:   ~700 tok  │  │
│  │                                │  │
│  │ + Task: "Build JWT refresh     │  │
│  │   endpoint"                    │  │
│  └────────────────────────────────┘  │
│                                      │
│  Context used: ~2.2% of 32K         │
│  Context free: ~97.8%               │
└──────────────────────────────────────┘
```

### Phase 2: Work Loop

```
┌─────────────────────────────────────────────────────────────────────┐
│                          WORK LOOP                                   │
└─────────────────────────────────────────────────────────────────────┘

  ┌─────────── TURN-BY-TURN CYCLE ──────────────┐
  │                                               │
  │  ┌─────────────────────────────────────────┐  │
  │  │  Model generates response               │  │
  │  │  (code, reasoning, tool calls)          │  │
  │  └──────────────────┬──────────────────────┘  │
  │                     │                          │
  │                     ▼                          │
  │  ┌─────────────────────────────────────────┐  │
  │  │  session_manager.record_turn()          │  │
  │  │  ├── INSERT INTO conversations          │  │
  │  │  │   (role, content, turn_number)       │  │
  │  │  └── Flight recorder -- always records  │  │
  │  └──────────────────┬──────────────────────┘  │
  │                     │                          │
  │                     ▼                          │
  │  ┌─────────────────────────────────────────┐  │
  │  │  context_tracker monitors token usage   │  │
  │  │                                          │  │
  │  │  ┌─────────────────────────────┐        │  │
  │  │  │ < 70%  ──── HEALTHY         │        │  │
  │  │  │ 70-84% ──── WARNING         │        │  │
  │  │  │ 85-89% ──── PREPARE         │        │  │
  │  │  │ >= 90% ──── CHECKPOINT NOW  │        │  │
  │  │  └─────────────────────────────┘        │  │
  │  └──────────────────┬──────────────────────┘  │
  │                     │                          │
  │                     ▼                          │
  │  ┌─────────────────────────────────────────┐  │
  │  │  Extract and save metadata:             │  │
  │  │  ├── Decisions? ──> decisions table      │  │
  │  │  │   (priority: critical/normal/minor)  │  │
  │  │  ├── Errors? ────> errors table          │  │
  │  │  │   (with lesson learned)              │  │
  │  │  └── Artifacts? ─> artifacts table       │  │
  │  │      (filename + SHA-256 hash)          │  │
  │  └──────────────────┬──────────────────────┘  │
  │                     │                          │
  │                     ▼                          │
  │  ┌─────────────────────────────────────────┐  │
  │  │  Every 10 turns: AUTOSAVE               │  │
  │  │  checkpoint_writer saves rolling         │  │
  │  │  checkpoint to DB (safety net)          │  │
  │  └─────────────────────────────────────────┘  │
  │                     │                          │
  │                     │  Loop back               │
  └─────────────────────┘                          │
                                                   │
  Continue until: CHECKPOINT trigger (80%)         │
                  or TASK_COMPLETE signal           │
                  or crash                         │
```

### Phase 3: Checkpoint at 80% Context

```
┌─────────────────────────────────────────────────────────────────────┐
│                    CHECKPOINT TRIGGER (80% context)                  │
└─────────────────────────────────────────────────────────────────────┘

  Context hits 80%
    │
    ▼
┌──────────────────────────────────────────────────────────────┐
│  Model writes structured CHECKPOINT block:                    │
│                                                               │
│  ## CHECKPOINT                                                │
│  Status: MID-TASK INTERRUPT                                   │
│  What I was doing: auth/jwt_refresh.py line 47, writing       │
│    the token rotation logic inside refresh_token()            │
│  What I completed:                                            │
│    - Token validation endpoint                                │
│    - Refresh token generation                                 │
│    - Redis token storage                                      │
│  What I was about to do:                                      │
│    1. Complete rotation logic (revoke old, issue new pair)    │
│    2. Add rate limiting to refresh endpoint                   │
│    3. Write integration tests                                │
│  Exact mid-task state:                                        │
│    refresh_token() has validation done, rotation half-written │
│  Context when interrupted:                                    │
│    CRITICAL: Tokens must be single-use (revoke on refresh)   │
│    Using Redis SET with TTL for token storage                 │
│    Access token: 15min, Refresh token: 7 days                │
│  Resume instruction:                                          │
│    Continue refresh_token() in auth/jwt_refresh.py at line 47 │
│    Complete the rotation logic: revoke old refresh token in   │
│    Redis, generate new access+refresh pair, return both.      │
│  RESET                                                        │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  checkpoint_writer.py                                         │
│  ├── Parse CHECKPOINT block with regex                        │
│  ├── Validate: all 6 fields present?                         │
│  │   ├── YES ── validated = 1                                │
│  │   └── NO  ── validated = 0 (still saved, flagged)         │
│  ├── INSERT INTO checkpoints                                  │
│  ├── Extract decisions ── INSERT INTO decisions               │
│  └── Extract errors ──── INSERT INTO errors                   │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  session_manager.end_session(reason='checkpoint')            │
│  ├── UPDATE sessions SET ended_at=NOW, end_reason='checkpoint'│
│  └── Record final turn_count and peak_context_pct            │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
          ╔════════════════════════════════════════╗
          ║                                        ║
          ║     F U L L   C O N T E X T   R E S E T║
          ║                                        ║
          ║     Context window: 0 tokens           ║
          ║     Clean slate. Fresh start.           ║
          ║     All state preserved in gyatt.db     ║
          ║                                        ║
          ╚════════════════════════════════════════╝
```

### Phase 4: Resume

```
┌─────────────────────────────────────────────────────────────────────┐
│                         RESUME SEQUENCE                              │
└─────────────────────────────────────────────────────────────────────┘

  New session starts (after reset)
    │
    ▼
┌──────────────────────────────────────────────────────────────┐
│  session_manager.start_session()                              │
│  ├── CRASH DETECTION:                                         │
│  │   SELECT * FROM sessions                                   │
│  │     WHERE task_id = ? AND ended_at IS NULL                │
│  │   ├── Found? ── Previous session crashed!                 │
│  │   │   └── UPDATE SET end_reason = 'crashed'               │
│  │   └── Not found? ── Clean shutdown last time              │
│  ├── INSERT new session (session_number incremented)          │
│  └── Return session handle                                    │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  checkpoint_loader.py assembles resume payload:               │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ QUERY 1: Latest validated checkpoint                    │  │
│  │ SELECT checkpoint_text FROM checkpoints                 │  │
│  │   WHERE task_id=? AND validated=1                       │  │
│  │   ORDER BY created_at DESC LIMIT 1                      │  │
│  │                                                          │  │
│  │ QUERY 2: Active decisions (critical first)              │  │
│  │ SELECT decision_text, priority FROM decisions            │  │
│  │   WHERE task_id=? AND status='active'                   │  │
│  │   ORDER BY                                               │  │
│  │     CASE priority                                        │  │
│  │       WHEN 'critical' THEN 1                            │  │
│  │       WHEN 'normal' THEN 2                              │  │
│  │       WHEN 'minor' THEN 3                               │  │
│  │     END                                                  │  │
│  │                                                          │  │
│  │ QUERY 3: Unresolved errors with lessons                 │  │
│  │ SELECT error_text, lesson FROM errors                    │  │
│  │   WHERE task_id=? AND resolved=0                         │  │
│  │                                                          │  │
│  │ QUERY 4: Artifact manifest                              │  │
│  │ SELECT filename, sha256_hash FROM artifacts              │  │
│  │   WHERE task_id=?                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  ASSEMBLE payload in this order:                              │
│                                                               │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  1. Task: jwt_refresh                                   │  │
│  │     Build the JWT refresh endpoint                      │  │
│  │     Current step: rotation logic in refresh_token()     │  │
│  │                                                          │  │
│  │  2. CHECKPOINT (session 3):                             │  │
│  │     [Latest validated checkpoint text]                   │  │
│  │                                                          │  │
│  │  3. ACTIVE DECISIONS:                                   │  │
│  │     [CRITICAL] Tokens must be single-use                │  │
│  │     [CRITICAL] Use Redis SET with TTL                   │  │
│  │     [normal] Access: 15min, Refresh: 7 days             │  │
│  │                                                          │  │
│  │  4. UNRESOLVED ERRORS:                                  │  │
│  │     Error: Race condition on concurrent refresh          │  │
│  │     Lesson: Use Redis WATCH for optimistic locking      │  │
│  │                                                          │  │
│  │  5. ARTIFACT MANIFEST:                                  │  │
│  │     auth/jwt_refresh.py   sha256:a1b2c3...              │  │
│  │     auth/token_store.py   sha256:d4e5f6...              │  │
│  │     tests/test_auth.py    sha256:g7h8i9...              │  │
│  │                                                          │  │
│  │  6. CRITICAL CONSTRAINTS (repeated for attention):      │  │
│  │     >>> Tokens MUST be single-use (revoke on refresh)   │  │
│  │     >>> Use Redis SET with TTL for token storage        │  │
│  │                                                          │  │
│  │  7. RESUME INSTRUCTION:                                 │  │
│  │     Continue refresh_token() in auth/jwt_refresh.py     │  │
│  │     at line 47. Complete rotation logic: revoke old     │  │
│  │     refresh token in Redis, generate new access+refresh │  │
│  │     pair, return both.                                  │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                               │
│  Total payload size: ~426 tokens                              │
│  Percentage of 32K window: ~1.3%                              │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  Model receives:                                              │
│                                                               │
│  ┌─ Stable refs (reloaded fresh) ─────────── ~700 tokens ─┐ │
│  │  project_goal.md                                         │ │
│  │  role_instructions.md                                    │ │
│  │  architecture.md                                         │ │
│  │  data_contracts.md                                       │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌─ Resume payload (from checkpoint_loader) ─ ~426 tokens ─┐ │
│  │  [assembled payload above]                               │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                               │
│  Total on resume: ~1,126 tokens (~3.5% of 32K)              │
│  Free for work:   ~30,874 tokens (~96.5%)                    │
│                                                               │
│  Model reads Resume Instruction and continues working.        │
└──────────────────────────────────────────────────────────────┘
```

### Phase 5: Task Complete

```
┌─────────────────────────────────────────────────────────────────────┐
│                        TASK COMPLETE                                 │
└─────────────────────────────────────────────────────────────────────┘

  Model signals TASK_COMPLETE
    │
    ▼
┌──────────────────────────────────────────────────────────────┐
│  task_registry.complete(task_id='jwt_refresh')                │
│  ├── UPDATE tasks SET status='completed',                     │
│  │                     completed_at=NOW                       │
│  └── Frees the partial unique index slot                     │
│      (new task can now be created as 'active')               │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  archiver.py generates markdown archive:                      │
│                                                               │
│  completed/jwt_refresh.md                                     │
│  ┌────────────────────────────────────────────────────────┐  │
│  │  # Task: JWT Refresh Endpoint                          │  │
│  │  Created: 2026-03-28 09:00                              │  │
│  │  Completed: 2026-03-28 14:30                            │  │
│  │  Duration: 5h 30m                                       │  │
│  │                                                          │  │
│  │  ## Checkpoint History                                  │  │
│  │  - Session 1: checkpoint at 80% (validated)             │  │
│  │  - Session 2: checkpoint at 82% (validated)             │  │
│  │  - Session 3: completed                                 │  │
│  │                                                          │  │
│  │  ## Decisions Made                                      │  │
│  │  - [CRITICAL] Single-use tokens with Redis              │  │
│  │  - [normal] Access 15min / Refresh 7 days               │  │
│  │  - [normal] Optimistic locking via WATCH                │  │
│  │                                                          │  │
│  │  ## Errors Encountered                                  │  │
│  │  - Race condition on concurrent refresh (RESOLVED)       │  │
│  │    Resolution: Redis WATCH + retry loop                 │  │
│  │                                                          │  │
│  │  ## Artifacts                                           │  │
│  │  - auth/jwt_refresh.py   sha256:a1b2c3...               │  │
│  │  - auth/token_store.py   sha256:d4e5f6...               │  │
│  │  - tests/test_auth.py    sha256:g7h8i9...               │  │
│  │                                                          │  │
│  │  ## Achievements                                         │  │
│  │  - Sessions survived: 3                                 │  │
│  │  - Total turns: 47                                      │  │
│  │  - Errors encountered: 1                                │  │
│  │  - Errors resolved: 1 (100%)                            │  │
│  │  - Crash recoveries: 0                                  │  │
│  │  - Peak context usage: 82%                              │  │
│  └────────────────────────────────────────────────────────┘  │
└──────────────────────────────┬───────────────────────────────┘
                               │
                               ▼
┌──────────────────────────────────────────────────────────────┐
│  Cleanup:                                                     │
│  ├── Task status ──> 'archived'                              │
│  ├── Conversation logs retained 30 days, then purged         │
│  ├── Checkpoints, decisions, errors: kept permanently in DB  │
│  └── System ready for next task                              │
└──────────────────────────────────────────────────────────────┘
```

### Complete Lifecycle State Machine

```
                    ┌─────────┐
                    │  START  │
                    └────┬────┘
                         │  User gives task
                         ▼
                    ┌─────────┐
           ┌───────│  ACTIVE  │◄──────────────────────┐
           │       └────┬────┘                         │
           │            │                              │
           │            │  Work loop                    │
           │            │  (turn by turn)              │
           │            ▼                              │
           │       ┌──────────┐    Context < 80%      │
           │       │ WORKING  │────────────────┐      │
           │       └────┬─────┘                │      │
           │            │                      │      │
           │            │ Context >= 80%       │      │
           │            ▼                      │      │
           │   ┌──────────────┐                │      │
           │   │ CHECKPOINTING│                │      │
           │   └──────┬───────┘                │      │
           │          │                        │      │
           │          │ Save + Reset           │      │
           │          ▼                        │      │
           │   ┌──────────────┐                │      │
           │   │   RESET      │                │      │
           │   │  (clean      │                │      │
           │   │   slate)     │                │      │
           │   └──────┬───────┘                │      │
           │          │                        │      │
           │          │ Resume                 │      │
           │          └────────────────────────┘──────┘
           │
           │  TASK_COMPLETE signal
           ▼
    ┌──────────────┐
    │  COMPLETED   │
    └──────┬───────┘
           │  Archive generated
           ▼
    ┌──────────────┐
    │  ARCHIVED    │
    └──────────────┘
```

---

## Resume Payload Assembly

### Token Budget Breakdown

```
┌─────────────────────────────────────────────────────────────────────┐
│                    WHAT THE MODEL SEES ON RESUME                     │
│                                                                     │
│  32,768 tokens total context window                                 │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ Stable refs (always reloaded fresh from MD files)             │  │
│  │                                                               │  │
│  │  project_goal.md ............ ~100 tokens                     │  │
│  │  role_instructions.md ....... ~200 tokens                     │  │
│  │  architecture.md ............ ~200 tokens                     │  │
│  │  data_contracts.md .......... ~200 tokens                     │  │
│  │  ─────────────────────────────────────                        │  │
│  │  Subtotal:                    ~700 tokens  (2.1%)             │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │ Resume payload (assembled by checkpoint_loader.py)            │  │
│  │                                                               │  │
│  │  Task description + step .... ~40 tokens                      │  │
│  │  Checkpoint text ............ ~150 tokens                     │  │
│  │  Active decisions ........... ~80 tokens                      │  │
│  │  Unresolved errors .......... ~60 tokens                      │  │
│  │  Artifact manifest .......... ~40 tokens                      │  │
│  │  Critical constraints (dup) . ~30 tokens                      │  │
│  │  Resume instruction ......... ~26 tokens                      │  │
│  │  ─────────────────────────────────────                        │  │
│  │  Subtotal:                    ~426 tokens  (1.3%)             │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                                                               │  │
│  │           FREE FOR ACTUAL WORK: ~31,642 tokens                │  │
│  │                                  (96.6%)                      │  │
│  │                                                               │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                     │
│  ┌ Visual bar ─────────────────────────────────────────────────┐   │
│  │██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░│   │
│  │ ^                                                           │   │
│  │ 3.4% used                                          96.6% free   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

### The Recency Bias Exploit

```
  Transformer attention distribution across context:

  Attention
  weight
    ▲
    │ ████                                                    ████████
    │ █████                                                 ██████████
    │ ██████                                               ███████████
    │ ████████                                            ████████████
    │ ██████████                                        ██████████████
    │ ████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░████████████████
    │ ██████████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░██████████████████
    └──────────────────────────────────────────────────────────────────▶
      START of context          MIDDLE (weak)          END of context

  The payload exploits this by placing CRITICAL constraints:
  1. In natural position (with other decisions)  ── catches primacy bias
  2. Repeated at the VERY END of payload         ── catches recency bias

  Result: Critical constraints get high attention from BOTH ends.
```

---

## What Gets Loaded vs What Doesn't

### The Split

```
┌─────────────────────────────────────────────────────────────────────┐
│                          LOADED ON RESUME                            │
│                          (~1,126 tokens)                             │
│                                                                     │
│  ┌─ Stable Refs (fresh from disk, not from DB) ──────────────────┐ │
│  │  project_goal.md           Always current, never stale         │ │
│  │  role_instructions.md      Reprocessed each session            │ │
│  │  architecture.md           Same content, fresh attention       │ │
│  │  data_contracts.md         No KV drift accumulation            │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ From gyatt.db (via checkpoint_loader.py) ────────────────────┐ │
│  │  Latest validated checkpoint   The "quest log"                 │ │
│  │  Active decisions              Prescriptive constraints        │ │
│  │  Unresolved errors             Don't repeat these mistakes     │ │
│  │  Artifact manifest             What files exist + their hashes │ │
│  │  Critical constraints (dup)    Repeated for recency bias       │ │
│  │  Resume instruction            "Continue from here"            │ │
│  └───────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────┐
│                       NOT LOADED ON RESUME                           │
│                       (stays in DB as backup)                        │
│                                                                     │
│  ┌─ conversations table ─────────────────────────────────────────┐ │
│  │  Full message history           Flight recorder only           │ │
│  │  Every user/assistant turn      For debugging, not execution   │ │
│  │  Raw tool outputs               Already processed              │ │
│  │  Old reasoning chains           Already reasoned               │ │
│  └───────────────────────────────────────────────────────────────┘ │
│                                                                     │
│  ┌─ Filtered out from other tables ──────────────────────────────┐ │
│  │  Resolved errors                No longer relevant             │ │
│  │  Revoked/superseded decisions   Replaced by newer ones         │ │
│  │  Old checkpoints                Only latest matters            │ │
│  │  Invalidated checkpoints        validated=0, not trustworthy   │ │
│  └───────────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────┘
```

### Why This Split Works

```
  Traditional agent (no checkpoint system):

  Session 1:  [system prompt] [history] [history] [history] [...] [NEW WORK]
              ████████████████████████████████████████████████████░░░░░░░░░
              60-80% wasted on old context                       20-40% free

  Session 5:  [system prompt] [accumulated history x5] [NEW WORK]
              ████████████████████████████████████████████████████████████░░
              90%+ wasted                                                tiny


  GyattMaxxer5000 (checkpoint/resume):

  Session 1:  [stable refs] [task] [────────── FREE SPACE ──────────────]
              ██░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
              ~2%                              ~98% free

  Session 5:  [stable refs] [resume payload] [────── FREE SPACE ────────]
              ███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
              ~3.4%                            ~96.6% free

  Session 50: [stable refs] [resume payload] [────── FREE SPACE ────────]
              ███░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░
              ~3.4%                            ~96.6% free
                                               ▲
                                    NEVER DEGRADES. Session 50 = Session 5.
```

---

## The Game Save Analogy

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       THE GAME SAVE ANALOGY                                 │
│                                                                             │
│  Think of the agent like Skyrim. You wouldn't load every save file          │
│  from every playthrough into memory at once. You load the latest save,      │
│  the game engine (base files), and your current quest log.                  │
└─────────────────────────────────────────────────────────────────────────────┘
```

| Game Concept | Agent Equivalent | DB Table | Saved to DB? | Loaded on Resume? |
|---|---|---|---|---|
| Base game files (.esm) | Stable refs (MD files) | -- | NO (on disk) | RELOADED fresh every time |
| Quest state / save file | Task checkpoint | checkpoints | YES | YES (latest validated only) |
| Player choices | Architecture decisions | decisions | YES | YES (active only, as constraints) |
| Failed quest attempts | Dead ends, errors | errors | YES | Only UNRESOLVED ones |
| Inventory | Code artifacts | artifacts | YES | Manifest only (names + hashes) |
| Full gameplay recording | Conversation history | conversations | YES | NEVER (flight recorder) |
| Save file metadata | Session stats | sessions | YES | Session count only |
| Character level / XP | Progress across sessions | -- | Implicit | Via checkpoint continuity |

### The Analogy Visualized

```
  ┌─────────────────────────────────────────────────┐
  │              GAME (Skyrim)                        │
  │                                                   │
  │  Installed game ── 40 GB on disk                  │
  │  Loaded into RAM ── ~4 GB (current area only)     │
  │  Save file ── ~15 MB (quest state, inventory)     │
  │  Your choices ── embedded in save                 │
  │  Failed attempts ── not saved (you reloaded)      │
  │  Full replay ── not recorded                      │
  │                                                   │
  │  On "Continue": load save + current area          │
  │  NOT: load entire game world into memory           │
  └─────────────────────────────────────────────────┘

  ┌─────────────────────────────────────────────────┐
  │              AGENT (GyattMaxxer5000)              │
  │                                                   │
  │  Project docs ── ~90 KB on disk                   │
  │  Loaded into context ── ~1,126 tokens             │
  │  Checkpoint ── ~150 tokens in DB                  │
  │  Decisions ── prescriptive constraints in DB      │
  │  Errors ── unresolved loaded, resolved archived   │
  │  Full history ── in DB, never loaded              │
  │                                                   │
  │  On resume: load refs + checkpoint + decisions    │
  │  NOT: load entire conversation history             │
  └─────────────────────────────────────────────────┘
```

---

## Hardware Layout

```
┌────────────────────────────────────────┐          ┌────────────────────────────────────────┐
│         WINDOWS PC (This Machine)      │          │         MAC MINI M4 16GB               │
│         Home / Research Station         │          │         Execution Engine                │
│                                        │          │                                        │
│  ┌──────────────────────────────────┐  │          │  ┌──────────────────────────────────┐  │
│  │  GyattMaxxer5000/               │  │          │  │  ~/Desktop/GyattMaxxer5000/      │  │
│  │  ├── ai_memory_architectures.md │  │          │  │  ├── pipeline/                   │  │
│  │  ├── blueprint_architecture1.md │  │          │  │  │   ├── db_init.py              │  │
│  │  ├── pipeline_architecture.md   │  │   SSH    │  │  │   ├── task_registry.py        │  │
│  │  ├── session_summary.md         │  │  (WSL)   │  │  │   ├── checkpoint_writer.py    │  │
│  │  ├── dev_log.md                 │◄─┼──────────┼─►│  │   ├── checkpoint_loader.py    │  │
│  │  └── research/                  │  │          │  │  │   ├── session_manager.py       │  │
│  │      ├── 00_master_overview.md  │  │          │  │  │   ├── archiver.py              │  │
│  │      ├── 01-06 research files   │  │          │  │  │   └── gyatt.db                │  │
│  │      └── ...                    │  │          │  │  │                                │  │
│  └──────────────────────────────────┘  │          │  │  └── workspace/                  │  │
│                                        │          │  │      └── (model's working area)  │  │
│  Hardware:                             │          │  └──────────────────────────────────┘  │
│  ├── i5 12th gen, 16GB RAM             │          │                                        │
│  ├── GTX 1650 4GB (inference useless)  │          │  ┌──────────────────────────────────┐  │
│  └── Role: Research, docs, SSH client  │          │  │  Ollama                          │  │
│                                        │          │  │  ├── Model: 9B (Q4_K_M)          │  │
└────────────────────────────────────────┘          │  │  ├── Context: 32K tokens         │  │
                                                    │  │  └── API: localhost:11434         │  │
┌────────────────────────────────────────┐          │  └──────────────────────────────────┘  │
│         OFFICE PC (RX 580 8GB)         │          │                                        │
│         Training Station                │          │  Hardware:                             │
│                                        │          │  ├── M4 chip, 16GB unified memory      │
│  ┌──────────────────────────────────┐  │          │  ├── 10-core GPU                       │
│  │  Fine-tuning pipeline           │  │          │  └── Role: Model inference, pipeline   │
│  │  ├── Unsloth + QLoRA            │  │          │                                        │
│  │  ├── Training data (~2000 ex)   │  │          └────────────────────────────────────────┘
│  │  ├── LoRA adapters output       │  │
│  │  └── Sentinel model (1.7B)      │  │          ┌────────────────────────────────────────┐
│  └──────────────────────────────────┘  │          │    6x MAC MINI M4 CLUSTER (via Exo)   │
│                                        │          │                                        │
│  Hardware:                             │          │  ┌──────┐ ┌──────┐ ┌──────┐           │
│  ├── i3 12th gen, 16GB RAM             │          │  │16GB  │ │16GB  │ │16GB  │           │
│  ├── RX 580 8GB (ROCm for training)   │          │  │Mini 1│ │Mini 2│ │Mini 3│           │
│  └── Role: LoRA fine-tuning, sentinel  │          │  └──┬───┘ └──┬───┘ └──┬───┘           │
│                                        │          │     │  Exo   │  Exo   │               │
└────────────────────────────────────────┘          │  ┌──┴───┐ ┌──┴───┐ ┌──┴───┐           │
                                                    │  │16GB  │ │16GB  │ │16GB  │           │
┌────────────────────────────────────────┐          │  │Mini 4│ │Mini 5│ │Mini 6│           │
│         VAST.AI H100 (Rental)          │          │  └──────┘ └──────┘ └──────┘           │
│         Heavy Compute                   │          │                                        │
│                                        │          │  Combined: 96GB (72GB usable)          │
│  ├── Used for heavy training jobs      │          │  Can run: 70B Q4 comfortably            │
│  ├── Large-scale data generation       │          │  Speed: ~5-10 tok/s (TB4, no RDMA)     │
│  └── Architect model (70B+) hosting    │          │  Role: Architect/Manager inference      │
└────────────────────────────────────────┘          └────────────────────────────────────────┘
```

### Data Flow Between Machines

```
  WINDOWS PC                    MAC MINI                     MAC CLUSTER
  (Research)                    (Execution)                  (70B Inference)
       │                             │                             │
       │   SSH: deploy scripts       │                             │
       │────────────────────────────►│                             │
       │                             │                             │
       │   SSH: read dev_log.md      │   Exo API: architect       │
       │◄────────────────────────────│   planning queries          │
       │                             │────────────────────────────►│
       │                             │                             │
       │                             │◄────────────────────────────│
       │                             │   structured plan response  │
       │                             │                             │
       │                             │                             │
       │                        ┌────┴────┐                        │
       │                        │gyatt.db │                        │
       │                        │         │                        │
       │                        │ All 7   │                        │
       │                        │ tables  │                        │
       │                        │ live    │                        │
       │                        │ here    │                        │
       │                        └─────────┘                        │
       │                             │                             │
  OFFICE PC                          │                             │
  (Training)                         │                             │
       │   SCP: trained LoRA         │                             │
       │────────────────────────────►│                             │
       │   adapters                  │                             │
```

---

## Quick Reference Card

```
┌─────────────────────────────────────────────────────────────────────┐
│                     QUICK REFERENCE                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Checkpoint triggers at:     80% context usage                      │
│  Emergency checkpoint at:    90% context usage                      │
│  Autosave frequency:         Every 10 turns (rolling)              │
│  Resume payload size:        ~426 tokens (~1.3% of 32K)            │
│  Stable refs size:           ~700 tokens (~2.1% of 32K)            │
│  Total overhead on resume:   ~1,126 tokens (~3.4% of 32K)          │
│  Free context on resume:     ~31,642 tokens (~96.6%)               │
│                                                                     │
│  Checkpoint fields (6):      What doing, What completed,            │
│                              What next, Mid-task state,            │
│                              Context/decisions, Resume instruction  │
│                                                                     │
│  Database tables (7):        tasks, sessions, checkpoints,          │
│                              decisions, errors, artifacts,          │
│                              conversations                         │
│                                                                     │
│  Pipeline scripts (6):       db_init, task_registry,               │
│                              checkpoint_writer, checkpoint_loader,  │
│                              session_manager, archiver              │
│                                                                     │
│  Active task limit:          1 (enforced by DB constraint)          │
│  Conversation log retention: 30 days after task completion          │
│  Crash detection:            ended_at IS NULL on previous session   │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

*Pipeline architecture document -- March 2026*
*System: GyattMaxxer5000 Checkpoint/Resume Pipeline*
*Architecture 1 implementation: Selective KV Context Management*
