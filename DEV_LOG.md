# The Scorpion — Dev Log
> Auto-updated after every response. Read this FIRST at session start.

---

## Project Status: Day 4 of 6
**Last Updated:** 2026-03-28 Session 2 End
**Plan File:** `~/.claude/plans/joyful-wiggling-melody.md`

---

## WORKING RIGHT NOW (Verified in Play Mode)
- Player movement, dodge, sprint (Invector handles it)
- Melee combat — attacks apply active element damageType
- Element switching Q/E (Fire/Lightning)
- Projectiles C key with auto-aim
- Ability 1 (F): Fire Tornado AoE / Lightning Burst AoE+stun
- Ability 2 (R): Fire Aura (burn on melee, orange glow VFX) / Lightning Speed (move+atk boost, blue glow VFX)
- Ultimate V: 8s time-slow + 50% damage + 30% atk speed + elemental burst at end with VFX + camera shake
- Adrenaline system: +2 hit, +5 kill, +10 finisher, +1 on damage taken
- Combo tracking: 3+ hits = finisher. Combo boosts MP/stamina regen.
- Style meter: D→S ranks, variety rewarded
- Camera: ZZZ style, camera shake on hits/finishers
- Wave system: 10 waves, enemy count doubles, max 10 on screen, trickle spawn
- Wave kill tracking + wave progression (WORKING — kills register, Wave CLEARED, next wave starts)
- Dead body cleanup: max 5 corpses, 5s auto-destroy
- Enemies spawn 5-10m around player, face player, force aggro via SetCurrentTarget
- ShadowAcolyteBehavior: sprint in, 2-hit combo, retreat 6m, circle, re-engage
- StoneSentinelBehavior: slow, blocks 40%, 50% light attack reduction
- Invector HUD: HP bar + stamina bar (working, not hidden)
- Melee attacks cost 0 stamina (StaminaFix.cs)
- Minimap: top-right (square — needs round mask)
- Invector watermark logo: DELETED from scene

## CUSTOM HUD STATE (ScorpionHUD.cs)
- Wave announcement (fade in/out on wave change)
- Start screen panel (PreGame state — title, start button, controls, quit)
- Pause menu panel (Esc — resume, restart, quit)
- Game Over panel (DEFEATED — stats + try again/quit)
- Victory panel (VICTORY — stats + play again/quit)
- Invector watermark text hiding
- Invector HUD is NOT hidden (it's running and showing HP/stamina)
- Our canvas sits on top at sortingOrder 10
- All panels code-generated, no prefabs needed

## KEY FILES TO KNOW
| File | What it does |
|------|-------------|
| `Assets/Scripts/UI/ScorpionHUD.cs` | Custom HUD overlay — currently just wave announce + watermark hide |
| `Assets/Scripts/UI/MinimapController.cs` | Top-right minimap camera (square, needs round) |
| `Assets/Scripts/Systems/WaveManager.cs` | 10-wave system, spawn near player, kill tracking, dead body cleanup |
| `Assets/Scripts/Systems/SpawnPointManager.cs` | Spawn point positions (currently unused — WaveManager spawns near player directly) |
| `Assets/Scripts/Combat/DamageInterceptor.cs` | Bridge between Invector combat and our systems — element damage, combo, adrenaline, style |
| `Assets/Scripts/Player/ElementSystem.cs` | Fire/Lightning abilities, projectiles, MP energy |
| `Assets/Scripts/Player/UltimateSystem.cs` | Adrenaline + ultimate activation with VFX |
| `Assets/Scripts/Player/StaminaFix.cs` | Zeros weapon stamina costs at runtime |
| `Assets/Scripts/Player/ScorpionInputHandler.cs` | Custom input: Q/E/F/R/V/C/LeftCtrl/Esc |
| `Assets/Scripts/Enemy/ShadowAcolyteBehavior.cs` | Fast enemy AI behavior |
| `Assets/Scripts/Enemy/StoneSentinelBehavior.cs` | Tank enemy AI behavior |
| `Assets/Scripts/Enemy/EnemyExtension.cs` | Loads SO data onto Invector enemy, hooks onDead |
| `Assets/Scripts/Enemy/EnemyStatusEffects.cs` | Burn DoT, stun |
| `Assets/Scripts/Enemy/ElementalDamageProcessor.cs` | Element resistance, triggers status effects |
| `Assets/Scripts/Enemy/EnemyPoiseSystem.cs` | Hidden stagger gauge |
| `Assets/Scripts/Camera/CameraRigidbodyFix.cs` | Fixes Invector camera Rigidbody bug via reflection |
| `Assets/Scripts/Core/Editor/DataAssetCreator.cs` | Editor tools: create SOs, wire scene, create prefabs, rebalance |
| `Assets/Scripts/Core/Editor/CameraSetupTool.cs` | ZZZ camera values + Rigidbody fix tool |

## SCENE HIERARCHY (key objects)
- `vMeleeController_Inventory` — Player (Invector controller + our custom scripts)
  - `Invector Components/vThirdPersonCamera` — Camera (has CameraRigidbodyFix + CameraShakeController)
  - `Invector Components/UI` — Invector's HUD canvas (HP/stamina sliders, logo deleted)
- `--- Managers ---/` — Parent for manager objects
  - `WaveManager` — wave spawning
  - `SpawnPointManager` — spawn points (4 children: N/S/E/W)
  - `AttackQueueManager` — limits simultaneous attackers
  - `ScorpionHUD` — our custom HUD overlay
  - `Minimap` — minimap camera + display
- `Directional Light`, `Global Volume`, `Ground`, `SpawnPoints` — environment

## SCRIPTABLE OBJECT ASSETS
All at `Assets/ScriptableObjects/`:
- `ElementData/` — Fire_Data.asset, Lightning_Data.asset
- `EnemyData/` — HollowMonk_Data (HP=60), ShadowAcolyte_Data (HP=40), StoneSentinel_Data (HP=120), FallenGuardian_Data (HP=500)
- `WaveData/` — Level1_Waves.asset (no longer used — WaveManager generates internally)
- `Events/` — OnEnemyKilled, OnPlayerDied, OnVictory, OnWaveStart, OnWaveClear, OnElementChanged, OnDamageDealt

## ENEMY PREFABS
At `Assets/Prefabs/Enemies/`:
- `HollowMonk_Prefab.prefab` — Basic gooner (used for basicEnemyPrefab)
- `ShadowAcolyte_Prefab.prefab` — Fast ninja (used for fastEnemyPrefab)
- `StoneSentinel_Prefab.prefab` — Tank (used for heavyEnemyPrefab)
All are clones of same Invector AI model — differentiated by behavior scripts added at spawn time

## WAVE COMPOSITION (WaveManager.cs)
- Wave 1-2: pure basic gooners
- Wave 3+: 1 tank + 1 more per 2 waves (wave 3=1, 5=2, 7=3, 10=4)
- Wave 5+: 1 ninja + 1 more per 2 waves (wave 5=1, 7=2, 9=3, 10=3)
- Rest filled with basic gooners
- Enemy count: +2 per wave: 3, 5, 7, 9, 11, 13, 15, 17, 19, 21
- Max 10 on screen at once

## DAMAGE BALANCE
| Source | Damage |
|--------|--------|
| Melee (Invector) | ~10/hit |
| Projectile (C) | 5 |
| Fire Tornado (F) | 8/tick × 3s |
| Lightning Burst (F) | 10 AoE |
| Fire Aura burn (R) | 3/tick |
| Ultimate burst Fire | 30 |
| Ultimate burst Lightning | 20 + 2s stun |

## TODO — NEXT SESSION PRIORITIES

### 1. HUD Elements (add to ScorpionHUD.cs incrementally)
- [ ] Wave counter persistent text (top-center after announcement shrinks)
- [ ] Round minimap (add circular mask to MinimapController)
- [ ] Combo counter under minimap (3+ hits, shows "5x COMBO")
- [ ] Adrenaline bar (bottom-center)
- [ ] MP/Energy bar
- [ ] Element indicator
- [ ] Ability cooldowns
- [ ] Style rank display

### 2. Enemy Types
- [ ] Elemental Ninja (throws projectiles, buffs allies, heals tanks)
- [ ] Different Mixamo models per enemy type (user will provide FBX files)

### 3. Boss AI
- [ ] BossController + BossPhaseManager
- [ ] Phase 1 (100-60%): sword combos + summon monks
- [ ] Phase 2 (60-30%): fire aura + summon acolytes
- [ ] Phase 3 (30-0%): enraged, no summons

### 4. Game Flow
- [x] Start screen (PreGame state, START GAME button, controls hint, QUIT)
- [x] Pause menu (Esc → PAUSED overlay, Resume/Restart/Quit buttons, cursor shown)
- [x] Game Over screen (DEFEATED + wave/kills/time stats, TRY AGAIN/QUIT)
- [x] Victory screen (VICTORY + kills/time stats, PLAY AGAIN/QUIT)
- [x] Cursor management (shown on menus, hidden during gameplay)
- [x] GameManager state machine (PreGame→Playing→Paused/GameOver/Victory)
- [x] Kill + time tracking for stats display

### 5. Polish
- [ ] Damage numbers
- [ ] Hit stop
- [ ] Weapon trails

## CRITICAL RULES
- **NEVER modify** `Assets/Invector-3rdPersonController/` files
- **Read research docs on-demand** from `docs/research/` — don't bulk-read
- **Update this DEV_LOG.md after every response**
- **Use MCP Unity** for scene/component changes, not manual editor
- **Invector HUD** is kept running — add custom elements as overlay, don't hide/disable it
- **Test step by step** — compile, play, verify, then next step

## SESSION HISTORY

### Session 1 — 2026-03-25
- Created DEV_LOG.md, fixed camera, built Day 3 scripts, wired scene

### Session 2 — 2026-03-27/28
- Ability VFX (code-generated fire/lightning particles)
- HUD font fix (LegacyRuntime), Invector watermark deleted
- Wave system redesign (doubling, max 10, trickle spawn)
- Wave tracking fix (integer counter, not GameObject list)
- Spawn near player + ForceAggroPlayer
- Minimap, wave announcement, combo counter, MP bar (various attempts)
- ShadowAcolyte + StoneSentinel behaviors
- Ultimate VFX overhaul
- Damage rebalance
- StaminaFix (melee = 0 stamina)
- HUD: tried custom HP bar (didn't work with Invector), reverted to Invector HUD
- Fixed RequireComponent error, dead body cleanup
- Wave system CONFIRMED WORKING: spawn → fight → kill → wave cleared → next wave

### Session 3 — 2026-03-28
- Added PreGame state to GameState enum
- Rebuilt GameManager: state machine with enter/exit, cursor management, kill/time stats, OnGameStateChanged event
- Built 4 UI panels in ScorpionHUD (all code-generated, no prefabs):
  - Start screen: title + subtitle + START GAME button + controls hint + QUIT
  - Pause menu: PAUSED + Resume/Restart/Quit buttons
  - Game Over: DEFEATED + wave/kills/time stats + TRY AGAIN/QUIT
  - Victory: VICTORY + kills/time stats + PLAY AGAIN/QUIT
- WaveManager: autoStartWaves=false, added StartFirstWave(), TotalKillsAllWaves tracking
- PlayerDeathHandler: cleaned up — removed manual Esc restart, HUD handles it now
- ScorpionInputHandler: blocks all input during PreGame state
- Compiled successfully (0 errors, warnings are all Invector deprecation)
- Changed wave progression: +2 per wave (3,5,7,...,21) instead of doubling
- Scaled down guaranteed tank/ninja counts to match smaller waves
- Added MP/Energy bar to HUD (bottom-left, blue fill bar, reads from ElementSystem)
- NEEDS TESTING: wire onEnemyKilledEvent to GameManager in Inspector

## KNOWN ISSUES
- "The referenced script (Unknown)" warning — orphaned component on player, harmless
- Invector deprecation warnings — harmless
- StaminaFix finds 0 weapons at start (InvokeRepeating checks every 2s)
- All enemy prefabs look identical (same 3D model) — waiting for Mixamo models
