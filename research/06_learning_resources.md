# Learning Resources
> YouTube, courses, newsletters, GitHub repos — everything to watch and read

---

## YouTube — Must Watch (In Order)

### Andrej Karpathy — The Gold Standard
Channel: youtube.com/@AndrejKarpathy

| Video | URL | What it teaches | Hours |
|-------|-----|----------------|-------|
| **Let's build GPT from scratch** | youtube.com/watch?v=kCc8FmEb1nY | Entire GPT in ~200 lines PyTorch. Attention, transformer blocks, training loop. **Start here.** | 2h |
| **Let's reproduce GPT-2 (124M)** | youtube.com/watch?v=l8pRSuU81PU | Production LLM training end-to-end. FlashAttention, torch.compile, distributed training, FineWeb dataset, loss curves. **The most valuable single video.** | 4h |
| **Let's build the GPT Tokenizer** | youtube.com/watch?v=zduSFxRajkE | BPE tokenization from scratch. Why tokenizer decisions affect model behavior. | 2h |
| **Intro to Large Language Models** | youtube.com/watch?v=zjkBMFhNj_g | LLMs, fine-tuning, RLHF, scaling — high-level but dense. | 1h |
| **State of GPT** (Microsoft Build 2023) | youtube.com/watch?v=bZQun8Y4L2A | Full pipeline: pretraining → SFT → RLHF → RLAIF. Practical and dense. | 1h |

**Neural Networks: Zero to Hero playlist:** youtube.com/playlist?list=PLAqhIrjkxbuWI23v9cThsA9GvCAUhRvKZ
- Starts from micrograd (backprop from scratch) → makemore → GPT
- Best curriculum if you're building from actual zero

---

### 3Blue1Brown — Visual Intuition
Channel: youtube.com/@3blue1brown

| Video | What it teaches |
|-------|----------------|
| "But what is a neural network?" | Visual intro to neural nets |
| "Gradient descent, how neural networks learn" | Backprop intuition |
| "But what is a GPT? Visual intro to transformers" (2023) | Best visual explanation of transformers |
| "Attention in transformers, step-by-step" (2024) | Attention mechanism visual walkthrough |
| "Visualizing Attention, a Transformer's Heart" | Self-attention intuition |

Search these titles on his channel — the intuition he builds is irreplaceable.

---

### Umar Jamil — Implementation Walkthroughs
Channel: youtube.com/@umarjamilai

| Video | What it teaches |
|-------|----------------|
| "Coding a Transformer from scratch on PyTorch" | Full transformer implementation |
| "Coding LLaMA 2 from scratch" | GQA, RoPE, RMSNorm, SwiGLU — the modern stack |
| "LoRA explained and implemented from scratch" | LoRA math + code |
| "Mistral / Mixtral explained" | Sliding window attention + MoE |

Best channel for going from "I understand the concept" to "I implemented it."

---

### Yannic Kilcher — Paper Walkthroughs
Channel: youtube.com/@YannicKilcher

Best for understanding papers deeply before implementing. Key videos:
- Attention is All You Need explained
- GPT-3 paper explained
- Flash Attention explained
- LoRA explained
- DPO / preference optimization
- Any DeepSeek paper

---

### Other Channels

| Channel | Handle | Best for |
|---------|--------|----------|
| Sebastian Raschka | @SebastianRaschka | LLMs from scratch, fine-tuning practical guides |
| StatQuest (Josh Starmer) | @statquest | Attention and transformers explained visually |
| The AI Epiphany (Gordic) | @TheAIEpiphany | Architecture paper deep dives |
| Trelis Research | @TrelisResearch | QLoRA, DPO, quantization workflows — code heavy |
| Two Minute Papers | @TwoMinutePapers | Quick summaries of new papers |

---

## Courses

| Course | URL | Cost | What it teaches |
|--------|-----|------|----------------|
| **Fast.ai Practical Deep Learning** | course.fast.ai | Free | Bottom-up DL, includes GPU training |
| **HuggingFace NLP Course** | huggingface.co/learn/nlp-course | Free | Transformers end-to-end |
| **DeepLearning.ai: Generative AI with LLMs** | deeplearning.ai | Paid | LLM pipeline overview |
| **Sebastian Raschka: LLM from Scratch** | magazine.sebastianraschka.com | Free/paid | Code companion to his book |

---

## Books / Long-Form

| Resource | URL | What it covers |
|----------|-----|----------------|
| **Build a Large Language Model (From Scratch)** — Sebastian Raschka | github.com/rasbt/LLMs-from-scratch | Full pretraining + fine-tuning from scratch with code |
| **The Annotated Transformer** — Sasha Rush | nlp.seas.harvard.edu/annotated-transformer | Original transformer paper with running PyTorch annotations |
| **Dive into Deep Learning** | d2l.ai | Full DL textbook, interactive, free |

---

## Newsletters (Read Weekly)

| Newsletter | URL | What it covers |
|-----------|-----|----------------|
| **Ahead of AI** (Sebastian Raschka) | magazine.sebastianraschka.com | Best deep-dive architecture analysis. Read every issue. |
| **Interconnects** (Nathan Lambert) | interconnects.ai | Open model ecosystem, RLHF, alignment |
| **The Gradient** | thegradient.pub | Academic-quality AI journalism |
| **The Batch** (DeepLearning.ai) | deeplearning.ai/the-batch | Weekly AI news roundup |

---

## Key GitHub Repos (Study These)

### For Understanding Architecture
| Repo | URL | Study focus |
|------|-----|-------------|
| **nanoGPT** | github.com/karpathy/nanoGPT | Start here. Entire GPT in ~300 lines. |
| **llm.c** | github.com/karpathy/llm.c | GPT-2 in C/CUDA. Teaches you the metal. |
| **LitGPT** | github.com/Lightning-AI/litgpt | LLaMA/Mistral/Gemma clean implementations |
| **MiniMind** | github.com/jingyaogong/minimind | nanoGPT for LLaMA-style (RoPE, SwiGLU, RMSNorm) |
| **HuggingFace transformers** | github.com/huggingface/transformers | modeling_llama.py is the production reference |
| **Mamba** | github.com/state-spaces/mamba | SSM reference + CUDA kernels |
| **RWKV-LM** | github.com/BlinkDL/RWKV-LM | Linear attention RNN |

### For Training
| Repo | URL | What it does |
|------|-----|-------------|
| **PEFT** | github.com/huggingface/peft | LoRA, DoRA, QLoRA — plug into any HF model |
| **TRL** | github.com/huggingface/trl | SFT, DPO, ORPO, PPO — full alignment |
| **axolotl** | github.com/axolotl-ai-cloud/axolotl | Config-driven fine-tune framework |
| **unsloth** | github.com/unslothai/unsloth | 2-5× faster QLoRA, less memory |
| **Megatron-LM** | github.com/NVIDIA/Megatron-LM | Large-scale distributed pretraining |
| **DeepSpeed** | github.com/microsoft/DeepSpeed | ZeRO optimizer, memory efficiency |

### For Inference
| Repo | URL | What it does |
|------|-----|-------------|
| **vLLM** | github.com/vllm-project/vllm | Production serving, PagedAttention |
| **llama.cpp** | github.com/ggerganov/llama.cpp | CPU+GPU inference, GGUF format |
| **ExLlamaV2** | github.com/turboderp/exllamav2 | Fastest NVIDIA consumer inference |
| **MLX** | github.com/ml-explore/mlx | Apple Silicon ML framework |
| **MLX-LM** | github.com/ml-explore/mlx-examples | LLaMA/Mistral/etc in MLX |
| **Exo** | github.com/exo-explore/exo | Distributed inference across devices |
| **Flash Attention** | github.com/Dao-AILab/flash-attention | The attention kernel |
| **LMCache** | github.com/LMCache/LMCache | KV cache persistence and reuse |

### For Evaluation
| Repo | URL | What it does |
|------|-----|-------------|
| **lm-evaluation-harness** | github.com/EleutherAI/lm-evaluation-harness | Gold standard for downstream evals |
| **RULER** | github.com/hsiehjackson/RULER | Long context benchmarks |

### For Quantization
| Repo | URL | Format |
|------|-----|--------|
| **bitsandbytes** | github.com/TimDettmers/bitsandbytes | NF4/INT8 for QLoRA |
| **AutoAWQ** | github.com/casper-hansen/AutoAWQ | AWQ quantization |
| **AutoGPTQ** | github.com/PanQiWei/AutoGPTQ | GPTQ quantization |

---

## yt-dlp — Download Videos for Offline Study

Already installed. Usage:
```bash
# Download a YouTube video
yt-dlp "https://youtube.com/watch?v=kCc8FmEb1nY" -o "karpathy_gpt.%(ext)s"

# Download entire playlist
yt-dlp "https://youtube.com/playlist?list=PLAqhIrjkxbuWI23v9cThsA9GvCAUhRvKZ" \
    -o "%(playlist_index)s_%(title)s.%(ext)s"

# Download audio only (for background listening)
yt-dlp -x --audio-format mp3 "URL"

# Download best quality mp4
yt-dlp -f "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]" "URL"
```

---

## arXiv Monitoring

Follow these for new papers daily:
- `arxiv.org/list/cs.LG/recent` — machine learning
- `arxiv.org/list/cs.CL/recent` — computation and language (NLP/LLM)
- `huggingface.co/papers` — curated daily paper highlights
- `paperswithcode.com` — papers + code + benchmarks

**RSS feeds** work well for these — add to any RSS reader.
