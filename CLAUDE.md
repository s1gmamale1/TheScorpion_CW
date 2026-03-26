# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**The Scorpion** — a fast-paced arena combat / hack-and-slash game built in Unity (C#). A masked warrior with dual blades fights through 10 waves of enemies using elemental powers (Fire + Lightning) in a single arena, culminating in a 3-phase boss fight. Visual reference: Zenless Zone Zero.

- **Engine**: Unity 6000.4.0f1 (URP)
- **Framework**: Invector Third Person Controller — Melee Combat Template (v2.6.5)
- **Unity Project Path**: `TheScorption_mvp/cw_1/`
- **Design Docs**: `Project Architechrure/extracted/TheScorpion/docs/`
- **Reference Scripts**: `Project Architechrure/extracted/TheScorpion/Assets/Scripts/` (written WITHOUT Invector — design intent only, not drop-in code)
- **Research Docs**: `docs/research/` (12 files) and `docs/tutorials/` (3 files) — Invector source analysis, community tips, game design references
- **Dev Log**: `DEV_LOG.md` at project root — **MUST read at session start, MUST update after every response**
- **Build Plan**: `~/.claude/plans/joyful-wiggling-melody.md` — 6-day build schedule

## Architecture

### Invector (Purchased Asset — DO NOT MODIFY)

Located in `Assets/Invector-3rdPersonController/`. Provides third-person controller, melee combat, health/stamina, dodge/roll, lock-on, animator, and Simple Melee AI.

**Damage Flow (critical to understand):**
```
Player Input → Animator → vMeleeAttackControl (StateMachineBehaviour)
→ vMeleeManager.SetActiveAttack() → vHitBox.OnTriggerEnter
→ vMeleeManager.OnDamageHit(vHitInfo) → target.TakeDamage(vDamage)
→ vHealthController events: onStartReceiveDamage → onReceiveDamage → onDead
```

**Key Invector hooks (subscribe via events, never modify source):**
- `vMeleeManager.onDamageHit` — when player's attack hits (energy gain, adrenaline, feedback)
- `vHealthController.onStartReceiveDamage` — before damage applied (element resistance)
- `vHealthController.onReceiveDamage` — after damage applied (status effects)
- `vHealthController.onDead` — on kill (wave tracking, adrenaline)
- `vDamage.damageType` — string field used for element identification ("Fire", "Lightning")

**Key Invector classes:**
- `vThirdPersonController` / `vThirdPersonInput` — player movement + camera reference
- `vMeleeCombatInput` / `vMeleeManager` — combat/combos, `onDamageHit` event
- `vHealthController` — health/damage, `onReceiveDamage`/`onDead` events
- `vThirdPersonCamera` — camera system with CameraStateList, `selfRigidbody` property
- Simple Melee AI (`vSimpleMeleeAI_Controller`) — coroutine-based states: Idle/Patrol/Chase/Attack

**AI setup requirements:** Tag=Enemy, Layer=Enemy, NavMesh baked, detection tags include "Player", MeleeManager damage layers include Player, Ragdoll required for hit reactions.

**Critical gotcha:** `vThirdPersonCamera.selfRigidbody` getter calls `AddComponent<Rigidbody>()`. If a Rigidbody already exists, it fails silently and returns null → NullReferenceException → camera never initializes. Fixed by `CameraRigidbodyFix.cs` which pre-assigns the existing Rigidbody via reflection in Awake (ExecutionOrder -100).

### Custom Systems (namespace: `TheScorpion.*`)

All custom scripts in `Assets/Scripts/` use composition — separate MonoBehaviours alongside Invector components, connected via events.

**Player GameObject stack:**
- Invector: `vThirdPersonController`, `vMeleeCombatInput`, `vMeleeManager`, `vHealthController`
- Custom: `ScorpionInputHandler` (Q/E/F/R/V keys), `ElementSystem`, `UltimateSystem`, `StyleMeter`, `DamageInterceptor`, `PlayerDeathHandler`

**Enemy GameObject stack:**
- Invector: Simple Melee AI, `vMeleeManager`, `vHealthController`, NavMeshAgent
- Custom: `EnemyExtension` (loads EnemyDataSO, hooks onDead), `EnemyStatusEffects` (burn/stun), `ElementalDamageProcessor` (element resistance), `EnemyPoiseSystem` (stagger gauge)

**Singleton managers:** `GameManager`, `WaveManager`, `SpawnPointManager`, `AttackQueueManager`, `CameraShakeController`

**Event system:** ScriptableObject event channels (`VoidEventChannelSO`, `IntEventChannelSO`, `FloatEventChannelSO`, `DamageEventChannelSO`) for decoupled communication.

**Data-driven:** `EnemyDataSO`, `WaveDataSO`, `ElementDataSO` ScriptableObjects hold all tunable values.

### Key Design Specs (from GDD)
- **Arena**: 25m × 25m courtyard, 4 spawn points (N/S/E/W)
- **Elements**: Fire (DoT, area denial) and Lightning (speed, CC). One active at a time, switch with Q/E
- **Element Energy**: Max 100, regen 3/sec + 5 per hit
- **Adrenaline**: +2 per hit, +5 per kill, +10 per combo finisher. Ultimate at 100: 8s time-slow + damage boost
- **Poise/Stagger**: Hidden gauge per enemy. Lightning +20 disruption, Fire +5. Break = 1.5s stagger window
- **Style Meter**: D/C/B/A/S ranks multiplying Adrenaline gain
- **Boss Phases**: Phase 1 (100-60% HP) sword combos + summons, Phase 2 (60-30%) fire aura, Phase 3 (30-0%) enraged

## MCP Tools

- **mcp-unity** — Direct Unity Editor control: get/set GameObjects, components, scenes, execute menu items, read console logs, recompile scripts
- **mac-commander** — Screenshots, click, type, window management
- **mac-mcp-server** — AppleScript tools: activate apps, key combinations, window control

**Unity is on a separate macOS Desktop/Space.** Switch desktops before taking screenshots. Use `mcp-unity` for programmatic interaction without needing the visual.

**Workflow:** Write script → `recompile_scripts` → check for errors → `update_component` to add to GameObjects → `save_scene` → test via Play mode (`key_combination` Cmd+P) → `get_console_logs` to verify.

## Development Rules

- **NEVER modify** files in `Assets/Invector-3rdPersonController/`
- Always extend Invector via events, callbacks, or separate MonoBehaviours
- Use `vDamage.damageType` strings for element identification
- Custom scripts go in `TheScorption_mvp/cw_1/Assets/Scripts/` organized by folder
- Reference scripts in `Project Architechrure/extracted/` are design intent only — rewrite for Invector compatibility
- Prioritize working prototype over polish (deadline: <1 week)
- User preference: autonomous execution — complete full stages without stopping, self-check, proceed to next stage

## Session Continuity (CRITICAL)

- **Read `DEV_LOG.md` at the start of every session** — it has full status of what's built, what's next, and session history
- **Update `DEV_LOG.md` after every substantive response** — append what was done, update checkboxes, note any issues. This is the persistent context across sessions, like auto-save in a game.
- **Read research docs on-demand**, not upfront — `docs/research/` has 12+ files. Only read the ones relevant to the current task to keep context lean. Always check if a research file exists for the topic before implementing.
- **ZZZ (Zenless Zone Zero) is the visual/gameplay reference** — camera feel, combat feel, UI style
