# Dev Log — GyattMaxxer5000 / Custom AI Build Project

> All responses and progress saved here. Most recent entries at the bottom.

---

## 2026-03-27 — Session Restart After Power Outage

### Progress Check

**Research phase: COMPLETE**

Previous session (March 27, 2026, earlier today) completed the full AI research collection. Here's what was built:

**Files created:**
- `ai_memory_architectures.md` — Full spec for all 3 novel agent memory architectures
- `session_summary.md` — 21-topic reference covering everything discussed
- `research/00_master_overview.md` — Index + 10 key insights + 4-week learning path
- `research/01_transformer_architectures.md` — GPT lineage, modern transformer stack
- `research/02_training_techniques.md` — Pretraining, LoRA/QLoRA, DPO, quantization
- `research/03_novel_architectures.md` — RWKV, Griffin, xLSTM, TTT, Jamba, Zamba
- `research/04_inference_and_kv_cache.md` — KV cache math, PagedAttention, inference engines
- `research/05_papers_index.md` — 49 curated papers with arXiv links
- `research/06_learning_resources.md` — YouTube, courses, newsletters, repos
- `CLAUDE.md` — Created this session (project guide for Claude Code)
- `dev_log.md` — This file, created this session

**Build phase: NOT STARTED**

The research is done. The architectures are fully designed. Nothing has been built yet.

### What Was Covered in the Research

The previous session covered (in order):
1. Your hardware setup + GPU VRAM reality check
2. Mac Mini M4 cluster (6x, 96GB via Exo, RDMA situation)
3. GPU prices March 2026 (RTX 3090 ~$400-600, 4090 ~$2,755, DGX Spark $4,699)
4. MacBook prices + M5 release (March 2026)
5. Mac Studio M4 Max 36GB ($2,315) — your target machine
6. DGX Spark — runs up to 200B Q4
7. Kimi K2.5 (1T MoE) — use API, can't run locally without datacenter
8. Model size practical guide (4B→200B, what each can do)
9. Token, context, and KV cache math
10. Current benchmarks (March 2026) — 6 models within 0.8% on SWE-bench
11. Kimi K2.5 tool call loop bug in OpenClaw v2026.3.7-3.8
12. The 3 memory architectures (full detail in ai_memory_architectures.md)
13. Agent stack design (Architect/Manager/Worker/Sentinel)
14. Self-aware context management (trained reflexes, checkpoint format)
15. Training data plan (~2000 examples from ClassAI logs)
16. Precomputed KV + speculative execution concepts
17. What already exists vs what's novel (the agent application layer is the gap)
18. Why this wasn't built before (timing is now right)
19. MacBook Neo ($599, A18 chip, useless for LLM work)
20. Build path (immediate next steps)
21. Key principles from the session

### Next Steps (Build Phase)

In order:
1. **Generate training dataset** — ~2000 checkpoint/resume examples from ClassAI agent logs
2. **Fine-tune Qwen3.5 4B** — LoRA on RX 580, 1-2 days, trains the checkpoint/reset reflex
3. **Build orchestrator wrapper** — ~100-200 lines Python, watches context %, triggers saves/resets
4. Stack: vLLM + LMCache + Ollama

Start with Architecture 1 (Selective KV) — lowest complexity, buildable with existing tools.

---

## 2026-03-27 — Can We Build AI From Scratch?

### Question
User asked: "with all the knowledge you have gathered, you can build AI from scratch with the new architecture?"

### Answer

Two interpretations:

| Option | What it means | Feasibility |
|--------|--------------|-------------|
| Fine-tune existing model | Take Qwen3.5 4B, LoRA-train checkpoint behavior, build agent system on top | Feasible now. RX 580. 1-2 days. |
| Pretrain new base model | Train transformer weights from zero — custom architecture, custom data | Needs H100 rental. Weeks. Very expensive. |

The build plan calls for Option A. The novel contribution is the **agent OS layer** — not a new base model. That's what no one has built yet.

**What I can write code for right now:**
- Full orchestrator (Python ~200 lines)
- KV file lifecycle manager + completion registry
- LoRA training script for Qwen3.5 4B on RX 580
- Checkpoint/resume training data generator
- Architecture 1, 2, and 3 implementation

**Next question posed to user:** What format are your ClassAI logs in?

---

## 2026-03-27 — KV-SSD Research (3 Agents)

### Task
Deployed 3 parallel agents to research NVIDIA, DeepSeek, and full state-of-the-art on KV cache → SSD offloading for Architecture 3.

### Key Findings

**DeepSeek MLA — 57x KV compression**
- Stores compressed latent vector (576 dims) instead of full K+V (32,768 dims for MHA)
- 70 KB/token vs 516 KB/token → SSD offloading becomes viable
- CRITICAL: must store compressed latent, not decompressed K/V (official DeepSeek impl gets this wrong)
- Qwen3.5 uses GQA (not MLA) — ~300 bytes/token, still much larger than MLA

**NVIDIA ICMSP (CES 2026)**
- BlueField-4 STX rack: 9.6 PB NVMe, 800 Gbps, ships H2 2026
- Dynamo software: G1(HBM)→G2(DRAM)→G3(NVMe)→G3.5(ICMSP)→G4(network)
- Datacenter only — not relevant to our hardware

**8 confirmed gaps in all existing systems (our novel contribution):**
1. No task-semantic KV block identity (all use content hash or recency)
2. No task-phase-aware preservation (LRU only)
3. No cross-task semantic KV reuse (prefix match only)
4. No KV DAG topology tracking (all assume flat linear sequence)
5. No selective sub-agent KV inheritance
6. No task-graph-driven prefetch
7. No cross-session semantic versioning
8. No task-criticality eviction signal

**Production tools to use as foundation:**
- LMCache: KV block storage + disk tier ✅
- vLLM: inference engine ✅
- NVIDIA Dynamo: G1-G4 hierarchy (optional) ✅

**What we build on top:**
- Task Registry (JSON)
- Block Tagger (metadata on write)
- Semantic Router (task-aware KV selection)
- Task-criticality eviction policy
- Prefetch Oracle (reads task graph)
- MD Archiver (task complete → text → delete KV)

### Output
New file: `research/07_kv_ssd_state_of_art_2026.md`

### Architecture 3 hardware mapping
- RX 580 8GB → G1 (hot, active task KV)
- 16GB RAM → G2 (warm, recently completed)
- NVMe SSD → G3 (cold, completed tasks)

### Decision
Build Architecture 1 first (no long-context KV issues with GQA at short context).
Architecture 3 needs either MLA-based model or better hardware for long context.

---

## 2026-03-27 — Phase 0 Complete + Phase 1 Orchestrator Written

### Environment
- Switched from Windows home PC (RX 580 4GB — too small) to Mac Mini M4 16GB
- Mac Mini: Apple M4 10-core, 16GB unified, macOS 15.6
- Fixed WSL Ubuntu (wrong registry BasePath — corrected to DaddysPC)
- Installed on Mac Mini via SSH: Homebrew 5.1.1, Python 3.11.15, venv at ~/gyatt_env
- Packages: requests, pydantic, tiktoken, rich, mlx-lm 0.31.1
- Ollama running with huihui_ai/qwen3.5-abliterated:9b-Claude (6.6GB, Q4_K_M)

### Files written to ~/Desktop/GyattMaxxer5000/
- context_tracker.py — token counting, health status (healthy/warning/critical)
- state_manager.py — save/load task state JSON, archive to MD, registry management
- orchestrator.py — main loop: resume/start, autosave every 10 turns, CHECKPOINT+RESET signal, TASK_COMPLETE signal
- stable/project_goal.md, role_instructions.md, architecture.md — stable reference files
- test_setup.py — verified all systems pass

### Test results
All green: Ollama reachable, model responds, all dirs exist, stable files ~195 tokens total, ContextTracker working

### Next
Phase 2: training data generation script

---

## 2026-03-27 — Created research_index.md

Created `research_index.md` in project root. One-stop navigation for all research files.

Contents:
- Brief summary of each file and what's inside
- Topic-based navigation table ("I want to know about X → go to Y")
- Build progress tracker with ✅/🔲 status

Next up: Architecture 1 orchestrator code.

---

## 2026-03-27 — Architecture 1 Full Blueprint

Created `blueprint_architecture1.md` — complete build plan for Selective KV Context Management.

### Phases
- Phase 0 (Day 1): Environment setup, model verification
- Phase 1 (Days 2-4): Orchestrator v0 — context simulation, NO fine-tuning needed, FIRST WORKING PROTOTYPE
- Phase 2 (Days 4-6): Training data generation — self-play using 9B model, ~2000 examples across 4 datasets
- Phase 3 (Days 6-8): LoRA fine-tune — Unsloth + QLoRA on RX 580, trains checkpoint/resume reflex
- Phase 4 (Days 8-10): True KV binary — replace JSON state with llama.cpp --prompt-cache binary files
- Phase 5 (Days 10-12): Integration + end-to-end testing
- Phase 6 (Days 12-14): Sentinel (optional, 1.7B quality monitor)

### Key decisions
- v0 uses Ollama API + JSON conversation history (no fine-tune needed to prove concept)
- v1 uses llama.cpp direct with --prompt-cache-all for binary KV save/load (<1s resume)
- Training data: self-play generation (no ClassAI logs needed yet)
- Fine-tuning: Unsloth QLoRA, 4-bit base, LoRA rank 16, 3 epochs, BF16
- NO LangChain/frameworks — pure Python orchestrator ~200 lines

### KV cache sizes for 9B GQA on RX 580
- 4k context: ~200MB, loads in ~0.5s
- 8k context: ~400MB, loads in ~1s

---

## 2026-03-28 — Phase 2 In Progress + Phase 3 Scripts Written

### Training Data Generation (running on Mac Mini)

**Bug fixed:** `think: False` must be at **top level** of Ollama request body, not inside `options`. When in `options`, model silently puts all output into a separate `thinking` field, leaving `response` empty → all examples skipped. Fix: moved to top level + regex strip of `<think>` tags as fallback + auto-append `\nRESET` when CHECKPOINT detected but RESET absent.

**Current progress (as of session start):**
- Dataset A (checkpoint writing): 125/125 ✅ — 88.8% valid (111 good examples)
- Dataset B (resume behavior): 113/125 (in progress) — 100% valid so far
- Dataset C (task completion): not started
- Dataset D (status JSON): not started
- Generator running as background process on Mac Mini

**Validation stats (from validate_data.py):**
- A: 111/125 valid | avg 1,304 chars | missing "Resume instruction:" in 14 examples (acceptable)
- B: 113/113 valid so far | avg 2,346 chars | 100% pass rate
- Quality is high — real code, specific file/function references, accurate format

### Phase 3 Scripts Written

All 3 files deployed to `~/Desktop/GyattMaxxer5000/`:

**validate_data.py** — Quality check for all 4 datasets before training.
- Checks structure (3-message format), content markers, field presence
- Shows valid%, avg length, missing fields, sample previews

**prepare_mlx_data.py** — Merges all 4 datasets, shuffles, 90/10 train/valid split.
- Output: `training_data/mlx/train.jsonl` + `valid.jsonl`
- MLX-LM expects these exact filenames in the data directory

**finetune.sh** — Full LoRA training command for Mac Mini M4 16GB.
- Base model: `mlx-community/Qwen3.5-9B-MLX-4bit` (already in MLX format, ~5GB)
- LoRA rank: 8 | alpha: 16 | lora_layers: 16 | batch: 2 | iters: 700
- grad_checkpoint: true (saves ~40% memory, fits in 16GB)
- Estimated memory: ~11GB total (5GB model + 4GB activations + 2GB OS)
- Includes post-training test (generates a checkpoint to verify behavior)

### Key Architecture Finding

Qwen3.5-9B is a **hybrid SSM+Attention** model (confirmed by HF model card):
- 32 total layers: 8 sets of (3× Gated DeltaNet → 1× Gated Attention)
- Attention layers at positions: 3, 7, 11, 15, 19, 23, 27, 31
- `lora_layers=16` hits the last 4 attention layers + 12 SSM blocks
- Sufficient for behavioral reflex training (checkpoint/reset behavior)
- HF repo: `huihui-ai/Huihui-Qwen3.5-9B-Claude-4.6-Opus-abliterated`

### Fine-tuning Stack Decision

Switched from Unsloth (CUDA/ROCm only) to **MLX-LM** (Apple Silicon native).
- MLX runs on Metal backend — 100% GPU on M4
- `mlx-community/Qwen3.5-9B-MLX-4bit` available pre-quantized (no conversion)
- After training: merge adapter with `mlx_lm.fuse`, convert to GGUF for Ollama

### Run Order (when data finishes)

```bash
python validate_data.py       # confirm 90%+ pass rate on all 4 datasets
python prepare_mlx_data.py    # create train.jsonl / valid.jsonl
pkill ollama                  # free 7GB RAM
bash finetune.sh              # ~1-2 hours training on M4
```

---

## 2026-03-28 — Critical Architecture Insight: Behavioral vs Infrastructure

### The Two Separate Problems

**Problem A — Behavioral:** Does the model know WHEN to checkpoint and in WHAT format?
- SOLVED via system prompt engineering (orchestrator injects context % and recommendation)
- Model reads stats, follows rules, emits `## CHECKPOINT ... RESET`
- Fine-tuning = optional polish for reliability, NOT required
- Proven working: model produced full JWT auth system + checkpointed correctly

**Problem B — Infrastructure:** WHERE does KV cache live and HOW do we manage it?
- 100% external tooling — model is completely unaware
- Like virtual memory: the process doesn't know about page tables, the OS handles it
- Stack: llama.cpp (binary KV) + LMCache (block management) + our custom layer

### The Model Doesn't Know About KV Files

The model doesn't decide which KV to load/offload. The orchestrator does. Just like:
- Microsoft Word doesn't know about memory pages → the OS kernel handles it
- Chrome doesn't need training to use your SSD → the OS handles storage
- The 9B model doesn't know about KV blocks → the orchestrator handles it

### Who Decides What (All Deterministic, Not Learned)

| Decision | Component | How |
|----------|-----------|-----|
| Which KV blocks belong to this task? | Task Registry (JSON) | Deterministic lookup |
| Which KV to load when starting a task? | Semantic Router (Python) | Rule-based: task → tagged blocks |
| Which KV to evict when memory full? | Eviction Policy (Python) | task-criticality > frequency > recency |
| When to save KV to SSD? | Orchestrator | Context % threshold |

### The Full Infrastructure Stack

```
┌─────────────────────────────────────────────────────┐
│  OUR CUSTOM LAYER (~500 lines Python)               │
│  Task Registry + Block Tagger + Semantic Router     │
│  + Eviction Policy + Prefetch Oracle                │
├─────────────────────────────────────────────────────┤
│  LMCACHE (existing, open source, arXiv:2404.18262)  │
│  KV block storage: GPU → RAM → Disk                 │
├─────────────────────────────────────────────────────┤
│  LLAMA.CPP (existing, open source)                  │
│  --prompt-cache-all: binary KV save/load            │
│  Apple Metal backend, GGUF format                   │
├─────────────────────────────────────────────────────┤
│  HARDWARE (Mac Mini cluster)                        │
│  GPU memory → hot | RAM → warm | NVMe → cold        │
└─────────────────────────────────────────────────────┘
```

### The 8 Gaps From research/07 — All Infrastructure, Zero Training

1. Task-semantic KV identity → Block Tagger (Python)
2. Task-phase-aware preservation → Eviction Policy (Python)
3. Cross-task semantic reuse → Semantic Router (Python)
4. KV DAG topology → Task Registry (JSON)
5. Selective sub-agent inheritance → Semantic Router (Python)
6. Task-graph-driven prefetch → Prefetch Oracle (Python)
7. Cross-session versioning → Task Registry (JSON + TTLs)
8. Task-criticality eviction → Eviction Policy (Python)

### NVIDIA Dynamo Parallel (research/07)

| NVIDIA Dynamo | Our Architecture 3 |
|---|---|
| KV Block Manager (KVBM) | Task Registry + Eviction Policy |
| LRU + priority eviction | Task-criticality eviction (novel) |
| G1(GPU) → G2(RAM) → G3(SSD) | GPU → RAM → NVMe (same tiers) |
| Inference engine unaware | Model unaware (same principle) |

### Key Conclusion

The novel contribution is SOFTWARE (agent OS layer), not model training. The model is a compute engine. KV management is transparent to it. All 8 innovations from research/07 are ~600 lines of Python on top of existing tools.

### Fine-Tuning Reality Check

Attempted LoRA fine-tuning on Mac Mini M4 16GB — OOM every time:
- research/02 clearly states: LoRA 7B needs 16-24GB, QLoRA needs 8-12GB
- 9B model on 16GB unified (shared with macOS) = not enough
- MLX-LM doesn't have paged optimizers like bitsandbytes (CUDA)
- Val loss was 0.843 BEFORE any training → model already knows the format
- Decision: skip fine-tuning for now, proceed with infrastructure build

### Orchestrator Test Results

Model (9B, no fine-tuning) successfully:
- Produced a complete JWT auth system (router, models, hashing, endpoints)
- Generated structured checkpoint format when prompted
- Resumed from saved state
- Ctrl+C save/exit worked cleanly

### Updated Build Path (No GPU Rental)

```
Phase 1: ✅ Done   — Orchestrator v0 (JSON replay, Ollama)
Phase 2: ✅ Done   — Training data (500 examples, saved for future use)
Phase 3: SKIP      — Fine-tuning (not required, model already capable)
Phase 4: NEXT      — Switch Ollama → llama.cpp for binary KV save/load
Phase 5: AFTER     — Add LMCache for KV block management + SSD tier
Phase 6: AFTER     — Build task registry + semantic router + eviction policy
Phase 7: AFTER     — Exo cluster for 70B Architect tier
```

---

## 2026-03-28 — Architecture 1 Complete: All 8 Gaps Implemented

### What Was Built

**Context Layer** — 6 components (~550 lines Python) deployed to Mac Mini:

| Component | File | Lines | Gaps Filled |
|-----------|------|-------|-------------|
| Task Registry | `task_registry.py` | ~80 | A (task-semantic identity), G (cross-session versioning) |
| Block Tagger | `block_tagger.py` | ~75 | A (every message tagged with task metadata) |
| Semantic Router | `semantic_router.py` | ~110 | C (cross-task reuse), E (selective inheritance) |
| Eviction Policy | `eviction.py` | ~65 | B (task-phase preservation), H (criticality eviction) |
| Prefetch Oracle | `prefetch.py` | ~75 | F (task-graph-driven prefetch) |
| MD Archiver | `archiver.py` | ~80 | G (completed tasks → markdown, raw blocks deleted) |
| Facade | `__init__.py` | ~70 | Wires all components together |

**Orchestrator v1** — `orchestrator_v1.py` integrates context layer.

### End-to-End Test: Full Lifecycle

```
START prime_test → task registered, activated
Turn 1 → model writes prime checker + checkpoint (3 blocks, 800 tokens)
CHECKPOINT → state saved, exit
RESUME → router selects all 3 blocks, model continues from instruction
TASK_COMPLETE → archiver writes completed/prime_test.md, blocks cleaned
```

### llama.cpp Compatibility

Qwen3.5 hybrid architecture not supported in upstream llama.cpp (rope.dimension_sections mismatch). Ollama works via patched fork. Binary KV save/load deferred — context layer is the higher-value build.

### What "Pseudo-Infinite Context" Means

Context window = RAM, SSD = archived tasks, eviction = page replacement. Model has fixed working memory but total knowledge grows indefinitely:
- Eviction removes low-criticality blocks before overflow
- Archival compresses completed tasks to markdown
- Semantic routing includes only relevant cross-task context
- Checkpoint/resume works across sessions with no context loss

NOT truly infinite within a single turn (still 32K). Information loss on archival. But the system never permanently fills up.

### Updated Build Progress

```
Phase 1: ✅ Done   — Orchestrator v0
Phase 2: ✅ Done   — Training data (500 examples)
Phase 3: SKIP      — Fine-tuning (model already capable)
Phase 4: DEFERRED  — Binary KV (llama.cpp doesn't support Qwen3.5 yet)
Phase 5: DEFERRED  — LMCache (waiting on llama.cpp support)
Phase 6: ✅ Done   — Context layer (all 8 gaps, ~550 lines Python)
Phase 7: NEXT      — Exo cluster for 70B Architect tier
```

---

## 2026-03-29 — Google TurboQuant Research (KV Cache 6x Compression)

### What It Is

Google released TurboQuant (March 25, 2026) — 3 algorithms that compress KV cache to 3 bits. Training-free, data-oblivious, works on any model.

- **PolarQuant**: Cartesian → polar coordinate compression for KV vectors
- **QJL**: 1-bit residual compression with Johnson-Lindenstrauss transform
- **TurboQuant**: Combines both → 6x KV compression, 8x attention speedup, zero accuracy loss

### Impact on Our Project

| Current (BF16 KV) | With TurboQuant (3-bit KV) |
|---|---|
| ~300 bytes/token | ~50 bytes/token |
| 32K context in 10MB | 32K context in 1.6MB |
| Frequent eviction needed | 6x less eviction |
| Effective context: 32K | Effective context: ~192K |

### Status

Lab breakthrough — not yet in llama.cpp/Ollama. Papers exist, implementation pending.

Papers: arXiv:2504.19874 (TurboQuant), arXiv:2406.03482 (QJL), arXiv:2502.02617 (PolarQuant)

### Action Item

Monitor llama.cpp repo for TurboQuant/QJL/PolarQuant integration. When it lands, it's a drop-in upgrade to all 3 architectures — no code changes needed, just a KV quantization flag.

---

## 2026-03-29 — Autonomous Mode + Continuous Context Management Proven

### Auto Mode Added

`--auto` flag: model runs autonomously, auto-sends "continue" after each turn. No human in the loop.

```bash
python orchestrator_v1.py --auto api_build "Build a complete REST API with FastAPI..."
```

### Continuous Context Cycle Proven

The checkpoint-restart loop was replaced with checkpoint-evict-continue:

```
Turn 1: Model planned + built entire API (registration, login, JWT, CRUD, rate limiting)
Turn 2: Checkpoint detected → EVICTED 6 blocks → context reset → auto-resumed
Turn 3: Model confirmed completion → TASK_COMPLETE → archived
```

Key log line:
```
[CHECKPOINT DETECTED — saving state + evicting to continue]
[EVICTED 6 blocks | context reset | continuing without restart]
```

No restart. No re-prompting. Context breathed — filled up, got trimmed, model kept going in the same process.

### Bugs Fixed

- **Think mode in orchestrator**: Added `"think": false` at top level of Ollama payload + fallback to combine thinking+response fields. Same fix as generate_data.py.
- **Timeout**: Increased to 600s + added `num_predict: 4096` to cap output per turn.
- **Auto mode**: `--auto` flag sends "continue" instead of waiting for user input.

### Architecture 1 — Fully Operational

All components working end-to-end on single Mac Mini M4 16GB:

| Feature | Status |
|---------|--------|
| Task-aware block tagging | ✅ |
| Semantic context routing | ✅ |
| Criticality-based eviction | ✅ |
| Autonomous execution (--auto) | ✅ |
| Checkpoint-evict-continue (no restart) | ✅ |
| Task archival to markdown | ✅ |
| Cross-session resume | ✅ |
| Prefetch oracle | ✅ (untested with multi-task) |

---

## 2026-03-30 — Checkpoint/Resume State-of-the-Art Research

### Task
Deep research on how production systems and academic papers handle agent session state checkpointing and resumption (2025-2026).

### Output
New file: `research/08_checkpoint_resume_state_of_art.md`

### Key Findings

**1. Checkpoint Format Landscape:**
- Letta (.af): Pydantic-based, saves messages + memory blocks + tools + config. DB-backed live, file for export.
- LangGraph: ormsgpack binary + JSON hybrid. Postgres for prod (3-table schema). Per-step checkpoints with channel versioning.
- AutoGen: Plain JSON. Message history only. Known bugs with state loss on restart.
- OpenAI: Opaque server-side. Auto-truncates when context fills — no developer control.
- MemOS (arXiv:2507.03724): OS-inspired MemCube abstraction with lifecycle management. Validates our Architecture 3.

**2. Save vs Reconstruct (critical finding):**
- JetBrains Dec 2025: Observation masking BEATS LLM summarization by 2.6% solve rate at 52% lower cost
- Save: reasoning chains, decisions, task state, compressed tool results
- Reconstruct: file contents, system prompts, tool outputs, environment observations
- Structured distillation (arXiv:2603.13017): 11x compression, 96% retrieval preserved — 4-field format per exchange

**3. Failure Modes:**
- Lost-in-the-middle (Liu 2024): 30%+ accuracy drop for middle-positioned info. ALL frontier models still affected in 2025.
- Du et al. 2025: Context length ALONE degrades performance even with forced attention on relevant tokens.
- Multi-agent failures (Cemri 2025): Conversation history loss, conversation resets, step repetition. SOTA systems only 25% correct.
- Long-context degradation: 50%+ performance drop at 100K tokens on models claiming 1M-2M windows.

**4. Game Save Analogy (Skyrim .ess):**
- Skyrim saves DELTAS from base game files, not full world state (Change Forms with bitflags)
- 43 change form types, only modified properties recorded
- Perfect analogy: base game files = stable references, change forms = task checkpoint, master files = never saved

### Design Implications
1. Our delta-based checkpoint format is validated by both game engines and production frameworks
2. Block tagger with task-semantic metadata is genuinely novel — no production system does this
3. Observation masking (our eviction approach) is proven superior to summarization
4. Checkpoint content should be positioned at START of context on resume (lost-in-the-middle mitigation)
5. Should adopt structured distillation (4-field format) for completed exchanges — 11x compression

### Proposed Checkpoint Format
See Section 4.4 of the research file — full JSON schema combining Skyrim's delta model + structured distillation + observation masking.

---

## 2026-03-30 — Pipeline v2 Built: SQLite-Based Checkpoint/Resume System

### What Was Done

Complete redesign of the checkpoint/resume pipeline. Isolated from old orchestrator (untouched). All new code in `~/Desktop/GyattMaxxer5000/pipeline/` on Mac Mini.

### Research Phase (3 parallel agents + 1 adversarial reviewer)

**Agent 1 — Internet research:** Surveyed checkpoint formats, game save systems (Skyrim .ess), context compression (ACON paper), failure modes (lost-in-the-middle confirmed across all 18 frontier models).

**Agent 2 — Project research papers:** Analyzed all 7 research files. Key finding: partial KV eviction creates holes in causal attention, causing the gibberish we saw. H2O paper: 20% of tokens get 80% of attention — evicting heavy hitters = incoherence.

**Agent 3 — Production systems:** LangGraph (3-table Postgres), MemGPT/Letta (agent-managed paging), Anthropic (compaction + structured notes), ACON (26-54% token reduction). Key finding: 65% of enterprise agent failures caused by context DRIFT, not exhaustion.

**Adversarial reviewer (10 problems found and addressed):**
1. No dependency graph → added artifacts_manifest with hashes
2. decisions.md/attention_notes.md overlap → merged into single context.md concept (now in DB)
3. No schema versioning → added schema_version table
4. No "abandoned approaches" → checkpoint format includes failed attempts
5. Compressed decisions lose nuance → priority flags (critical/normal/minor)
6. Primacy/recency bias → critical constraints repeated at END of resume payload
7. Non-atomic writes → SQLite transactions (crash-safe by design)
8. Checkpoint at 90% too late → moved to 80% (model less degraded)
9. Model writes own checkpoint (self-assessment bias) → validation flag
10. No task namespacing → task_id-based isolation in all tables

### Core Design Decision

**NEVER partially evict KV blocks.** Always do a FULL hard reset with structured semantic checkpoint. Like a game save — save the quest log, wipe the slate, reload only what matters.

### Why SQLite Instead of Flat Files

| Flat Files (old) | SQLite (new) |
|---|---|
| Temp dir + rename for atomicity | Built-in transactions |
| Read every file to query | SQL queries |
| No crash safety | WAL mode (crash-proof) |
| Filename-based relationships | Foreign keys |

### The 6 Lego Brick Scripts

| Script | Purpose | Status |
|---|---|---|
| `db_init.py` | Creates SQLite DB (7 tables, WAL mode) | Tested |
| `task_registry.py` | Task CRUD, one-active enforcement | Tested |
| `checkpoint_writer.py` | Parses model output → saves checkpoint + decisions + errors | Tested |
| `checkpoint_loader.py` | Assembles resume payload (~426 tokens) | Tested |
| `session_manager.py` | Session lifecycle + crash detection | Tested |
| `archiver.py` | Task complete → markdown archive with achievements | Tested |

### Database Schema (gyatt.db — 7 tables)

- `tasks` — one active at a time (partial unique index)
- `sessions` — one per reset/resume cycle, crash detection via NULL ended_at
- `checkpoints` — save files with validation flag
- `decisions` — prescriptive constraints (critical/normal/minor priority)
- `errors` — mistake log with lessons, resolution tracking
- `artifacts` — code files with SHA-256 hashes
- `conversations` — flight recorder (never loaded on resume, cleaned after 30 days)

### Resume Payload (~426 tokens, 1.3% of 32K window)

```
=== TASK ===           (description + step)
=== CHECKPOINT ===     (latest validated)
=== ACTIVE DECISIONS ===  (critical first)
=== UNRESOLVED ERRORS === (with lessons)
=== ARTIFACTS ===      (manifest)
=== CRITICAL CONSTRAINTS === (repeated for recency bias)
=== RESUME INSTRUCTION ===
```

### Integration Test Results

Full lifecycle verified:
- DB init → task create → session start → conversation recording
- Error/decision/artifact tracking → checkpoint write + validation
- Session end → resume payload assembly (426 tokens, correct format)
- Crash detection (un-ended sessions auto-marked)
- Task completion → archive with achievements → cleanup

### Architecture Doc Created

New file: `pipeline_architecture.md` — full visual representation with ASCII diagrams covering:
- System overview, script dependency map, all 6 scripts detailed
- ER diagram for all 7 tables with relationships
- Full lifecycle flow (5 phases with step-by-step diagrams)
- Token budget breakdown (96.6% free on resume)
- Recency bias exploit visualization
- Game save analogy (Skyrim mapping)
- Hardware layout with data flow

### Next Step

Build `orchestrator_v2.py` that wires the pipeline scripts to Ollama. Old orchestrator stays untouched.

---
