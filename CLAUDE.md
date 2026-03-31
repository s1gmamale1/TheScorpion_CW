# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

---

## Project 1: GyattMaxxer5000 (AI Agent Architecture Research + Build)

### What It Is

A **research knowledge base + build project** for AI agent memory management. Core problem: long-running AI agent sessions degrade because context windows fill with completed work. This repo documents and implements architectural solutions.

### Repository Structure

```
├── ai_memory_architectures.md   # Core design doc: 3 agent memory architectures
├── blueprint_architecture1.md   # Full step-by-step build plan for Architecture 1
├── agent_checkpoint.sql         # SQLite schema for checkpoint/resume pipeline
├── pipeline_architecture.md     # Pipeline v2 architecture with visual diagrams
├── session_summary.md           # Comprehensive reference across 21 topics
├── research_index.md            # Quick-reference index to all files
├── dev_log.md                   # Chronological log of all work done
└── research/
    ├── 00_master_overview.md    # Index + 10 key insights + 4-week learning path
    ├── 01-07_*.md               # Core research files
    └── 08_*.md                  # Checkpoint/resume + production persistence research
```

Start with `research/00_master_overview.md` for orientation, then `ai_memory_architectures.md` for the core designs.

### The Three Memory Architectures

**Architecture 1 — Selective KV Context Management:** Save KV cache only for the active unfinished task. Stable reference files (~700 tokens) are reloaded fresh each session. Good for single long-running tasks that need checkpoint/resume.

**Architecture 2 — Precomputed KV Execution Model:** Compile stable project knowledge into KV cache files offline, once. Load only relevant KV files per task. Context window is then 100% free for generation.

**Architecture 3 — Virtual Context Memory:** OS-inspired tiered memory. Hot task in GPU context; recent subtasks on SSD warm tier; earlier sessions on SSD cold tier; completed tasks archived as Markdown.

### The Agent Stack

```
Architect (70B+ or API)  →  Plans project, creates initial context
Manager (35B or API)     →  Oversees quality, reads sentinel alerts
Worker (fine-tuned 4B–35B) →  Executes tasks, autosaves every 10 turns,
                               self-resets at 90% context, resumes from checkpoint
Sentinel (fine-tuned 1.7B) →  Pattern-only drift/error/loop detection (~400MB RAM)
```

### Hardware Context

- Home PC: GTX 1650 4GB — inference only
- Office PC: RX 580 8GB — LoRA fine-tuning target
- 6× Mac Mini M4 16GB (96GB unified via Exo) — distributed 70B inference
- Vast.ai H100 — heavy compute rental

### GyattMaxxer Workflow Rules

- **Dev log:** Append every substantive response to `dev_log.md` with a timestamp header.
- **Research-first:** Before writing any config, script, memory estimate, or hyperparameter, consult the relevant research files. Calculate actual values from data — do not guess.

---

## Project 2: The Scorpion (Unity Arena Combat Game)

### What It Is

A fast-paced arena combat / hack-and-slash game built in Unity (C#). A masked warrior with dual blades fights through 10 waves of enemies using elemental powers (Fire + Lightning), culminating in a 3-phase boss fight. Visual reference: Zenless Zone Zero.

- **Engine**: Unity 6000.4.0f1 (URP)
- **Framework**: Invector Third Person Controller — Melee Combat Template (v2.6.5)
- **Unity Project Path**: `TheScorption_mvp/cw_1/`
- **Design Docs**: `Project Architechrure/extracted/TheScorpion/docs/`
- **Reference Scripts**: `Project Architechrure/extracted/TheScorpion/Assets/Scripts/` (design intent only, not drop-in code)
- **Research Docs**: `docs/research/` (12 files) and `docs/tutorials/` (3 files)
- **Dev Log**: `DEV_LOG.md` at project root
- **Build Plan**: `~/.claude/plans/joyful-wiggling-melody.md`

### Architecture

#### Invector (Purchased Asset — DO NOT MODIFY)

Located in `Assets/Invector-3rdPersonController/`. Provides third-person controller, melee combat, health/stamina, dodge/roll, lock-on, animator, and Simple Melee AI.

**Damage Flow:**
```
Player Input → Animator → vMeleeAttackControl (StateMachineBehaviour)
→ vMeleeManager.SetActiveAttack() → vHitBox.OnTriggerEnter
→ vMeleeManager.OnDamageHit(vHitInfo) → target.TakeDamage(vDamage)
→ vHealthController events: onStartReceiveDamage → onReceiveDamage → onDead
```

**Key Invector hooks (subscribe via events, never modify source):**
- `vMeleeManager.onDamageHit` — when player's attack hits
- `vHealthController.onStartReceiveDamage` — before damage applied
- `vHealthController.onReceiveDamage` — after damage applied
- `vHealthController.onDead` — on kill
- `vDamage.damageType` — string field for element identification

#### Custom Systems (namespace: `TheScorpion.*`)

All custom scripts use composition — separate MonoBehaviours alongside Invector components, connected via events.

**Singleton managers:** `GameManager`, `WaveManager`, `SpawnPointManager`, `AttackQueueManager`, `CameraShakeController`

**Event system:** ScriptableObject event channels for decoupled communication.

**Data-driven:** `EnemyDataSO`, `WaveDataSO`, `ElementDataSO` ScriptableObjects.

### Scorpion Development Rules

- **NEVER modify** files in `Assets/Invector-3rdPersonController/`
- Always extend Invector via events, callbacks, or separate MonoBehaviours
- Custom scripts go in `TheScorption_mvp/cw_1/Assets/Scripts/`
- Reference scripts in `Project Architechrure/extracted/` are design intent only
- Read `DEV_LOG.md` at the start of every session, update after every substantive response
- ZZZ (Zenless Zone Zero) is the visual/gameplay reference

### MCP Tools

- **mcp-unity** — Direct Unity Editor control
- **mac-commander** — Screenshots, click, type, window management
- **mac-mcp-server** — AppleScript tools
