# The Scorpion — Dev Log
> Auto-updated after every response. This is the persistent context file across sessions.

---

## Project Status: Day 4 of 6
**Last Updated:** 2026-03-28
**Plan File:** `~/.claude/plans/joyful-wiggling-melody.md`

---

## DONE

### Core Systems
- [x] GameManager (state, pause, restart, SetGameState)
- [x] ScorpionEnums (ElementType, GameState, EnemyType, StyleRank)
- [x] Event Channels (Void, Float, Int, Damage SO channels)
- [x] Editor Tools (PlayerSetup, CameraSetup, NavMeshBaker, DataAssetCreator, Rebalance)

### Player / MC (COMPLETE)
- [x] ScorpionInputHandler — Q/E/F/R/V/C/LeftCtrl/Esc input
- [x] ElementSystem — Fire/Lightning switching, MP energy, projectiles (C key, auto-aim), Fire Tornado AoE, Lightning Burst AoE+stun, Fire Aura buff (burn on melee), Lightning Speed buff (move+atk speed)
- [x] UltimateSystem — Adrenaline meter, 8s time-slow, +50% damage, +30% attack speed, elemental burst at end with VFX + camera shake
- [x] DamageInterceptor — melee applies element damageType, combo tracking (3+ = finisher), style meter integration, Fire Aura burn-on-hit, ultimate damage multiplier, combo regen bonus (MP + stamina)
- [x] StyleMeter — D/C/B/A/S ranks, variety rewarded, drops on hit
- [x] PlayerDeathHandler
- [x] Melee attacks cost 0 stamina (only sprint/dodge drain)

### Camera
- [x] CameraRigidbodyFix — pre-assigns Rigidbody via reflection (fixes Invector NullRef)
- [x] ZZZ-style camera values (smoothRot=20, centered, FOV=55, autoBehind)
- [x] CameraShakeController — attack/hit/heavy/custom shake profiles

### Enemy Systems
- [x] EnemyExtension — loads EnemyDataSO, hooks onDead for wave tracking
- [x] EnemyStatusEffects — burn DoT (with NavMesh guard), stun (uses Invector TriggerReaction)
- [x] ElementalDamageProcessor — per-enemy element resistance, triggers status effects + poise
- [x] EnemyPoiseSystem — hidden stagger gauge (Lightning +20, Fire +5, break = stagger)
- [x] AttackQueueManager — limits 2-3 simultaneous attackers

### Enemy AI Behaviors
- [x] ShadowAcolyteBehavior — sprint in, 2-hit combo, retreat 6m, circle, re-engage
- [x] StoneSentinelBehavior — slow tank, blocks 40%, 50% light attack reduction, heavy hits

### Wave System
- [x] WaveManager — 10 waves, enemy count doubles (3→6→12→24...), max 10 on screen, continuous trickle spawn
- [x] Wave composition: W1-2 pure basic, W3+ tanks (wave# count), W5+ elemental ninjas (wave# count), rest basic
- [x] SpawnPointManager — spawns 5-10m around player, NavMesh snap
- [x] ForceAggroPlayer — SetCurrentTarget on spawn for instant aggro
- [x] Enemy prefabs (HollowMonk, ShadowAcolyte, StoneSentinel) created and wired

### UI/HUD
- [x] ScorpionHUD — runtime-generated, bold text + shadows
- [x] Adrenaline bar (pulses yellow when full, "ULTIMATE READY" label)
- [x] MP bar (bottom-left, element-colored, labeled "MP")
- [x] Element indicator (FIRE/LIGHTNING with color)
- [x] Ability cooldowns (bottom-right, [F] and [R] with names/timers)
- [x] Style rank (top-right, D→S with colors, S = rainbow)
- [x] Wave counter (under minimap)
- [x] Wave announcement — center screen fade-in/out with scale punch ("WAVE 1", "FINAL WAVE" in red)
- [x] Combo counter — right-of-center, shows "5x MP +100%", pulses, color shifts
- [x] Invector watermark hidden
- [x] MinimapController — top-right, top-down orthographic camera, green player dot

### Data Assets (ScriptableObjects)
- [x] Fire_Data, Lightning_Data (rebalanced: tornado 8/tick, burst 10, burst ultimate 30/20)
- [x] HollowMonk (HP=60), ShadowAcolyte (HP=40), StoneSentinel (HP=120), FallenGuardian (HP=500)
- [x] Level1_Waves, all event channels

### Damage Rebalance (applied)
- Melee ~10/hit, Projectile 5, Fire Tornado 8/tick, Lightning Burst 10, Fire Burst 30, Lightning Burst 20
- Monk HP=60, Acolyte HP=40, Sentinel HP=120, Boss HP=500

---

## TODO

### Immediate Priority
- [ ] Test wave progression end-to-end (W1→W10)
- [ ] Fix any remaining NavMesh spawn issues
- [ ] Elemental Ninja enemy type (projectile-throwing, buff allies, heal tanks)

### Boss AI (Day 4)
- [ ] BossController.cs — 3-phase Fallen Guardian
- [ ] BossPhaseManager.cs — HP threshold transitions (100-60%, 60-30%, 30-0%)
- [ ] Phase 1: sword combos + summon 2 monks every 30s
- [ ] Phase 2: fire aura + ground slam fire wave + summon acolyte every 20s
- [ ] Phase 3: enraged +30% speed/damage, 360° spin slash, no summons
- [ ] BossHealthBarUI

### Game Flow
- [ ] Pause menu (Esc)
- [ ] Game Over screen (player death)
- [ ] Victory screen (all waves cleared)
- [ ] Restart functionality

### Polish (Day 5-6)
- [ ] Damage numbers popup
- [ ] Hit stop (frame freeze on heavy hits)
- [ ] Weapon trails
- [ ] Post-processing effects during Ultimate
- [ ] Sound effects
- [ ] Full playtest + build

---

## Session History

### Session 1 — 2026-03-25
- Recovered context, created DEV_LOG.md
- Fixed camera (CameraRigidbodyFix), ZZZ style values
- Built Day 3 enemy/wave scripts, wired scene
- DamageInterceptor overhaul, UltimateSystem fix, ScorpionHUD created

### Session 2 — 2026-03-27/28 (Current)
- Fixed ability VFX (removed broken Invector particles, code-generated fire/lightning particles)
- Fixed HUD font crash (Arial→LegacyRuntime), bar positioning, Invector watermark removal
- Redesigned wave system: doubling enemy count, max 10 on screen, trickle spawn
- Fixed wave tracking (integer counter instead of GameObject list)
- Spawn near player (5-10m circle) + ForceAggroPlayer
- Added minimap (top-right, orthographic camera)
- Wave announcement (center screen fade-in/out)
- Built ShadowAcolyteBehavior (hit-and-run) and StoneSentinelBehavior (tank)
- Wave composition: W1-2 basic, W3+ tanks, W5+ elemental ninjas
- Ultimate VFX overhaul: activation flash/particles, golden aura during, elemental burst VFX + heavy shake
- Damage rebalance: enemy HP doubled, ability damage halved
- Stamina: melee attacks free, only sprint/dodge drain
- MP bar + combo counter with regen bonus display
- Pushed to GitHub

---

## Known Issues / BUGS TO FIX NEXT SESSION
- **Wave progression not working** — enemies spawn but `OnEnemyDied()` never fires. No kill logs appear. Either `onDead` listener not connecting to spawned clones, or enemies aren't actually dying. CRITICAL — debug this first.
- **Wave announcement not appearing** — HUD code is correct but may not trigger if WaveManager.CurrentWave doesn't change (tied to wave bug above)
- **Minimap still square** — needs circular mask on the RawImage
- **StaminaFix found 0 weapons** — weapons may load via Invector inventory system later. InvokeRepeating handles it but needs verification.
- **"The referenced script (Unknown)" warning** — orphaned component reference on player
- **All 3 enemy prefabs look identical** — same 3D model, differentiated only by behavior scripts and stats
- **HUD approach decided**: Keep Invector's HUD for HP/Stamina (it works). Add custom elements as overlay. Invector watermark logo deleted from scene.
- Invector deprecation warnings (FindObjectsSortMode etc) — harmless

## NEXT SESSION PRIORITIES
1. Debug wave system — why aren't enemy kills registering? Add logging to SpawnEnemy and verify onDead hooks
2. Fix wave announcement display
3. Make minimap round
4. Add combo counter under minimap
5. Add adrenaline bar bottom center
6. Continue enemy AI differentiation
