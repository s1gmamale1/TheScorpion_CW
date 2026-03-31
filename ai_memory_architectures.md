# AI Agent Memory Architecture Concepts
> Designed through conversation — March 2026

---

## Architecture 1 — Selective KV Context Management

### One Line
Save only the KV for the active unfinished task. Load stable reference material fresh from MD files every session. Never accumulate context bloat.

### The Problem It Solves
Agent sessions fill up context with:
- Project architecture (already in MD files, doesn't need KV)
- Completed tasks (done, irrelevant to current work)
- Historical logs (useful for debugging, useless for execution)
- Previous session reasoning (already happened)

By the time agent gets to actual work, context is already 40-60% full. Quality degrades. Reset loses everything.

### How It Works

```
STABLE REFERENCE (reload fresh every session):
├── project_goal.md         ~100 tokens
├── role_instructions.md    ~200 tokens
├── architecture.md         ~200 tokens
└── data_contracts.md       ~200 tokens
Total: ~700 tokens. Reprocessed every session. Fine.

ACTIVE TASK KV (saved selectively):
└── unfinished_task.kv
    Only the tokens from TASK START to NOW
    The half-written function
    The current reasoning chain
    The partial implementation
    Size: ~1-2GB for typical task
    Load time: <1 second

COMPLETED WORK (never loaded):
└── logs.md / completed_tasks.md
    Archive only
    Never injected into context
    Reference if needed — text only, not KV
```

### Session Lifecycle

```
New session starts
↓
Load MD files (~700 tokens) — fresh reprocess
↓
Does unfinished_task.kv exist?
├── Yes → inject position offset → resume from exact point
│         context used: ~700 tokens + task size
│         ~5-10% of window used ✅
└── No  → start fresh task
          context used: ~700 tokens
          ~0.5% of window used ✅

Work happens
Every 10 turns → autosave unfinished_task.kv (rolling)
↓
Task completes?
├── Yes → generate MD summary from KV
│         delete unfinished_task.kv
│         archive to completed_tasks.md
│         reset clean
└── No  → continue

At 90% context:
├── Dump session work to logs.md
├── Final save unfinished_task.kv
└── RESET → resume from KV next session
```

### What Makes It Different From Everything Else
- RAG loads text into context (still costs tokens)
- Summarization loses detail (lossy)
- Full KV save grows forever (too big)
- This saves ONLY the irreplaceable active state
- Stable reference reloads cheaply from MD every time
- KV only for what can't be reconstructed from MD

### Key Properties
| Property | Value |
|----------|-------|
| Context at session start | ~700 tokens (stable refs) + task size |
| KV file size | ~1-2GB per active task |
| Load time | <1 second |
| Information loss | Near zero for active task |
| Cross-session continuity | Perfect |
| Model update safe | MD portion yes, KV portion no |

### Who Builds What
- **Model (fine-tuned):** Writes correct checkpoint format, resumes cleanly, knows what's active vs completed
- **Orchestrator (Python wrapper):** Manages file lifecycle, triggers saves/loads, watches context %
- **No custom inference engine needed**

---

## Architecture 2 — Precomputed KV Execution Model

### One Line
Compile all project knowledge into KV artifacts offline. At execution time, fetch and inject — never reprocess. Context window becomes pure execution space, not a knowledge store.

### The Problem It Solves
Every session, the model re-reads the same stable content:
- Architecture docs it already "knows"
- API specs that never change
- Role instructions identical every time
- Pattern libraries used repeatedly

This is wasted compute. The model's understanding of these docs doesn't change between sessions. Why recompute the same attention states every time?

### The Core Insight

```
Traditional inference:
Text → model processes tokens → builds KV → attends → generates
Happens EVERY session. Same docs. Same KV output. Repeated forever.

Precomputed model:
Text → model processes tokens → KV stored to disk (ONCE)
At inference: load precomputed KV → inject → generate
Computation happens ONCE. Reused INFINITELY.

This is compilation.
Source code → compiler → machine code (stored, reused)
Project docs → model → KV artifacts (stored, reused)
```

### How It Works

```
COMPILE PHASE (offline, done once per project):
PM or setup script runs:

for each stable document:
    run through model
    store resulting KV tensors to disk
    index by: document_id + role + component

Output:
├── architecture.kv       (project structure)
├── contracts.kv          (data contracts)
├── role_backend.kv       (backend dev instructions)
├── role_frontend.kv      (frontend dev instructions)
├── auth_patterns.kv      (auth code patterns)
├── api_spec.kv           (API specifications)
└── [any stable knowledge]

Size per file: 500MB - 2GB
Build time: minutes to hours (once)
Reuse: infinite

EXECUTE PHASE (per task):
Worker receives: "Build JWT refresh endpoint"

Lookup execution plan:
{
  "jwt_endpoint": [
    "architecture.kv",
    "auth_patterns.kv",
    "role_backend.kv",
    "contracts.kv"
  ]
}

Load relevant KV files:
Total: ~4GB loaded, ~2 seconds
Context tokens used: 0
Model attends to full project knowledge
Context window: 100% available for actual generation
```

### The "Recall vs Read" Distinction

```
Current model reads docs:
"Here is the architecture document: [2000 tokens]"
→ 2000 context tokens consumed
→ model builds understanding in real time
→ same computation every session

Precomputed model recalls docs:
"Load architecture.kv"
→ 0 context tokens consumed
→ model has full understanding instantly
→ already computed, just fetched

Like the difference between:
Reading a book = processing tokens = context cost
Having read it = recall = zero context cost
```

### Use Case
**Only works for fully defined projects.** Everything known upfront:
- Architecture defined ✅
- Contracts defined ✅
- Patterns established ✅
- Tasks planned ✅

For open-ended exploration → traditional approach needed.
For structured execution (ClassAI, defined products) → this is perfect.

### Cross-Attention Problem
KV for doc A was computed without seeing doc B. When combined, cross-document relationships aren't in the precomputed KV. 

Mitigation:
- Compute with task-type conditioning ("as seen by backend dev")
- Sentinel catches any resulting cross-doc errors
- Manager corrects if needed
- Acceptable quality tradeoff for structured tasks

### Key Properties
| Property | Value |
|----------|-------|
| Knowledge context cost | 0 tokens |
| Context window usage | Pure execution only |
| One-time compute cost | Hours (offline) |
| Per-session compute saved | Massive |
| Storage needed | ~10-50GB per project |
| Works for | Fully defined projects |

### Current Research Status
- Memorizing Transformers (Google, 2021) — similar concept
- RETRO (DeepMind, 2021) — retrieval augmented with precomputed representations
- RAGCache — prefix KV caching for retrieval
- Not yet: task-aware KV library for agentic structured execution

---

## Architecture 3 — Virtual Context Memory (OS-Level)

### One Line
Treat context window as RAM and SSD as virtual memory. KV pages swap in/out dynamically. Agent has effectively infinite context. SSDs are 350x cheaper than GPU VRAM.

### The Problem It Solves
Even with architectures 1 and 2, long running projects eventually accumulate state:
- Multiple tasks in flight simultaneously
- Cross-task references needed
- Project grows over days/weeks
- Single context window still finite

And fundamentally: GPU VRAM is expensive. SSD is cheap. Why store everything in GPU?

### The OS Analogy — Exact

```
OS virtual memory (solved 1960s):
RAM:    small, fast, active processes only
Disk:   large, cheap, inactive pages
Page fault: need something not in RAM → load from disk
Page evict: RAM full → swap inactive page to disk
Result: programs run as if RAM is infinite

Virtual Context Memory (your design):
Context window: small, fast, active task only
SSD:            large, cheap, completed task KV
KV fault:       need completed task context → load from SSD
KV evict:       context approaching limit → offload to SSD
Result:         agents run as if context is infinite
```

### The Storage Tiers

```
TIER 0 — GPU Context (hot):
What: currently executing task only
Size: 2-10% of context window
Speed: instant
Cost: expensive GPU VRAM
Contents: active function, current tool results, execution state

TIER 1 — SSD Warm (recent):
What: recently completed subtasks, currently relevant KV
Size: gigabytes
Speed: <1 second load
Cost: cheap NVMe
Contents: tasks completed this session, recent decisions

TIER 2 — SSD Cold (older):
What: earlier sessions, completed modules
Size: tens of gigabytes
Speed: 1-3 seconds load
Cost: cheap NVMe
Contents: historical decisions, older task KV

TIER 3 — MD Archive (permanent):
What: fully completed tasks converted to text
Size: kilobytes per task
Speed: instant (text, tiny)
Cost: negligible
Contents: human readable summaries, permanent record
KV: deleted after conversion
```

### The KV Lifecycle

```
TASK STARTS:
└── Create task KV entry in context

DURING TASK:
├── Every 10 turns: autosave KV to SSD (rolling)
├── Context > 70%: evict least-recently-used KV to SSD
├── Context > 85%: evict completed subtask KV to SSD
└── Context > 90%: emergency checkpoint + reset

ON TASK COMPLETION:
├── Model generates MD summary from task KV
├── Write summary to completed/{task_id}.md
├── Delete task KV from SSD
├── Update completion registry ✅
└── Free GPU memory

CROSS-TASK REFERENCE:
├── Worker needs context from completed auth module
├── Load auth_module.md as text (~100 tokens)
├── Answer question
└── Unload — don't keep in context
    (MD text for reference, not KV for execution)
```

### Completion Registry

```json
{
  "auth_module": {
    "status": "complete",
    "md_file": "completed/auth_module.md",
    "kv_deleted": true,
    "completed_at": "2026-03-24T14:32",
    "summary": "JWT auth with refresh rotation, Redis storage"
  },
  "user_endpoints": {
    "status": "complete",
    "md_file": "completed/user_endpoints.md", 
    "kv_deleted": true,
    "completed_at": "2026-03-24T15:10",
    "summary": "CRUD with validation, pagination"
  },
  "payment_integration": {
    "status": "in_progress",
    "kv_file": "warm/payment_integration.kv",
    "kv_deleted": false,
    "last_active": "2026-03-24T16:45",
    "current_step": "webhook handler line 47"
  }
}
```

Agent loads this registry. Zero context cost. Instantly knows full project state.

### The Economics

```
GPU VRAM cost per GB:    ~$21/GB (RTX 3090 used)
NVMe SSD cost per GB:    ~$0.06/GB (Samsung 990 Pro)
Ratio:                   350x cheaper on SSD

KV for 50k token context at 35B: ~15GB
Storing in GPU VRAM:  needs $315 worth of VRAM
Storing on SSD:       needs $0.90 worth of storage

For a multi-week project with 10 active task KV files:
Traditional: needs 150GB VRAM ($3,150 equivalent) 
Virtual context: needs 150GB SSD ($9)

You're trading expensive GPU memory
for cheap SSD storage
SSDs are the obvious choice
```

### What Existing Research Covers

| System | What it does | Gap |
|--------|-------------|-----|
| KVSwap (Nov 2025) | KV offload to disk during decoding | Not agent-aware |
| NVIDIA ICMSP (CES 2026) | Enterprise KV offload to NVMe | Cloud/datacenter focus |
| MTDS (Jan 2026) | Multi-tier dynamic KV storage | No task lifecycle |
| HiFC (NeurIPS 2025) | Direct GPU→SSD KV swap | Infrastructure only |
| LMCache | KV offload + sharing across queries | No agent task awareness |

**The gap:** None implement task-aware selective KV preservation, completion-triggered archival, or the agent OS lifecycle. Infrastructure solved. Agent application layer: not built.

### Key Properties
| Property | Value |
|----------|-------|
| Effective context | Infinite (bounded by SSD) |
| GPU memory used | Active task only (~2-10%) |
| Storage cost | ~$0.06/GB vs $21/GB GPU |
| Load latency | <1-3 seconds from NVMe |
| Information loss | Zero (KV preserved until task complete) |
| Final state | MD files (permanent, human readable, portable) |

---

## How They Relate

```
Architecture 1 (Selective KV):
Focused on: single task continuity across resets
Scope: one worker, one task, one session boundary
Complexity: low — orchestrator + fine-tune
Build first: yes

Architecture 2 (Precomputed KV):
Focused on: removing knowledge from context entirely
Scope: project-wide, compile-time optimization
Complexity: medium — needs KV injection interface
Build second: yes, after 1 works

Architecture 3 (Virtual Context / OS-level):
Focused on: infinite effective context for long projects
Scope: full project lifecycle, multi-task, multi-session
Complexity: high — needs modified inference engine
Build third: yes, when scale demands it

Combined:
Architecture 2 handles stable reference knowledge (precomputed)
Architecture 1 handles active task state (selective KV)
Architecture 3 handles the full project memory lifecycle (virtual)

Together: an agent that never forgets,
          never degrades,
          never hits context limits,
          runs on cheap hardware,
          leaves human-readable records
```

---

## Implementation Stack

```
Layer        | Component          | Tool
─────────────────────────────────────────────────
Model        | Fine-tuned 4B-35B  | LoRA on existing base
Inference    | KV offload         | LMCache + vLLM
Orchestrator | Task lifecycle     | Python wrapper (~200 lines)
Storage      | KV files           | NVMe SSD
Archive      | MD summaries       | Plain files / Notion
Registry     | Completion state   | JSON file
Sentinel     | Quality monitor    | Fine-tuned 1.7B
Manager      | Correction         | 35B or API model
Architect    | Planning           | 70B+ or API model
```

---

## What Needs Building vs What Already Exists

```
Already exists (use it):
├── LMCache — KV offload to SSD ✅
├── vLLM — inference engine with KV management ✅
├── Ollama — simple local serving ✅
└── LoRA fine-tuning pipeline — you have this ✅

Needs building:
├── Training dataset for checkpoint behavior
├── Orchestrator wrapper (vibe-codeable)
├── Completion registry manager
├── Sentinel fine-tune dataset
└── KV→MD conversion pipeline

Needs research (later):
├── KV injection interface for Architecture 2
├── Cross-document attention for precomputed KV
└── Full virtual context page manager
```

---

*Document generated from design conversation — March 2026*
*Concepts: context-selective KV, precomputed KV execution, virtual context memory*
