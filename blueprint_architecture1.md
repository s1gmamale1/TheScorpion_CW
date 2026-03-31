# Architecture 1 — Full Build Blueprint
## Selective KV Context Management
> Complete step-by-step plan. Every tool, method, and decision defined.
> March 27, 2026

---

## What We're Building

A self-managing agent session system. The model:
- Loads stable reference files fresh every session (~700 tokens overhead)
- Auto-saves active task state every 10 turns
- Writes a perfect checkpoint at 90% context then resets clean
- Resumes from the exact point it left off next session
- Archives completed tasks as human-readable MD, deletes the KV

**No context accumulation. No degradation. Infinite sessions.**

---

## System Architecture Diagram

```
SESSION START
│
├─ Load stable refs (MD files → ~700 tokens)
│   project_goal.md / role_instructions.md / architecture.md
│
├─ Check registry.json
│   ├─ Active task exists?
│   │   YES → Load task_state (v0: JSON history / v1: binary KV)
│   │          Inject → resume from exact point
│   └─ No active task → fresh start
│
│   WORK LOOP
│   ├─ Every turn: track context usage
│   ├─ Every 10 turns: autosave task_state (rolling)
│   ├─ At 70%: warn model → "approaching limit"
│   ├─ At 90%: trigger CHECKPOINT REFLEX
│   │           model writes CHECKPOINT block
│   │           orchestrator saves task_state
│   │           orchestrator sends RESET signal
│   │           session ends cleanly
│   └─ On TASK_COMPLETE signal:
│       generate MD summary from task context
│       write to completed/{task_id}.md
│       delete task_state
│       update registry.json ✅
│       reset clean
│
SESSION END
```

---

## File Structure (What Gets Created)

```
your_project/
├── orchestrator.py              ← the brain (~200 lines)
├── registry.json                ← task tracker (the "page table")
│
├── stable/                      ← reloaded fresh every session
│   ├── project_goal.md          ← ~100 tokens
│   ├── role_instructions.md     ← ~200 tokens
│   └── architecture.md          ← ~200 tokens
│
├── active/
│   ├── task_state.json          ← v0: conversation history (JSON)
│   └── task_state.cache         ← v1: binary KV cache (llama.cpp)
│
└── completed/
    ├── auth_module.md           ← archived completed tasks
    ├── user_endpoints.md
    └── ...
```

**registry.json structure:**
```json
{
  "current_task": {
    "id": "payment_integration",
    "status": "in_progress",
    "state_file": "active/task_state.json",
    "started_at": "2026-03-27T14:00",
    "last_saved": "2026-03-27T16:45",
    "current_step": "webhook handler line 47",
    "context_percent": 73
  },
  "completed": [
    {
      "id": "auth_module",
      "md_file": "completed/auth_module.md",
      "completed_at": "2026-03-27T13:00",
      "summary": "JWT auth with refresh rotation"
    }
  ]
}
```

---

## Phase 0 — Environment Setup
**Time: Day 1 (2-3 hours)**
**Goal: Model running, tools installed, baseline working**

### Step 0.1 — Verify Model
```bash
# Confirm model is loaded and using GPU (not CPU fallback)
ollama ps
# Should show: huihui_ai/qwen3.5-abliterated:9b-Claude  [GPU]

ollama show huihui_ai/qwen3.5-abliterated:9b-Claude
# Check: quantization level (want Q4_K_M), context size

# Quick test
ollama run huihui_ai/qwen3.5-abliterated:9b-Claude "Hello, what model are you?"
```

### Step 0.2 — Python Environment
```bash
pip install requests pydantic tiktoken rich
# requests: Ollama API calls
# pydantic: data validation for registry/state
# tiktoken: token counting (approximate for Qwen — use cl100k_base)
# rich: nice console output for the orchestrator
```

### Step 0.3 — Confirm Ollama API
```python
import requests

# Test Ollama API
response = requests.post("http://localhost:11434/api/chat", json={
    "model": "huihui_ai/qwen3.5-abliterated:9b-Claude",
    "messages": [{"role": "user", "content": "ping"}],
    "stream": False
})
print(response.json()["message"]["content"])
# Should respond instantly
```

### Step 0.4 — Measure Baseline
```python
# Count tokens on stable refs to know our overhead
# Target: under 700 tokens for all stable files combined
import tiktoken
enc = tiktoken.get_encoding("cl100k_base")  # close enough for Qwen

for f in ["project_goal.md", "role_instructions.md", "architecture.md"]:
    text = open(f"stable/{f}").read()
    print(f"{f}: {len(enc.encode(text))} tokens")
```

---

## Phase 1 — Orchestrator v0 (Context Simulation)
**Time: Days 2-4 (1-2 days of coding)**
**Goal: Full working system WITHOUT fine-tuning. Proves concept end-to-end.**
**Method: Save conversation history as JSON (not binary KV). Re-inject on resume.**

This is v0. It re-prefills the conversation history on resume (~10-15 seconds).
That's acceptable for development. v1 (Phase 4) replaces this with true KV binary.

### Step 1.1 — Context Tracker
```python
# context_tracker.py
import tiktoken

class ContextTracker:
    def __init__(self, model_context_limit=8192):
        self.limit = model_context_limit
        self.enc = tiktoken.get_encoding("cl100k_base")
        self.messages = []

    def add_message(self, role: str, content: str):
        self.messages.append({"role": role, "content": content})

    def count_tokens(self) -> int:
        total = sum(len(self.enc.encode(m["content"])) for m in self.messages)
        return total

    def percent_used(self) -> float:
        return (self.count_tokens() / self.limit) * 100

    def health(self) -> dict:
        pct = self.percent_used()
        return {
            "context_used": self.count_tokens(),
            "context_limit": self.limit,
            "context_percent": round(pct, 1),
            "health": "🟢 healthy" if pct < 70 else "🟡 warning" if pct < 85 else "🔴 critical",
            "recommendation": "continue" if pct < 70 else "prepare checkpoint" if pct < 85 else "CHECKPOINT NOW"
        }
```

### Step 1.2 — State Manager
```python
# state_manager.py
import json
from pathlib import Path
from datetime import datetime

class StateManager:
    def __init__(self, base_path: str = "."):
        self.base = Path(base_path)
        self.registry_path = self.base / "registry.json"
        self.active_path = self.base / "active" / "task_state.json"
        self.completed_path = self.base / "completed"
        self.completed_path.mkdir(exist_ok=True)
        (self.base / "active").mkdir(exist_ok=True)

    def load_registry(self) -> dict:
        if self.registry_path.exists():
            return json.loads(self.registry_path.read_text())
        return {"current_task": None, "completed": []}

    def save_registry(self, registry: dict):
        self.registry_path.write_text(json.dumps(registry, indent=2))

    def save_task_state(self, task_id: str, messages: list, step: str, context_pct: float):
        state = {
            "task_id": task_id,
            "messages": messages,
            "current_step": step,
            "context_percent": context_pct,
            "saved_at": datetime.now().isoformat()
        }
        self.active_path.write_text(json.dumps(state, indent=2))

    def load_task_state(self) -> dict | None:
        if self.active_path.exists():
            return json.loads(self.active_path.read_text())
        return None

    def archive_task(self, task_id: str, md_summary: str):
        out = self.completed_path / f"{task_id}.md"
        out.write_text(md_summary)
        self.active_path.unlink(missing_ok=True)  # delete active state

    def clear_active(self):
        self.active_path.unlink(missing_ok=True)
```

### Step 1.3 — Main Orchestrator
```python
# orchestrator.py
import requests
import json
from pathlib import Path
from context_tracker import ContextTracker
from state_manager import StateManager

OLLAMA_URL = "http://localhost:11434/api/chat"
MODEL = "huihui_ai/qwen3.5-abliterated:9b-Claude"
CONTEXT_LIMIT = 8192
AUTOSAVE_EVERY = 10  # turns
WARN_AT = 70         # percent
CHECKPOINT_AT = 90   # percent

STABLE_REFS = [
    "stable/project_goal.md",
    "stable/role_instructions.md",
    "stable/architecture.md"
]

# Checkpoint format the model will be trained to output
CHECKPOINT_TEMPLATE = """
## CHECKPOINT
Status: MID-TASK INTERRUPT
What I was doing: [exact file, line, action]
What I completed: [list with ✅]
What I was about to do: [ordered list]
Exact mid-task state: [partial code/work]
Context when interrupted: [key decisions, constraints]
Resume instruction: [exactly what to do next]
"""

SYSTEM_PROMPT_BASE = """
You are a focused task execution agent. You have access to the following context:
{stable_refs}

Context stats: {context_stats}

Rules:
- If context health is "prepare checkpoint": start wrapping up current step cleanly
- If context health is "CHECKPOINT NOW": immediately write a CHECKPOINT block and output RESET
- If you receive a RESUME signal: read the CHECKPOINT and continue from "Resume instruction"
- If task is done: write a TASK_COMPLETE block with a clean MD summary
"""

class Orchestrator:
    def __init__(self, project_path: str = "."):
        self.tracker = ContextTracker(CONTEXT_LIMIT)
        self.state = StateManager(project_path)
        self.turn_count = 0
        self.task_id = None

    def load_stable_refs(self) -> str:
        parts = []
        for ref_path in STABLE_REFS:
            p = Path(ref_path)
            if p.exists():
                parts.append(f"--- {p.name} ---\n{p.read_text()}")
        return "\n\n".join(parts)

    def chat(self, user_message: str) -> str:
        self.tracker.add_message("user", user_message)
        health = self.tracker.health()

        system = SYSTEM_PROMPT_BASE.format(
            stable_refs=self.load_stable_refs(),
            context_stats=json.dumps(health)
        )

        response = requests.post(OLLAMA_URL, json={
            "model": MODEL,
            "messages": [
                {"role": "system", "content": system},
                *self.tracker.messages
            ],
            "stream": False
        })

        reply = response.json()["message"]["content"]
        self.tracker.add_message("assistant", reply)
        self.turn_count += 1

        # Print context health
        print(f"[{health['health']} | {health['context_percent']}% | Turn {self.turn_count}]")

        # Autosave every N turns
        if self.turn_count % AUTOSAVE_EVERY == 0 and self.task_id:
            self.state.save_task_state(
                self.task_id,
                self.tracker.messages,
                "turn " + str(self.turn_count),
                health["context_percent"]
            )
            print(f"[AUTOSAVED at turn {self.turn_count}]")

        # Handle signals
        if "RESET" in reply:
            self._handle_reset()
        elif "TASK_COMPLETE" in reply:
            self._handle_completion(reply)

        return reply

    def _handle_reset(self):
        print("[RESET SIGNAL — saving state and ending session]")
        if self.task_id:
            health = self.tracker.health()
            self.state.save_task_state(
                self.task_id,
                self.tracker.messages,
                "checkpoint",
                health["context_percent"]
            )
        exit(0)  # clean exit — orchestrator restarts fresh next time

    def _handle_completion(self, reply: str):
        print("[TASK_COMPLETE — archiving and cleaning up]")
        self.state.archive_task(self.task_id, reply)
        reg = self.state.load_registry()
        reg["completed"].append({"id": self.task_id, "reply_preview": reply[:200]})
        reg["current_task"] = None
        self.state.save_registry(reg)
        self.task_id = None
        self.tracker.messages = []

    def resume_or_start(self, task_id: str, initial_message: str = None):
        self.task_id = task_id
        saved = self.state.load_task_state()

        if saved and saved["task_id"] == task_id:
            print(f"[RESUMING task '{task_id}' from {saved['saved_at']}]")
            print(f"[Last step: {saved['current_step']}]")
            self.tracker.messages = saved["messages"]
            # Send RESUME signal
            return self.chat("RESUME — continue from checkpoint")
        else:
            print(f"[STARTING new task '{task_id}']")
            reg = self.state.load_registry()
            reg["current_task"] = {"id": task_id, "status": "in_progress"}
            self.state.save_registry(reg)
            return self.chat(initial_message or f"Begin task: {task_id}")

# Usage
if __name__ == "__main__":
    orc = Orchestrator(project_path=".")
    orc.resume_or_start("payment_integration", "Build the Stripe webhook handler")

    while True:
        user_input = input("You: ")
        response = orc.chat(user_input)
        print(f"Agent: {response}\n")
```

### Step 1.4 — Test v0
```bash
# Run a test session — talk to the agent, let it work
python orchestrator.py

# Simulate context pressure — feed it a long task
# Watch it hit 90%, write checkpoint, send RESET
# Kill the process
# Restart — verify it resumes correctly
```

**Success criteria for Phase 1:**
- ✅ Session starts with stable refs loaded
- ✅ Context % tracked and displayed each turn
- ✅ Autosave fires every 10 turns
- ✅ At 90%: model writes CHECKPOINT, orchestrator saves, exits
- ✅ On restart: previous task resumes from checkpoint
- ✅ On TASK_COMPLETE: archives to MD, clears state

---

## Phase 2 — Training Data Generation
**Time: Days 4-6**
**Goal: ~2000 high-quality training examples for the 4 behaviors**
**Method: Self-play — use the 9B model to generate its own training data**

### The 4 Datasets

**Dataset A — Checkpoint Writing** (~500 examples)
```
Input:  task context at 85-90% window + context stats JSON
Output: perfect CHECKPOINT block in exact format
```

**Dataset B — Resume Behavior** (~500 examples)
```
Input:  system prompt + CHECKPOINT block + "RESUME" signal
Output: continues task correctly from "Resume instruction" field
```

**Dataset C — Task Completion Summary** (~300 examples)
```
Input:  completed task conversation
Output: clean MD summary with title, what was built, key decisions
```

**Dataset D — Status JSON** (~200 examples)
```
Input:  task state at various stages
Output: structured JSON status {"task_id", "status", "current_step", "progress_pct"}
```

### Step 2.1 — Generator Script
```python
# generate_training_data.py
import requests
import json
import random
from pathlib import Path

OLLAMA_URL = "http://localhost:11434/api/generate"
MODEL = "huihui_ai/qwen3.5-abliterated:9b-Claude"

# Example task contexts to vary the training data
TASK_TYPES = [
    "implementing JWT authentication endpoint",
    "building a database migration script",
    "writing unit tests for the payment module",
    "refactoring the user service class",
    "debugging a race condition in the worker queue",
    "adding pagination to the search API",
    "creating a CSV export feature",
    "optimizing a slow database query"
]

DATASET_A_PROMPT = """You are a training data generator.

Generate a realistic example of an AI coding agent that is in the middle of a task and needs to write a checkpoint because context is at 88%.

Task: {task}
Turns completed: {turns}

First write 3-5 turns of realistic work (code, reasoning, tool calls), then write the checkpoint.

The checkpoint MUST follow this exact format:
## CHECKPOINT
Status: MID-TASK INTERRUPT
What I was doing: [exact file, line, action]
What I completed: [list with ✅]
What I was about to do: [ordered list]
Exact mid-task state: [partial code/work if applicable]
Context when interrupted: [key decisions, constraints]
Resume instruction: [exactly what to do next — specific enough to resume cold]
RESET

Generate the full example now:"""

def generate_example(prompt_template: str, **kwargs) -> str:
    prompt = prompt_template.format(**kwargs)
    response = requests.post(OLLAMA_URL, json={
        "model": MODEL,
        "prompt": prompt,
        "stream": False,
        "options": {"temperature": 0.8, "num_predict": 1000}
    })
    return response.json()["response"]

def generate_dataset_a(n: int = 500) -> list:
    examples = []
    for i in range(n):
        task = random.choice(TASK_TYPES)
        turns = random.randint(15, 35)
        output = generate_example(DATASET_A_PROMPT, task=task, turns=turns)

        # Parse into input/output split at CHECKPOINT
        if "## CHECKPOINT" in output:
            split = output.split("## CHECKPOINT")
            examples.append({
                "instruction": f"You are working on: {task}. Context is at 88%. Write a checkpoint now.",
                "input": split[0].strip(),
                "output": "## CHECKPOINT" + split[1].strip()
            })
        print(f"Dataset A: {i+1}/{n}", end="\r")
    return examples

def save_dataset(examples: list, name: str):
    Path("training_data").mkdir(exist_ok=True)
    out = Path(f"training_data/{name}.jsonl")
    with open(out, "w") as f:
        for ex in examples:
            f.write(json.dumps(ex) + "\n")
    print(f"Saved {len(examples)} examples to {out}")

# Run generation
if __name__ == "__main__":
    print("Generating Dataset A (checkpoint writing)...")
    dataset_a = generate_dataset_a(500)
    save_dataset(dataset_a, "dataset_a_checkpoint")

    # Repeat for B, C, D with their own prompt templates
    print("Done. Check training_data/ folder.")
```

### Step 2.2 — Data Quality Check
```python
# validate_training_data.py
# Check every example has:
# - Non-empty input and output
# - Correct checkpoint format (## CHECKPOINT, all 6 fields present)
# - Output ends with RESET signal
# - No truncated examples
# Remove bad examples, log stats
```

### Step 2.3 — Format for Training
```python
# Convert to Alpaca format (what Unsloth expects):
{
  "instruction": "...",
  "input": "...",       # conversation context
  "output": "..."       # checkpoint / resume / summary
}
```

---

## Phase 3 — LoRA Fine-tune
**Time: Days 6-8 (1-2 days training on RX 580)**
**Goal: Bake checkpoint/resume/archive behavior into the model as a reflex**
**Tool: Unsloth (fastest, ROCm support)**
**Method: QLoRA — 4-bit base + LoRA adapters (fits in 8GB VRAM)**

### Step 3.1 — Setup Unsloth on ROCm (RX 580)
```bash
# Install ROCm version of PyTorch first
pip install torch torchvision torchaudio \
  --index-url https://download.pytorch.org/whl/rocm6.2

# Install Unsloth
pip install unsloth

# Verify GPU is detected
python -c "import torch; print(torch.cuda.get_device_name(0))"
# Should print: AMD Radeon RX 580 (or similar)
```

### Step 3.2 — Training Script
```python
# train_lora.py
from unsloth import FastLanguageModel
import torch
from datasets import load_dataset
from trl import SFTTrainer
from transformers import TrainingArguments

# Load base model in 4-bit (QLoRA)
model, tokenizer = FastLanguageModel.from_pretrained(
    model_name="Qwen/Qwen2.5-9B-Instruct",  # HF base weights
    max_seq_length=8192,
    dtype=None,           # auto-detect (BF16 on ROCm)
    load_in_4bit=True,    # QLoRA — fits in 8GB
)

# Add LoRA adapters
model = FastLanguageModel.get_peft_model(
    model,
    r=16,                 # LoRA rank — higher = more capacity, more VRAM
    target_modules=[      # which layers to adapt
        "q_proj", "k_proj", "v_proj", "o_proj",
        "gate_proj", "up_proj", "down_proj"
    ],
    lora_alpha=16,
    lora_dropout=0,
    bias="none",
    use_gradient_checkpointing="unsloth",  # 30% less VRAM
    random_state=42,
)

# Load training data
dataset = load_dataset("json", data_files={
    "train": [
        "training_data/dataset_a_checkpoint.jsonl",
        "training_data/dataset_b_resume.jsonl",
        "training_data/dataset_c_summary.jsonl",
        "training_data/dataset_d_status.jsonl",
    ]
})["train"]

# Alpaca prompt format
alpaca_prompt = """Below is an instruction that describes a task. Write a response.

### Instruction:
{}

### Input:
{}

### Response:
{}"""

def formatting_func(examples):
    texts = []
    for inst, inp, out in zip(
        examples["instruction"], examples["input"], examples["output"]
    ):
        texts.append(alpaca_prompt.format(inst, inp, out) + tokenizer.eos_token)
    return {"text": texts}

dataset = dataset.map(formatting_func, batched=True)

# Training config
trainer = SFTTrainer(
    model=model,
    tokenizer=tokenizer,
    train_dataset=dataset,
    dataset_text_field="text",
    max_seq_length=8192,
    args=TrainingArguments(
        per_device_train_batch_size=1,
        gradient_accumulation_steps=8,     # effective batch = 8
        warmup_steps=50,
        num_train_epochs=3,
        learning_rate=2e-4,
        fp16=False,
        bf16=True,                          # BF16 on ROCm
        logging_steps=10,
        output_dir="lora_output",
        save_steps=100,
        save_total_limit=3,
        lr_scheduler_type="cosine",
    ),
)

trainer.train()
model.save_pretrained("lora_output/final")
tokenizer.save_pretrained("lora_output/final")
print("Training complete.")
```

### Step 3.3 — Export to GGUF and Load in Ollama
```python
# Export merged model to GGUF
model.save_pretrained_gguf(
    "gguf_output",
    tokenizer,
    quantization_method="q4_k_m"   # same quant as base model
)
```

```bash
# Create Ollama model from GGUF
cat > Modelfile << 'EOF'
FROM ./gguf_output/model-q4_k_m.gguf

PARAMETER temperature 0.7
PARAMETER num_ctx 8192
SYSTEM "You are a focused task execution agent with self-aware context management."
EOF

ollama create gyattmaxxer-worker:v1 -f Modelfile

# Test the fine-tuned model
ollama run gyattmaxxer-worker:v1 "You are at 90% context. Write a checkpoint for your current task."
```

### Step 3.4 — Evaluate the Fine-tune
```python
# eval_checkpoint_behavior.py
# Test 50 examples from a held-out set
# Metrics:
# - Does it write a CHECKPOINT block? (binary)
# - Does it include all 6 required fields? (0-6 score)
# - Does it end with RESET? (binary)
# - Does resume correctly continue from checkpoint? (human eval)
# Target: >90% on all binary metrics
```

---

## Phase 4 — True KV Binary Cache
**Time: Days 8-10**
**Goal: Replace JSON conversation history with actual binary KV cache files**
**Tool: llama.cpp direct (what Ollama uses under the hood)**
**Why: No re-prefill cost on resume — instant pickup from exact point**

### Step 4.1 — Get llama.cpp Binary
```bash
# Ollama already bundles llama.cpp — find it:
# Windows: %LOCALAPPDATA%\Programs\Ollama\
# Or build from source:
git clone https://github.com/ggerganov/llama.cpp
cd llama.cpp
cmake -B build -DGGML_ROCM=ON    # for RX 580
cmake --build build --config Release -j4
```

### Step 4.2 — KV Cache Save/Load with llama.cpp
```bash
# The flag that makes Architecture 1 work at the binary level:
# --prompt-cache <file>        save/load KV cache to this file
# --prompt-cache-all           include ALL tokens (not just system prompt)

# First run (or after RESET): saves KV state to file
./build/llama-cli \
  --model ~/.ollama/models/blobs/[your-model-blob] \
  --prompt-cache active/task_state.cache \
  --prompt-cache-all \
  --ctx-size 8192 \
  --n-gpu-layers 99 \    # push all layers to GPU
  --interactive

# Resume run: loads KV state, continues from exact point
./build/llama-cli \
  --model [same model] \
  --prompt-cache active/task_state.cache \
  --ctx-size 8192 \
  --n-gpu-layers 99 \
  --interactive
  # llama.cpp detects the cache, skips prefill, resumes instantly
```

**KV cache file sizes for Qwen 9B (GQA) on 8GB RX 580:**
```
Context length | KV file size | Load time
2k tokens      | ~100MB       | <0.5s
4k tokens      | ~200MB       | ~0.5s
8k tokens      | ~400MB       | ~1s
```

### Step 4.3 — Orchestrator v1 (True KV)
```python
# Replace StateManager.save_task_state() and load_task_state()
# to use llama.cpp subprocess with --prompt-cache instead of JSON

# Orchestrator v1 runs llama.cpp as a subprocess
# Controls it via stdin/stdout
# Sends AUTOSAVE command → llama.cpp flushes KV cache to file
# On RESET → kills process, KV already saved
# On next start → spawns llama.cpp with --prompt-cache → resumes instantly

import subprocess

class LlamaCppSession:
    def __init__(self, model_path: str, cache_path: str, ctx_size: int = 8192):
        self.proc = subprocess.Popen(
            ["./llama.cpp/build/llama-cli",
             "--model", model_path,
             "--prompt-cache", cache_path,
             "--prompt-cache-all",
             "--ctx-size", str(ctx_size),
             "--n-gpu-layers", "99",
             "--interactive"],
            stdin=subprocess.PIPE,
            stdout=subprocess.PIPE,
            text=True
        )

    def send(self, message: str) -> str:
        self.proc.stdin.write(message + "\n")
        self.proc.stdin.flush()
        # Read response until next prompt
        response = []
        for line in self.proc.stdout:
            if line.strip() == ">":  # llama.cpp prompt
                break
            response.append(line)
        return "".join(response)

    def close(self):
        self.proc.terminate()
```

---

## Phase 5 — Integration & End-to-End Testing
**Time: Days 10-12**
**Goal: Full system working with fine-tuned model + true KV cache**

### Step 5.1 — Update Orchestrator to Use Fine-tuned Model
```python
# In orchestrator.py, change MODEL:
MODEL = "gyattmaxxer-worker:v1"  # the fine-tuned version

# Or for v1 (llama.cpp): point to the merged GGUF
MODEL_PATH = "gguf_output/model-q4_k_m.gguf"
```

### Step 5.2 — Integration Test Suite
```python
# test_architecture1.py

def test_autosave():
    """Verify state saves every 10 turns"""

def test_checkpoint_trigger():
    """Fill context to 90%, verify CHECKPOINT is written correctly"""

def test_resume_v0():
    """Save state as JSON, kill process, restart, verify resume"""

def test_resume_v1():
    """Save KV binary, kill process, restart, verify instant resume"""

def test_task_complete():
    """Complete a task, verify MD archive, verify registry update"""

def test_power_loss():
    """Kill process at turn 7 (before autosave at 10), restart
    Should resume from last autosave (turn 0) — acceptable data loss window"""
```

### Step 5.3 — Tune Parameters
After running tests, adjust:
- `AUTOSAVE_EVERY`: 10 turns default — reduce to 5 if power cuts are frequent
- `CHECKPOINT_AT`: 90% default — move to 85% if model needs more tokens to write good checkpoint
- `CONTEXT_LIMIT`: actual model limit vs usable limit (use 75% of technical limit for safety)

---

## Phase 6 — Sentinel (Optional, After Phase 5 Works)
**Time: Days 12-14**
**Goal: Add the 1.7B quality monitor**

The Sentinel runs on the office PC (RX 580) alongside the worker.
Worker runs on Mac Mini cluster (or Vast.ai for training).

```python
# sentinel.py — runs separately, watches worker output
# Binary output: DRIFT / OK
# Checks:
# - Does checkpoint contain all 6 required fields?
# - Is the current step consistent with previous steps?
# - Are file names / variable names consistent with earlier turns?
# - Is the model repeating itself (loop detection)?

# Fine-tune a 1.7B model (SmolLM2-1.7B or Qwen2.5-1.5B) for this
# Dataset: ~500 examples of DRIFT vs OK classification
```

---

## Timeline Summary

```
Day 1:   Phase 0 — Environment + model verification
Day 2-4: Phase 1 — Orchestrator v0 (context simulation, no fine-tune)
         ← FIRST WORKING PROTOTYPE HERE ←
Day 4-6: Phase 2 — Training data generation (~2000 examples)
Day 6-8: Phase 3 — LoRA fine-tune on RX 580 (training runs overnight)
Day 8-10:Phase 4 — True KV binary save/load with llama.cpp
Day 10-12:Phase 5 — Integration + testing end-to-end
Day 12-14:Phase 6 — Sentinel (optional)
```

---

## Tool Stack Summary

| Component | Tool | Why |
|-----------|------|-----|
| Inference (dev) | Ollama | Already running, easiest |
| Inference (prod) | llama.cpp direct | True KV cache save/load |
| Fine-tuning | Unsloth + QLoRA | Fastest on ROCm, fits 8GB |
| Training data | Self-play (9B via Ollama) | Don't need ClassAI logs |
| KV storage | llama.cpp `--prompt-cache` | Binary KV, <1s load |
| Orchestrator | Python (requests, subprocess) | Keep it simple, ~200 lines |
| Token counting | tiktoken (cl100k_base) | Close enough for Qwen |
| Registry | JSON file | Zero overhead, human readable |
| Archive | Markdown files | Permanent, portable, searchable |
| Sentinel | Qwen2.5-1.5B fine-tuned | Fits on RX 580 alongside worker |

---

## What NOT to Build

- ❌ Don't use LangChain/LangGraph/CrewAI — adds complexity, hides what's happening
- ❌ Don't use a vector database for Architecture 1 — registry.json is the index
- ❌ Don't try to build true KV before v0 works — prove concept first
- ❌ Don't fine-tune before orchestrator is tested — you won't know what behavior to train

---

## Success Criteria (Architecture 1 Complete)

- ✅ Agent runs indefinitely without context degradation
- ✅ Checkpoint writes at 90% — includes all 6 required fields
- ✅ Resume within 1 second (v1 with true KV binary)
- ✅ Completed tasks archived as clean readable MD
- ✅ Registry always reflects true system state
- ✅ Power loss recovery: resume from last autosave (max 10 turns lost)
- ✅ Stable refs always reloaded fresh — never stale

---

*Blueprint created: March 27, 2026*
*Build order: Arch 1 → Arch 2 → Arch 3*
*Hardware target: RX 580 (8GB) for worker + fine-tuning, Mac Mini cluster for 70B*
