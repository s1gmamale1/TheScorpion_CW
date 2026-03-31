# Training Techniques — Complete Reference
> Pretraining, fine-tuning, alignment, quantization

---

## 1. Pretraining From Scratch

### Data Pipeline

**Sources (in quality order):**
1. Wikipedia, Books — highest quality, smallest volume
2. GitHub code, arXiv — domain-specific quality
3. Filtered Common Crawl (CCNet, C4, FineWeb-Edu) — massive but needs filtering
4. WebText / Reddit outbound links — quality-filtered web

**Deduplication (critical):**
- MinHash LSH (datasketch library) for near-duplicate detection
- SHA-256 hashing for exact duplicates
- Suffix array method (Lee et al., arXiv:2107.06499)
- Duplicates cause memorization + hurt generalization

**Quality filtering:**
- Perplexity filter: train KenLM on Wikipedia, remove outlier-perplexity docs
- Heuristics: min length, alphabetic ratio, punctuation ratio, no excessive symbols
- Classifier: fastText trained on Wikipedia (positive) vs random web (negative)
- C4 pipeline: remove offensive content, short lines, non-punctuation endings

### Tokenization

| Method | Library | Used by | Vocab size |
|--------|---------|---------|-----------|
| Byte-level BPE | tiktoken | GPT-4, LLaMA-3 | 100K-128K |
| BPE via SentencePiece | sentencepiece | LLaMA-1, Mistral | 32K |
| Unigram LM | sentencepiece | T5, ALBERT | 32K |

**Key repos:**
- `github.com/openai/tiktoken` — fastest, Rust-based
- `github.com/google/sentencepiece`
- `github.com/huggingface/tokenizers`

### Infrastructure

**Distributed training paradigms:**
| Strategy | When | Tool |
|----------|------|------|
| DDP | Model fits on 1 GPU | PyTorch DDP |
| FSDP | Large model, simpler setup | PyTorch FSDP |
| Tensor Parallelism | Very large model, split layers | Megatron-LM |
| 3D Parallelism | Production scale | Megatron-DeepSpeed |

**Mixed precision:**
- **BF16** — preferred for training. Same exponent range as FP32, no overflow risk. Use this.
- FP16 — older, needs dynamic loss scaling
- FP8 — H100 specific, DeepSeek-V3 used for training

**Gradient checkpointing:** Trade 30% compute for 60-70% memory reduction.
- PyTorch: `torch.utils.checkpoint.checkpoint()`
- HuggingFace: `model.gradient_checkpointing_enable()`

### Optimizer Comparison

| Optimizer | Extra memory | Quality | Use case |
|-----------|-------------|---------|----------|
| AdamW | 2× params (m + v states) | Best | Default for everything |
| Lion | 1× params (1 moment) | Similar | Memory constrained |
| Sophia | 2× params + Hessian | ~2× sample efficiency | Still experimental |

**AdamW defaults:** β₁=0.9, β₂=0.95, ε=1e-8, weight_decay=0.1

### Learning Rate Schedule

**Cosine with warmup (standard):**
```
Phase 1: 0 → lr_max over ~1000-4000 steps (warmup)
Phase 2: lr_max → lr_min via cosine curve (rest of training)
lr_min ≈ lr_max / 10
```

**WSD (Warmup-Stable-Decay)** (MiniCPM, arXiv:2404.06395):
```
Phase 1: warmup
Phase 2: long stable phase at constant lr (most of training)
Phase 3: rapid decay (can be done at end, enables checkpoint reuse)
```
- Lets you extend training by adding more stable phase without restarting
- Increasingly adopted

### Loss Curves — Red Flags

| Symptom | Cause | Fix |
|---------|-------|-----|
| Sudden large spike | Bad batch, lr too high | Restart from last checkpoint |
| NaN / inf loss | FP16 overflow, lr too high | Switch to BF16, reduce lr, add grad clipping |
| Plateau too early | lr too low, dead neurons | Increase lr, check batch size |
| Large train-eval gap | Distribution mismatch | Check eval set quality |

**Always use:** `torch.nn.utils.clip_grad_norm_(model.parameters(), max_norm=1.0)`

### Scaling Laws (Must Read)

- **Kaplan et al. (2020):** Loss ~ power law with model size, data, compute. Model size dominated → led to GPT-3's approach. arXiv:2001.08361
- **Chinchilla (2022):** For a fixed compute budget, scale model AND data equally. ~20 tokens per parameter is optimal. This overturned Kaplan. arXiv:2203.15556

---

## 2. Fine-Tuning

### Full Fine-Tune vs LoRA vs QLoRA

| Method | VRAM needed (7B) | Quality | Best for |
|--------|-----------------|---------|----------|
| Full fine-tune | ~80GB (BF16 + AdamW) | Best | A100 clusters |
| LoRA | ~16-24GB | Very good | Most cases |
| QLoRA | ~8-12GB | Good | Consumer GPU (your RX 580) |

### LoRA Deep Dive (arXiv:2106.09685)

**How it works:**
```
Original: y = xW                    (W frozen)
LoRA:     y = xW + x(BA)           (B, A trainable; W frozen)
          where B ∈ ℝ^(d×r), A ∈ ℝ^(r×k), r << d
```

At inference, merge: `W_merged = W + (α/r) × BA` — zero inference overhead.

**Hyperparameter guide:**
| Param | Value | When to change |
|-------|-------|----------------|
| rank r | 16 | r=8 for simple tasks, r=64-128 for complex domain adaptation |
| alpha α | = r or 2r | Keep ratio α/r constant when changing r |
| target_modules | all-linear | q,k,v,o + gate,up,down proj — more = better for instruct tuning |
| dropout | 0.05-0.1 | 0 for very small datasets |

### LoRA Variants

| Variant | Key change | When to use |
|---------|-----------|-------------|
| DoRA (arXiv:2402.09353) | Decomposes into magnitude+direction, adapts both | Better quality at same rank — use instead of LoRA |
| LoRA+ (arXiv:2402.12354) | B matrix gets 16× higher LR than A | +1-2% free improvement |
| rsLoRA (arXiv:2312.03732) | Scaling α/√r instead of α/r | Better at high ranks |
| GaLore (arXiv:2403.03507) | Projects gradients not weights | Full training quality at LoRA memory |

### QLoRA (arXiv:2305.14314)

Quantize base model to **NF4** (4-bit NormalFloat), run LoRA adapters in BF16.

Three key innovations:
1. **NF4 quantization** — optimized for normally distributed weights (LLM weights are)
2. **Double quantization** — quantize the quantization constants, saves 0.37 bits/param
3. **Paged optimizers** — use CPU RAM as overflow for AdamW states

**On your RX 580 (8GB):** QLoRA lets you fine-tune 7B. Without it, 7B won't fit.

**Library:** `github.com/TimDettmers/bitsandbytes`

---

## 3. Alignment

### The Pipeline

```
Pretrained model → SFT (supervised fine-tune on demonstrations) → Alignment
```

Alignment options in order of complexity:

### PPO (original RLHF)
- Train reward model on preference pairs → use PPO to optimize policy toward reward
- Needs 4 models simultaneously in memory (policy, reference, reward, value)
- Complex, unstable, hard to tune
- **Library:** `github.com/huggingface/trl`

### DPO (arXiv:2305.18290) — the new standard
```
Loss = -E[log σ(β × (log π(y_win|x)/π_ref(y_win|x)) - log π(y_lose|x)/π_ref(y_lose|x))]
```
- No reward model, no RL loop
- Just binary cross-entropy on preference pairs
- Needs SFT model as reference
- β=0.1-0.5 controls deviation from reference

### ORPO (arXiv:2403.07691) — even simpler
- Combines SFT + preference alignment in one loss
- No reference model needed at all
- SFT loss acts as implicit regularizer
- ~33% less compute than DPO

### SimPO (arXiv:2405.14734) — often best on benchmarks
- Uses length-normalized log-prob as reward
- No reference model
- Adds target reward margin γ
- Outperforms DPO on AlpacaEval, ArenaHard

### Comparison
| Method | Reference model | Reward model | SFT stage | Complexity |
|--------|----------------|--------------|-----------|-----------|
| PPO | Yes | Yes | Yes | Very high |
| DPO | Yes | No | Yes | Medium |
| ORPO | No | No | No (combined) | Low |
| SimPO | No | No | Yes | Low |

---

## 4. Quantization

### Format Comparison

**GGUF (llama.cpp):** The standard for local deployment. Runs on CPU+GPU.

| Quantization | Bits/weight | Size vs FP16 | Quality loss |
|-------------|-------------|-------------|-------------|
| Q8_0 | 8.5 | 50% | < 0.1% |
| Q5_K_M | 5.7 | 35% | ~0.5-1% |
| Q4_K_M | 4.8 | 27% | ~1-2% — **default recommendation** |
| Q4_0 | 4.5 | 25% | ~2-3% |
| Q3_K_M | 3.5 | 20% | ~3-5% |
| Q2_K | 2.6 | 14% | ~8-15% |

K-quants (Q4_K, Q5_K): use higher precision for attention layers, lower for FFN. Always better than uniform quantization.

**AWQ (arXiv:2306.00978):** Activation-aware. Identifies and protects the ~1% of weights that matter most. Fast inference with optimized CUDA kernels. `github.com/casper-hansen/AutoAWQ`

**GPTQ (arXiv:2210.17323):** Second-order quantization. Slower to quantize, fast inference with exllama kernels. `github.com/PanQiWei/AutoGPTQ`

**EXL2 (exllamav2):** Variable bitrate per layer — some at 6-bit, some at 3-bit, tuned by calibration. Fastest CUDA inference on consumer GPUs. `github.com/turboderp/exllamav2`

**Rule of thumb:** Use Q4_K_M for most cases. Step up to Q5_K_M if you notice quality issues. Use EXL2 if maximizing speed on NVIDIA.

---

## 5. Key Training Repos

| Repo | URL | What it does |
|------|-----|-------------|
| PEFT | github.com/huggingface/peft | LoRA, DoRA, QLoRA, rsLoRA — plug into any HF model |
| TRL | github.com/huggingface/trl | SFT, DPO, ORPO, PPO — full alignment pipeline |
| axolotl | github.com/axolotl-ai-cloud/axolotl | Config-driven fine-tuning framework |
| unsloth | github.com/unslothai/unsloth | 2-5× faster QLoRA, less memory |
| Megatron-LM | github.com/NVIDIA/Megatron-LM | Large-scale distributed pretraining |
| DeepSpeed | github.com/microsoft/DeepSpeed | ZeRO optimizer stages, memory efficiency |
| LLMs-from-scratch | github.com/rasbt/LLMs-from-scratch | Raschka's book code, pretrain+finetune walkthrough |

---

## 6. Your Practical Setup (RX 580 Fine-Tuning)

```bash
# Install
pip install peft trl bitsandbytes transformers accelerate

# QLoRA fine-tune on 7B
from transformers import AutoModelForCausalLM, BitsAndBytesConfig
from peft import LoraConfig, get_peft_model

bnb_config = BitsAndBytesConfig(
    load_in_4bit=True,
    bnb_4bit_quant_type="nf4",
    bnb_4bit_use_double_quant=True,
    bnb_4bit_compute_dtype=torch.bfloat16
)

model = AutoModelForCausalLM.from_pretrained(
    "Qwen/Qwen2.5-7B",
    quantization_config=bnb_config,
    device_map="auto"
)

lora_config = LoraConfig(
    r=16,
    lora_alpha=32,
    target_modules="all-linear",
    lora_dropout=0.05,
    use_dora=True,       # DoRA instead of LoRA
    use_rslora=True,     # rsLoRA scaling
)
model = get_peft_model(model, lora_config)
```

Note: ROCm support for bitsandbytes on AMD is improving but can be spotty. If issues arise, use Ollama + llama.cpp for inference, and run fine-tuning on H100 via Vast.ai.
