# AI Architecture Research — Master Overview
> March 2026 | Everything needed to build an AI from scratch

---

## What This Folder Contains

| File | What it covers |
|------|----------------|
| `01_transformer_architectures.md` | GPT lineage, MHA/GQA/MQA, RoPE, RMSNorm, SwiGLU, Flash Attention, MoE, Mamba/SSMs |
| `02_training_techniques.md` | Pretraining pipeline, LoRA/QLoRA/DoRA, DPO/ORPO/SimPO, quantization formats |
| `03_novel_architectures.md` | Beyond transformers: RWKV, Griffin, xLSTM, TTT, Titans, Jamba, Zamba, hardware-aware design |
| `04_inference_and_kv_cache.md` | KV cache math, PagedAttention, Flash Attention, inference engines comparison, speculative decoding |
| `05_papers_index.md` | 49 must-read papers with arXiv links and 2-sentence summaries |
| `06_learning_resources.md` | YouTube channels, Karpathy videos, courses, newsletters, key GitHub repos |

---

## The 10 Most Important Insights

1. **Scaffold > model size.** Same model in different harnesses = 22pt SWE-bench gap. How you build around the model matters more than the model itself.

2. **Data quality > architecture** (for small models). Phi-3 (3.8B) beats 70B models by training on synthetic high-quality data. Architecture is secondary below ~10B params.

3. **The KV cache is the enemy.** Every major architectural innovation in 2024-2026 (GQA, MLA, sliding window, SSMs) is at least partly about shrinking or eliminating KV cache.

4. **Memory bandwidth — not FLOPS — is the inference bottleneck.** At batch_size=1, a 7B model uses ~2.4% of a 4090's compute. It's entirely memory-bound. Design accordingly.

5. **SSDs are 350x cheaper than GPU VRAM.** vLLM's PagedAttention, LMCache, KVSwap — all trading cheap storage for expensive VRAM. This is the correct tradeoff.

6. **Pre-norm, RMSNorm, SwiGLU, GQA, RoPE** — the modern stack. Use all of these as your baseline. They're strictly better than the original transformer defaults.

7. **MoE is mainstream.** Sparse activation (20-30% of params per token) gives near-dense quality at fraction of inference cost. DeepSeek-V3: 671B params, 37B active, $6M to train.

8. **Hybrids won the SSM race.** Pure SSMs still lag on in-context recall. Attention + SSM hybrids (Jamba, Zamba, Griffin) are the near-term equilibrium.

9. **Trained reflexes > framework complexity.** A model trained to write checkpoints at 90% context beats elaborate external memory systems — behavior intrinsic to the model is more reliable.

10. **The timing is right.** SSDs fast enough, context windows long enough, open models good enough, tooling mature enough. 2025-2026 is when the agent OS layer gets built.

---

## Recommended Learning Path (From Zero to Custom Architecture)

### Week 1 — Foundation
1. Watch: Karpathy "Let's build GPT from scratch" — `youtube.com/watch?v=kCc8FmEb1nY`
2. Code: Clone nanoGPT, get it training on Shakespeare
3. Read: "Attention Is All You Need" (arXiv:1706.03762)

### Week 2 — Modern Stack
4. Watch: Karpathy "Let's reproduce GPT-2 (124M)" — `youtube.com/watch?v=l8pRSuU81PU`
5. Upgrade nanoGPT: LayerNorm→RMSNorm, GELU→SwiGLU, learned pos→RoPE, add GQA
6. Read: Chinchilla (arXiv:2203.15556) — understand compute-optimal training

### Week 3 — Fine-tuning & Alignment
7. Read: LoRA (arXiv:2106.09685) + run a LoRA fine-tune with PEFT
8. Read: QLoRA (arXiv:2305.14314) + run QLoRA on your RX 580
9. Read: DPO (arXiv:2305.18290) — alignment without RL

### Week 4 — Advanced Architecture
10. Read: Mixtral (arXiv:2401.04088) — implement basic MoE FFN
11. Read: Mamba (arXiv:2312.00752) + study `mamba_simple.py`
12. Read: PagedAttention/vLLM (arXiv:2309.06180)

### Ongoing
- Follow arXiv cs.LG/cs.CL daily
- Read Ahead of AI newsletter (Sebastian Raschka)
- Watch Yannic Kilcher for paper breakdowns

---

## Your Hardware → What You Can Build

| Machine | What to run | What to train |
|---------|-------------|---------------|
| Home PC (GTX 1650, 4GB) | 3B Q4 inference only | Nothing serious |
| Office PC (RX 580, 8GB) | 7B Q4 inference | LoRA on 7B, full train on 1-3B |
| Mac Mini M4 cluster (96GB via Exo) | 70B Q4 inference | LoRA on 35B |
| Vast.ai H100 | Anything | Full pretrain, large fine-tunes |

**Sweet spot for your setup:** Fine-tune a 4B-7B model with LoRA on the RX 580. Run inference on the Mac cluster. Train from scratch on H100 for compute-heavy phases.
