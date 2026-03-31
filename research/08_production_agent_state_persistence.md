# 08: Production Agent State Persistence & Session Resumption (2025-2026)

Research compiled March 2026. Covers how production AI agent frameworks handle state persistence, context compression, and session resumption.

---

## 1. LangGraph Checkpointing

LangGraph has the most mature checkpointing system in the open-source agent ecosystem.

### What Exactly Gets Saved

A `Checkpoint` is a TypedDict with these fields:

| Field | Type | Content |
|-------|------|---------|
| `v` | int | Format version (currently `1`) |
| `id` | str | UUID v6 with monotonic counter (sortable chronologically) |
| `ts` | str | ISO 8601 timestamp |
| `channel_values` | dict | The actual graph state, indexed by channel name |
| `channel_versions` | dict | Version identifier per channel |
| `versions_seen` | dict | Maps node IDs to which channel versions they have already processed |
| `updated_channels` | list | Channels modified in this checkpoint |

The `source` field distinguishes origin: `"input"` (initial), `"loop"` (during execution), `"update"` (manual state edit), or `"fork"` (branching from existing checkpoint).

### Three-Table Storage Model

All persistent checkpoint implementations (Postgres, SQLite, Redis, DynamoDB, Couchbase) use the same logical schema:

| Table | Primary Key | Purpose |
|-------|-------------|---------|
| `checkpoints` | `(thread_id, checkpoint_ns, checkpoint_id)` | Main record. Primitive values (str, int, float, bool, None) stored inline as JSON. Complex values stored by reference. |
| `checkpoint_blobs` | `(thread_id, checkpoint_ns, channel, version)` | Serialized complex objects, keyed by channel name + version string. |
| `checkpoint_writes` | `(thread_id, checkpoint_ns, checkpoint_id, task_id, idx)` | Pending writes awaiting application. Enables fault tolerance. |

On retrieval, all three tables are joined in a single query round-trip to reconstruct a complete `CheckpointTuple`.

### Serialization

Default serializer: `JsonPlusSerializer` — uses `ormsgpack` with a fallback to extended JSON. Handles LangChain types, datetimes, enums, and custom objects via `serde.dumps_typed()` / `serde.loads_typed()`.

### Channel Versioning

Version strings follow the format `"{step:032}.{random:016}"` — zero-padded step counter plus random suffix. This enables lexicographic sorting, conflict-free concurrent writes, and deduplication via `versions_seen`.

### Pending Writes & Fault Tolerance

Two-phase commit mechanism:

1. **Record phase**: `put_writes(config, writes, task_id)` saves `(channel, value)` tuples without applying them.
2. **Apply phase**: Next checkpoint creation applies all accumulated writes atomically.

Special negative indices distinguish system writes: `-1` = node error, `-2` = scheduled task, `-3` = interrupt, `-4` = resume value.

If a node fails mid-superstep, LangGraph stores pending writes from nodes that completed successfully. On resume, successful nodes do not re-run.

### Durability Modes

- **`"sync"`** (default): Checkpoint after every superstep. Full resumability.
- **`"exit"`**: Checkpoint only at graph completion. Minimal storage overhead.
- **`"async"`**: Background checkpointing during next step execution. Better throughput.

### What LangGraph Does NOT Save

- **Raw LLM context window state / KV cache**: Only graph-level state (channel values) is persisted. The actual token-level context or KV cache of the underlying LLM is not checkpointed.
- **In-flight LLM generation**: If the LLM is mid-generation when a crash occurs, that partial output is lost.
- **Ephemeral runtime state**: Thread pools, network connections, callback handlers.
- **Model weights or configuration**: The checkpoint assumes the same model will be available on resume.

### Known Limitations

- `InMemorySaver` uses nested `defaultdict` in-memory — no durability, no scalability.
- Custom application types need explicit serialization handlers registered with the serde system.
- Checkpoint size scales with state complexity. Large graph states (e.g., full message histories with tool outputs) can become expensive to persist every superstep.
- No built-in compaction of old checkpoints — the full checkpoint chain grows unbounded unless you prune manually.

---

## 2. MemGPT / Letta — Virtual Context Management

### Core Concept

Inspired by OS virtual memory: the LLM's fixed context window is "physical memory," and external storage is "disk." The agent manages its own paging via function calls.

### Memory Tiers

**Tier 1 — Main Context (always in the LLM's context window):**

| Section | Writability | Typical Size |
|---------|-------------|--------------|
| System instructions | Read-only | ~1,076 tokens |
| Core memory (persona + user facts) | Writable via tools | ~86 tokens (5,000 char limit) |
| Conversation history (FIFO queue) | Auto-managed | Variable |

**Tier 2 — External Context (requires explicit retrieval):**

| Store | Content | Access Method |
|-------|---------|---------------|
| Recall storage | Complete uncompressed event history | `conversation_search` (text lookup) |
| Archival storage | General read-write overflow datastore | `archival_memory_search` (semantic embedding search) |

### Paging Mechanism — What Actually Happens

There is NO automatic paging. The agent must explicitly call functions to manage its own context:

- `conversation_search(query)` — text-based lookup in recall storage
- `archival_memory_search(query)` — semantic embedding-based retrieval from archival
- `core_memory_append(field, value)` — add to in-context core memory
- `core_memory_replace(field, old, new)` — edit in-context core memory
- `archival_memory_insert(content)` — write to archival storage

When messages exceed the FIFO queue capacity, old messages are evicted and a **recursive summary** replaces them. The evicted messages move to recall storage. The context then shows something like: "4 previous messages between you and the user are stored in recall memory (use functions to access them)."

### What Gets Saved to Disk

- Core memory blocks: XML-structured with labels, values, character counts.
- Archival entries: Text strings with semantic embeddings in a vector database.
- Recall entries: Full message history with timestamps.
- All persistent state stored in a database (SQLite for local, Postgres for production).

### What Does NOT Get Saved

- The LLM's internal KV cache or attention state.
- Intermediate reasoning traces (unless the agent explicitly writes them to archival).
- The quality of the recursive summary — once messages are evicted and summarized, the original detail is only recoverable via explicit `conversation_search`.

### Known Limitations and Failure Modes

1. **No automatic paging**: If the agent forgets to call retrieval functions, it has context blindness. The model must be competent enough to know when it needs to search.
2. **Fixed core memory size**: 5,000 character limit constrains persona/user fact storage.
3. **Retrieval quality depends on embedding quality**: Bad queries = missed relevant memories.
4. **Summarization fidelity loss**: Recursive summaries of evicted messages degrade information. Each summarization cycle loses detail.
5. **Latency overhead**: Every out-of-context retrieval adds a tool call round-trip.
6. **No failure recovery**: No explicit retry logic for retrieval failures documented.
7. **Early maturity**: One practitioner noted they "can do very little with this agent right now" as of early 2025.

---

## 3. CrewAI

### State Persistence

CrewAI has **no built-in checkpointing for long-running workflows**. State persistence is limited to:

- Task outputs passed sequentially between agents in a crew.
- Human checkpoints: supervisors can review/refine outputs before tasks proceed.

### What Gets Saved

- Task outputs from completed steps (passed to next agent in sequence).
- No persistent state store across sessions.
- No resume-from-failure capability.

### What Does NOT Get Saved

- Intermediate agent reasoning.
- Conversation history between agent and LLM.
- No checkpoint/resume for interrupted workflows.

### Known Limitations

- If a multi-step workflow fails partway through, it must restart from the beginning.
- Limited control over agent-to-agent communication.
- No mechanism for long-running session continuity.

---

## 4. Microsoft AutoGen / Agent Framework

### AutoGen Teachable Agents (Legacy, now maintenance mode)

The `Teachability` capability stores lessons in a **local vector database**:

1. **Vectorization**: Conversation snippets and user corrections converted to embeddings.
2. **Storage**: Persisted in a local vector DB (configured via `path_to_db_dir`).
3. **Retrieval**: On new queries, semantically similar past lessons retrieved and injected into prompt context.
4. **Adaptation**: Retrieved context modifies response generation at runtime — no model weight updates.

**What gets stored**: Conversation history segments, user corrections, derived lessons.
**What does NOT get stored**: Model state, attention patterns, intermediate reasoning.

**Limitations**:
- Runtime-only adaptation — the base model is unchanged.
- Relies on embedding similarity — poor queries miss relevant lessons.
- Adversarial vulnerability: can internalize incorrect feedback without moderation.
- Single-user preference bias without diversity weighting.

### AutoGen 0.4 (January 2025)

- Redesigned architecture with modular memory components.
- Asynchronous messaging with event-driven and request/response patterns.
- Conversation history is **in-memory by default** — less robust than checkpointing.

### Microsoft Agent Framework (2025-2026, replacing AutoGen)

AutoGen is now in maintenance mode. Agent Framework merges AutoGen's agent abstractions with Semantic Kernel's enterprise features:

- **Session-based state management** with type safety and middleware.
- **Graph-based workflows** for multi-agent orchestration.
- **Pause/persist/resume from any checkpoint** — fault tolerance for long-running workflows.
- **Telemetry integration** built in.
- GA target: end of Q1 2026 with stable versioned APIs.

Limited public technical detail on the checkpoint format as of March 2026.

---

## 5. Anthropic's Research on Agent Coherence

### Context Engineering (from Anthropic's engineering blog)

Three primary techniques for long-horizon agent coherence:

**Compaction:**
- Summarize conversation approaching context limit, restart with condensed summary.
- Preserves: architectural decisions, unresolved bugs, implementation details.
- Discards: redundant tool outputs, resolved intermediate steps.
- Lightest-touch version: tool result clearing — remove raw results from previously executed tool calls deep in history.
- Anthropic provides `compact-2026-01-12` as a provider-native compaction API that auto-triggers at ~50,000 tokens.

**Structured Note-Taking (Agentic Memory):**
- Agent maintains persistent notes outside context window (to-do lists, NOTES.md files).
- Reloaded on session resume.
- Demonstrated in Pokemon gameplay: agents tracked maps, achievements, and strategies across thousands of steps.

**Multi-Agent Architectures:**
- Sub-agents handle focused tasks with clean context windows.
- Each sub-agent explores extensively (tens of thousands of tokens) but returns only 1,000-2,000 token condensed summaries.
- Sub-agents store structured artifacts in external systems and return lightweight references.
- Introducing parallelization cut research time by up to 90% for complex queries.

### Key Finding: Context Pollution

"Every token added to the context window competes for the model's attention." Stuffing hundreds of thousands of tokens degrades reasoning about what matters. The goal is: "the smallest set of high-signal tokens that maximize the likelihood of your desired outcome."

### Multi-Agent Research System Architecture

- Orchestrator-worker pattern: lead agent coordinates, delegates to specialized sub-agents.
- Sub-agents are "intelligent filters" that compress information before returning to lead.
- When approaching ~200K tokens, the system spawns fresh sub-agents with clean contexts.
- Continuity maintained through careful handoffs and external memory storage.
- Detailed task decomposition prevents duplicate work (vague instructions caused sub-agents to "perform the exact same searches").
- Regular checkpoints + deterministic retry logic for fault tolerance.

---

## 6. Context Compression & Conversation State Distillation

### Technique Comparison

| Technique | Token Reduction | Fidelity | Cost | Works With |
|-----------|----------------|----------|------|------------|
| Sliding window (truncation) | High | Very low — loses continuity | Free | Any model |
| Rolling LLM summarization | Moderate | Moderate — details drift across cycles | High — reprocesses full history | Any model |
| Anchored iterative summarization | Moderate | High — extends rather than regenerates | Low — only summarizes new evictions | Any model |
| ACON (failure-driven optimization) | 26-54% | 95%+ task accuracy preserved | Medium — requires paired trajectory analysis | Any model (gradient-free) |
| Provider-native compaction (Anthropic) | Moderate | High | Low — API-managed | Anthropic models |
| Embedding-based compression | 80-90% | Low for verbatim details | Medium | Any model with embeddings |

### ACON Paper (arXiv 2510.00615)

The most rigorous academic work on agent context compression:

- **Mechanism**: Paired trajectory analysis — when full context succeeds but compressed context fails, an LLM analyzes the failure cause and updates compression guidelines in natural language.
- **Gradient-free**: No parameter updates needed. Works with closed-source API models.
- **Results**: 26-54% peak token reduction while preserving task success. Enables small LMs to achieve 20-46% performance improvements as long-horizon agents.
- **Validated on**: AppWorld, OfficeBench, Multi-objective QA (all requiring 15+ interaction steps).
- **Can distill** optimized compression guidelines into smaller models for cost-efficient deployment.

### Production Compression Ratios (from Zylos Research)

| Content Type | Compression Ratio | Notes |
|-------------|-------------------|-------|
| Old conversation history | 3:1 to 5:1 | Prioritize decisions/outcomes |
| Tool outputs/observations | 10:1 to 20:1 | Keep conclusions only |
| Recent messages (5-7 turns) | No compression | Preserve fully |
| System prompt | No compression | Never compress |

**Trigger threshold**: Compress at 70% of available context budget. Performance degrades beyond ~30,000 tokens even in large-window models.

### Anchored Iterative Summarization (Factory's approach)

When compression triggers:
1. Identify newly-evicted message span.
2. Summarize only that segment.
3. Merge into persistent anchor state containing: intent, changes made, decisions, next steps.

Benchmark: 4.04 accuracy score vs. Anthropic's 3.74 and OpenAI's 3.43 for preserving technical details (file paths, error messages).

### Critical Finding: Context Drift vs. Exhaustion

"Context drift kills agents before context limits do." 65% of 2025 enterprise AI failures involved drift or memory loss during multi-step reasoning, NOT raw context exhaustion.

**Drift symptoms**:
- Agents redo completed work
- Goal statements shift in wording
- Technical details become incorrect
- System prompt instructions forgotten

**Detection**: Distributed tracing can identify the exact turn where drift begins.

---

## 7. Redis / Database-Backed Agent State (Production Patterns)

### Architecture: Redis for Hot State

Production agents typically use Redis for sub-millisecond access to active state:

**Short-Term Memory (Thread-Level)**:
- `RedisSaver` / `AsyncRedisSaver`: Full checkpoint history per thread.
- `ShallowRedisSaver`: Only latest checkpoint (less storage, faster).
- Data stored as Redis JSON — nested structures representing full graph state.
- Thread ID acts as conversation identifier.

**Long-Term Memory (Cross-Thread)**:
- `RedisStore` / `AsyncRedisStore`: Persistent user information, learned patterns.
- Vector search via Redis Query Engine for semantic retrieval.
- HNSW indexing for accuracy-critical retrieval; IVF for enterprise scale (100M+ vectors).

### Hybrid Redis + Postgres Pattern

The production-grade architecture:

```
Agent reads/writes hot state to Redis (sub-ms)
         |
Background worker periodically:
  - Reads completed turns from Redis
  - Batch-inserts into Postgres with computed embeddings
  - Postgres handles: analytical queries, full history, compliance/audit
```

Redis handles: current goal, last tool output, sliding window of recent messages.
Postgres handles: full conversation archives, cross-session analytics, regulatory retention.

### Redis Agent Memory Server

Open-source server (github.com/redis/agent-memory-server) with structured lifecycle:

**Memory record fields**:
- Core: text content, memory type (semantic/episodic/message), topics, entities
- Temporal: created_at, persisted_at, updated_at, last_accessed, event_date
- Organization: user_id, session_id, namespace, memory_hash
- ID: ULID format (sortable)

**Working memory**: Redis with automatic TTL (default 1 hour).
**Long-term memory**: Vector database with embeddings.

**Automatic forgetting** via Docket (Redis-based task scheduler):
- Age-based: default 90 days max
- Inactivity-based: default 30 days without access
- Budget-based: keep top N most recently accessed
- Combined: must be both old AND inactive (unless exceeding hard age limit)

Critical: without a running background task worker, automatic forgetting does not occur.

### Memory Type Taxonomy in Production

| Memory Type | Storage | Access Pattern |
|-------------|---------|----------------|
| Episodic | Vector DB + event logs | Temporal queries, "what happened when" |
| Semantic | Structured DB + vector DB for concept embeddings | Factual lookups |
| Procedural | Workflow DB + vector DB for task similarity | "How do I do X" |

### Performance Targets

- Voice AI: sub-second end-to-end (speech-to-text -> memory retrieval -> inference -> TTS)
- High-throughput: hundreds of inferences/second
- Redis vector queries: low millisecond range
- Redis key lookups: sub-millisecond

---

## 8. Cross-Cutting Analysis: What This Means for the Three Architectures

### Mapping to Your Designs

| Production System | Most Similar To | Key Insight |
|-------------------|-----------------|-------------|
| LangGraph checkpointing | Architecture 1 (Selective KV Context Management) | They save graph state, not KV cache. Nobody in production saves raw KV cache across sessions yet. |
| MemGPT/Letta | Architecture 3 (Virtual Context Memory) | OS-inspired tiered memory, but relies on the agent to manage its own paging — no automatic eviction. |
| Anthropic compaction | Architecture 2 (Precomputed KV) concept, adapted | Separate stable knowledge from active reasoning. Their multi-agent approach keeps sub-agent contexts clean. |
| Redis hot/cold pattern | Architecture 3 tiers | Redis = hot tier, Postgres = warm/cold, but for structured state, not KV cache. |
| ACON compression | Complements all three | Compression guidelines could define what enters your KV cache vs. what gets archived. |

### The Gap Your Architectures Fill

No production framework currently saves or restores raw KV cache state. They all serialize at a higher abstraction level (graph state, message history, structured memory records). Your Architecture 2 (precomputed KV for stable knowledge) and Architecture 1 (KV checkpoint/resume for active tasks) operate at a lower level that could provide faster resume and better fidelity than message-level serialization.

The critical finding from ACON and Anthropic's work is that **what you compress matters more than how much you compress**. Failure-driven optimization of compression guidelines is the current state of the art.

### Production Consensus (as of early 2026)

1. **Trigger compaction at 70% context budget**, not at the limit.
2. **Tool outputs are the most compressible content** (10:1 to 20:1 ratio, keep conclusions only).
3. **Context drift is the #1 failure mode**, not context exhaustion.
4. **Structured anchor state** (intent + decisions + next steps) beats full-history summarization.
5. **Multi-agent with clean contexts** outperforms single-agent with managed context.
6. **Redis for hot state, Postgres for cold** is the de facto production pattern.
7. **Nobody saves raw KV cache in production yet** — this remains an open implementation gap.

---

## Sources

- [LangGraph Checkpointing Architecture (DeepWiki)](https://deepwiki.com/langchain-ai/langgraph/4.1-checkpointing-architecture)
- [LangGraph Persistence Docs](https://docs.langchain.com/oss/python/langgraph/persistence)
- [LangGraph & Redis Integration](https://redis.io/blog/langgraph-redis-build-smarter-ai-agents-with-memory-persistence/)
- [MemGPT Paper (arXiv 2310.08560)](https://arxiv.org/abs/2310.08560)
- [MemGPT Virtual Context Management (Leonie Monigatti)](https://www.leoniemonigatti.com/blog/memgpt.html)
- [Letta/MemGPT Implementation (Terse Systems)](https://tersesystems.com/blog/2025/02/14/adding-memory-to-llms-with-letta/)
- [Letta Docs](https://docs.letta.com/concepts/memgpt/)
- [Anthropic: Effective Context Engineering for AI Agents](https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents)
- [Anthropic: How We Built Our Multi-Agent Research System](https://www.anthropic.com/engineering/multi-agent-research-system)
- [ACON Paper (arXiv 2510.00615)](https://arxiv.org/abs/2510.00615)
- [AI Agent Context Compression Strategies (Zylos Research)](https://zylos.ai/research/2026-02-28-ai-agent-context-compression-strategies)
- [AutoGen Teachable Agents (Analytics Vidhya)](https://www.analyticsvidhya.com/blog/2025/12/autogen-teachable-agent/)
- [Microsoft Agent Framework Overview](https://learn.microsoft.com/en-us/agent-framework/overview/)
- [Redis AI Agent Memory](https://redis.io/blog/ai-agent-memory-stateful-systems/)
- [Redis Agent Memory Server (GitHub)](https://github.com/redis/agent-memory-server)
- [Redis Agent Memory Server — Memory Lifecycle](https://redis.github.io/agent-memory-server/memory-lifecycle/)
- [CrewAI vs LangGraph vs AutoGen Comparison (OpenAgents)](https://openagents.org/blog/posts/2026-02-23-open-source-ai-agent-frameworks-compared)
- [Agent State Management: Redis vs Postgres (SitePoint)](https://www.sitepoint.com/state-management-for-long-running-agents-redis-vs-postgres/)
