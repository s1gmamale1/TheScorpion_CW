# Combat & Gameplay Mechanics — Implementation Guide

Comprehensive tutorials and patterns for building a hack-and-slash arena combat game in Unity.

---

## 1. Melee Combat System (Combos, Hit Detection, Damage)

### Combo System via Animator FSM
- Animation states for each attack in combo chain
- Transitions using trigger parameters (`NextAttack`)
- Uncheck "Has Exit Time" for responsive input
- Each attack state returns to idle pose at end of clip

### Hit Detection
- **Hitbox colliders** activated during specific animation frames via Animation Events
- Attach trigger collider to weapon, enable/disable via `EnableHitbox`/`DisableHitbox`
- Alternative: SphereCast/BoxCast from weapon during active frames

### Damage Pipeline
- Interface `IDamageable` with `TakeDamage(DamageInfo info)`
- `DamageInfo` struct: amount, element type, knockback direction, attacker reference
- Decouples damage dealing from receiving

### Invector Integration
- Hook into `vMeleeManager.onDamageHit` and `onAttack` events
- Layer elemental damage and combo tracking on top — don't replace hit detection

---

## 2. Enemy AI State Machine

### Simple: Enum + Switch
```csharp
enum EnemyState { Idle, Patrol, Chase, Attack, Retreat, Death }
// switch in Update() routes to behavior methods
```

### Scalable: State Pattern
- Each state is a class implementing `IState` with `Enter()`, `Execute()`, `Exit()`
- `StateMachine` holds current state and calls these methods
- Recommended for 3 enemy types — share states, customize transitions

### Transition Conditions
- Distance checks (detection range → chase, attack range → attack)
- Health thresholds (retreat when low)
- Line-of-sight raycasts

### Per-Enemy-Type Customization
| Enemy | Behavior |
|-------|----------|
| **Hollow Monk** | Simple Chase → Attack loop |
| **Shadow Acolyte** | Adds Strafe and Retreat states |
| **Stone Sentinel** | Longer wind-ups, Block state |

---

## 3. Wave Spawner System

### Wave Data Structure
```csharp
[System.Serializable]
public class Wave {
    public GameObject[] enemyPrefabs;
    public int enemyCount;
    public float spawnRate;
    public float timeBetweenWaves;
}
```
Use ScriptableObject for designer-friendly editing.

### Coroutine-Based Spawning
- Iterate through waves, spawn at intervals with `yield return new WaitForSeconds(spawnRate)`
- Track alive count; when zero → start next wave after delay

### Spawn Points
- Empty GameObjects at N/S/E/W positions
- Random selection or round-robin
- Ensure enemies don't all come from one direction

### Difficulty Progression
- Wave 1-3: Hollow Monks only
- Wave 4-6: Mix in Shadow Acolytes
- Wave 7-9: Add Stone Sentinels
- Wave 10: Boss trigger

---

## 4. Multi-Phase Boss Fight

### Health-Threshold Phase Transitions
- `OnHealthPercentChanged` event
- Below threshold (60%, 30%) → `TransitionToPhase(nextPhase)`
- Optional invulnerability during transition + cinematic animation

### Phase as State Machine
| Phase | HP Range | Behavior |
|-------|----------|----------|
| Phase 1 | 100-60% | Sword combos + periodic summons |
| Phase 2 | 60-30% | Fire aura (continuous AoE), wave attacks |
| Phase 3 | 30-0% | Enraged, faster attacks, no pauses |

### Attack Pattern Sequencing
- Each phase has list of attack ScriptableObjects
- Boss cycles through or picks weighted-random
- Each attack SO defines: animation trigger, damage, range, cooldown, spawns

---

## 5. Elemental Damage System (Fire / Lightning)

### Element Enum + DamageInfo
```csharp
enum ElementType { None, Fire, Lightning }
// Include in DamageInfo struct
// Receiver checks resistances/weaknesses
```

### Status Effects
- **Fire** → `BurnEffect`: X dmg every 0.5s for 3s + movement slow
- **Lightning** → `ShockEffect`: stun for 1s, no knockback on heavy
- `StatusEffectManager` component on each entity holds/ticks active effects

### Element Switching
- `currentElement` field, Q/E keys toggle
- Switching changes: weapon VFX, ability set, element type on outgoing damage
- Event `OnElementSwitch` notifies HUD and VFX

### Energy System
```csharp
float elementEnergy; // max 100
// Regen 3/sec via Update()
// +5 per hit via combat event callback
// Abilities consume energy
```

### Enemy Interaction Matrix
```csharp
float[,] damageMultiplier; // indexed by [attackElement, enemyType]
// Fire vs Fast = 1.5x + guaranteed burn
// Lightning vs Heavy = 1.0x + stun, no knockback
```

---

## 6. Ultimate Ability (Adrenaline Rush)

### Adrenaline Meter
- Float 0-100
- +2 per hit (`onDamageHit`), +5 per kill (`onEnemyDeath`), +10 per combo finisher
- At 100 → ultimate available

### Time-Slow Implementation
```csharp
Time.timeScale = 0.3f;
Time.fixedDeltaTime = 0.02f * Time.timeScale; // CRITICAL for physics
// Player uses Time.unscaledDeltaTime for normal-speed movement
```

### Duration Management
```csharp
yield return new WaitForSecondsRealtime(8f); // NOT WaitForSeconds
Time.timeScale = 1f;
Time.fixedDeltaTime = 0.02f;
```

### Damage Boost
- During ultimate: multiply outgoing damage by 1.5x
- Apply via `damageMultiplier` field in damage pipeline

---

## 7. HUD (Health Bar, Cooldowns, Energy)

### Health Bar
- UI `Slider` or `Image` (fill type) bound to `currentHealth / maxHealth`
- Delayed-damage effect: lerp secondary "damage bar" trailing behind actual health

### Cooldown Indicators
- `Image.Type.Filled` with `fillMethod = Radial360`
- Update `fillAmount = currentCooldown / maxCooldown` each frame
- Darkened ability icon overlay

### Canvas Optimization
- Static elements on one Canvas, dynamic on another
- Cache all component references in `Awake()`

---

## 8. VFX / Particle Effects

### Hit Effects
- Short-burst particle system (0.2-0.5s lifetime)
- Spawn at hit point via object pool
- `Additive` shader for bright impacts

### Fire VFX
- Spritesheet + `Texture Sheet Animation` module
- Orange/yellow → red color over lifetime
- `Emission` module for continuous fire on burning enemies

### Lightning VFX
- `Line Renderer` with randomized points for bolt
- Particle system for sparks at impact
- Or shader-based for stylized look

### Always Pool VFX
- `ParticleSystem.Stop()` and return to pool, never Destroy

---

## 9. Screen Shake / Camera Effects

### Cinemachine Impulse (Recommended)
- `CinemachineImpulseSource` (emits shake)
- `CinemachineImpulseListener` (on virtual camera)
- `Raw Signal` (noise profile)

### Triggering
```csharp
impulseSource.GenerateImpulse(velocity);
// Light hit = small, heavy hit = large, boss = massive
```

### Game Feel Extras
- **Hit-stop**: Freeze frame 0.05s on hit (`Time.timeScale = 0` briefly)
- **Chromatic aberration** post-processing on big hits
- **Slight zoom** on critical strikes

---

## 10. NavMesh Enemy Pathfinding

### Setup
- Bake NavMesh on arena floor
- `NavMeshAgent` on enemies with per-type speed/stopping distance

### Chase Behavior
- `agent.SetDestination(player.position)` every 0.2-0.5s (not every frame)
- `agent.isStopped = true` during attacks

### Avoidance Priority
- Boss: 0 (highest), Heavy: 10, Standard: 50
- Prevents enemies blocking each other

### Arena-Specific
- 25x25m arena = simple NavMesh
- Pillars as `NavMeshObstacle` with carving for dynamic paths

---

## 11. Animator / Blend Trees

### Locomotion Blend Tree
- 2D Freeform Directional with `MoveX`/`MoveZ` floats
- Maps: idle (0,0), forward (0,1), strafe left (-1,0), right (1,0), back (0,-1)

### Attack Sub-State Machine
- Sub-state for attacks with trigger-based transitions
- Chain combos via `NextAttack` trigger in input window

### Layer Setup
- Base Layer = locomotion
- Override Layer = upper body attacks (avatar mask, legs keep moving)
- Additive Layer = hit reactions

---

## 12. Object Pooling

### Unity Built-in (2021+)
```csharp
UnityEngine.Pool.ObjectPool<T>
// Get() and Release() with configurable capacity
// Callbacks: createFunc, actionOnGet, actionOnRelease, actionOnDestroy
```

### Pool Manager Pattern
- Central singleton with dictionaries keyed by prefab
- `PoolManager.Instance.Spawn(prefab, position, rotation)` instead of `Instantiate`

### Pre-warming
- During loading/wave countdown, pre-instantiate expected max enemies

### What to Pool
- Enemies, VFX particles, damage numbers, audio sources
- Anything created/destroyed frequently

### Performance Impact
- Up to 90% reduction in frame time spikes
- Eliminates most GC allocations from spawning

---

## Recommended Build Order for The Scorpion

1. **GameManager + WaveManager** — Core game loop, enemy spawning with object pooling
2. **Enemy AI (FSM)** — State pattern + NavMesh. Start with Hollow Monk, clone for others
3. **ElementSystem** — Fire/Lightning switching, hook into Invector's `onDamageHit`
4. **UltimateSystem** — Adrenaline meter, time-slow via `Time.timeScale`
5. **BossAI** — Extended FSM with phase transitions
6. **HUD** — Bind UI to systems via events (not polling)
7. **VFX + Game Feel** — Particles, screen shake, hit-stop (polish but makes it feel great)
