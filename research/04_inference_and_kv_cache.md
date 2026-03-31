# Inference Optimization & KV Cache
> PagedAttention, speculative decoding, inference engines, KV management

---

## KV Cache — The Fundamentals

### What It Is
During autoregressive generation, every previous token's Key and Value tensors must be recomputed — or cached. The KV cache stores these to avoid recomputation.

**Memory formula:**
```
KV cache = 2 × n_layers × n_kv_heads × d_head × seq_len × bytes_per_element

Example: LLaMA 3 70B (BF16)
= 2 × 80 × 8 × 128 × 4096 × 2 bytes
= ~13.4 GB at 4096 context

At 32K context: ~107 GB   ← this is why long context is hard
```

### Why It's the Bottleneck
- KV cache is 150,000× heavier per token than raw text storage
- For a 7B model in FP16 at batch=1: model weights = 14GB, KV cache at 8K context = ~4GB
- Serving 100 concurrent users with 4K context each: 400GB KV cache alone
- This is why PagedAttention, GQA, MLA, sliding window, and SSMs all exist

---

## Reducing KV Cache Size

### Grouped Query Attention (GQA)
Share KV heads across groups of Q heads.

```
MHA:  32 Q heads, 32 KV heads → full KV cache
GQA:  32 Q heads, 8 KV heads  → 4× smaller KV cache (LLaMA 3 default)
MQA:  32 Q heads, 1 KV head   → 32× smaller KV cache (quality drops)
```

**arXiv:2305.13245** — retrofit from MHA by mean-pooling existing KV heads.

### Multi-head Latent Attention (MLA — DeepSeek)
Compress K, V into a small latent vector. Cache the latent (tiny). Decompress at attention time.

```
Standard: cache K, V tensors directly (d_head × n_heads per token)
MLA:      cache compressed latent c_KV (d_c << n_heads × d_head per token)
          decompress: K, V = up_proj(c_KV)
```
5-13× KV cache reduction vs MHA. **arXiv:2405.04434**

### Sliding Window Attention (Mistral)
Each token only attends to the last W tokens (window = 4096 in Mistral). KV cache bounded at W × layers. Context can be longer than W via chunking.

Every 4th layer uses full attention to capture long-range dependencies.

### KV Cache Quantization
- INT8 KV cache: ~2× memory reduction, minimal quality loss. Supported in vLLM.
- INT4 KV cache: ~4× reduction, quality degrades more.
- FP8 KV: H100-specific.

### Selective KV Eviction
**H2O (arXiv:2306.14048):** ~20% of tokens ("heavy hitters") get >80% of attention mass. Keep only those + recent tokens. 5× KV reduction with minimal quality loss.

**SnapKV (arXiv:2404.14469):** Observe attention patterns during prompt processing → predict which KV entries matter → compress before generation. Constant KV size regardless of prompt length.

**KVSharer (arXiv:2410.18517):** Different layers often have similar KV patterns → share them. 30% KV reduction, plug-and-play, no retraining.

---

## KV Cache Offloading

### The Economics
```
GPU VRAM: ~$21/GB (RTX 3090 used)
NVMe SSD: ~$0.06/GB (Samsung 990 Pro)
Ratio:    350× cheaper on SSD
```

For a multi-week agentic project with 10 active task KV files:
- Traditional: 150GB VRAM ($3,150 worth)
- SSD offload: 150GB NVMe ($9)

### LMCache (arXiv:2404.18262)
KV cache storage layer that persists and reuses KV across requests. For repeated system prompts or long-context RAG: up to 12× time-to-first-token reduction.

- Works with vLLM
- KV stored to SSD or RAM, fetched on match
- github.com/LMCache/LMCache

### KVSwap (Nov 2025)
KV offload to SSD during decoding — model accesses SSD-resident KV as if it were in GPU memory. Paper-stage.

### NVIDIA ICMSP (CES 2026)
Enterprise KV→NVMe solution. Persistent KV across runs. Datacenter-focused.

---

## Inference Engines Compared

| Engine | Format | Best for | Key feature |
|--------|--------|----------|-------------|
| **vLLM** | HF weights | Production serving | PagedAttention, continuous batching |
| **llama.cpp** | GGUF | Local, CPU+GPU | No dependencies, Apple Metal, quantization |
| **Ollama** | GGUF (via llama.cpp) | Developer ease | Wraps llama.cpp, model management |
| **ExLlamaV2** | EXL2 | Max speed on NVIDIA | Variable bitrate quant, fastest consumer GPU |
| **MLX** | MLX weights | Apple Silicon | Unified memory, Metal, no CUDA |
| **Exo** | Various | Multi-device cluster | Auto-discovers peers, distributes model |
| **TensorRT-LLM** | TensorRT | NVIDIA production | NVIDIA-optimized, highest FLOP utilization |

### vLLM Deep Dive

**PagedAttention (arXiv:2309.06180):**
- KV cache managed like OS virtual memory pages
- Pages are non-contiguous in physical memory — eliminates fragmentation
- Enables KV cache sharing (prefix caching, parallel sampling)
- 2-24× throughput improvement vs HuggingFace

**Continuous batching:**
- Process requests at token level, not batch level
- Add/remove sequences dynamically mid-batch
- GPU never sits idle waiting for padded sequences to finish

**Key features:**
- Prefix caching (free repeated system prompt processing)
- Speculative decoding
- Multi-LoRA serving
- OpenAI-compatible API

```bash
pip install vllm
vllm serve Qwen/Qwen2.5-7B-Instruct --quantization awq
```

### llama.cpp / Ollama

llama.cpp:
- C/C++, zero runtime dependencies
- GGUF format (Q4_K_M is the gold standard)
- GPU offloading: `--n-gpu-layers N` (offload N layers to GPU, rest on CPU)
- Apple Metal, CUDA, Vulkan, OpenCL backends

Ollama (wrapper):
```bash
ollama pull qwen2.5:7b
ollama run qwen2.5:7b
# API at localhost:11434
```

For your Mac Minis: Ollama with Metal is the easiest path. Use `--num-gpu 1 --metal` implicitly.

### Exo (Your Mac Cluster)

Distributes model across multiple machines. Auto-discovers via mDNS.

```bash
pip install exo
# Run on each Mac Mini — they find each other
exo
# Endpoint: http://main-mini:52415/v1/chat/completions
```

72GB combined on 6×M4 16GB → can run 70B Q4 comfortably at 5-10 tok/s over WiFi, 8-15 tok/s over ethernet.

---

## Speculative Decoding

**The insight:** Large model verification is much cheaper than generation. A small draft model generates N candidate tokens in parallel, large model verifies them all in one forward pass.

```
Draft model: generate tokens t1, t2, ..., tN (fast, parallel)
Large model: verify all N tokens in one pass
If t1...tk accepted, t(k+1) rejected → accept first k, sample new from position k+1
Result: same output distribution as large model alone, 2-3× faster
```

**Paper:** arXiv:2211.17192 (Google, 2023)

**Draft model selection:**
- Must be from the same model family (same tokenizer at minimum)
- 10-20× smaller than target
- E.g., Qwen2.5-0.5B drafts for Qwen2.5-7B
- Acceptance rate depends on task — code tasks have high acceptance, creative tasks lower

**Built into vLLM** — just configure `speculative_model` parameter.

---

## Agent Architectures

### ReAct Pattern (arXiv:2210.03629)
Interleave reasoning traces and action steps:
```
Thought: I need to check the file system
Action: bash("ls -la")
Observation: [file listing]
Thought: Now I can see the structure...
Action: read_file("main.py")
...
```
Every tool-using agent system is built on this pattern.

### Memory Types in Agents

| Memory type | Storage | Lifetime | Access |
|-------------|---------|----------|--------|
| In-context | Context window | Session | Instant |
| Episodic (KV) | GPU/SSD | Cross-session | <1s load |
| Semantic (MD) | File system | Permanent | Text injection |
| Parametric | Model weights | Forever | Built-in |

### MemGPT / Letta (arXiv:2310.08560)
OS-inspired memory hierarchy:
- "Main memory" = limited context window (fast, small)
- "External storage" = unlimited disk (slow, large)
- LLM issues memory management function calls (load, save, search)
- Reference architecture for stateful long-running agents

### Multi-Agent Frameworks

| Framework | Style | Best for |
|-----------|-------|----------|
| AutoGen (Microsoft) | Conversation-based | Multi-agent chat pipelines |
| CrewAI | Role-based | Structured team workflows |
| LangGraph | Graph-based state machine | Complex branching workflows |
| Swarm (OpenAI) | Lightweight handoffs | Simple agent routing |

**For your architecture:** Skip frameworks. Build the Python orchestrator directly (~200 lines). Frameworks add complexity that obscures what's happening.

---

## Benchmarking Inference

```python
import time

def benchmark(model, prompt, n_tokens=100):
    start = time.perf_counter()
    tokens = model.generate(prompt, max_new_tokens=n_tokens)
    elapsed = time.perf_counter() - start

    tps = n_tokens / elapsed
    ttft = measure_time_to_first_token(model, prompt)

    return {
        "tokens_per_second": tps,
        "time_to_first_token_ms": ttft * 1000,
        "memory_gb": torch.cuda.max_memory_allocated() / 1e9
    }
```

**Standard metrics:**
- **TTFT** (time to first token) — latency
- **TPS** (tokens per second) — throughput
- **Peak GPU memory** during inference
- **KV cache size** at target context length
