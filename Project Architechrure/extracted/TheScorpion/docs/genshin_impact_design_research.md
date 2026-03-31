# Genshin Impact Game Design Research
## Design Patterns for The Scorpion Arena Combat Game

*Research compiled: 2026-03-22*
*Purpose: Extract actionable design patterns from Genshin Impact's combat system for application to The Scorpion*

---

## TABLE OF CONTENTS
1. [Combat System Architecture](#1-combat-system-architecture)
2. [Elemental Reaction System](#2-elemental-reaction-system)
3. [Damage Formula & Technical Math](#3-damage-formula--technical-math)
4. [Combo System & Attack Chains](#4-combo-system--attack-chains)
5. [Animation System & Canceling](#5-animation-system--canceling)
6. [Hitlag, Game Feel & Impact Feedback](#6-hitlag-game-feel--impact-feedback)
7. [Poise & Interruption Resistance](#7-poise--interruption-resistance)
8. [Enemy AI Behavior Patterns](#8-enemy-ai-behavior-patterns)
9. [Boss Fight Phase Design](#9-boss-fight-phase-design)
10. [Elemental Burst / Ultimate Design](#10-elemental-burst--ultimate-design)
11. [Character Switching & Team Rotation](#11-character-switching--team-rotation)
12. [VFX & Particle Effects](#12-vfx--particle-effects)
13. [Art Style & Rendering](#13-art-style--rendering)
14. [UI/UX & HUD Design](#14-uiux--hud-design)
15. [Design Philosophy & What Makes It Feel Good](#15-design-philosophy--what-makes-it-feel-good)
16. [Actionable Takeaways for The Scorpion](#16-actionable-takeaways-for-the-scorpion)

---

## 1. COMBAT SYSTEM ARCHITECTURE

Genshin Impact is a **systems-driven game that uses key aspects of animation-driven games** to provide visceral game feel with broad appeal. This hybrid approach is critical to understand.

### Core Combat Loop
- **Real-time action combat** with a party of 4 switchable characters
- Each character has: **Normal Attacks** (3-6 hit chains), **Charged Attack** (stamina cost, more damage/knockback), **Plunging Attack** (aerial), **Elemental Skill** (E ability, cooldown), **Elemental Burst** (ultimate, energy cost)
- Five weapon types: Sword, Claymore, Polearm, Bow, Catalyst — each with distinct attack speeds, ranges, and stagger properties
- **Stamina** governs: sprinting, dodging, charged attacks, swimming, climbing

### What Makes It Work
- Combat is simple at surface level (tap to attack) but deep underneath (reactions, cancels, rotations)
- Every action has clear INPUT -> FEEDBACK -> RESULT loop
- The elemental system creates a **combinatorial explosion** of possibilities from simple ingredients
- "No level of core gameplay automatization" — the game demands player engagement at all times

**Scorpion Application:** Our dual-element system (Fire + Lightning) is simpler than Genshin's 7 elements, but the same principle applies: make the base combat feel good (Invector handles this), then layer elemental interactions on top for depth.

---

## 2. ELEMENTAL REACTION SYSTEM

This is Genshin's crown jewel design system. The entire game revolves around applying Element A, then triggering Element B to cause a **Reaction**.

### The Reaction Matrix (15 reactions total)

**Amplifying Reactions** (multiply the triggering hit's damage):
| Reaction | Elements | Multiplier | Notes |
|----------|----------|------------|-------|
| Vaporize | Hydro + Pyro | 2.0x (Hydro triggers) / 1.5x (Pyro triggers) | Best single-hit amplifier |
| Melt | Pyro + Cryo | 2.0x (Pyro triggers) / 1.5x (Cryo triggers) | Order matters! |

**Transformative Reactions** (deal separate fixed damage, scale with level + Elemental Mastery):
| Reaction | Elements | Effect | Multiplier |
|----------|----------|--------|------------|
| Overloaded | Electro + Pyro | AoE Pyro explosion + knockback | 2.75x |
| Electro-Charged | Electro + Hydro | Electro DoT, spreads to nearby wet enemies | 2.0x per tick |
| Superconduct | Electro + Cryo | AoE Cryo damage + 40% Physical RES reduction | 1.5x |
| Swirl | Anemo + any | Spreads element to nearby enemies + AoE damage | 0.6x |
| Shatter | Blunt + Frozen | Physical damage on frozen targets | 3.0x |
| Frozen | Hydro + Cryo | Immobilizes target (CC) | N/A |
| Burning | Dendro + Pyro | Pyro DoT | 0.25x per tick |
| Bloom | Dendro + Hydro | Creates Dendro Core (timed explosive) | 2.0x |
| Hyperbloom | Electro + Dendro Core | Homing projectile damage | 3.0x |
| Burgeon | Pyro + Dendro Core | Immediate AoE detonation | 3.0x |

**Additive Reactions** (flat damage bonus added to base):
| Reaction | Elements | Effect |
|----------|----------|--------|
| Quicken | Dendro + Electro | Applies Quicken aura (enables below) |
| Aggravate | Electro on Quickened | +flat Electro damage bonus (1.15x level mult) |
| Spread | Dendro on Quickened | +flat Dendro damage bonus (1.25x level mult) |

**Utility Reaction:**
| Reaction | Elements | Effect |
|----------|----------|--------|
| Crystallize | Geo + any | Creates elemental shield shard |

### Elemental Gauge Theory (Hidden System)
- When an element is applied, it creates an **aura** with **gauge units** (typically 1U or 2U)
- The aura decays over time
- Reactions consume gauge units from the aura
- This determines: how long an element stays, how many reactions you can trigger, and whether the aura is fully consumed

### Key Design Principles
1. **Order matters**: Pyro-then-Hydro gives different multiplier than Hydro-then-Pyro
2. **Reactions are the main damage amplifier**, not raw stats alone
3. **Every element interacts with every other element** — no dead combinations
4. **Reactions have side effects**: knockback (Overloaded), CC (Frozen), debuffs (Superconduct)
5. **Internal cooldowns (ICD)**: Most abilities can only trigger reactions every 2.5s or every 3 hits, preventing infinite reaction spam

**Scorpion Application:**
- Our Fire element should provide: DoT (like Burning), area denial (like Overloaded's AoE)
- Our Lightning element should provide: speed buff + CC stun (like Superconduct's debuff + Electro-Charged's chain)
- **Critical insight**: Make element-switching CREATE reactions on already-afflicted enemies. If an enemy has Fire DoT and you switch to Lightning, trigger an "Overcharge" reaction for bonus burst damage. This is the Genshin pattern.
- Consider: Fire on Lightning-stunned enemy = "Ignite" (extends stun + explosion). Lightning on Burning enemy = "Overcharge" (burst damage + spread)

---

## 3. DAMAGE FORMULA & TECHNICAL MATH

Genshin's damage formula is multiplicative with many independent layers. Understanding this helps design our own.

### Complete Formula
```
DMG = BaseDMG × (1 + DMGBonus%) × CRIT × EnemyDefMult × EnemyResMult × ReactionMult
```

Where:
- **BaseDMG** = ATK × Talent% (or DEF/HP/EM × Talent% for some abilities)
- **DMGBonus** = 1 + sum of all damage bonus percentages (elemental, physical, etc.)
- **CRIT** = 1 + CritDMG% (if crit lands; base 5% rate, 50% damage)
- **EnemyDefMult** = (CharLvl + 100) / ((CharLvl + 100) + (EnemyLvl + 100) × (1 - DefReduction))
  - At equal levels: exactly 0.5 (50% damage reduction from DEF)
  - DEF reduction is hard-capped at 90%
- **EnemyResMult** varies by resistance value:
  - RES < 0%: `1 - (RES/2)` (negative res = bonus damage)
  - 0% ≤ RES < 75%: `1 - RES`
  - RES ≥ 75%: `1 / (4 × RES + 1)` (diminishing returns)
- **ReactionMult** = depends on reaction type (see Section 2)

### Amplifying Reaction Formula
```
AmplifyingMult = ReactionBase × (1 + (2.78 × EM)/(1400 + EM) + ReactionBonus)
```

### Transformative Reaction Formula
```
TransformativeDMG = ReactionMult × LevelMult × (1 + (16 × EM)/(2000 + EM) + ReactionBonus) × EnemyResMult
```

### Average DPS Accounting for Crit
```
AvgCrit = 1 + clamp(CritRate, 0%, 100%) × CritDMG
```

**Scorpion Application:**
- Use a simplified multiplicative formula: `BaseDMG × ElementBonus × EnemyWeakness × CritMult`
- Key insight: **multiplicative layers feel more rewarding** because improving any single layer boosts total output significantly
- The Defense formula ensures enemies at your level always halve your damage — a natural scaling mechanism
- The Resistance system with negative values (bonus damage on debuffed enemies) rewards elemental play — steal this for element-weakness interactions

---

## 4. COMBO SYSTEM & ATTACK CHAINS

### Normal Attack Chains
- Sword/Polearm: 4-5 hit chains, fast, low individual damage
- Claymore: 4-5 hit chains, slow, high stagger/damage per hit
- Catalyst: 3-4 hit chains, ranged elemental damage
- Bow: Variable, can aim or rapid fire
- **Each hit in a chain has different**: damage%, hitbox, animation timing, stagger level, and hitlag

### Charged Attacks
- Sword: Quick forward thrust (stamina cost ~20)
- Claymore: Spinning slash (high stamina, high damage, continuous)
- Polearm: Forward dash-thrust
- Bow: Aimed shot with elemental infusion
- Catalyst: Homing projectile

### Combo Flow Design
1. Normal attacks are the **bread** — consistent, low-commitment damage
2. Charged attacks are the **butter** — higher risk/reward, stamina gated
3. Elemental Skill is the **spice** — unique per character, moderate cooldown (6-20s)
4. Elemental Burst is the **crescendo** — high impact, energy gated
5. Character swap is the **multiplier** — triggers rotations and reaction chains

### Key Insight: The "N2C" Pattern
The most common optimized combo is "Normal 2, Charged" (N2C) — two normal attacks into a charged attack, then cancel the recovery animation. This creates a **rhythmic loop** that skilled players optimize.

**Scorpion Application:**
- Design our normal attack chain (via Invector) with escalating damage per hit (e.g., hit 1: 80%, hit 2: 90%, hit 3: 110%, hit 4: 140%, hit 5: 180%)
- Gate the strongest finisher behind completing the chain — reward commitment
- Make charged attacks consume stamina but deal elemental damage based on active element
- The "N2C" pattern maps to our system: Normal attacks build adrenaline, charged attacks spend element energy for big payoff

---

## 5. ANIMATION SYSTEM & CANCELING

### How Genshin's Animation System Works
- Attacks are divided into: **startup frames**, **active frames** (damage registers), **recovery frames**
- The game **processes damage before animation completion** — this is the key design decision that enables canceling
- Different input types have **priority**: Burst > Skill > Dash > Jump > Normal Attack
- Recovery animations are **interruptible** by higher-priority inputs

### Cancel Types (Fastest to Slowest)
1. **Dash Cancel**: Tap dash after damage registers. Fastest, costs stamina.
2. **Jump Cancel**: Tap jump after damage registers. Free, slightly slower.
3. **Swap Cancel**: Switch characters to end animation. Used in rotations.
4. **Skill Cancel**: Use Elemental Skill to interrupt normals at any point.
5. **Burst Cancel**: Use Burst to interrupt — recharges energy during animation.

### DPS Impact
- Animation canceling can effectively **double sustained DPS** for characters with long recovery animations
- Creates a high skill ceiling without punishing casual players (you can still play without canceling)

### Design Philosophy
- Canceling is a **semi-intentional emergent mechanic** — the system allows it by design but doesn't teach it
- This creates layers: casuals enjoy the base combat, enthusiasts discover canceling, theorycrafters optimize frame-perfect rotations

**Scorpion Application:**
- Ensure Invector's animation system allows dash-canceling out of attack recovery frames
- Make dodge-roll (Invector built-in) cancel any attack recovery
- Element switch (Q/E) should cancel current animation — this rewards aggressive element swapping
- Do NOT lock players into long animations unless it's a deliberate commitment (like Ultimate activation)

---

## 6. HITLAG, GAME FEEL & IMPACT FEEDBACK

This is what makes Genshin's combat **feel** good despite being relatively simple mechanically.

### Hitlag (Hitstop/Freeze Frames)
- When melee attacks connect, **both attacker and target entity speeds drop to 1%** (sometimes 5%) for a brief duration
- This creates a "frozen in time" sensation that communicates **weight and impact**
- Entity speed multipliers vary: 0.01 (most common), 0.02, 0.03, 0.05, or 0.1
- **Every hit in a multi-hit combo can have different hitlag values** — heavier finishers get more hitlag
- Hitlag duration is specific to each attack and is NOT affected by Attack Speed buffs
- Hitlag **extends ability durations** — if you're in a timed buff and trigger hitlag, the buff timer pauses during the freeze

### Camera Shake
- Attacks that connect trigger camera shake proportional to hit impact
- **Forced Camera Shake**: Some abilities (Overloaded explosions, ground slams) shake the camera even on miss
- Camera shake is adjustable in settings (important for accessibility)

### Other Feedback Layers
- **Damage numbers**: Float up from impact point, color-coded by element
- **Elemental particles**: Burst from enemies on hit, flow toward character
- **Sound design**: Distinct hit sounds per weapon type and element
- **Knockback/Stagger**: Visual confirmation that your attack disrupted the enemy
- **Slow-motion on kills**: Subtle time dilation on final hits (especially in Spiral Abyss)

### The Feedback Stack (What Fires on Every Hit)
1. Hitlag freeze (1-5 frames)
2. Camera shake (proportional to impact)
3. Damage number popup
4. Hit VFX (sparks, element splash)
5. Hit SFX (weapon impact + element sound)
6. Enemy stagger/knockback animation
7. Elemental particle generation
8. Screen flash (on crits or reactions)

**Scorpion Application — THIS IS THE MOST IMPORTANT SECTION:**
- Implement **hitlag/hitstop**: On every melee hit, freeze Time.timeScale to ~0.01 for 0.05-0.1 seconds. Scale duration with attack power (light attacks: 2 frames, heavy attacks: 5 frames, Ultimate: 8 frames).
- Implement **camera shake**: Use Cinemachine impulse sources. Light hit = subtle, Heavy hit = medium, Reaction trigger = strong, Ultimate activation = screen-wide.
- Layer ALL feedback simultaneously: hitlag + camera shake + VFX + SFX + damage number + enemy stagger
- **The secret**: It's not any single effect that creates good game feel — it's ALL of them firing at the same time, perfectly synchronized to the moment of impact.
- Make elemental reactions trigger an EXTRA layer of feedback on top of normal hit feedback (bigger shake, unique VFX, distinct SFX)

---

## 7. POISE & INTERRUPTION RESISTANCE

### Hidden Poise Bar System
- Every character and enemy has an **invisible poise bar** that drains when receiving hits
- When poise reaches 0, the unit becomes **vulnerable** — the next hit triggers a stagger animation
- Different stagger levels: Level 1 (minor shake) through Level 9 (heavy launch with displacement)

### Poise Values
| Unit Type | Poise | Refill Rate | Reset Time |
|-----------|-------|-------------|------------|
| Melee Characters | 100 | 5/sec | 2 sec |
| Ranged Characters | 50 | 3/sec | 3 sec |
| Basic Grunts (Nobushi) | 100 | 5/sec | — |
| Humanoid Demibosses (Mitachurl) | 210 | 20/sec | — |
| Boss-tier Enemies | 2000 | 100/sec | — |

### Interruption Resistance (Hyperarmor)
- A **multiplier** reducing incoming poise damage (e.g., 0.5 = half poise damage taken)
- Certain abilities grant temporary hyperarmor (e.g., Claymore charged attacks, some Elemental Skills)
- Stacks multiplicatively

### Shield Mechanics
- ANY active shield provides **complete poise immunity** (0.0 multiplier)
- This is why shields are so powerful — not just damage absorption but stagger immunity

### Co-op Scaling
Enemy poise multiplies by player count (2x for 2 players, etc.)

**Scorpion Application:**
- Implement hidden poise on all enemies:
  - Hollow Monk (basic): 100 poise, breaks easily
  - Shadow Acolyte (fast): 60 poise, breaks very easily but recovers fast (high refill rate)
  - Stone Sentinel (heavy): 300 poise, very hard to break, slow refill
  - Fallen Guardian (boss): 1000+ poise, practically unbreakable except during phase transitions
- Player poise: Give the Scorpion ~100 poise, with Adrenaline Rush granting hyperarmor (0.0 multiplier = unstaggerable during Ultimate)
- Lightning element should deal bonus poise damage (stun effect = faster poise break)
- Fire element should deal bonus HP damage but normal poise damage

---

## 8. ENEMY AI BEHAVIOR PATTERNS

### Architecture: Hierarchical State Machine + Decision Tree

### Core States
1. **Idle**: Wait for trigger (player proximity). Random idle duration (min/max).
2. **Wander/Patrol**: Move between predefined wander points within base range.
3. **Alert/Pre-Attack**: Player detected. Move into attack range. Queue in attack order.
4. **Attack**: Execute attack pattern. Check if still in range (GetInAttackRange()). Chase if player moves, give up if too far.
5. **Hurt**: Stagger reaction when poise breaks. Brief vulnerability.
6. **Die**: Death animation + loot drop.

### Key Design Patterns
- **Attack Queuing**: A GameManager maintains a "Preattack Enemies" list. The enemy at the front attacks first. This prevents all enemies from attacking simultaneously — creates rhythm.
- **Base Range vs AI Range**: Base Range = how close player must be to trigger aggro. AI Range = how far the enemy will chase from their spawn.
- **Distance-Based Decisions**: GetInAttackRange() continuously checks if the enemy is close enough to attack. If not, they chase. If the player retreats too far, they disengage.
- **Turn-Taking**: Enemies take turns attacking rather than mobbing — this is essential for fair-feeling combat against groups.

### Enemy Archetypes (Genshin's Approach)
- **Melee Rushers** (Hilichurls): Simple attack patterns, low poise, aggressive chase
- **Ranged Harassers** (Hilichurl Archers): Stay at distance, shoot, flee if approached
- **Shielded Tanks** (Mitachurls): High poise, shield blocks frontal attacks, requires elemental counter or flanking
- **Elemental Specialists** (Abyss Mages): Elemental shields requiring counter-element, teleport away, cast area spells
- **Mini-Bosses** (Lawachurls): Multi-phase within single enemy, rage state, ground pounds, charges

**Scorpion Application:**
- **Hollow Monk** (basic) = Hilichurl pattern: simple 2-3 hit combo, low poise, dies fast. These are fodder to make player feel powerful.
- **Shadow Acolyte** (fast) = Abyss Mage pattern: teleports/dashes away, ranged harassment, requires gap-closing. Lightning element should be effective (speed buff to catch them + stun).
- **Stone Sentinel** (heavy) = Mitachurl pattern: high poise, slow wind-up attacks, blocks frontal. Fire element should be effective (DoT bypasses block, area denial controls space).
- **CRITICAL: Implement attack queuing** via WaveManager. Never let more than 2-3 enemies attack simultaneously. Others should circle, reposition, or play "threatening" idle animations.

---

## 9. BOSS FIGHT PHASE DESIGN

### Genshin's Boss Phase Design Principles

**La Signora (Best simple boss design):**
- Phase 1 (Cryo): Ice attacks, freezing arena, heat orbs to collect for survival
- Phase 2 (Pyro): Fire attacks, burning arena, cryo orbs to collect for survival
- Design principle: **Flip the mechanics between phases** — force adaptation
- Key strength: "Minimal invulnerability throughout both phases" — respect the player's time

**Raiden Shogun (Best complex boss design):**
- Standard Phase: Electro projectiles, wave slashes, illusion clones (spot the real one), rotating lightning combos
- Baleful Shadowlord Phase: Colossal attacks, must interact with environment (Flowers of Remembrance for shields), Electroculi destruction mechanics
- Design principle: **Alternate between reaction-heavy and mechanic-heavy phases**
- Vulnerability windows after solving the "puzzle" (destroying correct illusion)

**Azhdaha (Best cinematic boss design):**
- Phase 1: Physical attacks only
- Phase 2: Absorbs Ley Line energy through limbs (camera shifts to top-down angle during transition), gains first element
- Phase 3: Absorbs elemental energy through tail (different camera angle), gains second element, random elements each week
- Design principle: **Phase transitions are spectacles** — use camera work and VFX to make them feel epic
- **Replayability through randomization** — different elements each week

**Dottore (Best mechanic boss design):**
- Uses lunar manipulation system (unique mechanic specific to fight)
- "High Damage Punishment" — actively punishes burst-damage strategies
- Design principle: **Reward understanding over brute force**

### Universal Boss Design Patterns in Genshin
1. **Telegraphed attacks**: Every boss attack has a visual/audio tell. The stronger the attack, the longer the telegraph.
2. **Vulnerability windows**: After big attacks or phase mechanics, boss is briefly exposed.
3. **Arena interaction**: Boss fights use the arena itself as a mechanic (temperature, obstacles, environmental hazards).
4. **Escalation**: Each phase is more aggressive than the last.
5. **Phase transitions are invulnerable cutscenes** — they serve as breathers and spectacle.
6. **Counter-element rewards**: Using the right element against a boss phase is strongly rewarded.

**Scorpion Application for The Fallen Guardian (3-phase boss):**
- **Phase 1 (100-60% HP)**: Sword combos + summons. Telegraphed 3-hit combos with clear dodge windows. Summons small groups of Hollow Monks (1-2 at a time). Player learns boss patterns.
- **Phase 2 (60-30% HP)**: Fire aura + wave attack. Arena becomes hazardous (fire patches). Boss gains fire-element attacks. Lightning should be more effective here (counter-element). Camera shift during transition.
- **Phase 3 (30-0% HP)**: Enraged pure aggression. Faster attacks, shorter telegraphs, no summons. Boss glows with intensity. Hitlag on boss attacks should increase to sell the power. This is the "DPS check" — can you finish before they overwhelm you?
- **STEAL**: Make phase transitions into brief spectacles. Boss screams, camera pulls back, VFX explosion, then new phase begins. 2-3 seconds of invulnerability during transition = breathing room.

---

## 10. ELEMENTAL BURST / ULTIMATE DESIGN

### Energy System
- Energy is generated by: Elemental Skill use (particles), defeating enemies, and some passive abilities
- Energy costs range from 40 to 90 depending on character
- Energy is element-typed: same-element particles give 3x more energy than off-element
- On-field character receives more energy than off-field characters

### Burst Design Patterns
- **Nuke Bursts**: Single massive damage instance (Eula, Raiden). Satisfying because of big number.
- **Transformation Bursts**: Change character's attack mode for a duration (Raiden's sword form, Xiao's anemo infusion). Satisfying because of sustained power fantasy.
- **Support Bursts**: Create persistent effects (Bennett's ATK buff field, Xingqiu's rain swords). Satisfying because of enabling combos.
- **Defensive Bursts**: Heal or shield (Barbara, Zhongli). Satisfying because of clutch saves.

### Invincibility Frames
- Burst activation grants varying i-frames during the casting animation
- This is both a defensive tool (dodge through damage) and a spectacle moment
- "Cinematic" bursts freeze the game briefly and show a close-up of the character

### Character Expression
- Every Burst is unique and tells the character's story through animation
- The Burst animation IS the character's identity — it's what players remember

**Scorpion Application for Adrenaline Rush (Ultimate):**
- Activation: Full adrenaline bar (100). Built by: +2 per hit, +5 per kill, +10 per combo finisher.
- Effect: 8 seconds of time-slow + damage boost (like a Transformation Burst)
- **STEAL from Genshin**: Grant i-frames during activation animation. Brief cinematic close-up of the Scorpion's mask, then time slows.
- During Adrenaline Rush: Grant hyperarmor (unstaggerable), visual intensity increase (screen desaturation + character glow), amplified hitlag on all attacks
- **STEAL**: Make the activation itself deal damage in an AoE around the player (like many Genshin bursts). Enemies nearby get knocked back. This creates space and feels powerful.

---

## 11. CHARACTER SWITCHING & TEAM ROTATION

### How Switching Works in Genshin
- Instant swap on button press (1-4 keys on PC, D-pad on controller)
- 1-second cooldown between swaps
- Swap-in character inherits camera position and targeting
- The act of swapping is itself a combat tool: triggers reactions, cancels animations, repositions

### Team Rotation Design
- **Fixed Rotations**: Specific ability order over ~20 seconds, then repeat
- **Flexible Rotations**: Adapt to situation, swap when convenient
- The rotation creates RHYTHM — a predictable cadence of setup -> amplify -> damage -> recharge

### Why It Works
- Swapping keeps combat from being monotonous — you're always doing something different
- It creates a "mastery curve" — learning optimal rotation order is the endgame skill
- Every character feels fresh because you only play them for 5-10 seconds at a time

**Scorpion Application:**
- We don't have character switching, but we have **element switching** (Q/E)
- Design element switching to feel like a mini-rotation: Fire mode for DoT setup -> Switch to Lightning for burst reaction -> Switch back to Fire for area control
- The 1-second cooldown is good design — prevents spamming, creates intentionality
- Consider a brief visual flourish on element switch (weapon trail changes color, small particle burst)

---

## 12. VFX & PARTICLE EFFECTS

### Genshin's VFX Philosophy
- Every character's abilities have **signature visual cues**: specific colors, shapes, and particle behaviors
- VFX communicate mechanics AND identity simultaneously
- Layered particle emitters create effects that read at multiple distances

### Technical Approach
- **Stylized over realistic**: Effects are painterly/anime-inspired, not photorealistic
- **Stencil shaders**: Control precisely where effects render (portals, area effects)
- **Parallax layering**: Overlapping particles moving at different speeds create depth
- **Performance-first**: Models, textures, and effect density adjust with distance. Mobile-first design ensures everything runs on low hardware.

### VFX Design Language
- **Pyro**: Orange/red, flame particles, heat distortion, ember trails
- **Electro**: Purple/violet, sharp angular particles, lightning bolts, electric arcs
- **Hydro**: Blue, fluid/splash particles, bubble effects, water rings
- **Cryo**: Light blue/white, ice crystal particles, frost spreading, snowflake shapes
- **Anemo**: Green/teal, swirl particles, wind lines, leaf effects
- **Geo**: Yellow/amber, crystalline particles, rock fragments, geometric shapes
- **Dendro**: Green, leaf/vine particles, bloom effects, organic shapes

### Elemental Reaction VFX
- Reactions get their OWN unique VFX on top of the triggering element's VFX
- Overloaded: Explosion ring + camera shake
- Vaporize: Steam burst + water splash
- Melt: Heat shimmer + ice crack
- The reaction VFX is the **reward signal** that confirms you triggered the combo

**Scorpion Application:**
- **Fire Element VFX**: Orange/red weapon trails, ember particles on swings, fire DoT creates flickering ground circles, burning enemies emit smoke particles
- **Lightning Element VFX**: Blue-white/electric blue weapon trails, arc particles between nearby enemies, stun creates crackling electricity on enemy, ground lightning bolts on ability use
- **Element Switch VFX**: Brief dual-color burst (fire + lightning particles simultaneously), weapon trail color transition
- **Reaction VFX**: When triggering Fire+Lightning reaction, unique orange-blue explosion VFX + extra camera shake
- **Ultimate VFX**: Time-slow should have visual treatment (desaturation + radial blur on edges, player character gets bright glow/aura)

---

## 13. ART STYLE & RENDERING

### Genshin's NPR (Non-Photorealistic Rendering) Approach

**Cel Shading Core:**
- Binary shadow transitions: characters fully enter shadow when crossing thresholds (no smooth gradient)
- BUT with a key modification: the shadow edge has a **subtle color gradient** (artificial subsurface scattering) that prevents it from looking flat
- Each character has unique **shadow ramp textures** that define their gradient colors
- Shadow ramps adjust for day/night lighting

**Facial Shading:**
- Uses a **custom light map** that shifts and mirrors based on face direction relative to light
- NOT the same technique as body shading — faces get special treatment to always look good
- This prevents ugly shadow patterns across the face

**Outline Rendering:**
- Inverted hull technique: Solidify modifier with negative thickness + flipped normals
- Some materials colorize the outline to match the base material
- Thin, dark outlines on all characters

**Metallic Rendering:**
- Matcap reflections combined with real-time specular shading
- Variable roughness levels per material

**Hair Rendering:**
- Anime-style shine marks serve as masks for real-time specular
- Anisotropic highlights along hair strands

**Rim Lighting:**
- Appears on both lit and shadow sides (unusual)
- Maintains even thickness regardless of mesh topology
- Best implemented through compositing with blurred alpha layers

**Key Insight:** "Most of the shader's elements are faked both for better performance and for full control over art direction." They prioritize LOOKING RIGHT over being physically accurate.

**Scorpion Application:**
- Since we're using Unity URP, consider implementing a simplified toon shader for the Scorpion character
- Priority: Get the shadow ramp right (hard shadow edge with subtle color bleed)
- Face shadow map is a nice-to-have but not critical for MVP
- Outlines can be done with inverted hull or edge detection post-process
- For our dark/gritty aesthetic, use cooler shadow ramps (deep blue/purple shadows instead of warm)

---

## 14. UI/UX & HUD DESIGN

### Genshin's HUD Layout

**Screen Corners (Combat HUD):**
- **Top-Left**: Minimap with markers, compass
- **Top-Right**: Quest tracker, event notifications
- **Bottom-Right**: Party list (4 character portraits), each showing:
  - Character portrait/icon
  - HP bar (under portrait)
  - Energy meter (circular, around portrait, fills as energy accumulates)
  - Elemental Burst ready indicator (glows when full)
  - Cooldown timer overlay (on Elemental Skill)
- **Bottom-Center**: Action buttons (Normal Attack, Elemental Skill, Elemental Burst, Sprint/Dodge)
- **Center**: Crosshair/targeting indicator

**Dynamic Elements:**
- **Stamina bar**: Yellow segmented bar, appears near character only when depleting/recharging, auto-hides when full
- **Enemy HP bars**: Appear above enemy heads when engaged
- **Damage numbers**: Float up from impact point, color-coded by element
- **Status effects**: Arrows around character (up = buff, down = debuff)
- **Elemental aura icons**: Small element icon above enemy HP bar showing current aura

### Design Principles
- **Clean, minimal aesthetic** — UI doesn't compete with the action
- **Auto-hide non-essential elements** — stamina bar disappears when full
- **Color-coding everywhere** — every element has a consistent color across all UI elements
- **Information hierarchy**: HP and energy (survival) > cooldowns (tactical) > buffs (optimization)

**Scorpion Application — HUD Design:**
- **Top-Left**: Wave counter (Wave X/10), enemy count remaining
- **Bottom-Left**: HP bar (health), Stamina bar (below HP)
- **Bottom-Center**: Element indicator (Fire/Lightning icon with active glow), Element Energy bar
- **Bottom-Right**: Adrenaline meter (fills dramatically), ability cooldown icons
- **Center**: Combo counter (when chaining hits), damage numbers
- Auto-hide stamina when full, auto-hide combo counter when not in combo
- Color scheme: Fire abilities = orange UI accents, Lightning abilities = blue UI accents, switch dynamically
- Boss HP bar: Large bar at top-center of screen with phase markers at 60% and 30%

---

## 15. DESIGN PHILOSOPHY & WHAT MAKES IT FEEL GOOD

### The Five Pillars of Genshin's Combat Feel

**1. Immediate, Layered Feedback**
Every action produces multiple simultaneous feedback signals (visual, audio, haptic, camera). The "juice" comes from redundancy — you receive the same message through 4+ channels at once.

**2. Meaningful Choices in Real-Time**
Element selection, character order, when to dodge, when to burst — the player is constantly making micro-decisions. There's no "spam attack to win."

**3. Depth Through Simplicity**
Individual systems are simple (apply element, trigger reaction). The depth comes from how they INTERACT. Two simple systems combining create complexity without complexity in learning.

**4. Reward Mastery Without Punishing Casuals**
Animation canceling doubles DPS but you can complete all content without it. Optimal rotations exist but flexible play works too. The skill ceiling is high but the skill floor is low.

**5. Spectacle as Reward**
Elemental Bursts, Reactions, critical hits — the game constantly rewards you with visual/audio spectacle. Big numbers, big explosions, big camera moments. The player feels powerful.

### What Specifically Feels Good
- **The hitstop** on every hit (weight)
- **The camera shake** on impact (power)
- **The elemental particle flow** toward your character (progression)
- **The damage number crits** in yellow (dopamine)
- **The reaction trigger VFX** (mastery confirmation)
- **The Burst cinematic** (character fantasy)
- **The enemy ragdolling** from Overloaded (comedy + power)
- **The freeze-then-shatter** combo (setup + payoff)

### The Core Design Pattern
**Setup -> Trigger -> Payoff -> Reset**
1. Apply Element A (setup)
2. Switch and apply Element B (trigger)
3. Reaction fires with full feedback stack (payoff)
4. Wait for cooldowns, rebuild energy (reset)

This loop runs every 5-10 seconds and is the heartbeat of combat.

---

## 16. ACTIONABLE TAKEAWAYS FOR THE SCORPION

### Priority 1: Game Feel (Implement First)
1. **Hitlag system**: Freeze time for 2-5 frames on melee impact. Scale with attack weight.
2. **Camera shake**: Cinemachine impulse on every hit. Scale with damage.
3. **Damage numbers**: Floating text, color-coded (white = physical, orange = fire, blue = lightning, yellow = crit)
4. **Hit VFX**: Spark/slash effect on every impact + element-colored particles
5. **Hit SFX**: Layer weapon sound + element sound on every hit

### Priority 2: Element Interaction System
1. **Element Application**: Hitting enemies with fire attacks applies "Burning" aura. Lightning attacks apply "Shocked" aura.
2. **Cross-Element Reactions**: If you switch elements and hit an enemy with an existing aura:
   - Fire on Shocked enemy = "Overcharge" (AoE burst damage + knockback)
   - Lightning on Burning enemy = "Ignite" (extended stun + spread fire to nearby)
3. **Enemy-Element Weakness**: Fast enemies (Shadow Acolyte) take extra poise damage from Lightning. Heavy enemies (Stone Sentinel) take extra HP damage from Fire DoT.
4. **Visual Language**: Burning enemies have fire particles + orange glow. Shocked enemies have electricity arcs + blue glow. Reaction triggers get unique combined VFX.

### Priority 3: Boss Phase Design
1. Phase transitions should be brief spectacles (2-3 second invulnerable animations)
2. Each phase should change the "rules" — new attack patterns, new arena hazards
3. Telegraph all boss attacks with visual tells proportional to danger level
4. Create vulnerability windows after big attacks (reward dodging with damage opportunities)
5. Phase 3 should feel genuinely dangerous — faster attacks, shorter windows, more visual intensity

### Priority 4: Poise/Stagger System
1. Give all enemies hidden poise values
2. Different enemy types break at different rates
3. Lightning attacks deal bonus poise damage
4. Adrenaline Rush grants complete poise immunity
5. Stagger animations create safe windows for continued comboing

### Priority 5: Animation Polish
1. Ensure dodge-canceling works out of all attack recoveries
2. Element switch should cancel current animation
3. Ultimate activation should have i-frames
4. Normal attack chains should have escalating damage and hitlag
5. Final hit of combo chain should have extra hitlag + camera shake + knockback

### Priority 6: HUD Implementation
1. Minimal, auto-hiding elements
2. Adrenaline meter should be prominent and satisfying to fill (particle effects as it approaches full)
3. Element indicator should dynamically change color
4. Boss HP bar with phase markers
5. Wave counter always visible

---

## SOURCES

### Combat System & Game Design
- [Combat - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Combat)
- [Gameplay and Combat System - Game8](https://game8.co/games/Genshin-Impact/archives/296718)
- [Game Design Analysis - Anuflora](https://www.anuflora.com/game/?p=4448)
- [Genshin Impact Game Deconstruction - GameRefinery](https://www.gamerefinery.com/genshin-impact-deconstruction/)
- [The Art of Design in Genshin Impact - Medium](https://medium.com/@ariannayu666/the-art-of-design-in-genshin-impact-a-comprehensive-analysis-from-game-worlds-to-strategic-8be55587fe55)

### Elemental System
- [Elemental Reactions Guide - Game8](https://game8.co/games/Genshin-Impact/archives/297558)
- [Elemental Reactions & Resonance - Genshin.gg](https://genshin.gg/elements/)
- [All Elemental Reactions Explained - GameRant](https://gamerant.com/genshin-impact-elemental-reactions-guide/)

### Damage Formula
- [Damage Formula - KQM Theorycrafting Library](https://library.keqingmains.com/combat-mechanics/damage/damage-formula)
- [Damage - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Damage)
- [Total Damage Calculation Formula - HoYoLAB](https://www.hoyolab.com/article/307351)

### Animation & Game Feel
- [How To Animation Cancel - TheGamer](https://www.thegamer.com/genshin-impact-animation-canceling-explained-guide/)
- [Combat Tech Guide - Into the Blue Sky](https://intothebluesky.com/2022/05/10/genshin-impact-combat-tech-guide/)
- [Hitlag - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Hitlag)
- [Frames - KQM Theorycrafting Library](https://library.keqingmains.com/combat-mechanics/frames)

### Poise & Stagger
- [Poise Guide - BitTopup News](https://news.bittopup.com/news/genshin-impact-poise-guide-master-hidden-stagger-system)
- [Interruption Resistance - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Interruption_Resistance)
- [Poise - KQM Theorycrafting Library](https://library.keqingmains.com/combat-mechanics/poise)

### Enemy AI
- [Recreating Genshin Impact Enemy AI - Medium (Minoqi)](https://minoqi.medium.com/partially-recreating-genshin-impacts-enemy-ai-w-state-machines-a2b723c51b26)
- [Enemy - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Enemy)

### Boss Design
- [Raiden Shogun Boss Guide - Game8](https://game8.co/games/Genshin-Impact/archives/328147)
- [Ranking All Weekly Bosses - GameRant](https://gamerant.com/genshin-impact-best-worst-every-all-weekly-boss/)
- [Azhdaha Design - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Azhdaha/Design)

### VFX & Rendering
- [Genshin Impact VFX: Mona Breakdown - RealTimeVFX](https://realtimevfx.com/t/genshin-impact-vfx-mona-magic-breakdown/16874)
- [Genshin Impact Character Shader Breakdown Unity URP - ArtStation](https://adrianmendez.artstation.com/projects/wJZ4Gg)
- [Recreating the Genshin Impact Shader - Ben Ayers](https://bjayers.com/blog/9oOD/blender-npr-recreating-the-genshin-impact-shader)
- [URPSimpleGenshinShaders - GitHub](https://github.com/NoiRC256/URPSimpleGenshinShaders)

### UI/UX
- [Game UI Database - Genshin Impact](https://www.gameuidatabase.com/gameData.php?id=470)
- [Genshin Impact Mobile UX/UI Redesign - Yelin Park](https://yelinpark.com/genshin-ui)
- [Genshin Starter Guide UI Explanation - HoYoLAB](https://www.hoyolab.com/article/5921298)

### GDC & Development
- [GDC Vault: Crafting an Anime Style Open World](https://www.gdcvault.com/play/1027539/-Genshin-Impact-Crafting-an)
- [GDC 2021 - MiHoYo Wiki](https://mihoyo.fandom.com/wiki/Game_Developers_Conference_2021)

### Character Switching & Rotations
- [Team Building Guide - KQM](https://keqingmains.com/misc/team-building/)
- [What Are Team Rotations - FandomSpot](https://www.fandomspot.com/genshin-impact-what-are-team-rotations/)

### Elemental Burst
- [Elemental Burst - Genshin Impact Wiki (Fandom)](https://genshin-impact.fandom.com/wiki/Elemental_Burst)
- [Elemental Burst Guide - Game8](https://game8.co/games/Genshin-Impact/archives/305180)
