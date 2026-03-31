# Full Session Summary
> March 27, 2026 — Everything discussed, organized by topic

---

## 1. Hardware & Local LLM Setup

### Your Current Hardware
- **Home PC:** i5 12th gen, 16GB RAM, GTX 1650 (4GB VRAM) — inference useless, coordinator only
- **Office PC:** i3 12th gen, 16GB RAM, RX 580 (8GB VRAM) — can run 7B Q4, LoRA fine-tuning viable
- **MacBook M2 Pro 8GB** — outdated for serious LLM work, too tight even for 14B
- **6x Mac Mini M4 16GB** at office — your best local asset
- **Vast.ai H100** — renting for heavy inference

### GPU VRAM Reality Check
```
GTX 1650:  4GB  → 3B Q4 max, useless for 35B cluster
RX 580:    8GB  → 7B Q4, fine-tuning only
RTX 3090:  24GB → 35B Q4 tight (2GB headroom)
RTX 4090:  24GB → same VRAM as 3090, 2x faster
RTX 5090:  32GB → comfortable Q4, scarce/expensive
DGX Spark: 128GB → up to 200B Q4
Mac M4 unified memory: RAM = VRAM (no PCIe bottleneck)
```

### Why Apple Silicon is Special
- Unified memory = RAM and VRAM are the same pool
- No PCIe transfer overhead between CPU and GPU
- M4 Max: 546 GB/s bandwidth vs DDR5 PC: ~51 GB/s
- Makes 36GB Mac Studio more capable than 24GB RTX for large models

---

## 2. Mac Mini Cluster (Your Best Asset)

### 6x Mac Mini M4 16GB via Exo
```
6 × 16GB = 96GB combined pool via Exo
Usable: ~72GB (after macOS overhead)
Can run: 70B Q4 comfortably ✅
Speed over WiFi: ~5-10 tok/s for 35B
Speed over ethernet: ~8-15 tok/s
```

### RDMA Over Thunderbolt 5 — The Big Update
- **macOS 26.2 + Exo 1.0** added RDMA over Thunderbolt 5
- Reduces inter-Mac latency from 300μs → 3μs (99% reduction)
- **Critical catch:** Only works on M4 PRO and above (Thunderbolt 5)
- Your 6 base M4 Minis have Thunderbolt 4 — **RDMA not available**
- Without RDMA: ~5-10 tok/s on 70B
- With RDMA (M4 Pro cluster): ~25-32 tok/s on 70B
- Benchmark: 4x M3 Ultra Mac Studios ran Kimi K2 1T at 25 tok/s with RDMA

### Exo Setup
```bash
pip install exo
# Run on each machine — auto-discovers peers
exo
# API endpoint: http://main-mini:52415/v1/chat/completions
```

---

## 3. GPU Prices (March 2026)

| GPU | VRAM | New | Used |
|-----|------|-----|------|
| RTX 3090 | 24GB | discontinued | ~$400-600 |
| RTX 4090 | 24GB | ~$2,755 | ~$2,200 |
| RTX 5090 | 32GB | $4,000-5,000+ (scalped) | ~$1,600 used |
| DGX Spark | 128GB | $4,699 (just raised from $3,999) | — |

### RTX 5090 Reality
- MSRP $1,999 but impossible to find at that price
- Street price $2,900-5,000+
- Memory shortage driving prices up through mid-2026
- 27-35% faster than RTX 4090 for 25% more MSRP

---

## 4. MacBook Prices (March 2026)

### M5 is current gen (released March 2026)
| Model | RAM | Price |
|-------|-----|-------|
| M5 MacBook Pro 14" | 24GB | ~$2,199 |
| M5 Max MacBook Pro 14" | 36-128GB | from $3,599 |
| M4 Pro (last gen, deals) | 24GB | ~$1,600-1,800 refurb |

### For LLM Work
- 16GB: runs up to 13-14B Q4 comfortably. 27B: no.
- 24GB: runs 27B Q4 comfortably (14GB headroom). Sweet spot.
- 36GB+: runs 35B Q4 with room to spare

---

## 5. Mac Studio (The One You're Looking At)

### M4 Max 36GB — AED 8,499 (~$2,315)
- Fits Qwen3.5-35B-A3B Q4 (~22GB) with ~14GB headroom ✅
- 512GB SSD is fine — use external NVMe for model storage
- External NVMe (Samsung T7, ~$60) loads models in seconds
- **Verdict: solid buy for 35B work**

### Mac Studio Lineup
| Config | RAM | Price |
|--------|-----|-------|
| M4 Max 36GB | 36GB | $1,999 |
| M4 Max 48GB | 48GB | ~$2,599 |
| M4 Max 64GB | 64GB | ~$2,999 |
| M3 Ultra 96GB | 96GB | $3,999 |

### M5 Mac Studio coming mid-2026 — if not urgent, wait

---

## 6. DGX Spark

- **Price:** $4,699 (raised from $3,999 Feb 2026 due to memory shortage)
- **Specs:** 128GB unified memory, 1 PFLOP AI, 4TB NVMe, tiny desktop
- **Runs:** Up to 200B Q4 comfortably on single unit
- **Does NOT run:** Kimi K2.5 1T (needs 240GB+ minimum)
- **Officially orderable** from nvidia.com and partners (ASUS, Dell, MSI, Lenovo)
- **With your 6 Mac Minis via Exo:** 128 + 72 = 200GB pool

---

## 7. Kimi K2.5 — The Monster

```
Architecture: 1T total params, 32B active (MoE)
Context: 256K tokens
Quantized Q4: ~600GB
Minimum to run: 240GB+ RAM (1.8-bit extreme quant)
Usable speed needs: 256GB+ RAM for ~10 tok/s
Full quality: 2x H100 or 8x A100

Your options to run locally:
2x DGX Spark: 256GB → barely fits 1.8-bit, ~1-2 tok/s
4x Mac Studio M3 Ultra 512GB: runs at 25 tok/s with RDMA
```

**Verdict:** Use API. Local hosting is out of reach without datacenter hardware.

---

## 8. Model Size Practical Guide

### What each size can actually do

| Size | RAM Q4 | Best for | Breaks on |
|------|--------|----------|-----------|
| 4B | ~3GB | Autocomplete, simple tasks, detection | Multi-file projects |
| 9B | ~6GB | Single file coding, basic agents | Complex reasoning chains |
| 14B | ~9GB | Multi-file awareness, reliable tool calls | Novel architecture decisions |
| 30-35B MoE (A3B) | ~22GB | Large projects, agentic work | Very complex novel problems |
| 70B | ~42GB | Production agents, near-frontier | Hardware barrier |
| 120B | ~70GB | Frontier-quality, orchestrator | 96GB+ RAM needed |
| 200B MoE | ~128GB | Near-frontier, complex systems | Extreme hardware |

### Key Insight
**Scaffold matters more than model size.** Same model in different agent harnesses scored 22 points apart on SWE-bench. A well-structured 4B agent can outperform a poorly structured 70B one on structured tasks.

---

## 9. Tokens, Context, and KV Cache

### Token Basics
```
1 token ≈ 0.75 words ≈ 4 bytes (bf16) ≈ 2 bytes (fp16)
1,000 tokens ≈ 2KB text
1M tokens ≈ 2MB text
```

### KV Cache Size Per Token
```
Formula: 2 × layers × kv_heads × head_dim × bytes
For Qwen3.5-35B-A3B (approximate):
~300 bytes per token in KV cache

Context | KV Cache Size
4k      | ~1.2GB
32k     | ~10GB
128k    | ~40GB
256k    | ~80GB
```

KV cache is ~150,000x heavier per token than raw text storage. This is why context length is a RAM problem not a storage problem.

### Why Large Context Degrades
- Transformers use O(n²) attention — 2x longer = 4x compute
- "Lost in the middle" — models attend well to start/end, poorly to middle
- Effective quality degrades past ~60-70% of technical limit
- Practical rule: use context to 60-70%, checkpoint before degradation

---

## 10. Current Benchmarks (March 2026)

### SWE-bench Verified (real GitHub issue resolution)
```
Claude Opus 4.6:    80.8% (highest)
Gemini 3.1 Pro:     80.6%
MiniMax M2.5:       80.2%
GPT-5.4:            ~80%
Kimi K2.5:          76.8%
```

Six models within 0.8% of each other. The scaffold matters more than the model at this level.

### For Local Models
- Qwen3-30B-A3B: 69.6 on Tau2-bench (agentic benchmark) — competitive with proprietary
- Llama 3.3 70B: GPT-4 class performance locally at 30+ tok/s on Mac Studio M4 Max

---

## 11. Kimi K2.5 Tool Call Loop Bug

**Confirmed bug in OpenClaw v2026.3.7-3.8:**
- Kimi K2.5 loops on tool calls instead of producing text output
- Model thinks about using tools but only generates text blocks
- Outputs tool calls as literal plain text instead of structured calls
- **Workaround:** Downgrade to OpenClaw v2026.3.2
- Not your code — confirmed OpenClaw regression

---

## 12. The Three Memory Architectures (Full Detail in ai_memory_architectures.md)

### Architecture 1 — Selective KV Context Management
- Save ONLY the KV for the unfinished active task
- Reload stable reference docs (architecture, contracts, role) fresh as MD text every session
- MD files = ~700 tokens on reload
- Active task KV = ~1-2GB, loads in <1 second
- Near-zero context usage at session start
- **Buildable now with existing tools**

### Architecture 2 — Precomputed KV Execution
- Compile stable project knowledge → KV artifacts offline (once)
- At execution: fetch precomputed KV, zero context cost for knowledge
- Context window = 100% available for actual generation
- Like compiled code — runs from precomputed artifacts, doesn't recompile
- **Needs custom KV injection interface in inference engine**

### Architecture 3 — Virtual Context Memory (OS-Level)
- Context window = RAM (small, fast, active only)
- SSD = virtual memory (large, cheap, completed work)
- KV pages swap in/out dynamically like OS pages
- Completed tasks: KV → MD summary → delete KV
- SSD is 350x cheaper than GPU VRAM
- **Infrastructure exists (LMCache, KVSwap, NVIDIA ICMSP)**
- **Agent application layer: not built yet — the gap**

---

## 13. Agent Architecture You Designed

### The Full Stack
```
Architect (70B+ or API):
- Plans project once
- Creates structured plan + initial context.md
- Rarely invoked after planning

Manager (35B or API):
- Quality oversight
- Reads sentinel alerts
- Corrects drift
- Activates infrequently — checkpoint reviews only

Worker (fine-tuned small model, 4B-35B):
- Executes tasks
- Autosaves context.md + task KV every 10 turns
- Emits RESET signal at 90% context
- Resumes from KV instantly
- Never accumulates context

Sentinel (fine-tuned 1.7B):
- Pure detection, zero production
- Checks each file and worker log
- Detects drift, errors, loops, naming inconsistency
- Reports to manager immediately
- ~400MB RAM, runs on your 8GB office PC
- Can run multiple sentinels in parallel for different checks
```

### Why Sentinel Works
Detection is fundamentally easier than generation. Binary output: DRIFT / OK. Doesn't need to understand WHY, just THAT something is wrong. A 1.7B model fine-tuned specifically for pattern matching can catch errors a 70B model misses because it hyperfocuses on consistency checks.

### Mode Switching
```
Planning mode: full context, all resources, thinking model
               builds exact pipeline + context.md

Execution mode: tight context (~2k tokens), efficient model
               executes against plan, never sees full history

Same agent, different modes — or separate Manager/Worker agents
```

---

## 14. Self-Aware Context Management

### What to train into the model
```
At 90% context:
→ STOP
→ Write perfect checkpoint to context.md
→ Emit RESET signal
→ Session resets
→ Resumes from checkpoint

This is a TRAINED REFLEX not a framework
Narrow, deterministic, high-frequency
Simple enough for 4B to learn reliably
```

### Checkpoint Format
```markdown
## CHECKPOINT
Status: MID-TASK INTERRUPT
What I was doing: [exact file, line, action]
What I completed: [list with ✅]
What I was about to do: [ordered list]
Exact mid-task state: [partial code/work]
Context when interrupted: [key decisions, constraints]
Resume instruction: [exactly what to do next]
```

### Context Stats Injection
```python
{
  "context_used": 45000,
  "context_limit": 131072,
  "context_percent": 34%,
  "health": "🟢 healthy",
  "recommendation": "continue"
}
```
Injected before every model call. Model trained to respond to health signals.

---

## 15. Training Data for Your Fine-Tune

### What to train (small dataset, big impact)
```
Dataset A — Checkpoint writing (~500-1000 examples):
Input:  task context at 85-90% window
Output: perfect context.md checkpoint

Dataset B — Resume behavior (~500-1000 examples):
Input:  context.md + RESUME signal
Output: continues correctly without re-reading history

Dataset C — Task completion summary (~300-500 examples):
Input:  completed task context
Output: clean MD summary in your format

Dataset D — Status signals (~300-500 examples):
Input:  various task states
Output: structured JSON status updates

Total: ~2000 examples
Source: your existing ClassAI agent logs (synthetic generation)
Training time: 1-2 days on RX 580 with LoRA
```

---

## 16. Precomputed KV / Speculative Execution Concepts

### Speculative Decoding (exists)
Small draft model predicts next 4-8 tokens, large model verifies. If correct: accept all (2-4x speedup). Already in production at Anthropic, Google.

### KV Prefetching (your idea, partially exists)
```
Task classifier (0.5B) identifies task type
→ Prefetch KV for known patterns before session starts
→ Worker generates → verification not computation
→ 40-60% faster for structured known tasks
```

### Why This Works for Structured Tasks
Your tasks aren't arbitrary. "Write JWT endpoint in FastAPI" has ~90% predictable first 2-3k tokens. Precomputation accuracy: 70-90% for first tokens. Branching: 10-20 realistic paths vs astronomical for arbitrary generation.

---

## 17. What Already Exists vs What's Yours

### Already solved by others
| System | What | Status |
|--------|------|--------|
| KVSwap (Nov 2025) | KV offload to SSD during decoding | Paper |
| NVIDIA ICMSP (CES 2026) | Enterprise KV→NVMe, persistent across runs | Announced |
| LMCache | KV offload + sharing, works with vLLM | Production |
| HiFC (NeurIPS 2025) | Direct GPU→SSD at near-DRAM speed | Paper |
| MTDS (Jan 2026) | Multi-tier dynamic KV storage | Paper |
| MemGPT/Letta | Tiered memory for agents | Production |

### Your novel contribution
None of the above implement:
- Task-aware selective KV preservation
- "Keep only unfinished task KV, discard the rest"
- KV → MD archival on task completion
- Completion registry as page table
- Sentinel cross-validating KV health
- The full agent OS lifecycle you designed
- Applied specifically to structured agentic execution workflows

The infrastructure is solved. The agent application layer on top of it: not built.

---

## 18. Why This Wasn't Built Before

1. **Problem didn't exist** — context windows were tiny until 2023, sessions were short
2. **SSDs too slow** — until Gen5 NVMe in 2024-2025, GPU→SSD latency was unusable
3. **"Bigger context" easier to sell** — 1M context headline vs "efficient 2k context"
4. **Transformer math wasn't obviously compatible** — attention entanglement made naive page swap break
5. **Agentic use cases came late** — chatbots don't need week-long continuity
6. **Wrong people never in same room** — OS/systems thinkers, inference engineers, agent builders never collaborated

**2025-2026:** All six reversed simultaneously. Timing is perfect.

---

## 19. MacBook Neo (brand new, March 11 2026)

- Apple's cheapest laptop ever: $599 ($499 student)
- First Mac with A18 Pro chip (iPhone 16 Pro chip, not M-series)
- 8GB unified memory, 256GB SSD
- **For LLM work: useless** — 8GB, even less than your current M2 Pro
- Target: students and budget consumers
- Your M2 Pro is more capable

---

## 20. Build Path (Practical Next Steps)

### Immediate (this week)
```
1. Generate training dataset
   From ClassAI agent logs
   ~2000 checkpoint/resume examples
   Synthetic generation with existing pipeline

2. Fine-tune Qwen3.5 4B LoRA
   On RX 580 (same as existing fine-tune work)
   New dataset = new behavior
   1-2 days training

3. Vibe-code orchestrator wrapper
   ~100-200 lines Python
   Watches context %
   Triggers saves/resets
   Manages file lifecycle
```

### Tools to use
```
vLLM + LMCache: KV offload to SSD (already works)
Ollama: simple local serving
LoRA fine-tuning: your existing pipeline
Python orchestrator: vibe-code with Claude Code
```

### Don't build from scratch
- LMCache handles KV offload ✅
- vLLM handles inference ✅
- Your LoRA pipeline handles training ✅
- You add: agent task awareness layer on top

---

## 21. Key Principles From This Conversation

```
1. Context management > model size for structured tasks
   A 4B with perfect context management beats 70B with bad context
   On known, structured, decomposable tasks

2. Scaffold matters more than model
   Same model, different harness = 22 point SWE-bench difference
   Your OpenClaw architecture matters more than which model you use

3. Separation of concerns
   Knowledge (precomputed/MD) ≠ Reasoning (context window)
   Mixing them is the root of the problem

4. SSDs are 350x cheaper than GPU VRAM
   Store cold KV on disk not in GPU
   Load only what's actively needed

5. Trained reflexes > framework complexity
   A model that writes checkpoints at 90% as a reflex
   beats elaborate external memory management systems
   because the behavior is intrinsic not bolted on

6. The completion registry is the page table
   Know what's done, what's in progress, what's next
   Zero context cost — just a JSON file

7. Sentinel solves the "unreliable worker" problem
   Worker doesn't need to be self-aware of drift
   Sentinel is the external awareness layer
   Tiny, fast, specific — does one thing perfectly
```

---

*Session: March 27, 2026*
*Topics: Hardware, Mac clusters, RDMA, model sizes, benchmarks, token/KV theory, agent architectures, memory management, virtual context memory, build path*
*Key output: ai_memory_architectures.md (detailed specs for all 3 architectures)*
