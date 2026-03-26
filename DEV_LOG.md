# The Scorpion — Dev Log
> Auto-updated after every response. This is the persistent context file across sessions.

---

## Project Status: Day 3 of 6 (Starting)
**Last Updated:** 2026-03-25
**Plan File:** `~/.claude/plans/joyful-wiggling-melody.md`

---

## What's Built (Day 1-2 Complete)

### Core (Assets/Scripts/Core/)
- [x] `GameManager.cs` — Game state, pause, restart
- [x] `ScorpionEnums.cs` — Shared enums
- [x] `EventChannels/VoidEventChannelSO.cs` — Parameterless events
- [x] `EventChannels/FloatEventChannelSO.cs` — Float events
- [x] `EventChannels/IntEventChannelSO.cs` — Int events
- [x] `EventChannels/DamageEventChannelSO.cs` — Damage events
- [x] `Editor/PlayerSetupTool.cs` — Auto-setup player components
- [x] `Editor/CameraSetupTool.cs` — Camera rig setup
- [x] `Editor/NavMeshBaker.cs` — NavMesh baking utility
- [x] `Editor/DataAssetCreator.cs` — Creates SO asset instances

### Data (Assets/Scripts/Data/)
- [x] `ElementDataSO.cs` — Fire/Lightning config
- [x] `EnemyDataSO.cs` — Enemy stats template
- [x] `WaveDataSO.cs` — Wave composition data

### Player (Assets/Scripts/Player/)
- [x] `ScorpionInputHandler.cs` — Q/E/F/R/V custom input (coexists with Invector)
- [x] `ElementSystem.cs` — Fire/Lightning switching, energy, abilities
- [x] `UltimateSystem.cs` — Adrenaline meter, time-slow ultimate
- [x] `StyleMeter.cs` — DMC-style variety rank (D/C/B/A/S)
- [x] `PlayerDeathHandler.cs` — Death flow

### Combat (Assets/Scripts/Combat/)
- [x] `DamageInterceptor.cs` — Hooks vMeleeManager.onDamageHit, bridges element/adrenaline/energy

### Enemy (Assets/Scripts/Enemy/)
- [x] `EnemyStatusEffects.cs` — Burn DoT, stun, element reactions

### VFX (Assets/Scripts/VFX/)
- [x] `CameraShakeController.cs` — Screen shake profiles

---

## What's Next (Day 3)

### Priority: Waves + Combat Systems
- [x] `WaveManager.cs` — 10-wave spawn progression using WaveDataSO
- [x] `SpawnPointManager.cs` — N/S/E/W spawn point logic with random offset
- [ ] `ObjectPoolManager.cs` — Enemy pooling (deferred — Instantiate/Destroy works for MVP)
- [x] `AttackQueueManager.cs` — Limits 2-3 simultaneous attackers (singleton)
- [x] `EnemyPoiseSystem.cs` — Hidden stagger gauge (Lightning +20, Fire +5, break = stagger)
- [x] `EnemyExtension.cs` — Loads EnemyDataSO onto Invector, hooks onDead for wave tracking + adrenaline
- [x] `ElementalDamageProcessor.cs` — Per-enemy element resistance + triggers EnemyStatusEffects
- [ ] Configure 3 enemy prefabs via MCP Unity
- [ ] Create WaveDataSO asset with 10 wave definitions
- [ ] Create EnemyDataSO assets (HollowMonk, ShadowAcolyte, StoneSentinel)
- [ ] Set up spawn points in scene + WaveManager/SpawnPointManager/AttackQueueManager GameObjects

### Day 4: Boss + Ability 2
- [ ] `BossController.cs` — 3-phase Fallen Guardian
- [ ] `BossPhaseManager.cs` — HP threshold transitions
- [ ] Fire Aura buff, Lightning Speed buff
- [ ] Enemy behavior polish

### Day 5: UI/HUD
- [ ] `ScorpionHUD.cs` — All HUD elements
- [ ] `BossHealthBarUI.cs`
- [ ] `DamagePopupManager.cs`
- [ ] `StyleMeterUI.cs`
- [ ] Pause/GameOver/Victory panels

### Day 6: Polish
- [ ] HitStopController, HitFeedbackController
- [ ] WeaponTrailController, PerfectDodgeDetector
- [ ] VFX polish, post-processing
- [ ] Full playtest + build

---

## Architecture Notes
- **Composition over inheritance** — custom MonoBehaviours sit alongside Invector, hook via events
- **Key Invector hooks:** `vMeleeManager.onDamageHit`, `vHealthController.onStartReceiveDamage/onDead`, `vSimpleMeleeAI_Controller.canAttack/lockMovement`
- **Invector Q remap:** rollInput must be remapped from Q to LeftAlt in Inspector
- **Reference scripts** in `Project Architechrure/extracted/` are design intent only (no Invector), must be rewritten

---

## Session History

### Session — 2026-03-25 (Current)
- Recovered context after /reset wiped previous session
- Previous session built Day 1-2 scripts but never created this log file
- Created DEV_LOG.md and memory entries for persistence
- Re-read all 5 Invector research/tutorial docs (source analysis, online docs, deep integration, core tutorials, AI tutorials)
- Fully loaded on Invector architecture, damage flow, AI system, extension patterns
- Ready to start Day 3 work
- **FIXED: Camera not following player** — duplicate Rigidbody on vThirdPersonCamera blocked Invector's Init(). Created `CameraRigidbodyFix.cs` (runs in Awake with ExecutionOrder -100) that pre-assigns existing Rigidbody to Invector's private `_selfRigidbody` field via reflection. NullRef gone, camera initializes.
- Updated ZZZ-style camera values (smoothRot=20, xSens=4, ySens=2.5, dist=3.2, FOV=55, autoBehind=ON)

---

## Known Issues / Blockers
- None currently tracked (previous session context lost)
- Need to verify all Day 1-2 scripts compile clean in Unity before proceeding
