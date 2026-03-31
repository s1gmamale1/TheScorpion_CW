# Research Index
> Quick reference for all files in this project. Find anything in seconds.

---

## Core Design Documents

### `ai_memory_architectures.md`
**The main design doc.** All 3 novel agent memory architectures, fully spec'd.
- **Architecture 1 — Selective KV:** Save only active task KV. Reload stable refs from MD every session. ~700 tokens overhead at session start. Buildable now with existing tools.
- **Architecture 2 — Precomputed KV:** Compile stable project knowledge → KV artifacts offline (once). Context window = 100% free for generation. Needs KV injection interface.
- **Architecture 3 — Virtual Context Memory:** OS-style paging. Context = RAM, SSD = virtual memory. KV swaps in/out dynamically. SSDs are 350x cheaper than GPU VRAM.
- Also covers: agent stack (Architect/Manager/Worker/Sentinel), build order, implementation stack.

### `session_summary.md`
**21-topic reference from the founding session (March 27, 2026).** Everything discussed in one place.
- Sections 1-6: Hardware, GPU prices, Mac cluster, RDMA, Mac Studio, DGX Spark
- Sections 7-10: Kimi K2.5, model size guide, token/KV math, benchmarks (March 2026)
- Sections 11-14: OpenClaw bug, the 3 architectures (brief), agent stack, context management
- Sections 15-19: Training data plan, speculative KV concepts, what exists vs what's novel, why now, MacBook Neo
- Sections 20-21: **Build path (start here for next steps)**, key principles

---

## Research Files

### `research/01_transformer_architectures.md`
**Go here for: how transformers work, the modern stack, attention math.**
- GPT lineage: GPT-1 → GPT-2 → GPT-3 → GPT-4 → modern open models
- Original attention (MHA) → GQA → MQA → Flash Attention
- Modern stack: Pre-norm, RMSNorm, SwiGLU, RoPE, GQA, Flash Attention — why each replaced the original
- MoE (Mixture of Experts): sparse activation, routing, DeepSeek-V3 (671B total, 37B active)
- Mamba/SSMs overview (see 03 for deep dive)

### `research/02_training_techniques.md`
**Go here for: how to train/fine-tune a model, LoRA, quantization, alignment.**
- Pretraining from scratch: data pipeline, deduplication, quality filtering, tokenization, distributed training
- LoRA / QLoRA / DoRA: how low-rank adaptation works, when to use each
- Alignment: DPO, ORPO, SimPO — preference training without RL
- Quantization: GPTQ, AWQ, GGUF, NF4 — formats and tradeoffs
- **Key numbers for our build:** LoRA on RX 580 (8GB) feasible for 7-9B models

### `research/03_novel_architectures.md`
**Go here for: alternatives to transformers, SSMs, hybrids.**
- Mamba (Dec 2023): selective SSM, O(1) inference per token, no KV cache
- Mamba2 (May 2024): State Space Duality, 2-8x faster training
- RWKV: linear RNN that runs like transformer training, inference like RNN
- Griffin (Google): attention + SSM hybrid — best of both
- xLSTM: extended LSTM with exponential gating
- TTT (Test-Time Training): hidden state = tiny model that trains on each token
- Jamba / Zamba: production MoE + SSM hybrids
- **Key takeaway:** Hybrids win. Pure SSMs lag on exact recall. Attention still needed for precise lookups.

### `research/04_inference_and_kv_cache.md`
**Go here for: KV cache math, inference engines, how KV offloading works.**
- KV cache formula: `2 × n_layers × n_kv_heads × d_head × seq_len × bytes`
- Why KV is the bottleneck: 150,000x heavier per token than raw text
- GQA: 4x KV reduction. MQA: 32x but quality drops. MLA (DeepSeek): 57x, quality preserved.
- PagedAttention (vLLM): paged virtual memory for KV blocks
- Flash Attention: IO-aware tiling, 2-4x faster, O(L) memory
- Speculative decoding: small draft model + large verify model = 2-4x speedup
- Inference engines: vLLM, Ollama, SGLang, LMCache — when to use each

### `research/05_papers_index.md`
**Go here for: finding the right paper to read.**
- 49 curated papers, priority ordered Tier 1→4
- **Tier 1 (read first):** Attention Is All You Need, GPT-3, Scaling Laws, Chinchilla, FlashAttention, LoRA, QLoRA, DPO
- **Tier 2:** LLaMA 1/2/3, Mistral, Mixtral, FlashAttention-2, GQA, RoPE, RMSNorm, SwiGLU
- **Tier 3:** Mamba, RWKV, Griffin, Phi-3, Gemma, Qwen2.5, DeepSeek-V2/V3
- **Tier 4:** Inference/KV papers — PagedAttention, vLLM, speculative decoding, SGLang
- Each entry: year, arXiv link, 2-sentence summary

### `research/06_learning_resources.md`
**Go here for: where to learn — videos, courses, repos.**
- Karpathy YouTube (start here): "Let's build GPT", "Reproduce GPT-2", "Build the Tokenizer"
- 3Blue1Brown: visual attention intuition
- Yannic Kilcher: paper breakdowns
- Courses: fast.ai, DeepLearning.AI, CS324 Stanford
- Newsletters: Ahead of AI (Sebastian Raschka), The Batch, Import AI
- Key GitHub repos: nanoGPT, LLaMA, Mistral, Mamba, vLLM, LMCache, Axolotl, Unsloth

### `research/07_kv_ssd_state_of_art_2026.md`
**Go here for: everything about KV cache → SSD offloading, Architecture 3 research.**
- **DeepSeek MLA:** 57x KV compression (32,768 dims → 576). Must store compressed latent not decompressed K/V.
- **NVIDIA ICMSP:** BlueField-4 STX, 9.6 PB NVMe rack, ships H2 2026. Dynamo software: G1→G4 tier hierarchy.
- **Production tools:** LMCache (use as foundation), MTDS (vLLM plugin, -25-31% TTFT), KVSwap (on-device), HiFC (GPU Direct Storage)
- **Agent-aware research:** SideQuest (model-driven KV importance), KEEP (embodied agents), Agent Memory Below Prompt (closest to our Arch 3)
- **The 8 gaps:** what no system has built — all 8 are exactly what Architecture 3 implements
- **Performance numbers:** NVMe KV reload ~500ms at 4K context Q4, HiFC achieves DRAM-level speed

---

## Navigation by Topic

| I want to know about... | Go to |
|------------------------|-------|
| How does attention work? | `research/01` |
| How do I fine-tune on RX 580? | `research/02` |
| What is LoRA / QLoRA? | `research/02` |
| What is Mamba / RWKV / SSMs? | `research/03` |
| KV cache memory formula | `research/04` |
| Which inference engine to use? | `research/04` |
| Which papers to read first? | `research/05` |
| Where to watch/learn? | `research/06` |
| DeepSeek MLA architecture | `research/07` |
| NVIDIA ICMSP details | `research/07` |
| What existing KV-SSD systems do | `research/07` |
| The 3 memory architectures (full spec) | `ai_memory_architectures.md` |
| Build path / next steps | `session_summary.md` §20 |
| Hardware setup & GPU prices | `session_summary.md` §1-5 |
| Agent stack design | `session_summary.md` §13 |
| Training data plan | `session_summary.md` §15 |
| What's novel vs what exists | `session_summary.md` §17 |

---

## Build Progress Tracker

| Phase | Status | File |
|-------|--------|------|
| Research — transformer fundamentals | ✅ Done | `research/01` |
| Research — training & fine-tuning | ✅ Done | `research/02` |
| Research — novel architectures | ✅ Done | `research/03` |
| Research — inference & KV cache | ✅ Done | `research/04` |
| Research — papers index (49 papers) | ✅ Done | `research/05` |
| Research — learning resources | ✅ Done | `research/06` |
| Research — KV-SSD state of art 2026 | ✅ Done | `research/07` |
| Architecture design (all 3) | ✅ Done | `ai_memory_architectures.md` |
| **Architecture 1 — Orchestrator code** | 🔲 Next | TBD |
| Architecture 1 — Training data gen | 🔲 Next | TBD |
| Architecture 1 — LoRA fine-tune | 🔲 Next | TBD |
| Architecture 2 — Precomputed KV | 🔲 Later | TBD |
| Architecture 3 — Virtual context OS | 🔲 Later | TBD |

---

*Last updated: March 27, 2026*
