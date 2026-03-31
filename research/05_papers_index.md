# Must-Read Papers Index
> 49 papers with arXiv links and summaries | Priority ordered

---

## Tier 1 — Read These First (Foundational)

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 1 | Attention Is All You Need | 2017 | [1706.03762](https://arxiv.org/abs/1706.03762) | Introduced the Transformer. Everything builds on this. |
| 2 | Language Models are Few-Shot Learners (GPT-3) | 2020 | [2005.14165](https://arxiv.org/abs/2005.14165) | Proved scale alone unlocks emergent capabilities via in-context learning. |
| 3 | Scaling Laws for Neural Language Models | 2020 | [2001.08361](https://arxiv.org/abs/2001.08361) | Power-law relationships between model size, data, compute. Foundation for all compute-optimal training. |
| 4 | Training Compute-Optimal LLMs (Chinchilla) | 2022 | [2203.15556](https://arxiv.org/abs/2203.15556) | 20 tokens per parameter is optimal. Overturned GPT-3's approach. Why modern models train on far more data. |
| 5 | FlashAttention | 2022 | [2205.14135](https://arxiv.org/abs/2205.14135) | IO-aware attention via tiling. Exact, 2-4× faster, O(L) memory. Now the default everywhere. |
| 6 | LoRA | 2021 | [2106.09685](https://arxiv.org/abs/2106.09685) | Low-rank adaptation. Made fine-tuning accessible. Every PEFT method references this. |
| 7 | QLoRA | 2023 | [2305.14314](https://arxiv.org/abs/2305.14314) | 4-bit NF4 quant + LoRA. Fine-tune 65B on one 48GB GPU. Democratized instruction-tuning. |
| 8 | Direct Preference Optimization (DPO) | 2023 | [2305.18290](https://arxiv.org/abs/2305.18290) | Alignment without reward model or RL. Binary cross-entropy on preference pairs. Now the standard. |

---

## Tier 2 — Architecture (Modern Stack)

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 9 | LLaMA (Meta) | 2023 | [2302.13971](https://arxiv.org/abs/2302.13971) | Open-weights model that established modern stack: RMSNorm, SwiGLU, RoPE. Sparked open-source LLM ecosystem. |
| 10 | LLaMA 2 | 2023 | [2307.09288](https://arxiv.org/abs/2307.09288) | 2T token training, GQA at 70B, extensive RLHF pipeline. Best safety analysis in open literature. |
| 11 | The LLaMA 3 Herd | 2024 | [2407.21783](https://arxiv.org/abs/2407.21783) | 8B-405B, 15T+ tokens, 128K context, GQA throughout. Most comprehensive open model technical report. |
| 12 | Mistral 7B | 2023 | [2310.06825](https://arxiv.org/abs/2310.06825) | Sliding window attention + GQA. 7B beats LLaMA-2 13B. Proved architecture+data quality > raw params. |
| 13 | Mixtral of Experts | 2024 | [2401.04088](https://arxiv.org/abs/2401.04088) | 8 experts, top-2 routing. 47B total, 13B active. LLaMA-2 70B quality at 13B inference cost. |
| 14 | FlashAttention-2 | 2023 | [2307.08691](https://arxiv.org/abs/2307.08691) | 2× FA1 speed via better parallelism and fewer non-matmul ops. ~70% of A100 peak. |
| 15 | FlashAttention-3 | 2024 | [2407.08608](https://arxiv.org/abs/2407.08608) | H100-specific: TMA async, warp specialization, FP8. 75% of H100 peak. |
| 16 | GQA | 2023 | [2305.13245](https://arxiv.org/abs/2305.13245) | Grouped Query Attention: few KV heads shared across Q groups. KV cache reduction with near-MHA quality. |
| 17 | RoFormer / RoPE | 2021 | [2104.09864](https://arxiv.org/abs/2104.09864) | Rotary Position Embedding. Encodes relative position implicitly. Now the default in all modern models. |
| 18 | GLU Variants / SwiGLU | 2020 | [2002.05202](https://arxiv.org/abs/2002.05202) | SwiGLU FFN: ~1 perplexity point better than GELU FFN. Used in LLaMA, PaLM, Gemma, Qwen. |
| 19 | RMSNorm | 2019 | [1910.07467](https://arxiv.org/abs/1910.07467) | Root Mean Square normalization. Faster than LayerNorm, same quality. Standard in all modern LLMs. |
| 20 | ALiBi | 2022 | [2108.12409](https://arxiv.org/abs/2108.12409) | Linear bias to attention logits. Good length extrapolation. Used in BLOOM, MPT. |

---

## Tier 3 — DeepSeek & MoE (Study These)

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 21 | DeepSeek-V2 | 2024 | [2405.04434](https://arxiv.org/abs/2405.04434) | Multi-head Latent Attention (MLA): 93% KV cache reduction. Fine-grained MoE (160 experts). GPT-4 quality at fraction of cost. |
| 22 | DeepSeek-V3 | 2024 | [2412.19437](https://arxiv.org/abs/2412.19437) | 671B/37B active, trained for ~$6M. Multi-token prediction, FP8 training, auxiliary-free load balancing. |
| 23 | DeepSeek-R1 | 2025 | [2501.12948](https://arxiv.org/abs/2501.12948) | Pure RL (GRPO) elicits emergent reasoning without CoT bootstrapping. Matches OpenAI o1 on math/code. |
| 24 | DeepSeekMath / GRPO | 2024 | [2402.03300](https://arxiv.org/abs/2402.03300) | Group Relative Policy Optimization: RL without a value model. Backbone of R1 training. |
| 25 | Switch Transformer | 2021 | [2101.03961](https://arxiv.org/abs/2101.03961) | First trillion-parameter MoE model. Top-1 routing, expert capacity buffer. Foundation for all MoE work. |

---

## Tier 4 — SSMs & Novel Architectures

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 26 | Mamba | 2023 | [2312.00752](https://arxiv.org/abs/2312.00752) | Selective SSM: input-dependent B, C, Δ. Linear inference, O(1) memory per step. First competitive SSM for language. |
| 27 | Mamba-2 / SSD | 2024 | [2405.21060](https://arxiv.org/abs/2405.21060) | Proves SSMs ≡ structured attention. Multi-head Mamba. 2-8× faster training. |
| 28 | RWKV-4 | 2023 | [2305.13048](https://arxiv.org/abs/2305.13048) | Linear attention RNN: parallel training, recurrent inference. Scaled to 14B. |
| 29 | RWKV-6 (Eagle/Finch) | 2024 | [2404.05892](https://arxiv.org/abs/2404.05892) | Matrix-valued states + data-dependent decay. Closes gap with Mamba. |
| 30 | Griffin / Hawk | 2024 | [2402.19427](https://arxiv.org/abs/2402.19427) | RG-LRU gated recurrence + local attention. RecurrentGemma deployed on HuggingFace. |
| 31 | xLSTM | 2024 | [2405.04517](https://arxiv.org/abs/2405.04517) | sLSTM (exponential gates) + mLSTM (matrix memory). LSTM revival. NXAI deployed xLSTM-7B. |
| 32 | Jamba (AI21) | 2024 | [2403.19887](https://arxiv.org/abs/2403.19887) | Transformer + Mamba + MoE hybrid. 52B total, 12B active, 256K context. First large hybrid deployed. |
| 33 | Zamba | 2024 | [2405.16712](https://arxiv.org/abs/2405.16712) | 7B Mamba + shared global attention. 1 attention layer per ~6 Mamba layers. Tiny KV cache. |
| 34 | TTT (Test-Time Training) | 2024 | [2407.04620](https://arxiv.org/abs/2407.04620) | Hidden state is a tiny model updated by gradient step per token. Apple/Stanford. Research only. |
| 35 | Titans | 2025 | [2501.00663](https://arxiv.org/abs/2501.00663) | Neural long-term memory updated by surprise-driven gradients + attention short-term memory. Google. |
| 36 | Hyena Hierarchy | 2023 | [2302.10866](https://arxiv.org/abs/2302.10866) | Long implicit convolutions via FFT. O(n log n). Basis for StripedHyena and Evo genomic model. |
| 37 | MiniMax-Text-01 | 2025 | [2501.08313](https://arxiv.org/abs/2501.08313) | 456B MoE, 1M context via Lightning Attention (linear) + sparse softmax interleave. |

---

## Tier 5 — Inference & KV Management

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 38 | PagedAttention / vLLM | 2023 | [2309.06180](https://arxiv.org/abs/2309.06180) | KV cache in non-contiguous pages (OS-inspired). Continuous batching. 2-24× throughput gain. |
| 39 | Speculative Decoding | 2023 | [2211.17192](https://arxiv.org/abs/2211.17192) | Draft model generates tokens, large model verifies in one pass. 2-3× faster, identical output distribution. |
| 40 | LMCache | 2024 | [2404.18262](https://arxiv.org/abs/2404.18262) | KV cache persistence and reuse across requests. 12× TTFT reduction for repeated system prompts. |
| 41 | SnapKV | 2024 | [2404.14469](https://arxiv.org/abs/2404.14469) | Compress KV before generation using prompt attention patterns. Constant KV size regardless of prompt length. |
| 42 | H2O | 2023 | [2306.14048](https://arxiv.org/abs/2306.14048) | Heavy-hitter oracle: 20% of tokens get 80% of attention. Keep only those. 5× KV reduction. |
| 43 | KVSharer | 2024 | [2410.18517](https://arxiv.org/abs/2410.18517) | Share KV across similar layers. 30% reduction, plug-and-play, no retraining. |
| 44 | MagicPIG | 2024 | [2410.16179](https://arxiv.org/abs/2410.16179) | LSH-based attention approximation over large KV caches. Sub-linear lookup. 2-4× speedup on long context. |
| 45 | Test-Time Compute Scaling | 2024 | [2408.03314](https://arxiv.org/abs/2408.03314) | More inference compute (best-of-N, beam search + process rewards) can match 10× model scaling. |

---

## Tier 6 — Alignment & Agent Systems

| # | Paper | Year | arXiv | Summary |
|---|-------|------|-------|---------|
| 46 | ORPO | 2024 | [2403.07691](https://arxiv.org/abs/2403.07691) | SFT + alignment in one loss, no reference model. 33% less compute than DPO. |
| 47 | SimPO | 2024 | [2405.14734](https://arxiv.org/abs/2405.14734) | Length-normalized reward + margin. No reference model. Outperforms DPO on AlpacaEval. |
| 48 | ReAct | 2022 | [2210.03629](https://arxiv.org/abs/2210.03629) | Interleave reasoning and action steps. Foundation of every tool-using agent. |
| 49 | MemGPT / Letta | 2023 | [2310.08560](https://arxiv.org/abs/2310.08560) | LLM as OS: context = RAM, disk = external storage, LLM issues memory function calls. Reference for long-running agents. |

---

## By Topic Quick Reference

**Start building a transformer:** 1, 6, 9, 14, 16, 17, 18, 19
**Compute budget decisions:** 3, 4
**Fine-tune on consumer GPU:** 6, 7, 8, 46, 47
**Scale up to MoE:** 13, 21, 22, 25
**Replace attention entirely:** 26, 27, 28, 30, 31
**Speed up inference:** 5, 15, 38, 39, 40, 45
**Build agents:** 48, 49
**Cutting edge 2025:** 23, 24, 35, 45
