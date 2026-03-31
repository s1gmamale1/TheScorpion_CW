# THE SCORPION — Unity Setup Guide

## Step 1: Project Setup
1. Create new Unity project (3D Core, URP optional)
2. Copy the `Assets/Scripts/` folder into your Unity project
3. Install TextMeshPro (Window → TextMeshPro → Import TMP Essential Resources)

## Step 2: Scene Setup

### Arena
1. Create a Plane (scale 2.5, 2.5, 2.5 = 25m × 25m)
2. Or use ProBuilder: Tools → ProBuilder → New Shape → Plane
3. Add 4 walls using Cubes (scale to box in the arena)
4. Bake NavMesh: Window → AI → Navigation → Bake

### Spawn Points
1. Create 4 empty GameObjects at each wall: `SpawnPoint_North`, `SpawnPoint_South`, `SpawnPoint_East`, `SpawnPoint_West`
2. Position them just inside each wall

## Step 3: Player Setup

1. Import Mixamo character (FBX with skin)
2. Create empty `Player` GameObject, tag it **"Player"**
3. Place Mixamo model as child
4. Add to Player root:
   - `CharacterController` (set Height=1.8, Radius=0.3, Center Y=0.9)
   - `PlayerController.cs`
   - `PlayerCombat.cs`
   - `PlayerHealth.cs`
   - `ElementSystem.cs`
   - `UltimateSystem.cs`
5. Set Layer to "Player"

### Animator Setup
1. Create Animator Controller: `PlayerAnimator`
2. Add parameters:
   - `Speed` (Float)
   - `Dodge` (Trigger)
   - `LightAttack` (Trigger)
   - `HeavyAttack` (Trigger)
   - `ComboCount` (Int)
   - `Death` (Trigger)
3. Import Mixamo anims and create states:
   - Idle → Run (blend tree on Speed)
   - Any State → Dodge (trigger)
   - Any State → LightAttack (trigger) → back to Idle
   - Any State → HeavyAttack (trigger) → back to Idle
   - Any State → Death (trigger)

## Step 4: Enemy Prefabs

### Layers
1. Create layer **"Enemy"** (use for all enemies)
2. Set `enemyLayer` on PlayerCombat and ElementSystem to "Enemy"

### Basic Enemy (Hollow Monk)
1. Import Mixamo character
2. Add: `NavMeshAgent`, `EnemyAI`, `EnemyHealth`, `CapsuleCollider`
3. Set EnemyAI → enemyType = Basic
4. Set EnemyHealth → maxHealth = 30
5. Set Layer = "Enemy", Tag = "Enemy"
6. Save as Prefab

### Fast Enemy (Shadow Acolyte)
1. Same as Basic but:
   - EnemyAI → enemyType = Fast
   - EnemyHealth → maxHealth = 20

### Heavy Enemy (Stone Sentinel)
1. Same as Basic but:
   - EnemyAI → enemyType = Heavy
   - EnemyHealth → maxHealth = 80, lightAttackReduction = 0.5
   - Scale model up 1.3x

### Boss (The Fallen Guardian)
1. Import distinct Mixamo character (larger/armored)
2. Add: `NavMeshAgent`, `BossAI` (NOT EnemyAI), `EnemyHealth`, `CapsuleCollider`
3. Set EnemyHealth → maxHealth = 300
4. Assign summonPoints and enemy prefabs in BossAI
5. Save as Prefab

### Enemy Animator
1. Add parameters: `Speed` (Float), `Attack` (Trigger), `Stunned` (Bool), `Death` (Trigger)
2. For boss, add extra: `DashAttack`, `Slam`, `Spin` (Triggers)

## Step 5: Managers

### GameManager
1. Create empty `GameManager` → attach `GameManager.cs`

### WaveManager
1. Create empty `WaveManager` → attach `WaveManager.cs`
2. Assign all 4 enemy prefabs
3. Assign all 4 spawn point transforms

### Camera
1. Select Main Camera → attach `CameraController.cs`
2. Assign player transform

## Step 6: UI (Canvas)

1. Create Canvas (Screen Space - Overlay)
2. Attach `HUDController.cs`
3. Create UI elements and assign in inspector:

```
Canvas
├── HealthBar (UI → Slider, anchor top-left)
│   └── Fill: Red (#FF3333)
├── AdrenalineBar (UI → Slider, below health)
│   └── Fill: Yellow (#FFCC00)
├── WaveText (TextMeshPro, anchor top-right)
├── ElementIndicator (UI → Image, anchor bottom-left, 60×60)
├── EnergyBar (UI → Slider, below element indicator)
├── Ability1 (UI → Image, anchor bottom-right)
│   └── CooldownOverlay (Image, Filled type)
│   └── CooldownText (TextMeshPro)
├── Ability2 (same structure as Ability1)
├── ComboText (TextMeshPro, center, large font, disabled)
├── GameOverPanel (Panel, disabled)
│   └── "GAME OVER" text + Restart button (OnClick → HUDController.OnRestartClicked)
├── VictoryPanel (Panel, disabled)
│   └── "VICTORY" text + Restart button
└── PausePanel (Panel, disabled)
    └── "PAUSED" text + Resume button (OnClick → HUDController.OnResumeClicked)
```

## Step 7: Quick Test Checklist
- [ ] Player moves with WASD, camera follows mouse
- [ ] Space = dodge with brief invincibility
- [ ] Left click = light attack, right click = heavy
- [ ] Q/E switches element (indicator color changes)
- [ ] F/R uses abilities (check console logs)
- [ ] Enemies spawn, chase, attack, die
- [ ] Waves progress (check console: "WAVE X/10")
- [ ] Adrenaline fills, V activates ultimate (time slows)
- [ ] Boss spawns on wave 10 with phase changes
- [ ] Game Over on death, Victory on wave 10 clear

## Input Summary
| Action | Key | Controller |
|--------|-----|------------|
| Move | WASD | Left Stick |
| Sprint | L-Shift | L3 |
| Dodge | Space | B |
| Light Attack | LMB | X |
| Heavy Attack | RMB | Y |
| Element Prev | Q | D-pad Left |
| Element Next | E | D-pad Right |
| Ability 1 | F | RB |
| Ability 2 | R | LB |
| Ultimate | V | LT+RT |
| Pause | Esc | Start |
