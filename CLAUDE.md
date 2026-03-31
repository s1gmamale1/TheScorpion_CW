# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

This is a **research knowledge base**, not a software project. There are no build commands, tests, or dependencies. All content is Markdown documentation (~90KB) covering AI agent architecture design and LLM fundamentals.

**Core problem being researched:** Long-running AI agent sessions degrade because context windows fill with completed work and stable reference material gets reprocessed every session. This repository documents architectural solutions to that problem.

## Repository Structure

```
GyattMaxxer5000/
├── ai_memory_architectures.md   # Core design doc: 3 agent memory architectures
├── blueprint_architecture1.md   # Full step-by-step build plan for Architecture 1
├── session_summary.md           # Comprehensive reference across 21 topics
├── research_index.md            # Quick-reference index to all files
├── dev_log.md                   # Chronological log of all work done
└── research/
    ├── 00_master_overview.md    # Index + 10 key insights + 4-week learning path
    ├── 01_transformer_architectures.md
    ├── 02_training_techniques.md
    ├── 03_novel_architectures.md
    ├── 04_inference_and_kv_cache.md
    ├── 05_papers_index.md       # 49 curated papers with summaries
    ├── 06_learning_resources.md
    └── 07_kv_ssd_state_of_art_2026.md  # KV cache + SSD offloading research
```

Start with `research/00_master_overview.md` for orientation, then `ai_memory_architectures.md` for the core designs.

## The Three Memory Architectures

**Architecture 1 — Selective KV Context Management:** Save KV cache only for the active unfinished task. Stable reference files (~700 tokens) are reloaded fresh each session. Good for single long-running tasks that need checkpoint/resume.

**Architecture 2 — Precomputed KV Execution Model:** Compile stable project knowledge (architecture docs, API specs, patterns) into KV cache files offline, once. Load only relevant KV files per task. The context window is then 100% free for generation.

**Architecture 3 — Virtual Context Memory:** OS-inspired tiered memory. Hot task in GPU context; recent subtasks on SSD warm tier; earlier sessions on SSD cold tier; completed tasks archived as human-readable Markdown. SSDs are 350× cheaper than GPU VRAM.

## The Agent Stack

```
Architect (70B+ or API)  →  Plans project, creates initial context
Manager (35B or API)     →  Oversees quality, reads sentinel alerts
Worker (fine-tuned 4B–35B) →  Executes tasks, autosaves KV every 10 turns,
                               self-resets at 90% context, resumes from checkpoint
Sentinel (fine-tuned 1.7B) →  Pattern-only drift/error/loop detection (~400MB RAM)
```

**Key principle:** Knowledge (precomputed KV / Markdown) is separate from reasoning (context window).

## Hardware Context

The owner's current setup:
- Home PC: GTX 1650 4GB — inference only
- Office PC: RX 580 8GB — LoRA fine-tuning target
- 6× Mac Mini M4 16GB (96GB unified via Exo) — distributed 70B inference
- Vast.ai H100 — heavy compute rental
- Target purchase: Mac Studio M4 Max 36GB (~$2,300) to run 35B locally

## Planned Implementation (from session_summary.md)

1. Generate ~2000 training examples from agent logs
2. Fine-tune Qwen3.5 4B with LoRA on the RX 580
3. Build a Python orchestrator wrapper (~100–200 lines)
4. Stack: vLLM + LMCache (KV offload), Ollama (local serving)

**Current status:** Research phase is complete. Architecture 1 has a full build blueprint. Build phase has not started.

## Workflow Rules

- **Dev log:** Append every substantive response to `dev_log.md` with a timestamp header. This is the persistent record of all project work.
- **Research-first:** Before writing any config, script, memory estimate, or hyperparameter, consult the relevant research files:
  - `research/02_training_techniques.md` for training memory requirements
  - `research/04_inference_and_kv_cache.md` for KV cache / inference math
  - `research/07_kv_ssd_state_of_art_2026.md` for architecture-specific details
  - Calculate actual values from data before setting parameters — do not guess.
