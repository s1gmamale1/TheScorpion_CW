# Transformer Architectures — Deep Reference
> From "Attention Is All You Need" to the modern stack

---

## The Original Transformer (2017)

**Paper:** Vaswani et al. — arXiv:1706.03762

### Architecture Decisions & Why

| Component | Original Choice | Why |
|-----------|----------------|-----|
| Attention heads | 8 heads, d_head=64 | Quality-vs-cost optimum; subspaces specialize |
| Scaling | 1/√d_k | Prevents softmax saturation at high dimensions |
| Positional encoding | Sinusoidal (not learned) | Generalizes beyond training length; PE(pos+k) is linear in PE(pos) |
| Normalization | Post-norm (LayerNorm after residual) | Standard at the time; worse than pre-norm at depth |
| FFN expansion | 4× d_model | Arbitrary but robust; acts as key-value memory (Geva et al., 2021) |
| Activation | ReLU | Standard; GELU is strictly better |

---

## GPT Lineage

### GPT-1 (2018) — 117M params
- Pre-training on BooksCorpus, then task-specific fine-tune
- First to use GELU activation over ReLU
- Post-norm (same as original transformer)

### GPT-2 (2019) — 117M to 1.5B
**Key changes:**
- **Pre-norm** introduced — LayerNorm moved to *input* of each block: `x + Sublayer(LayerNorm(x))`
- Weight init scaled by `1/√N` (N = residual layers)
- Extra LayerNorm after final self-attention block
- Context 1024 tokens, vocab 50,257

### GPT-3 (2020) — 175B
- Same pre-norm as GPT-2, same GELU
- Demonstrated scaling laws hold: emergent in-context learning at scale
- Training: 300B tokens from filtered Common Crawl, WebText2, Books, Wikipedia

### GPT-4 (2023) — architecture undisclosed
- Widely believed to be MoE (~8 experts, ~220B active of ~1.8T total)
- 128K context (GPT-4 Turbo)
- RLHF + instruction tuning pipeline

---

## The Modern Stack (LLaMA-style)

Every major open model (LLaMA 1/2/3, Mistral, Qwen, Gemma, Falcon) converged on this:

```
Pre-norm (RMSNorm before each sublayer)
RMSNorm (not LayerNorm)
SwiGLU FFN (not GELU FFN)
RoPE positional encoding (not learned, not sinusoidal)
GQA (not full MHA)
Flash Attention
No bias terms in attention/FFN projections
```

### Why Each Piece

**RMSNorm vs LayerNorm:**
```
LayerNorm: normalize by mean AND variance, has β shift param
RMSNorm:   normalize by RMS only (no mean subtraction, no β)
```
- ~7-15% faster (removes mean subtraction)
- Empirically matches LayerNorm quality (re-centering not needed)
- Fewer params

**SwiGLU vs GELU:**
```
Standard FFN:  FFN(x) = GELU(xW₁)W₂
SwiGLU FFN:   FFN(x) = (Swish(xW) ⊗ xV)W₂   (3 matrices, not 2)
```
- ~1 perplexity point better on language modeling
- Hidden dim must be reduced to 2/3 × 4 × d_model to keep param count equal
- Gating lets FFN selectively pass/block components

**RoPE vs Learned vs ALiBi:**
- **Learned**: capped at max_seq_len, no inductive bias
- **ALiBi**: linear bias to attention logits, good length extrapolation, no learned params — used in BLOOM, MPT
- **RoPE** (winner): rotates Q,K vectors before dot-product; dot product depends only on *relative* position; works with Flash Attention; extensible via frequency scaling (YaRN, LongRoPE)

**Pre-norm vs Post-norm:**
- Post-norm: gradients must pass through LayerNorm in residual path → instability at depth
- Pre-norm: residual path is always `x + (...)` → clean gradient flow
- Pre-norm trains stably at 100+ layers with no special warmup tricks

---

## Attention Variants

### MHA → MQA → GQA

**The problem:** KV cache grows as `n_layers × n_heads × d_head × seq_len`. At 96 layers, 96 heads, d_head=128, seq=8192 in fp16: ~200GB.

| Variant | KV heads | KV cache size | Quality |
|---------|----------|---------------|---------|
| MHA | = Q heads (e.g., 32) | Full | Best |
| MQA | 1 shared | 32× smaller | -slight |
| GQA (winner) | G groups (e.g., 8) | 4× smaller | ≈ MHA |

**GQA** (arXiv:2305.13245): divide Q heads into G groups, each group shares 1 KV head. LLaMA 2 70B uses 8 KV heads (32Q/8KV). GQA is now the default everywhere.

**MLA (DeepSeek-V2):** Most extreme — compress KV into a small latent vector, decompress at attention time. 5-13× KV cache reduction vs MHA. arXiv:2405.04434

---

## Flash Attention

**The problem:** Naive attention materializes `[L × L]` attention matrix in HBM. For L=8192 that's ~134MB per layer per batch — memory-bound, not compute-bound.

**Solution:** Tiling + online softmax. Process Q, K, V in blocks that fit in SRAM. Never write the full attention matrix to HBM. Recompute during backward (cheaper than storing).

| Version | Speedup vs naive | Key innovation |
|---------|-----------------|----------------|
| FA1 (2022) | 2-4× | Tiling + online softmax — arXiv:2205.14135 |
| FA2 (2023) | 2× vs FA1 | Parallelism over seq dim, fewer non-matmul ops — arXiv:2307.08691 |
| FA3 (2024) | 1.5-2× vs FA2 | H100 TMA async, FP8 support — arXiv:2407.08608 |

All are **exact** (not approximate). Drop-in replacement via `flash_attn` package or `torch.nn.functional.scaled_dot_product_attention`.

**Flash Attention repo:** https://github.com/Dao-AILab/flash-attention

---

## Mixture of Experts (MoE)

### How It Works
```
Standard:  output = FFN(x)
MoE:       gate = Router(x)                          # logits over N experts
           top_k = select top-k experts
           output = Σ gate_weight[i] × Expert_i(x)   # weighted sum
```

Sparsity: top-2 of 8 experts = 25% of FFN params active. But over a batch, all experts are used.

### Router Problems & Solutions
| Problem | Solution |
|---------|----------|
| Expert collapse (1-2 experts dominate) | Auxiliary load-balancing loss |
| Load imbalance (some experts overloaded) | Expert capacity cap + token dropping |
| Feedback loop (popular experts get better, used more) | Jitter noise + expert dropout |

**DeepSeek innovation:** Fine-grained MoE (64-256 small experts instead of 8 large ones) + shared always-on experts + auxiliary-free balancing via routing bias correction.

### Production MoE Models
| Model | Total params | Active params | Experts | Routing |
|-------|-------------|---------------|---------|---------|
| Mixtral 8x7B | 46.7B | 12.9B | 8 | top-2 |
| DeepSeek-V2 | 236B | 21B | 160 | top-6 |
| DeepSeek-V3 | 671B | 37B | 256+1 | top-8 |
| Qwen1.5-MoE | 14.3B | 2.7B | 64 | top-4 |

---

## State Space Models (Mamba)

### SSMs vs Transformers
| Property | Transformer | SSM (Mamba) |
|----------|-------------|-------------|
| Training complexity | O(L²) | O(L) via parallel scan |
| Inference memory | O(L) KV cache, grows | O(1) fixed state |
| Long-range modeling | Explicit attention | Recurrent state |
| In-context recall | Excellent | Weaker |
| Hardware efficiency | Matmul-dominant (ideal) | Custom CUDA needed |

### Mamba (2023) — arXiv:2312.00752
**The key:** Make SSM parameters *input-dependent* (selective). B, C, Δ are functions of the current token — the model can selectively remember or forget based on content.

```
Classical SSM (fixed):  h_t = A·h_{t-1} + B·x_t          (A,B,C same for all inputs)
Mamba (selective):      h_t = Ā(Δ_t)·h_{t-1} + B̄(Δ_t,B_t)·x_t    (functions of x_t)
```

Training uses a custom parallel scan CUDA kernel (FlashMamba). Inference is pure recurrence — O(1) per token.

### Mamba2 (2024) — arXiv:2405.21060
- Proves SSMs and attention are formally equivalent (State Space Duality)
- Multi-head structure → tensor parallelism friendly
- 2-8× faster training than Mamba1

### Hybrid Models (the current equilibrium)

| Model | Ratio | Benefit |
|-------|-------|---------|
| Jamba (AI21) | 7 Mamba : 1 Attention | 256K context, 52B total |
| Zamba (Zyphra) | ~6 Mamba : 1 shared Attention | Tiny KV cache |
| Griffin (Google) | Alternating RG-LRU + local attention | Runs on TPU |
| RecurrentGemma | Griffin architecture | Deployed on HuggingFace |

**Pure SSMs still lag on tasks requiring exact recall.** Hybrids are the practical answer.

---

## Key Repos for Architecture Study

| Repo | URL | What to study |
|------|-----|---------------|
| nanoGPT | github.com/karpathy/nanoGPT | Start here. ~300 lines, entire GPT |
| LitGPT | github.com/Lightning-AI/litgpt | LLaMA/Mistral/Gemma clean implementations |
| HuggingFace transformers | modeling_llama.py | Production modern stack |
| Mamba | github.com/state-spaces/mamba | SSM reference + CUDA kernels |
| MiniMind | github.com/jingyaogong/minimind | nanoGPT for LLaMA-style (RoPE, RMSNorm, SwiGLU) |
| vLLM | github.com/vllm-project/vllm | PagedAttention, continuous batching |
| Flash Attention | github.com/Dao-AILab/flash-attention | Triton/CUDA kernel study |
| Megatron-LM | github.com/NVIDIA/Megatron-LM | Distributed training at scale |
