# 08 — Agent Checkpoint/Resume: State of the Art (2025-2026)

> Research compiled March 2026. Covers production frameworks, academic papers, failure modes, and the game save analogy.

---

## 1. What Format Should Agent Checkpoints Be Saved In?

### 1.1 Letta/MemGPT: Agent File (.af)

**Format:** Open file format for serializing stateful AI agents. Released January 2025.

**What it saves:**
- Model configuration (context window limits, model name, embedding model)
- Complete message history with `in_context` flags marking which messages are in the active window
- System prompt (behavioral definition)
- Memory blocks (editable personality traits, user information)
- Tool definitions with source code + JSON schemas
- Tool sequencing rules and environment variables

**What it does NOT save:**
- Archival memory passages (Letta's long-term recall store) — planned for future
- Multi-agent state — planned for future

**Key design choice:** State is persisted to a database (not Python variables). The `.af` file is for portability/sharing — the live system uses DB-backed persistence. Letta v1 introduced "Context Repositories" with git-based versioning of memory.

**Schema:** Defined in `pydantic_agent_schema.py` in the Letta repo — Pydantic models, serialized to structured format.

**Source:** https://github.com/letta-ai/agent-file

---

### 1.2 LangGraph: Checkpoint System

**Format:** Custom serialization via `JsonPlusSerializer` using `ormsgpack` (optimized MessagePack).

**Storage backends:**
- SQLite (dev only): 2 tables — `checkpoints` + `checkpoint_writes`, all data inline as BLOBs
- PostgreSQL (production): 3 tables — `checkpoints` (JSONB for primitives), `checkpoint_blobs` (binary for complex objects), `checkpoint_writes` (pending async operations)

**What a checkpoint contains:**
- `channel_values` — current state values for every channel in the graph
- `channel_versions` — monotonically increasing version numbers per channel
- `versions_seen` — which graph nodes have consumed which channel versions
- `pending_writes` — list of `(task_id, channel, value)` tuples for async operations

**Serialization details:**
- Primitive types (str, int, float, bool, None) stored inline as queryable JSON
- Complex objects (LangChain messages, custom classes) serialized to binary blobs via ormsgpack
- Type metadata wrapping for reconstruction

**Thread model:** Each checkpoint belongs to a `(thread_id, checkpoint_ns)` pair. Namespace allows multiple independent checkpoint chains within one thread. Version IDs use `"{version:032}.{hash:016}"` for lexicographic sorting with concurrent-write safety.

**Latest versions:** `langgraph-checkpoint-sqlite` 3.0.3 (Jan 2026), `langgraph-checkpoint-postgres` 3.0.2 (Jan 2026).

**Source:** https://deepwiki.com/langchain-ai/langgraph/4.2-checkpoint-implementations

---

### 1.3 AutoGen (Microsoft): save_state() / load_state()

**Format:** Plain JSON dictionaries.

**What it saves per agent type:**
- `AssistantAgent`: model_context (all LLM messages). Structure:
  ```json
  {
    "type": "AssistantAgentState",
    "version": "1.0.0",
    "llm_messages": [
      {"content": "...", "source": "user", "type": "UserMessage"},
      {"content": "...", "source": "assistant_agent", "type": "AssistantMessage"}
    ]
  }
  ```
- `Teams`: Nested structure wrapping all child agent states + team_id
  ```json
  {
    "type": "TeamState",
    "version": "1.0.0",
    "agent_states": { /* nested */ },
    "team_id": "..."
  }
  ```
- Custom agents: Empty state by default — must override `save_state()` and `load_state()`

**Persistence:** Serialized with standard `json.dump()` to files. No built-in DB layer.

**Known issues (2025):**
- TTL policy proposed but not merged — old messages accumulate without cleanup
- GraphFlow state gets stuck after interruption during agent transitions (Issue #7043)
- FastAPI sample doesn't actually persist history/state to JSON (Issue #6981)
- No built-in state persistence for WebSurfer agent (Issue #4572)

**Source:** https://microsoft.github.io/autogen/stable//user-guide/agentchat-user-guide/tutorial/state.html

---

### 1.4 OpenAI Assistants API: Thread Persistence

**Format:** Opaque — OpenAI manages internally. Developers interact via API only.

**What it saves:**
- Complete message history per thread (up to 100,000 messages)
- File attachments and tool call results
- Run metadata and status

**Context management:** When thread size exceeds model's context window, the API "smartly truncates messages, before fully dropping the ones it considers the least important." This is a black box — no developer control over what gets dropped.

**Key limitation:** No API to list threads. Developers must store `thread_id` values in their own database. The persistence is server-side and opaque.

**Deprecation note:** OpenAI released the Agents SDK (March 2025) as the production successor. The Agents SDK provides explicit session memory with two strategies:
- **Context trimming**: Drop older turns, keep last N turns. Zero latency overhead.
- **Context compression**: LLM-based summarization of older context.
- **Storage options**: Redis, SQLAlchemy, Dapr state store, or OpenAI Conversations API.

**Source:** https://developers.openai.com/api/docs/assistants/deep-dive

---

### 1.5 Academic Papers on Agent State

**MemOS: A Memory OS for AI Systems** (arXiv:2507.03724, July 2025)
- Treats memory as a first-class OS resource with lifecycle management
- Core abstraction: **MemCube** — encapsulates content + metadata (provenance, versioning)
- Three memory types: plaintext, activation-based, parameter-level
- Three-layer architecture: Memory API → Scheduling/Management → Storage/Infrastructure
- MemCubes can be composed, migrated, and fused over time
- Multi-level permission control and context-aware activation
- **Directly relevant to our Architecture 3** — validates the OS-inspired tiered approach

**Source:** https://arxiv.org/abs/2507.03724

---

### 1.6 Summary: Checkpoint Format Comparison

| Framework | Format | Storage | What's Saved | Granularity |
|-----------|--------|---------|-------------|-------------|
| Letta (.af) | Pydantic → structured | Database (live) / file (export) | Messages, memory blocks, tools, config | Full agent snapshot |
| LangGraph | JsonPlusSerializer → ormsgpack | SQLite/Postgres | Channel values, versions, pending writes | Per-graph-step |
| AutoGen | Plain JSON | Files | Message history per agent/team | Per-session |
| OpenAI | Opaque (server-side) | OpenAI servers | Messages, files, tool results | Per-thread |
| MemOS | MemCube (structured) | Tiered (GPU/RAM/SSD) | Content + metadata + provenance | Per-memory-unit |

**Key insight for our project:** Every production system saves message history. The differentiator is metadata — Letta's `in_context` flags, LangGraph's channel versions, MemOS's provenance tracking. Our block tagger already does this with task-semantic metadata, which none of these systems have.

---

## 2. What Should Be Saved vs. Reconstructed?

### 2.1 JetBrains Research (December 2025): Observation Masking vs. Summarization

This is the most practically useful finding for our architecture.

**Setup:** Tested two context compression strategies on coding agents (SWE-bench).

**Observation masking** (winner):
- Keep agent's reasoning and action history in full
- Replace older environment observations (tool outputs, file contents) with placeholders
- Rolling window: latest 10 turns of full detail, older turns masked
- Result: 2.6% higher solve rate, 52% lower cost vs. summarization

**LLM summarization** (loser):
- Compress reasoning + actions + observations into summaries
- Result: Extended agent trajectories by 13-15% (summaries smooth over stop signals)
- Summary generation consumed 7%+ of total costs per instance
- "Summaries may actually smooth over signs indicating that the agent should already stop trying"

**Conclusion: Preserve reasoning chains and decisions verbatim. Replace old observations with placeholders. Do NOT summarize everything.**

**Source:** https://blog.jetbrains.com/research/2025/12/efficient-context-management/

---

### 2.2 ACON: Optimizing Context Compression for Long-Horizon Agents (arXiv:2510.00615, Oct 2025)

**Key insight:** Naive truncation and generic summarization both lose critical details. Different tasks need different compression strategies.

**What must be preserved (task-dependent):**
- Factual history
- Action-outcome relationships
- Evolving environment states
- Success preconditions
- Future decision cues

**Method:** Gradient-free optimization of compression guidelines:
1. Find paired trajectories where full context succeeds but compressed context fails
2. Use capable LLM to analyze what the compression lost
3. Update compression prompt to preserve that class of information
4. Repeat until convergence

**Results:** 26-54% peak token reduction while preserving task performance. Distills into smaller compressor models retaining 95%+ accuracy.

**Source:** https://arxiv.org/abs/2510.00615

---

### 2.3 Structured Distillation (arXiv:2603.13017, March 2026)

**The most recent paper — directly applicable to our checkpoint format.**

**Problem:** When context fills, agents summarize their own history. This is lossy compression applied iteratively — information loss compounds over long sessions.

**Solution:** Distill each exchange into a 4-field compound object:
1. `exchange_core` — what happened (compressed)
2. `specific_context` — relevant details
3. `thematic_room_assignments` — categorization
4. `files_touched` — regex-extracted file references

**Results:**
- 371 tokens/exchange → 38 tokens/exchange (11x compression)
- 96% of verbatim retrieval performance (Mean Reciprocal Rank) preserved
- Tested across 4,182 conversations from 6 software engineering projects

**Source:** https://arxiv.org/abs/2603.13017

---

### 2.4 Recursive Summarization for Long-Term Dialogue Memory (arXiv:2308.15022)

**Method:** After each session, generate updated memory: `M_new = LLM(current_session, previous_memory, prompt)`

**What's preserved:** Personality traits, preferences, speaker characteristics — capped at 20 sentences.

**What's lost:** Specific utterances and granular details. Error rates: fabricated facts 2.7%, incorrect relationships 3.2%, missing details 3.9%.

**Performance:** 48.2% win rate vs. 11.9% loss rate against MemoryBank baseline in pairwise evaluation.

**Source:** https://arxiv.org/abs/2308.15022

---

### 2.5 Practical Save/Reconstruct Decision Matrix

Based on all the research above:

| Category | Save or Reconstruct? | Rationale |
|----------|---------------------|-----------|
| **Agent's reasoning chain** | SAVE verbatim | JetBrains: masking observations but keeping reasoning wins |
| **Decisions made** | SAVE verbatim | Action-outcome relationships are critical (ACON) |
| **Task state / progress** | SAVE structured | Our checkpoint format already does this |
| **Tool call results** | SAVE compressed | 4-field structured distillation (2603.13017) |
| **Environment observations** | RECONSTRUCT | Replace with placeholders (JetBrains finding) |
| **Code file contents** | RECONSTRUCT | Re-read from disk — always fresher than cached |
| **System prompt** | RECONSTRUCT | Reload from stable files — never changes mid-task |
| **Personality / preferences** | SAVE as memory block | Recursive summary (2308.15022) or Letta memory blocks |
| **Error traces / stack traces** | DISCARD | One-shot diagnostic value only |
| **Intermediate drafts** | DISCARD | Only final artifact matters |

**This validates our Architecture 1 design:** Save task state + decisions. Reload stable references (~700 tokens) fresh each session. The novel addition from this research: use structured distillation on tool results rather than raw inclusion.

---

## 3. Failure Modes of Naive Checkpoint/Resume

### 3.1 Lost in the Middle (Liu et al., 2024 — TACL)

**The landmark paper.** Published in Transactions of the ACL, 2024.

**Finding:** LLMs perform best on information at the beginning or end of context, with 30%+ accuracy drop for information in the middle of a 20-document context.

**Cause:** Rotary Position Embedding (RoPE) creates a decay effect — stronger attention to early and late tokens. This mirrors the human serial-position effect (primacy + recency bias), but it's surprising because self-attention is theoretically position-invariant.

**U-shaped performance curve:** Accuracy is high for positions 1-3 and 18-20, lowest at positions 8-12 in a 20-document sequence.

**2025 update (Chroma study):** Tested 18 frontier models including GPT-4.1, Claude Opus 4, Gemini 2.5 — ALL still show degradation as input length increases.

**Du et al. (2025):** Even replacing irrelevant tokens with whitespace and forcing attention only to relevant tokens, performance drops 13.9-85% as input length increases. Context length alone degrades performance independent of retrieval quality.

**Implication for checkpoint/resume:** If you naively prepend a long checkpoint summary at the start and then continue generating, the model will attend strongly to the very start and very end of context, but the checkpoint content in the middle (between system prompt and current turn) will be partially ignored. **This is why our block tagger + semantic router approach is critical — load only relevant context, keep it short, position it where the model will actually attend to it.**

**Source:** https://arxiv.org/abs/2307.03172

**Proposed fix:** Multi-scale Positional Encoding (Ms-PoE) — plug-and-play approach, no fine-tuning needed. Source: https://openreview.net/forum?id=fPmScVB1Td

---

### 3.2 Multi-Agent System Failures (Cemri et al., arXiv:2503.13657, March 2025)

**Taxonomy of 14 failure modes across 3 categories:**

**Category 1 — Specification & System Design:**
- Task/role violations
- **Conversation history loss** — agents revert to earlier states, losing recent interactions
- Inadequate role definitions

**Category 2 — Inter-Agent Misalignment:**
- **Conversation reset** — unwarranted dialogue restarts that lose context and progress
- **Step repetition** — unnecessary reiteration of completed steps
- Failure to seek clarification
- Information withholding between agents

**Category 3 — Task Verification & Termination:**
- Premature termination
- Inadequate verification

**Sobering stat:** State-of-the-art multi-agent systems (ChatDev) achieve only 25% correctness. Even with improved prompts and topology redesign, improvement is ~14%.

**Source:** https://arxiv.org/abs/2503.13657

---

### 3.3 Coherence Degradation Patterns (Multiple Sources, 2024-2025)

**From "How Do LLMs Fail In Agentic Scenarios" (arXiv:2512.07497):**
- Models begin tasks with correct reasoning but performance degrades mid-execution
- Failures include: malformed tool calls, loss of structure in JSON output, forgetting earlier decisions
- Coherence degradation under extended operation is a consistent pattern

**From "When Refusals Fail" (arXiv:2512.02445, December 2024):**
- Models with 1M-2M token context windows show severe degradation at 100K tokens
- Performance drops exceed 50% for both benign and harmful tasks
- Refusal rates shift unpredictably at extended context lengths

**Context pollution:**
- Models collapse to 30% or lower accuracy when irrelevant distractor text is introduced
- Failure to filter for relevance — the model treats all context as potentially useful

**From ACON paper:**
- Naive token truncation loses critical details essential for long-horizon reasoning
- Generic summarization is almost as bad — it doesn't know what's task-critical
- Information loss compounds over iterative summarization cycles

---

### 3.4 Failure Mode Summary for Our Architecture

| Failure Mode | Risk to Us | Our Mitigation |
|-------------|-----------|----------------|
| Lost in the middle | HIGH — resumed context sits in middle positions | Semantic router loads only relevant blocks; keep total context short |
| Conversation history loss | MEDIUM — on checkpoint restore | Structured checkpoint format preserves decisions explicitly |
| Step repetition after resume | MEDIUM — model re-does completed work | Task registry tracks completion; checkpoint includes progress state |
| Context pollution | HIGH — loading too many blocks | Criticality-based eviction; only task-relevant blocks loaded |
| Coherence degradation over time | HIGH — long autonomous runs | Checkpoint-evict-continue cycle resets context regularly |
| Compounding summarization loss | LOW — we don't recursively summarize | We use observation masking + structured distillation instead |
| JSON structure loss | MEDIUM — checkpoint format corruption | Validation on save; schema-based checkpoint structure |

---

## 4. Game Save Systems as Analogy

### 4.1 Skyrim/Bethesda: The Delta Model

Bethesda's Creation Engine save files (.ess) are the best analogy for agent checkpoints because **they save deltas from a known base state, not the entire world**.

**Save file structure:**
1. **Header** — Player name, level, location, race (= agent identity metadata)
2. **Plugin list** — Which mods are loaded (= which tools/capabilities are active)
3. **Global Data Tables:**
   - Table 1 (Types 0-8): Player location, misc stats, weather, audio, created objects
   - Table 2 (Types 100-114): Process lists, combat state, UI, quests, dialogue, detection
   - Table 3 (Types 1000-1005): Papyrus scripts, animations, timers, temp effects
4. **Change Forms** — The core innovation: only objects that differ from their base definition
5. **FormID Array** — Reference mapping for modified objects

**The key insight: Change Forms.**
Every NPC, item, quest, and location has a base definition in the game's master files (.esm). The save file only records **what changed** — an NPC moved, a quest advanced, an item was picked up. This is delta compression at the entity level.

**Change Form types (43 categories):** NPCs, items, quests, cells, projectiles, references, actors — each with bitflags indicating which properties were modified.

**Source:** https://en.uesp.net/wiki/Skyrim_Mod:Save_File_Format

---

### 4.2 The Agent-to-Game Mapping

| Game Concept | Agent Equivalent | What Gets Saved |
|-------------|-----------------|-----------------|
| **World State** | Project files + codebase | RECONSTRUCTED from disk (never saved in checkpoint) |
| **Player State** | Agent config + capabilities | System prompt, model params, tool definitions — RECONSTRUCTED from stable files |
| **Quest State** | Task progress + decisions | SAVED — this is the checkpoint. What was decided, what was completed, what's next |
| **NPC State** | Conversation history | SAVED as structured distillation (compressed, not verbatim) |
| **Inventory** | Artifacts produced | RECONSTRUCTED from disk (code files, docs exist independently) |
| **Change Forms (deltas)** | Checkpoint diff | SAVED — only what changed since last known state |
| **Master files (.esm)** | Stable reference docs | NEVER saved — always reloaded fresh |
| **Mod plugins (.esp)** | Tool/capability definitions | RECONSTRUCTED from tool registry |
| **Papyrus scripts** | Active workflows | SAVED — running task state, pending operations |

---

### 4.3 Design Principles from Game Saves

**1. Save deltas, not snapshots.**
Skyrim doesn't serialize the entire world. It records what changed from the base game files. Our agent should save what changed from the project's base state (files on disk + stable references).

**2. Separate what the player did from what the world is.**
Games distinguish player actions (quest flags, inventory, skills) from world state (NPC positions, weather). Our agent should separate task decisions from environment observations.

**3. Don't save what you can regenerate.**
Zelda doesn't save player position — it restarts you at the dungeon entrance. That's intentional: position is cheap to reconstruct. Our agent shouldn't save file contents or tool outputs — they can be re-read from disk.

**4. Single save slot prevents state explosion.**
Pokemon's single save prevents branching timelines. For agents: maintain one canonical task state per task, not multiple branching histories.

**5. Save points create natural boundaries.**
Dark Souls saves at bonfires — architecturally meaningful locations. Our checkpoint should trigger at task-meaningful moments (subtask completion, major decision, context threshold), not arbitrary intervals.

**6. The save file format must survive version changes.**
Bethesda's FormID system handles mod load order changes gracefully. Our checkpoint format should use semantic identifiers (task names, not array indices) so it survives schema changes.

---

### 4.4 Proposed Checkpoint Format (Informed by All Research)

Based on the Skyrim delta model + structured distillation + observation masking:

```json
{
  "format_version": "1.0",
  "checkpoint_type": "task_delta",

  "identity": {
    "task_id": "api_build_v2",
    "agent_model": "qwen3.5-9b",
    "created_at": "2026-03-30T14:22:00Z",
    "context_usage_at_save": 0.72
  },

  "quest_state": {
    "objective": "Build REST API with auth + CRUD",
    "completed_steps": [
      "Designed schema with 3 tables",
      "Implemented JWT auth (router + middleware)",
      "Built /users CRUD endpoints"
    ],
    "current_step": "Implementing /posts CRUD endpoints",
    "next_steps": ["Rate limiting", "Error handling", "Tests"],
    "key_decisions": [
      {"decision": "Used bcrypt for password hashing", "reason": "Industry standard, no rainbow table risk"},
      {"decision": "Chose SQLAlchemy over raw SQL", "reason": "Migration support needed"}
    ],
    "blockers": []
  },

  "change_forms": [
    {
      "type": "artifact",
      "path": "app/auth/router.py",
      "status": "complete",
      "summary": "JWT login/register endpoints, 47 lines"
    },
    {
      "type": "artifact",
      "path": "app/models/user.py",
      "status": "complete",
      "summary": "SQLAlchemy User model with hashed password"
    },
    {
      "type": "artifact",
      "path": "app/routes/posts.py",
      "status": "in_progress",
      "summary": "GET /posts done, POST /posts partial"
    }
  ],

  "distilled_context": [
    {
      "exchange_core": "User requested FastAPI REST API with auth",
      "specific_context": "PostgreSQL backend, JWT tokens, bcrypt hashing",
      "files_touched": ["app/main.py", "app/auth/router.py", "app/models/user.py"]
    }
  ],

  "do_not_save": {
    "note": "These are reconstructed on resume, never checkpointed",
    "categories": [
      "system_prompt (reload from stable/role_instructions.md)",
      "file_contents (re-read from disk)",
      "tool_outputs (re-execute if needed)",
      "error_traces (diagnostic only)",
      "intermediate_drafts (only final artifacts matter)"
    ]
  }
}
```

---

## 5. Key Papers & URLs Index

### Frameworks
- Letta Agent File (.af): https://github.com/letta-ai/agent-file
- Letta/MemGPT platform: https://github.com/letta-ai/letta
- LangGraph checkpoint docs: https://docs.langchain.com/oss/python/langgraph/persistence
- LangGraph checkpoint implementations: https://deepwiki.com/langchain-ai/langgraph/4.2-checkpoint-implementations
- AutoGen state management: https://microsoft.github.io/autogen/stable//user-guide/agentchat-user-guide/tutorial/state.html
- OpenAI Assistants deep dive: https://developers.openai.com/api/docs/assistants/deep-dive
- OpenAI Agents SDK session memory: https://cookbook.openai.com/examples/agents_sdk/session_memory

### Academic Papers
- **Lost in the Middle** (Liu et al., 2024): https://arxiv.org/abs/2307.03172 — U-shaped attention, 30%+ accuracy drop in middle positions
- **Why Do Multi-Agent LLM Systems Fail?** (Cemri et al., 2025): https://arxiv.org/abs/2503.13657 — 14 failure modes, conversation history loss
- **ACON: Context Compression for Long-Horizon Agents** (Kang et al., 2025): https://arxiv.org/abs/2510.00615 — 26-54% token reduction via optimized compression
- **Structured Distillation for Agent Memory** (2026): https://arxiv.org/abs/2603.13017 — 11x compression, 96% retrieval preservation
- **MemOS: Memory OS for AI** (2025): https://arxiv.org/abs/2507.03724 — OS-inspired tiered memory, MemCube abstraction
- **Recursive Summarization for Dialogue Memory** (2023): https://arxiv.org/abs/2308.15022 — Progressive memory refinement
- **When Refusals Fail: Long-Context Agents** (2024): https://arxiv.org/abs/2512.02445 — 50%+ degradation at 100K tokens
- **How Do LLMs Fail in Agentic Scenarios?** (2024): https://arxiv.org/abs/2512.07497 — Mid-execution coherence degradation
- **Found in the Middle (Ms-PoE fix)**: https://openreview.net/forum?id=fPmScVB1Td — Plug-and-play positional encoding fix

### Technical Articles
- **JetBrains: Efficient Context Management** (Dec 2025): https://blog.jetbrains.com/research/2025/12/efficient-context-management/ — Observation masking > LLM summarization
- **Mem0: Context Engineering Guide** (Oct 2025): https://mem0.ai/blog/context-engineering-ai-agents-guide
- **Mem0: Chat History Summarization** (Oct 2025): https://mem0.ai/blog/llm-chat-history-summarization-guide-2025

### Game Save System References
- Skyrim Save File Format (UESP): https://en.uesp.net/wiki/Skyrim_Mod:Save_File_Format
- Save Architecture Part 1 (Hedberg Games): https://www.hedberggames.com/blog/save-architecture-part-1
- Save System Design Part 3 (Game Developer): https://www.gamedeveloper.com/design/save-system-design-pt-3
- Game Serialization (Gabriel's Virtual Tavern): https://jorenjoestar.github.io/post/serialization_for_games/

---

## 6. Implications for Our Architecture

### What this research validates:
1. **Our delta-based checkpoint format is correct.** Skyrim proves it. Letta's `.af` and LangGraph's channel versioning both follow this pattern. Save what changed, reconstruct everything else.
2. **Observation masking > summarization.** JetBrains proved this on coding agents specifically. Our eviction of old tool outputs while keeping decision history is exactly right.
3. **The block tagger is novel.** None of the production frameworks (Letta, LangGraph, AutoGen, OpenAI) tag context blocks with task-semantic metadata. They all use position-based or recency-based management.
4. **Lost-in-the-middle is real and unsolved.** Even frontier models in 2025 show it. Our architecture mitigates it by keeping loaded context short and task-relevant rather than dumping everything into a long context.
5. **MemOS validates Architecture 3.** Independent research (July 2025) arrived at the same OS-inspired tiered memory concept with lifecycle management.

### What we should add based on this research:
1. **Structured distillation for completed exchanges.** Instead of raw message history in checkpoints, compress each exchange into the 4-field format from arXiv:2603.13017 (11x compression, 96% retrieval preservation).
2. **Explicit "do not save" list.** Every checkpoint should declare what was intentionally excluded, so the resume logic knows to reconstruct it.
3. **Checkpoint positioning awareness.** When loading checkpoint context for resume, place it at the START of context (system prompt position), not in the middle. Lost-in-the-middle research says this is where the model attends most strongly.
4. **Compression guideline optimization.** Adopt ACON's approach of finding cases where compressed context fails and updating the compression strategy. This could be automated over time.
