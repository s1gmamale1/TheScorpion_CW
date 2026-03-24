# Unity 3D Game Dev — Reddit & Community Tips

Compiled from r/unity3d, r/gamedev, and game dev communities. Focused on building a fast-paced 3D arena combat game.

---

## 1. General Beginner Tips

- **Use Unity LTS releases for production.** Unity 6 LTS is current recommended (early 2026).
- **Build small projects** after tutorials — a single-screen puzzle or short level forces you to solve problems tutorials never cover.
- **Read Unity's Scripting API docs directly.** Looking up unfamiliar classes/methods is more valuable than any tutorial.
- **Use C# namespaces** to organize code; mirror folder structure to namespace hierarchy.

## 2. Common Mistakes to Avoid

- **No GDD:** Without even a basic GDD, scope creep kills projects. Start small, build fast.
- **Too much logic in Update():** Runs every frame. Move infrequent checks to coroutines or timers.
- **Constantly Instantiating/Destroying:** Creates GC stutters. Use **object pooling**.
- **Default import settings:** Textures imported uncompressed at full res bloat file size. Always configure compression.
- **Messy Hierarchy:** Naming convention + organization from day one.
- **Moving Rigidbody via Transform:** Use forces/velocity instead. Otherwise physics breaks.
- **No version control:** Use Git from the start.
- **Save scenes every 15-20 mins:** Unity crashes happen.
- **Script name vs class name mismatch:** Always rename both together.

## 3. Performance Optimization (3D)

- **Object Pooling:** Pre-instantiate reusable objects (enemies, VFX). 80-90% improvement when spawning 10-20+ objects/sec. Reset AI state, health, animations on pool return. Disable physics before deactivation.
- **LOD (Level of Detail):** Reduce triangles for distant objects automatically.
- **Occlusion Culling:** Exclude objects not visible to camera from rendering.
- **Texture Optimization:** Enable mipmaps for 3D textures. Use appropriate compression.
- **SRP Batcher:** Enable in URP for draw call batching.
- **GPU Instancing:** For many identical objects (enemy waves of same type).
- **Split UI Canvases:** Dynamic HUD elements on one Canvas, static on another. Prevents rebuilding all UI every frame.
- **Profile early and often:** Unity Profiler for CPU, GPU, memory, rendering.
- **Burst Compiler:** Use with Jobs for compute-heavy operations.

## 4. Melee Combat System Tips

- **Get movement right first.** Combat is built on top of locomotion.
- **ScriptableObjects for attack data:** Data-driven combos, editable without code changes.
- **Animation Events for hit detection:** Trigger damage at exact frame weapon connects, not animation start.
- **Combo flow:** Idle > Attack1 > Attack2 > Attack3 > Idle. Miss input window = return to Idle.
- **Animator transitions for combat:** Uncheck "Has Exit Time", transition duration = 0, offset = 0. Snappy, responsive.
- **Attack animations should end near idle pose** to avoid ugly interpolation.
- **Coordinated enemy attacks:** "Free-flow" system — enemies circle player, attack one at a time (Batman Arkham style).
- **Invector-specific:** Extend via partial classes or event hooks. Never modify Invector source directly.

## 5. Wave-Based Arena Game Design

- **Separate WaveManager from SpawnManager:** WaveManager = pacing/progression; SpawnManager = instantiation.
- **Track:** currentWave, enemiesToSpawn, enemiesAlive, enemiesKilled. Dedicated counter for wave completion.
- **Escalation beyond "more enemies":** Increase health, damage, speed, or introduce new types.
- **Stagger spawns:** Don't spawn all at once. Control delay between groups.
- **Cap spawn rates:** Unlimited acceleration = unplayable.
- **Object pool all enemies:** Pre-warm during loading. Separate pools per type.
- **Breathing room between waves:** Brief pause for recovery.

## 6. Project Structure

- **Root folder:** `_TheScorpion/` (underscore pins to top, separates from third-party assets)
- **Inside:** `Scripts/`, `Prefabs/`, `Materials/`, `Animations/`, `Audio/`, `Scenes/`, `UI/`, `VFX/`
- **No spaces in file/folder names.**
- **Use namespaces:** `TheScorpion.Combat`, `TheScorpion.AI`, `TheScorpion.Wave`
- **Keep third-party assets in their own folders.** Never mix your scripts into Invector's hierarchy.

## 7. Animator Controller for Combat

- **State machine:** Idle, Locomotion, Attack1-N, Dodge, Hit Reaction, Death. Sub-state machines for grouping.
- **Disable "Has Exit Time" on combat transitions.** Critical for responsive combat.
- **Animation Events:** Place at exact keyframe for damage, SFX, VFX spawning, combo window open/close.
- **Transition duration = 0** for attack chains. Longer only for returning to idle.
- **Combo window system:** Open window via Animation Event mid-attack. Player input during window = next combo. Window closes = return to idle.

## 8. Enemy AI (FSM)

- **Use Finite State Machine:** Idle, Patrol, Chase, Attack, Stagger, Death. ONE state at a time.
- **Simple approach:** Enum + switch statement. Complex: class-based state pattern (Enter/Execute/Exit).
- **Common transitions:**
  - Idle -> Chase (player detected)
  - Chase -> Attack (in range)
  - Attack -> Chase (out of range)
  - Any -> Stagger (hit)
  - Any -> Death (HP <= 0)
- **Per-type behavior:** Base FSM class, override specific states per enemy type.
- **Attack coordination:** Central "combat manager" controls which enemies attack simultaneously.
- **Reset AI state on pool return.**

## 9. URP Lighting — Dark/Moody Atmosphere

- **Ambient lighting:** Lighting Settings > Environment Lighting Source > "Color" > near-black.
- **Rim lighting:** Shader Graph rim light for silhouettes against dark backgrounds.
- **Baked lighting** for static objects (performance + quality).
- **Strategic point lights:** Pools of light in dark arena for dramatic contrast.
- **Post-processing:** Bloom (fire/lightning glow), Vignette (screen edge darkening), Color Grading (desaturate for grim tone).
- **Element-specific lighting:** Fire = warm orange point lights; Lightning = bright white/blue flash. Animate Light.intensity.

## 10. Invector Tips

- **Never modify Invector source directly.** Extend via inheritance, partial classes, or event hooks.
- **Use Invector's event system:** UnityEvents on vThirdPersonController and vMeleeCombatInput.
- **Lock-On system:** Built into Melee Template. Configure distance/angle for 25m arena.
- **vMeleeAI as base:** Extend with FSM states rather than building AI from scratch.
- **Community add-ons:** ShadesOfInsomnia SpellSystem on GitHub shows how to extend Invector with abilities.

## 11. Elemental Ability System Design

- **ScriptableObjects for elements:** Damage values, DoT params, VFX prefabs, SFX per element.
- **Interaction matrix:** 2D array or dictionary for element-vs-enemy-type interactions.
- **GAS pattern:** Attributes + Tags + Effects + Modifiers for clean element interactions.

## 12. Multi-Phase Boss Fight

- **State machine per phase:** Unique behaviors, attacks, movement per phase. HP thresholds trigger transitions.
- **Build one phase at a time.** Get Phase 1 working completely before Phase 2.
- **Modular systems:** Separate health, state machine, attacks, arena feedback into distinct components.
- **Escalation:** Phase 1 teaches patterns. Phase 2 adds mechanics. Phase 3 combines at higher intensity.
- **Polish:** Music layers per phase, screen shake on big attacks, telegraph warnings.

## Priority for 1-Week Deadline

1. **Don't build what Invector provides.** Focus custom code on ElementSystem, WaveManager, BossAI.
2. **Object pool everything** that spawns/despawns.
3. **ScriptableObjects for data** (waves, elements, attacks).
4. **FSM for all AI** — enum+switch for basic, class-based for boss.
5. **Profile after each major feature.**
6. **Save and commit to Git constantly.**
