# KV Cache → SSD: State of the Art (2025-2026)
> Research compiled March 27, 2026 via 3 parallel research agents

---

## The Big Picture

The field has solved the **mechanics** of moving KV cache to SSD.
Nobody has built the **semantic/task-aware agent OS layer** on top.
That is exactly what Architecture 3 builds.

---

## 1. DeepSeek — MLA (Multi-head Latent Attention)

### What it is
Instead of storing full K+V tensors, MLA compresses them into a single low-dimensional latent vector before storage.

### The math
```
Standard MHA (128 heads, head_dim=128):
  KV per token per layer = 128 × 128 × 2 = 32,768 dims
  Storage: ~516 KB/token total model (LLaMA 405B scale)

MLA (DeepSeek V2/V3):
  C^KV = W^DKV · h_t    → compressed latent: 512 dims
  + decoupled RoPE:       → +64 dims
  Total stored: 576 dims per token per layer
  Storage: ~70 KB/token total model

Compression vs MHA: 57x
Compression vs GQA-8: ~4x
Quality vs MHA: BETTER (per-head expressiveness preserved)
```

### How reconstruction works
At inference time, K and V are reconstructed via learned up-projection matrices:
```
K = W^UK · C^KV
V = W^UV · C^KV
```
These are matrix multiplications — cheap on GPU, far cheaper than the bandwidth savings.

### CRITICAL implementation gotcha
The official DeepSeek implementation **decompresses** MLA back to full K/V before storing — negating all compression gains. KTransformers fixed this by absorbing the decompression matrices into layer weights, storing the compressed 576-dim latent directly.

**If we use MLA or a model with MLA, we must store the compressed latent — not the decompressed K/V.**

### Practical impact on SSD offloading
- 70 KB/token makes SSD storage economically viable
- DeepSeek's API uses disk KV cache for prefix reuse: $0.014/M tokens (cache hit) vs $0.14/M (recompute) — 10x cost reduction
- DeepSeek 3FS (open source): 40 GiB/s KV cache I/O throughput via NVMe + RDMA

### DeepSeek V4 "Engram" (speculative, not yet published)
- Tiered KV hierarchy like CPU cache: GPU hot → CPU DRAM warm → SSD cold
- Reported 40% GPU memory reduction vs V3
- Conditional memory module: O(1) knowledge lookup separate from attention

---

## 2. NVIDIA — ICMSP + Dynamo

### ICMSP (Inference Context Memory Storage Platform)
Announced CES 2026, expanded GTC March 2026. **Ships H2 2026.**

```
Hardware: BlueField-4 STX storage rack
- 64x BlueField-4 DPUs per rack
- ~9.6 PB NVMe flash per rack
- 800 Gbps per DPU via Spectrum-X RDMA
- Bypasses CPU — GPU talks directly to DPU
- Hardware-level multi-tenant KV isolation
```

Not consumer hardware. Datacenter only. Not relevant to our build hardware.

### NVIDIA Dynamo — The Software Layer
Open source (GTC March 2025). This IS relevant to us.

```
Tier hierarchy (G1→G4):
G1   → GPU HBM (active KV)
G2   → CPU DRAM (overflow)
G3   → Local NVMe (warm reuse)
G3.5 → ICMSP rack (cross-node shared)
G4   → Networked object storage (cold archive)
```

**Key components:**
- **KV Block Manager (KVBM):** Moves KV blocks across tiers. LRU + priority-weight eviction. Inference-engine-agnostic (supports vLLM, SGLang, TRT-LLM).
- **KV-Aware Router:** Routes requests to GPU nodes that already hold the relevant KV prefix.
- **NIXL (NVIDIA Inference Transfer Library):** Unified transfer API across all tiers — abstracts NVLink, InfiniBand, RoCE, S3.

**Performance claims:** 5x more tokens/sec, 4-5x better power efficiency for long-context/agentic workloads.

### HiFC (NeurIPS 2025)
Academic paper, not NVIDIA product, but uses NVIDIA's GPUDirect Storage.
- GPU→SSD direct transfer, bypasses CPU and host DRAM entirely
- Uses pSLC (pseudo-SLC) zones on commodity NVMe — only writes to highest-performance flash region
- Achieves DRAM-level swap throughput at 4.5x lower 3-year TCO

### Blackwell NVFP4 KV Cache
- 4-bit floating point KV storage (Blackwell GPUs only)
- 50% smaller vs FP8, <1% accuracy loss
- 2x longer context or 2x larger batch in same VRAM

### Kioxia GP Series SSD (2027)
- 100M IOPS target — 33x current enterprise NVMe
- GPU-direct access over PCIe 7.0
- Being co-developed with NVIDIA

---

## 3. The Current Production Landscape

### LMCache — Most Production-Ready
```
Hierarchy: GPU VRAM → CPU RAM → Disk → Redis/S3
Integration: vLLM native, SGLang supported
Cache key: content-hash of 256-token chunks
Sharing: cross-instance (any vLLM process can reuse blocks)
Performance: 2.3-14x throughput, 7-92% lower ITL
```
This is our foundation for Architecture 3. Use it, don't replace it.

### MTDS (Jan 2026, Springer)
- vLLM plugin: GPU→DRAM→SSD tiering
- Reduces TTFT by 25-31%
- Hit rate predictor: frequency-based, not semantic

### KVSwap (Nov 2025)
- Targets on-device (edge/mobile) inference
- Stores full KV on disk, keeps only keys in RAM for prefetching
- Overlaps disk I/O with compute across layers
- Single device, single session only

### Agent Memory Below the Prompt (Feb 2026, arXiv:2603.04428)
**Closest to what we're building.** Persists per-agent KV in Q4 quantized format to disk.
```
- Tested on Apple M4 Pro
- Eliminates 15.7s re-prefill per agent session restart
- Q4 KV reload: ~500ms (hides behind prior agent's decode)
- Per-agent isolated block pool in safetensors format
- Survives server restarts
```
Gap: whole agent cache saved/restored as a blob. No intra-agent task selectivity.

---

## 4. The 8 Gaps — What Nobody Has Built

These are the exact things Architecture 3 implements. Confirmed by research.

| Gap | What's missing | What we build |
|-----|----------------|---------------|
| **A. Task-semantic KV identity** | All systems use content hash or recency — no concept of "this block = tool call N of task T" | Tag every KV block: task_id, phase, type (system/tool/reasoning/result) |
| **B. Task-phase-aware preservation** | LRU evicts based on access time, not task state | "Keep planning KV until task completes, regardless of access frequency" |
| **C. Cross-task semantic reuse** | LMCache reuses by token prefix match only | Semantic routing: agent B gets relevant KV from agent A without exact prefix match |
| **D. KV DAG topology** | All systems assume flat linear KV sequence | Track branching reasoning paths; evict abandoned branches, preserve successful ones |
| **E. Selective sub-agent KV inheritance** | No filtering on spawn | Parent→child KV: inherit planning context, not retrieval documents |
| **F. Task-graph-driven prefetch** | KVSwap: attention importance. LMCache: prefix match | "Agent entering code phase → prefetch code context KV, not retrieval docs" |
| **G. Cross-session semantic versioning** | Agent Memory Below Prompt: monolithic per-agent blob | Per-task TTLs: planning KV permanent, tool-response KV expires 24h |
| **H. Task-criticality eviction** | All: recency/frequency based | "This block is architecturally critical to active task → never evict regardless of age" |

---

## 5. Implications for Our Architecture 3 Build

### What to use off the shelf
```
LMCache         → KV block storage + disk tier (our foundation)
vLLM            → Inference engine
NVIDIA Dynamo   → KV block manager (if we want the G1-G4 hierarchy)
HiFC approach   → GPUDirect Storage (bypass CPU for SSD transfers)
```

### What we build on top
```
Task Registry   → JSON file: task_id, phase, KV block list, TTL, criticality
Block Tagger    → Tags every KV block with task metadata on write
Semantic Router → Selects which KV blocks to load per task (not prefix hash)
Eviction Policy → task-criticality > frequency > recency
Prefetch Oracle → Reads task graph, prefetches KV for next phase
MD Archiver     → On task complete: KV → compressed MD summary → delete KV
```

### MLA consideration for our model choice
- Qwen3.5 uses **GQA** (not MLA)
- GQA: ~4x smaller KV than MHA, but ~14x larger than MLA
- For Architecture 3, consider: use DeepSeek-V3 via API for the worker (MLA native), or accept GQA math for our fine-tuned Qwen model
- GQA numbers on RX 580 (8GB): 9B Q4 → ~300 bytes/token KV → 32k context ≈ 10GB → **won't fit with GQA at long context**
- Solution: Architecture 1 first (short context, KV saved only for active task). Architecture 3 later with MLA-based model or better hardware.

### Hardware reality for Architecture 3
```
Our hardware:          What it means
RX 580 (8GB)    →  KV tier G1 (hot, active task only)
16GB RAM        →  KV tier G2 (warm, recently completed)
NVMe SSD        →  KV tier G3 (cold, completed tasks)
Mac Mini cluster→  KV tier G1 for 70B inference
```

---

## 6. Key Papers to Read for Architecture 3

| Paper | arXiv | Why |
|-------|-------|-----|
| Agent Memory Below the Prompt | 2603.04428 | Closest implementation to what we need |
| SideQuest | 2602.22603 | Model-driven KV importance for agentic reasoning |
| MemOS | 2507.03724 | Full agent OS memory architecture |
| LMCache | 2510.09665 | Our production foundation |
| HiFC | NeurIPS 2025 | GPU→SSD direct transfer technique |
| DeepSeek-V2 | 2405.04434 | MLA architecture (57x KV compression) |
| KVFlow | arXiv Jul 2025 | Task-graph-aware KV prefetch (closest to gap F) |

---

*Research compiled March 27, 2026*
*Sources: 3 parallel research agents covering NVIDIA, DeepSeek, and full state-of-the-art landscape*
*Agent IDs: a4554d1145b0a1243 (NVIDIA), a94daedd587a82d3f (DeepSeek), abd4cdcb07bd57fe3 (landscape)*
