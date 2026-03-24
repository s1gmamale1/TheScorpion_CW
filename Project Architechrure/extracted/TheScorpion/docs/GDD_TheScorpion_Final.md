# THE SCORPION — Game Design Document (Final)
## Fast-Paced Arena Combat Action Game

**Engine:** Unity | **Language:** C# | **Platform:** Windows | **Target:** MVP Prototype

---

## 1. Concept

### Premise
The Scorpion is a masked warrior — a former shrine guardian betrayed and left for dead by a corrupted order of elemental monks. Resurrected by a forbidden ritual, he returns to the sacred shrines to reclaim his stolen elemental powers and destroy the order that betrayed him.

### Genre
Action / Arena Combat / Hack-and-Slash

### Core Fantasy
You are an unstoppable blade-wielding force cutting through waves of enemies, switching between elemental powers mid-combo to devastate everything in the arena.

### MVP Scope (2 Weeks)
- 1 playable level (Shrine Entrance) with wave system + boss
- Player: movement, dodge, dual-blade combos, 2 elements (Fire + Lightning)
- 3 enemy types (Basic, Fast, Heavy) + 1 Boss
- Adrenaline ultimate system
- Full HUD (health, adrenaline, element indicator, wave counter)

### Deferred (Post-MVP)
- Water & Wind elements
- Element combination system
- Levels 2 & 3
- Elite enemies
- Blade/armor upgrade progression
- Audio/particle polish

---

## 2. Player Character

### Identity
**Name:** The Scorpion
**Look:** Dark armored figure, green glowing mask, dual blades
**Animation Source:** Mixamo (idle, run, dodge, attack anims)

### Movement
| Action | Input | Details |
|--------|-------|---------|
| Move | WASD / Left Stick | 8-directional, speed = 7 units/sec |
| Sprint | Left Shift / L3 | Speed = 11 units/sec, drains stamina |
| Dodge | Space / B button | i-frames (0.2s), 5m dash, 0.8s cooldown |
| Jump | Not in MVP | Deferred — flat arenas only |

### Combat
**Weapon:** Dual Blades (always equipped)

**Attack Types:**
| Attack | Input | Damage | Speed |
|--------|-------|--------|-------|
| Light | Left Click / X | 10 | Fast (0.3s) |
| Heavy | Right Click / Y | 25 | Slow (0.7s) |
| Combo Finisher | After 3+ chain | 35 | Medium (0.5s) |

**Combo System:**
Attacks chain if input within 0.6s window. Each hit increments combo counter. At 3+ hits, next attack becomes a finisher with bonus damage and knockback.

| Combo | Sequence | Effect |
|-------|----------|--------|
| Blade Rush | L → L → L | Fast 3-hit, finisher = wide slash |
| Crushing Chain | L → L → H | 2 fast + heavy slam, knockdown |
| Power Opener | H → L → L | Armor-break into rapid follow-up |

**Disarm Mechanic (MVP-lite):**
Heavy enemy slam attacks have 30% chance to trigger a 1.5s "stagger" state where the player deals 50% reduced damage (visual: blades flicker). Not a full disarm — simplified for MVP.

---

## 3. Elemental System (MVP: Fire + Lightning)

### Rules
- One element active at a time
- Switch with Q/E or D-pad Left/Right
- Each element has 2 abilities on cooldown
- Abilities cost elemental energy (regenerates over time + on hit)

### Fire
**Theme:** Damage over time, area denial

| Ability | Input | Cooldown | Cost | Effect |
|---------|-------|----------|------|--------|
| Fire Tornado | Ability 1 (F / RB) | 8s | 40 energy | AoE spin, 15 dmg/tick for 3s, radius 4m |
| Fire Aura | Ability 2 (R / LB) | 12s | 30 energy | 6s buff, all attacks add 5 burn dmg/tick for 2s |

### Lightning
**Theme:** Speed, crowd control

| Ability | Input | Cooldown | Cost | Effect |
|---------|-------|----------|------|--------|
| Lightning Burst | Ability 1 (F / RB) | 6s | 35 energy | Explosion at player pos, 20 dmg + 1.5s stun, radius 3m |
| Lightning Speed | Ability 2 (R / LB) | 15s | 50 energy | 5s buff, +40% attack speed, +25% move speed |

### Element Energy
- Max: 100
- Regen: 3/sec passive, +5 per hit landed
- Switching elements does NOT reset energy

### Deferred Elements

**Wind** (Post-MVP):
- Wind Slash: Ranged projectile, 12 dmg, pierces enemies
- Wind Jump: Dash upward + forward, AoE landing

**Water** (Post-MVP):
- Water Heal: Regen 5 HP/sec for 6s
- Water Shield: 50% damage reduction for 4s

### Element Combination Matrix (Post-MVP)
| Combo | Effect | Description |
|-------|--------|-------------|
| Fire + Lightning | Overcharge | Lightning stun spreads burn to nearby enemies |
| Fire + Wind | Firestorm | Wind Slash becomes flaming projectile, 2x damage |
| Fire + Water | Steam Cloud | AoE blind, enemies miss 50% for 4s |
| Lightning + Wind | Thunderclap | Wind dash leaves chain lightning trail |
| Lightning + Water | Electrocute | Stunned enemies take 3x water ability damage |
| Wind + Water | Frozen Gale | Slow enemies 60% in AoE for 5s |

---

## 4. Ultimate System

### Adrenaline Meter
- Max: 100
- Gain: +2 per hit dealt, +5 per kill, +10 per combo finisher
- Drain: None (persists until used)

### Adrenaline Rush (Ultimate)
**Input:** V / both triggers
**Duration:** 8 seconds
**Effects:**
- Time slows 50% (player moves at normal speed)
- +50% damage
- +30% attack speed
- Screen vignette + color shift (visual only)
- Ends with Elemental Burst: massive AoE based on active element

| Element | Burst Effect |
|---------|-------------|
| Fire | Ring of fire, 60 dmg to all enemies in 8m |
| Lightning | Chain lightning hits all enemies on screen, 40 dmg + 2s stun |

---

## 5. Enemy Design

### Enemy-Element Interactions
| Enemy | Fire | Lightning |
|-------|------|-----------|
| Basic | Normal damage | Normal damage, full stun |
| Fast | Burn slows them (good counter) | Short stun (0.5s only) |
| Heavy | Resist burn (50% less tick) | Full stun but no knockback |

### Basic Enemy — Hollow Monk
**HP:** 30 | **Damage:** 8 | **Speed:** 3 units/sec
- Slow approach, single-swing melee attack
- 1s wind-up (telegraphed), 0.8s recovery
- Spawns in groups of 3-5
- No special abilities

### Fast Enemy — Shadow Acolyte  
**HP:** 20 | **Damage:** 12 | **Speed:** 8 units/sec
- Dashes toward player, 2-hit combo
- Retreats after attacking (hit-and-run pattern)
- 0.4s wind-up (hard to react)
- Vulnerable during retreat (1s window)

### Heavy Enemy — Stone Sentinel
**HP:** 80 | **Damage:** 20 | **Speed:** 2 units/sec
- Slow walk, overhead slam attack
- Slam has AoE shockwave (2m radius)
- 1.5s wind-up (dodge window)
- 30% chance to trigger player stagger on hit
- Armor: takes 50% reduced damage from light attacks

### Boss — The Fallen Guardian
**HP:** 300 | **Speed:** 4 units/sec

**Phase 1 (100-60% HP):**
- 3-hit sword combo (L → L → H pattern, mirrors player)
- Dash attack with 1s telegraph
- Summons 2 Basic Monks every 30s

**Phase 2 (60-30% HP):**
- Gains fire aura (contact damage 5/sec)
- New attack: ground slam → fire wave (linear, dodge sideways)
- Summons 1 Fast Acolyte every 20s
- 20% faster movement

**Phase 3 (30-0% HP):**
- Enraged: +30% attack speed, +20% damage
- New attack: 360° spin slash (dodge or take 30 dmg)
- No more summons, pure aggression
- Shorter recovery windows

---

## 6. Wave System — Level 1: Shrine Entrance

### Arena
- 25m × 25m open courtyard
- Stone walls (collision boundaries)
- Decorative elements: torii gate, lanterns, fog (no gameplay impact)
- Single spawn zone per wall (4 total, randomized)

### Wave Table
| Wave | Enemies | Composition |
|------|---------|-------------|
| 1 | 3 | 3 Basic |
| 2 | 5 | 5 Basic |
| 3 | 4 | 3 Basic + 1 Fast |
| 4 | 6 | 4 Basic + 2 Fast |
| 5 | 5 | 3 Basic + 1 Fast + 1 Heavy |
| 6 | 7 | 4 Basic + 2 Fast + 1 Heavy |
| 7 | 6 | 2 Basic + 3 Fast + 1 Heavy |
| 8 | 8 | 4 Basic + 2 Fast + 2 Heavy |
| 9 | 6 | 2 Fast + 2 Heavy + 2 Fast |
| 10 | 1 | **The Fallen Guardian (Boss)** |

- 3s delay between waves
- Enemies spawn at random wall spawn points
- Wave clears when all enemies dead

---

## 7. HUD Layout

```
┌──────────────────────────────────────────┐
│ [HP BAR]              [WAVE: 3/10]       │
│ [ADRENALINE BAR]                         │
│                                          │
│                                          │
│              (gameplay)                   │
│                                          │
│                                          │
│ [ELEMENT: FIRE]    [ABILITY1] [ABILITY2] │
│ [ENERGY BAR]       [CD: 3s]   [READY]   │
└──────────────────────────────────────────┘
```

- HP: Red bar, top-left
- Adrenaline: Yellow bar below HP, glows when full
- Wave counter: Top-right
- Element indicator: Bottom-left, color-coded (red/blue)
- Ability icons: Bottom-right with cooldown overlay
- Energy bar: Below element indicator

---

## 8. Technical Architecture

### Scene Hierarchy
```
GameManager (singleton)
├── LevelManager
├── WaveManager  
├── UIManager
│
PlayerRoot
├── PlayerController
├── PlayerCombat
├── PlayerHealth
├── ElementSystem
├── UltimateSystem
│
EnemySpawner
├── SpawnPoint_North
├── SpawnPoint_South
├── SpawnPoint_East
├── SpawnPoint_West
```

### Script Map
| Script | Responsibility |
|--------|---------------|
| GameManager.cs | Game state, pause, restart |
| PlayerController.cs | Movement, dodge, input |
| PlayerCombat.cs | Attacks, combos, damage dealing |
| PlayerHealth.cs | HP, damage taking, death |
| ElementSystem.cs | Element switching, abilities, energy |
| UltimateSystem.cs | Adrenaline tracking, ultimate activation |
| EnemyBase.cs | Base class for all enemies |
| EnemyAI.cs | State machine (Idle → Chase → Attack → Retreat) |
| EnemyHealth.cs | HP, damage, death, element reactions |
| WaveManager.cs | Wave progression, spawning |
| HUDController.cs | All UI updates |
| CameraController.cs | Follow cam with combat zoom |
| DamageSystem.cs | Central damage calculation + element modifiers |

### Input (New Input System)
| Action | Keyboard | Controller |
|--------|----------|------------|
| Move | WASD | Left Stick |
| Light Attack | LMB | X |
| Heavy Attack | RMB | Y |
| Dodge | Space | B |
| Element Switch | Q/E | D-pad L/R |
| Ability 1 | F | RB |
| Ability 2 | R | LB |
| Ultimate | V | LT + RT |
| Pause | Esc | Start |

---

## 9. Art & Asset Direction

### Sources
- **Character Models:** Mixamo (humanoid rigs + animations)
- **Environment:** Unity Asset Store / free Japanese shrine packs
- **SFX:** Freesound.org, Mixkit, or equivalent
- **VFX:** Unity Particle System (fire, lightning, hit effects)

### Visual Targets
- Dark, desaturated environment with fog
- Green glow on player (mask, blade trails)
- Red/orange for fire effects, blue/white for lightning
- Minimal UI — clean, semi-transparent HUD elements
- Screen shake on heavy hits and ultimate activation

### Animation List (Mixamo)
| Animation | Mixamo Search Term |
|-----------|-------------------|
| Idle | "sword idle" or "fighting idle" |
| Run | "sword run" or "running" |
| Dodge | "roll" or "dodge" |
| Light Attack 1 | "sword slash" |
| Light Attack 2 | "sword combo" |
| Heavy Attack | "great sword slash" or "overhead" |
| Hit React | "hit reaction" |
| Death | "death" |
| Ultimate | "power up" or "scream" |

---

## 10. MVP Development Schedule (2 Weeks)

### Week 1: Core Systems
| Day | Task |
|-----|------|
| 1 | Scene setup, ProBuilder arena, camera controller |
| 2 | PlayerController (move, sprint, dodge) + Mixamo import |
| 3 | PlayerCombat (light, heavy, combo chain) |
| 4 | EnemyBase + EnemyAI state machine + Basic Enemy |
| 5 | DamageSystem + PlayerHealth + EnemyHealth |
| 6 | WaveManager + EnemySpawner (waves 1-5 testable) |
| 7 | Buffer / bug fixing / playtesting |

### Week 2: Elements, Boss, Polish
| Day | Task |
|-----|------|
| 8 | ElementSystem (Fire + Lightning abilities) |
| 9 | Fast Enemy + Heavy Enemy behaviors |
| 10 | UltimateSystem (adrenaline + burst) |
| 11 | Boss — The Fallen Guardian (3 phases) |
| 12 | HUD (all UI elements) |
| 13 | VFX, SFX, screen shake, hit feedback |
| 14 | Final polish, full 10-wave playtest, build |
