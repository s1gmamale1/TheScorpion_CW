# Invector Third Person Controller - Melee Combat Template v2.6.5
## Comprehensive Research & Integration Guide for "The Scorpion"

**Last Updated**: 2026-03-22
**Template Version**: 2.6.5 (January 10, 2026)
**Unity Requirement**: 2022.3.12 LTS or higher

---

## Table of Contents
1. [Architecture Overview & Class Hierarchy](#1-architecture-overview--class-hierarchy)
2. [Setting Up a Player Character](#2-setting-up-a-player-character)
3. [Layers, Tags & Project Settings](#3-layers-tags--project-settings)
4. [Melee Combat System](#4-melee-combat-system)
5. [Damage & Health System](#5-damage--health-system)
6. [Input System](#6-input-system)
7. [Animator Controller Structure](#7-animator-controller-structure)
8. [AI System (Enemy Creation)](#8-ai-system-enemy-creation)
9. [Lock-On Target System](#9-lock-on-target-system)
10. [Inventory & Weapon System](#10-inventory--weapon-system)
11. [Key Events & Callbacks](#11-key-events--callbacks)
12. [Extending Invector for The Scorpion](#12-extending-invector-for-the-scorpion)
13. [Common Pitfalls & Solutions](#13-common-pitfalls--solutions)
14. [Integration Strategy for Element System](#14-integration-strategy-for-element-system)
15. [API Quick Reference](#15-api-quick-reference)

---

## 1. Architecture Overview & Class Hierarchy

### Core Inheritance Chain (Player)

```
vHealthController                    (Health/Stamina management, TakeDamage)
  └── vCharacter                     (Death behavior, ragdoll, action triggers, interfaces)
      └── vThirdPersonMotor          (Physics, rigidbody, ground detection, movement)
          └── vThirdPersonAnimator   (Animation control, layers, blend trees)
              └── vThirdPersonController  (Sprint, Crouch, Jump, Roll, Strafe)
```

### Core Inheritance Chain (Input - drives the controller)

```
vThirdPersonInput                    (Base input: movement, camera, jump, sprint, roll)
  └── vMeleeCombatInput              (Adds: attack, block, lock-on, melee manager ref)
      └── vShooterMeleeInput         (Adds: aim, shoot - NOT needed for melee-only)
```

### Core Inheritance Chain (AI)

```
vHealthController
  └── vCharacter
      └── v_AIMotor                  (NavMeshAgent, pathfinding, AI states)
          └── v_AIAnimator           (AI animation control)
              └── v_AIController     (Target detection, patrol, chase, attack routines)
```

### Key Namespaces

| Namespace | Contents |
|-----------|----------|
| `Invector` | vDamage, vHealthController, base types |
| `Invector.vCharacterController` | vThirdPersonMotor/Animator/Controller, vThirdPersonInput, vCharacter |
| `Invector.vCharacterController.AI` | v_AIController, v_AIMotor, v_AIAnimator |
| `Invector.vMelee` | vMeleeManager, vMeleeWeapon, vMeleeAttackObject, vHitBox, vHitInfo |
| `Invector.vItemManager` | vItemManager, vEquipArea, vItem |

### Key Interfaces

| Interface | Purpose |
|-----------|---------|
| `vIHealthController` | Health get/set, onReceiveDamage, onDead events |
| `vIDamageReceiver` | TakeDamage(vDamage), onReceiveDamage event |
| `vICharacter` | animator, ragdolled, onActiveRagdoll |
| `vIMeleeFighter` | Melee combat interface for AI/player |
| `vIAttackReceiver` | OnReceiveAttack(vDamage, vIMeleeFighter) |
| `vIAttackListener` | FinishAttack, OnEnableAttack, OnDisableAttack |

---

## 2. Setting Up a Player Character

### Quick Setup (10-Second Method)

1. Import the FBX model (must be **Humanoid** rig type)
2. Navigate to `Invector > Melee Combat > Create Melee Controller`
3. Assign the humanoid FBX model
4. Click "Create"

### What Gets Auto-Created

The wizard automatically sets up:
- **Capsule Collider** (sized to model proportions)
- **Rigidbody** (configured for character physics)
- **Layer assignments** (Player layer)
- **Tag** ("Player")
- **vThirdPersonController** component
- **vMeleeCombatInput** component
- **vMeleeManager** component
- **ThirdPersonCamera** (separate GameObject)
- **UI Canvas** with health/stamina HUD

### Manual Component Checklist

If setting up manually, ensure:
1. `vThirdPersonController` (or subclass) on root
2. `vMeleeCombatInput` on root
3. `vMeleeManager` on root
4. `Animator` with proper Invector Animator Controller
5. `Rigidbody` (use kinematic = false, gravity = true)
6. `Capsule Collider` (height matching character)
7. Proper layer/tag assignment

---

## 3. Layers, Tags & Project Settings

### Required Import

**CRITICAL**: Import the project settings via `Invector > Import ProjectSettings` or manually import:
- `TagManager.asset` — Tags and Layers
- `InputManager.asset` — Xbox360 controller mappings
- `DynamicsManager.asset` — Collision matrix

### Standard Layers

| Layer | Purpose |
|-------|---------|
| `Player` | Player character root and children |
| `Enemy` | Enemy AI root and capsule collider |
| `BodyPart` | Ragdoll colliders (for precise hit detection) |
| `IgnoreRaycast` | Weapons (prevents self-detection) |
| `Triggers` | Trigger volumes, interaction zones |
| `StopMove` | Invisible walls (prevents walking in place) |
| `Default` | Ground, environment |

### Standard Tags

| Tag | Purpose |
|-----|---------|
| `Player` | Player character root |
| `Enemy` | Enemy AI root |
| `Weapon` | Melee weapon objects |
| `CompanionAI` | Companion characters |

### Layer Rules

- Player root + all children: **Player** layer, **Player** tag
- Enemy root + capsule: **Enemy** layer, **Enemy** tag
- Enemy ragdoll bones: **BodyPart** layer
- Weapons: **IgnoreRaycast** layer, **Weapon** tag
- Ground: **Default** layer

---

## 4. Melee Combat System

### vMeleeManager — Central Combat Component

Located on the player/AI root. Manages all melee combat mechanics.

**Key Properties:**
```
defaultDamage = 10           // Base unarmed damage
defaultAttackDistance = 1.0   // Attack range
defaultStaminaCost = 20.0    // Stamina per attack
defaultStaminaRecoveryDelay = 1.0
defaultDefenseRate = 50      // Block effectiveness (%)
defaultDefenseRange = 90     // Block angle (degrees)
```

**Key Methods:**
```csharp
GetAttackID()                 // Current attack animation ID
GetDefenseID()                // Current defense animation ID
GetMoveSetID()                // Current weapon moveset
GetAttackDistance()            // Ideal attack range
GetAttackStaminaCost()        // Current attack stamina cost
SetLeftWeapon(vMeleeWeapon)   // Equip left hand
SetRightWeapon(vMeleeWeapon)  // Equip right hand
OnDamageHit(vHitInfo)         // Process damage event
OnRecoilHit(vHitInfo)         // Process recoil event
CanBlockAttack(Vector3)       // Check if attack can be blocked
CanBreakAttack()              // Check if defense breaks attack
```

**Key Events:**
```csharp
onDamageHit                   // Fired when damage lands on target
```

### vMeleeWeapon — Weapon Configuration

**Attack Properties:**
```
meleeType         // OnlyAttack, OnlyDefense, AttackAndDefense
attackID          // Animation ID for attack moveset
movesetID         // Locomotion moveset while equipped
distanceToAttack  // Ideal range
staminaCost       // Per-attack stamina drain
breakAttack       // Can interrupt enemy attacks
```

**Defense Properties:**
```
defenseID         // Defense animation ID
defenseRate       // Damage reduction % (0-100)
defenseRange      // Block angle in degrees
recoilID          // Recoil animation when blocking
```

**Damage Properties (inherited from vMeleeAttackObject):**
```
damage.damageValue         // Base damage
damage.staminaBlockCost    // Stamina drain on blocked hit
damage.ignoreDefense       // Bypass blocking
damage.activeRagdoll       // Trigger ragdoll on hit
damage.damageType          // String type (e.g., "Fire", "Lightning")
damage.reaction_id         // Hit reaction animation
damage.recoil_id           // Recoil animation
```

### Combo System

Combos are configured in the **Animator Controller**, not in code:

1. Attack animations are placed in the UpperBody layer
2. Triggered by `ATK_ID` (attack) and `DEF_ID` (defense) parameters
3. Each attack state needs the `vMeleeAttackBehaviour` StateMachineBehaviour with:
   - **StartDamage / EndDamage** — Frame window when hitbox is active
   - **AllowMovementAt** — When rotation unlocks during attack
   - **RecoilID** — Wall-impact reaction
   - **ReactionID** — Hit-reaction on target
   - **ResetTrigger** — Marks final combo hit
4. Transitions between attacks create combo chains
5. "You can set up as many combos as you want, just put the attack animation and apply a transition"

### vMeleeAttackObject — Hit Detection

Attached to weapons/body parts. Contains hitboxes and processes hits.

**Key:**
```csharp
hitBoxes              // List<vHitBox> - collision detectors
damage                // vDamage - damage data
canApplyDamage        // bool - enable/disable
onDamageHit           // Event on successful hit
onRecoilHit           // Event on recoil
onEnableDamage        // Event when damage activates
SetActiveDamage(bool) // Toggle hitbox active state
ApplyDamage(hitBox, collider, damage) // Apply damage to target
```

### vHitBox — Collision Detection

Individual colliders on weapon/body parts that detect hits. Created automatically by the character wizard for hands and feet.

---

## 5. Damage & Health System

### vDamage — Damage Data Class

```csharp
public class vDamage {
    public int damageValue = 15;           // Damage amount
    public float staminaBlockCost = 5;     // Stamina drain when blocked
    public float staminaRecoveryDelay = 1; // Recovery pause after hit
    public bool ignoreDefense;             // Bypass blocking
    public bool activeRagdoll;             // Trigger ragdoll
    public Transform sender;               // Who dealt damage
    public Transform receiver;             // Who received damage
    public Vector3 hitPosition;            // World hit point
    public bool hitReaction = true;        // Play reaction anim
    public int recoil_id = 0;             // Recoil animation ID
    public int reaction_id = 0;           // Reaction animation ID
    public string damageType;              // Custom type string (USE THIS FOR ELEMENTS!)

    // Constructors
    public vDamage();
    public vDamage(int value);
    public vDamage(vDamage damage);  // Copy constructor

    // Methods
    public void ReduceDamage(float damageReduction); // Apply % reduction
}
```

### vHealthController — Health Management

```csharp
public class vHealthController {
    // Fields
    public int maxHealth = 100;
    public float healthRecovery = 0f;
    public float healthRecoveryDelay = 0f;

    // Properties
    public int MaxHealth { get; protected set; }
    public float currentHealth { get; protected set; }
    public bool isDead { get; set; }
    public bool canRecoverHealth { get; }

    // Events (CRITICAL FOR CUSTOM SYSTEMS)
    public OnReceiveDamage onReceiveDamage { get; protected set; }  // Every TakeDamage call
    public OnDead onDead { get; protected set; }                     // When health reaches 0

    // Methods
    public void TakeDamage(vDamage damage);
    public void ChangeHealth(int value);        // Modify current
    public void ChangeMaxHealth(int value);     // Modify max
}
```

### vThirdPersonMotor — Stamina System

```csharp
// Stamina Fields
public float maxStamina = 200f;
public float staminaRecovery = 1.2f;
public float sprintStamina = 30f;
public float jumpStamina = 30f;
public float rollStamina = 25f;

// Stamina Events
public UnityEvent OnStaminaEnd;  // When stamina hits 0

// Stamina Methods
public void ReduceStamina(float value, bool accumulative);
public void ChangeStamina(int value);
public void ChangeMaxStamina(int value);
public void StaminaRecovery();
```

### vDamageReceiver — Body Part Damage

```csharp
public class vDamageReceiver : MonoBehaviour, vIDamageReceiver, vIAttackReceiver {
    public float damageMultiplier = 1f;   // Per-body-part multiplier
    public bool overrideReactionID;        // Custom reaction per part
    public int reactionID;                 // Specific reaction anim

    public OnReceiveDamage onReceiveDamage; // Per-receiver event

    public void TakeDamage(vDamage damage);
    public void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker);
}
```

### vObjectDamage — Environmental/Trap Damage

Attach to any object to make it deal damage on collision:

```
damageType        // String type for particles
damageValue       // Damage amount
staminaBlockCost  // Stamina cost if blocked
ignoreDefense     // Bypass blocking
activeRagdoll     // Trigger ragdoll
reaction_id       // Hit reaction (-1 = none)
continuousDamage  // Repeat damage (good for fire)
damageFrequency   // Interval between damage ticks
```

### Custom Damage Type System (Community Pattern)

For element-based damage modification:
```csharp
// Add DamageModifier component to characters
// Array of float values per damage type (-100 to +100)
// Negative = resistance, Positive = vulnerability
// Hook into vMeleeAttackObject.ApplyDamage() to apply modifier
// Match damage.damageType string to modifier index
```

---

## 6. Input System

### Input Architecture

Invector uses `GenericInput` class supporting simultaneous keyboard + gamepad:
- Index [0]: Keyboard mapping
- Index [1]: Gamepad mapping
- Index [2]: Alternative mapping

### vThirdPersonInput — Base Input (Basic Locomotion)

```csharp
// Movement
public GenericInput horizontalInput;  // "Horizontal" / "LeftAnalogHorizontal"
public GenericInput verticallInput;   // "Vertical" / "LeftAnalogVertical"

// Actions
public GenericInput jumpInput;        // "Space" / "X"
public GenericInput sprintInput;      // "LeftShift" / "LeftStickClick"
public GenericInput crouchInput;      // "C" / "Y"
public GenericInput strafeInput;      // "Tab" / "RightStickClick"
public GenericInput rollInput;        // "Q" / "B"

// Camera
public GenericInput rotateCameraXInput;  // "Mouse X" / "RightAnalogHorizontal"
public GenericInput rotateCameraYInput;  // "Mouse Y" / "RightAnalogVertical"
public GenericInput cameraZoomInput;     // "Mouse ScrollWheel"

// Control Methods
public void SetLockBasicInput(bool value);   // Lock all input
public void SetLockCameraInput(bool value);  // Lock camera only
public void MoveCharacter(Vector3/Transform); // Force movement
public void SetWalkByDefault(bool value);    // Walk-only mode

// References
public vThirdPersonController cc;     // Controller reference
public vThirdPersonCamera tpCamera;   // Camera reference
public vHUDController hud;            // HUD reference
```

### vMeleeCombatInput — Melee Input (extends vThirdPersonInput)

Adds attack, block, lock-on inputs:
```csharp
public vMeleeManager meleeManager;    // Reference to melee manager
public bool isBlocking;               // Currently blocking?
```

### Adding Custom Input (The Scorpion's Element System)

**Approach 1: Separate MonoBehaviour (RECOMMENDED - No Invector modification)**
```csharp
// Create a new script that reads input independently
// and calls your custom systems. Reference Invector components as needed.
public class ElementInputController : MonoBehaviour {
    private vThirdPersonController cc;
    private vMeleeCombatInput meleeInput;

    // Define your own GenericInput or use Unity's Input directly
    void Update() {
        if (Input.GetKeyDown(KeyCode.Q)) SwitchElement(ElementType.Fire);
        if (Input.GetKeyDown(KeyCode.E)) SwitchElement(ElementType.Lightning);
        if (Input.GetKeyDown(KeyCode.R)) ActivateAbility();
    }
}
```

**Approach 2: Extend vMeleeCombatInput (More integrated but modifies hierarchy)**
```csharp
public class ScorpionMeleeInput : vMeleeCombatInput {
    public GenericInput elementSwitchInput;
    public GenericInput abilityInput;

    protected override void InputHandle() {
        base.InputHandle();
        ElementInput();
        AbilityInput();
    }
}
```

### vGenericAction — Interaction Input

For interactable objects (doors, pickups, etc.):
- Add `vGenericAction` to player
- Add `vTriggerGenericAction` to interactable objects
- Configure input mappings in Inspector
- **Don't add too many** — consolidate under one action with different responses

---

## 7. Animator Controller Structure

### Animator Layers

| Layer | Purpose | Mask |
|-------|---------|------|
| Base Layer | Locomotion (walk, run, idle, jump) | Full Body |
| UpperBody | Attack animations, weapon movesets | Upper Body AvatarMask |

Additional masks available: LeftArm, RightArm, FullBody

### Key Animator Parameters

| Parameter | Type | Purpose |
|-----------|------|---------|
| `InputMagnitude` | Float | Movement speed (0-1) |
| `MoveSet_ID` | Int | Current moveset (unarmed=0, per weapon) |
| `ActionState` | Int | Current action state |
| `isGrounded` | Bool | On ground check |
| `isStrafing` | Bool | Strafe movement mode |
| `isSprinting` | Bool | Sprint state |
| `isCrouching` | Bool | Crouch state |
| `isDead` | Bool | Death state |
| `WeakAttack` | Trigger | Light attack trigger |
| `StrongAttack` | Trigger | Heavy attack trigger |
| `RecoilID` | Int | Recoil animation ID |
| `ReactionID` | Int | Hit reaction animation ID |
| `TriggerRecoil` | Trigger | Trigger recoil anim |
| `TriggerReaction` | Trigger | Trigger reaction anim |
| `ATK_ID` | Int | Attack ID (per weapon) |
| `DEF_ID` | Int | Defense ID |
| `HitDirection` | Int | Direction of incoming hit |
| `ResetState` | Trigger | Return to idle |

### Locomotion Setup

Uses Blend Trees:
- **Free Movement**: Blend by InputMagnitude (Idle → Walk → Run → Sprint)
- **Strafe Movement**: 2D Blend by horizontal/vertical speed
- Unarmed is base animation set; weapons override via MoveSet_ID

### Attack Animation Setup

Each attack state in the UpperBody layer needs:
1. `vMeleeAttackBehaviour` StateMachineBehaviour attached
2. Configure: StartDamage, EndDamage, AllowMovementAt, RecoilID, ReactionID
3. Chain attacks via transitions for combos
4. Last combo hit sets `ResetTrigger = true`

### Animation Tags

| Tag | Effect |
|-----|--------|
| `LockMovement` | Prevents movement, allows rotation |
| `LockRotation` | Prevents rotation |
| `Attack` | Currently attacking |
| `CustomAction` | Generic action playing |

### Using Mixamo Animations

1. Download from Mixamo as FBX (In Place for attacks)
2. Import to Unity, set to Humanoid rig
3. Replace animation clips in the Animator Controller states
4. Ensure root motion settings match (usually In Place for combat anims)
5. Adjust `vMeleeAttackBehaviour` timing per new animation

---

## 8. AI System (Enemy Creation)

### Quick AI Creation

1. Navigate to `Invector > Melee Combat > Create NPC`
2. Select Character Type: **Enemy AI**
3. Assign humanoid FBX model
4. Click Create
5. **Bake NavMesh** (CRITICAL — AI dies instantly without it)

### v_AIController — AI Brain

**Target Management:**
```csharp
SetCurrentTarget(Transform target)
RemoveCurrentTarget()
AddTagsToDetect(string tag)
RemoveTagToDetect(string tag)
```

**Attack Control:**
```csharp
FinishAttack()
OnEnableAttack()
OnDisableAttack()
ResetAttackTriggers()
BreakAttack(int breakAtkID)
OnRecoil(int recoilID)
OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
```

**Events:**
```csharp
onIdle    // UnityEvent — entering idle state
onChase   // UnityEvent — pursuing target
onPatrol  // UnityEvent — patrolling waypoints
```

**Update Frequencies:**
```
stateRoutineIteration = 0.15f       // AI state check interval
destinationRoutineIteration = 0.25f // Path update interval
findTargetIteration = 0.25f         // Target scan interval
smoothSpeed = 5f                    // Movement smoothing
```

### AI Behavioral States

The AI cycles through coroutine-based states:
1. **Idle()** — Standing still, waiting
2. **PatrolWaypoints()** — Following waypoint path
3. **PatrolSubPoints()** — Exploring within waypoint area
4. **Chase()** — Pursuing detected target
5. **MeleeAttackRoutine()** — Executing melee attack sequence

### AI Configuration in Inspector

**Detection:**
- `agressiveAtFirstSight` — Chase on first detect (vs passive until attacked)
- Field of View — Detection cone angle
- Min Detect Distance — Sense enemies even outside FOV
- Strafe Distance — Circling distance during combat

**Combat Behavior:**
- Block Chance — Probability of blocking
- Roll Chance — Probability of dodge rolling
- Attack Count — Attacks per sequence
- Attack Rotation Speed — Turn speed while attacking

**Movement:**
- `agent` (NavMeshAgent) — Pathfinding component
- Strafe Distance — Distance maintained during combat
- Attack Area — Range for initiating attacks

### Creating Custom Enemy Types (For The Scorpion)

**Hollow Monk (Basic):**
- Standard v_AIController
- Medium detection range, aggressive at first sight
- Low block chance, no roll
- 1-2 attack combos

**Shadow Acolyte (Fast):**
- Higher movement speed on NavMeshAgent
- Higher roll chance
- Quick attack animations
- Lower health

**Stone Sentinel (Heavy):**
- Slower NavMeshAgent speed
- High block chance
- Heavy attack animations with longer wind-up
- Higher health, more damage

### Waypoint System

Create via `3rd Person Controller > Component > New Waypoint Area`:
- **Shift + Left Click** — Place waypoints
- **Shift + Right Click** — Reposition waypoints
- **Ctrl + Click** — Create patrol sub-points

Properties:
- Max Visitors — Limit AI at each point
- Time to Stay — Duration at point
- Random Waypoints — Unpredictable paths

---

## 9. Lock-On Target System

### Setup

Add via `3rd Person Controller > Components > Lock-On`

### Configuration

- Activated through `LockOnInput` in vThirdPersonController
- Shows sprite indicator over locked target
- `aimImageSize` — Indicator size
- `Sprite Height` — Vertical offset

### Limitation

"This Lock-On currently works exclusively with our AI, it will not work out of the box with Non-Invector Characters because it needs the vCharacter interface."

All enemies MUST have vCharacter (or subclass) component for lock-on to detect them.

---

## 10. Inventory & Weapon System

### Option A: Full Inventory (vItemManager)

1. Add via `Invector > Inventory > ItemManager`
2. Assign inventory prefab and `vMelee_ItemListData`
3. Configure EquipPoints (default: LeftArm, RightArm)
4. Set up equipment slots and quick-access slots

**Weapon Item requires:**
- `Original Object` — Instantiated weapon with `vMeleeWeapon`
- `DropObject` — Collectable prefab
- `Attributes` — Preserve stats on drop/collect

### Option B: No Inventory (vCollectableStandalone)

Simpler system for games without inventory management:
1. Add `vCollectShooterMeleeControl` component
2. Create `defaultEquipPoint` GameObject
3. Weapons are both equipped and collectable objects

### Creating a Melee Weapon

1. `Invector > Melee Combat > Create Melee Weapon`
2. Select weapon mesh
3. System auto-creates hitbox structure
4. Set layer to **IgnoreRaycast**, tag to **Weapon**
5. Configure damage, stamina cost, attack/defense IDs

### Weapon Holder Manager

For weapon holstering/unholstering with animations:
- Requires ItemManager
- Add `vWeaponHolder` to empty GameObjects
- Configure EquipPointName, ItemID, HolderObject, WeaponObject

---

## 11. Key Events & Callbacks

### Health/Damage Events (vHealthController)

```csharp
// Subscribe to damage events
var healthController = GetComponent<vHealthController>();
healthController.onReceiveDamage.AddListener((vDamage damage) => {
    Debug.Log($"Took {damage.damageValue} damage of type {damage.damageType}");
    // Custom logic: apply DoT, stun, element effects
});

healthController.onDead.AddListener((GameObject deadObj) => {
    Debug.Log("Character died!");
    // Custom logic: adrenaline gain, wave counter, loot
});
```

### Melee Combat Events (vMeleeManager)

```csharp
var meleeManager = GetComponent<vMeleeManager>();
meleeManager.onDamageHit.AddListener((vHitInfo hitInfo) => {
    // Fired when THIS character's attack hits a target
    var targetCollider = hitInfo.targetCollider;
    var damage = hitInfo.attackObject.damage;
    // Custom logic: element application, adrenaline gain, combo tracking
});
```

### Melee Attack Events (vMeleeAttackObject)

```csharp
attackObject.onDamageHit  // Successful hit
attackObject.onRecoilHit  // Recoil (hit wall/shield)
attackObject.onEnableDamage // Hitbox activated
```

### Character Events (vCharacter)

```csharp
onActionEnter   // Trigger action started
onActionStay    // In trigger action
onActionExit    // Trigger action ended
onActiveRagdoll // Ragdoll state change
```

### Motor Events (vThirdPersonMotor)

```csharp
OnStaminaEnd    // Stamina depleted
```

### AI Events (v_AIController)

```csharp
onIdle          // AI entered idle
onChase         // AI started chasing
onPatrol        // AI started patrolling
```

### Damage Receiver Events (vDamageReceiver)

```csharp
// Per-body-part damage events
bodyPartReceiver.onReceiveDamage.AddListener((vDamage damage) => {
    // Custom logic per body part
});
```

---

## 12. Extending Invector for The Scorpion

### Element System Integration Strategy

**Use `vDamage.damageType` for element identification:**
```csharp
// When player attacks with Fire element active:
// Hook into vMeleeAttackObject or vMeleeManager to modify damage before it's applied

public class ElementDamageModifier : MonoBehaviour {
    private vMeleeManager meleeManager;
    private ElementSystem elementSystem;  // Your custom system

    void Start() {
        meleeManager = GetComponent<vMeleeManager>();
        meleeManager.onDamageHit.AddListener(OnPlayerHit);
    }

    void OnPlayerHit(vHitInfo hitInfo) {
        // Modify the damage type based on active element
        hitInfo.attackObject.damage.damageType = elementSystem.activeElement.ToString();

        // Add adrenaline on hit
        ultimateSystem.AddAdrenaline(2f);
    }
}
```

**Apply element effects on damage received:**
```csharp
public class ElementDamageReceiver : MonoBehaviour {
    private vHealthController healthController;

    void Start() {
        healthController = GetComponent<vHealthController>();
        healthController.onReceiveDamage.AddListener(OnDamageReceived);
    }

    void OnDamageReceived(vDamage damage) {
        switch (damage.damageType) {
            case "Fire":
                StartCoroutine(ApplyBurnDoT(damage.sender));
                break;
            case "Lightning":
                ApplyStun(duration: 1.5f);
                break;
        }
    }

    IEnumerator ApplyBurnDoT(Transform source) {
        // Fire DoT: X damage per second for Y seconds
        float duration = 4f;
        float tickRate = 0.5f;
        float damagePerTick = 3f;

        for (float t = 0; t < duration; t += tickRate) {
            if (healthController.isDead) yield break;
            var dotDamage = new vDamage((int)damagePerTick);
            dotDamage.damageType = "Fire";
            dotDamage.hitReaction = false;  // No flinch on DoT ticks
            dotDamage.sender = source;
            healthController.TakeDamage(dotDamage);
            yield return new WaitForSeconds(tickRate);
        }
    }

    void ApplyStun(float duration) {
        // Disable AI or player input temporarily
        var aiController = GetComponent<v_AIController>();
        if (aiController != null) {
            // Stun AI
            StartCoroutine(StunCoroutine(aiController, duration));
        }
    }
}
```

### Ultimate System (Adrenaline Rush) Integration

```csharp
public class AdrenalineSystem : MonoBehaviour {
    private vMeleeManager meleeManager;
    private vHealthController healthController;

    void Start() {
        meleeManager = GetComponent<vMeleeManager>();
        // +2 adrenaline per hit dealt
        meleeManager.onDamageHit.AddListener(hit => AddAdrenaline(2f));
    }

    public void ActivateAdrenalineRush() {
        // 8-second time slow + damage boost
        Time.timeScale = 0.3f;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        // Modify damage multiplier
        StartCoroutine(AdrenalineRushCoroutine());
    }
}
```

### Custom Input for Elements (No Invector Modification)

```csharp
public class ScorpionInputController : MonoBehaviour {
    [Header("Element Inputs")]
    public KeyCode fireElementKey = KeyCode.Alpha1;
    public KeyCode lightningElementKey = KeyCode.Alpha2;
    public KeyCode ability1Key = KeyCode.Q;
    public KeyCode ability2Key = KeyCode.E;
    public KeyCode ultimateKey = KeyCode.R;

    private ElementSystem elementSystem;
    private AdrenalineSystem adrenalineSystem;
    private vThirdPersonController cc;

    void Update() {
        if (cc.isDead) return;

        if (Input.GetKeyDown(fireElementKey))
            elementSystem.SwitchElement(ElementType.Fire);
        if (Input.GetKeyDown(lightningElementKey))
            elementSystem.SwitchElement(ElementType.Lightning);
        if (Input.GetKeyDown(ability1Key))
            elementSystem.UseAbility1();
        if (Input.GetKeyDown(ability2Key))
            elementSystem.UseAbility2();
        if (Input.GetKeyDown(ultimateKey))
            adrenalineSystem.ActivateAdrenalineRush();
    }
}
```

### Wave Manager Integration

```csharp
public class WaveManager : MonoBehaviour {
    public Transform[] spawnPoints;  // N, S, E, W

    void SpawnEnemy(GameObject prefab, Transform spawnPoint) {
        var enemy = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        // Hook into Invector's death event for wave tracking
        var health = enemy.GetComponent<vHealthController>();
        health.onDead.AddListener((obj) => {
            enemiesAlive--;
            // Award adrenaline
            FindObjectOfType<AdrenalineSystem>().AddAdrenaline(5f);
            CheckWaveComplete();
        });

        // Set AI target to player
        var ai = enemy.GetComponent<v_AIController>();
        ai.SetCurrentTarget(playerTransform);
    }
}
```

### Boss AI Integration

```csharp
// Boss extends v_AIController behavior through events and state checking
public class BossController : MonoBehaviour {
    private v_AIController aiController;
    private vHealthController healthController;

    private enum BossPhase { Phase1, Phase2, Phase3 }
    private BossPhase currentPhase = BossPhase.Phase1;

    void Start() {
        healthController = GetComponent<vHealthController>();
        healthController.onReceiveDamage.AddListener(CheckPhaseTransition);
    }

    void CheckPhaseTransition(vDamage damage) {
        float healthPercent = healthController.currentHealth / healthController.MaxHealth;

        if (healthPercent <= 0.6f && currentPhase == BossPhase.Phase1) {
            TransitionToPhase2();
        } else if (healthPercent <= 0.3f && currentPhase == BossPhase.Phase2) {
            TransitionToPhase3();
        }
    }
}
```

---

## 13. Common Pitfalls & Solutions

### Critical Setup Issues

| Problem | Cause | Solution |
|---------|-------|----------|
| Character floating/falling | Wrong ground layer | Set Ground Layer to "Default" |
| Camera through walls | Wrong culling layer | Set Culling Layer to "Default" |
| No damage dealt | Wrong weapon layer | Set weapon to "IgnoreRaycast" layer |
| AI dies instantly on play | No NavMesh | **Bake NavMesh** before testing |
| AI won't attack player | Wrong HitDamageTags | Set "Player" in MeleeManager > HitProperties > HitDamageTags |
| AI floats slightly | NavMeshAgent height wrong | Adjust NavMeshAgent height, verify capsule collider |
| Character snaps back after teleport | Direct transform.position | Use Invector's movement methods or loading system |
| Weapon invisible in inventory | Wrong layer | Set weapon to "IgnoreRaycast" |
| Lock-on doesn't find targets | Missing vCharacter | All targets need vCharacter component |
| Hit detection inconsistent | Layers wrong | Player=Player layer, Enemy=Enemy layer, BodyPart for ragdoll |
| Capsule collider wrong size | Scale/rig issue | Adjust height to match character feet-to-head |
| No ragdoll hit detection | Colliders disabled | Uncheck "Disable Colliders" in ragdoll component |
| Input not responding | Wrong InputManager | Import vProjectSettings package |

### Performance Tips

- AI state routines use coroutine iteration delays (0.15-0.25s) — don't make these too small
- Use NavMesh Agent priority to prevent AI pileups
- SphereSensor detection has max distance — set appropriately

### Integration Best Practices

1. **NEVER modify Invector source code** — always extend via events, subclassing, or separate components
2. **Use composition** — add your custom MonoBehaviours alongside Invector components
3. **Hook into events** — onReceiveDamage, onDead, onDamageHit for custom logic
4. **Use damageType string** — perfect for element system identification
5. **Respect the layer system** — incorrect layers are the #1 cause of issues
6. **Test with NavMesh** — always bake before testing AI
7. **Backup before updating** — Invector updates can break custom integrations

---

## 14. Integration Strategy for Element System

### Recommended Architecture (Composition Pattern)

```
[Player GameObject]
├── vThirdPersonController     (Invector - DO NOT MODIFY)
├── vMeleeCombatInput           (Invector - DO NOT MODIFY)
├── vMeleeManager               (Invector - DO NOT MODIFY)
├── ScorpionInputController     (Custom - handles Q/E/R/1/2 inputs)
├── ElementSystem               (Custom - element state, energy, switching)
├── ElementDamageModifier       (Custom - hooks into onDamageHit)
├── AdrenalineSystem            (Custom - tracks adrenaline, ultimate)
└── HUDController               (Custom - updates UI elements)

[Enemy GameObject]
├── v_AIController              (Invector - DO NOT MODIFY)
├── vMeleeManager               (Invector - DO NOT MODIFY)
├── ElementDamageReceiver       (Custom - applies DoT, stun, etc.)
└── EnemyTypeConfig             (Custom - Monk/Acolyte/Sentinel settings)

[Boss GameObject]
├── v_AIController              (Invector - DO NOT MODIFY)
├── vMeleeManager               (Invector - DO NOT MODIFY)
├── ElementDamageReceiver       (Custom - applies DoT, stun)
├── BossController              (Custom - phase management)
└── BossAbilities               (Custom - summons, fire aura, etc.)
```

### Element Energy System

```csharp
// Separate MonoBehaviour, hooks into Invector events
public class ElementSystem : MonoBehaviour {
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float energyRegenRate = 3f;    // Per second
    public float energyPerHit = 5f;        // Bonus on melee hit

    private vMeleeManager meleeManager;

    void Start() {
        meleeManager = GetComponent<vMeleeManager>();
        meleeManager.onDamageHit.AddListener(OnMeleeHit);
    }

    void OnMeleeHit(vHitInfo hitInfo) {
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyPerHit);
    }

    void Update() {
        // Passive regen
        currentEnergy = Mathf.Min(maxEnergy, currentEnergy + energyRegenRate * Time.deltaTime);
    }
}
```

### Hooking Elemental Damage Into Melee Attacks

The key insight: modify the `vDamage.damageType` string on your weapon's `vMeleeAttackObject.damage` when switching elements, then read it on the receiver side.

```csharp
// On element switch, update weapon damage type
public void SwitchElement(ElementType newElement) {
    activeElement = newElement;
    var meleeManager = GetComponent<vMeleeManager>();

    // Update right weapon damage type
    if (meleeManager.rightWeapon != null) {
        meleeManager.rightWeapon.damage.damageType = newElement.ToString();
    }
    // Update left weapon damage type
    if (meleeManager.leftWeapon != null) {
        meleeManager.leftWeapon.damage.damageType = newElement.ToString();
    }
}
```

---

## 15. API Quick Reference

### Most Used Classes

| Class | Get With | Purpose |
|-------|----------|---------|
| `vThirdPersonController` | `GetComponent<vThirdPersonController>()` | Player movement/actions |
| `vMeleeCombatInput` | `GetComponent<vMeleeCombatInput>()` | Player input handling |
| `vMeleeManager` | `GetComponent<vMeleeManager>()` | Combat management |
| `vHealthController` | `GetComponent<vHealthController>()` | Health/damage |
| `v_AIController` | `GetComponent<v_AIController>()` | AI behavior |
| `vDamage` | `new vDamage(int)` | Damage data |

### Most Used Events

| Event | On Class | Signature |
|-------|----------|-----------|
| `onReceiveDamage` | vHealthController | `Action<vDamage>` |
| `onDead` | vHealthController | `Action<GameObject>` |
| `onDamageHit` | vMeleeManager | `Action<vHitInfo>` |
| `OnStaminaEnd` | vThirdPersonMotor | `UnityEvent` |
| `onIdle` | v_AIController | `UnityEvent` |
| `onChase` | v_AIController | `UnityEvent` |

### Most Used Methods

```csharp
// Deal damage to anything with vHealthController
target.GetComponent<vHealthController>().TakeDamage(new vDamage(25));

// Check if character is dead
bool dead = healthController.isDead;

// Get current health percentage
float pct = healthController.currentHealth / healthController.MaxHealth;

// Lock player input (for cutscenes, stun)
GetComponent<vThirdPersonInput>().SetLockBasicInput(true);

// Set AI target
aiController.SetCurrentTarget(playerTransform);

// Change stamina
motor.ReduceStamina(20f, false);

// Trigger animation
animator.CrossFadeInFixedTime("CustomAbility", 0.1f);

// Check animator tag
controller.IsAnimatorTag("Attack");
```

---

## Sources

- [Unity Asset Store - Invector Melee Combat Template](https://assetstore.unity.com/packages/tools/game-toolkits/invector-third-person-controller-melee-combat-template-44227)
- [Invector Official Website](https://www.invector.xyz/)
- [Invector Official Forum & FAQ](https://invector.proboards.com/thread/11/faq-frequently-asked)
- [Invector API Reference (vHealthController)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_health_controller.html)
- [Invector API Reference (vMeleeManager)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_melee_1_1v_melee_manager.html)
- [Invector API Reference (vThirdPersonInput)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_third_person_input.html)
- [Invector API Reference (vThirdPersonController)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_third_person_controller.html)
- [Invector API Reference (vThirdPersonMotor)](https://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_third_person_motor.html)
- [Invector API Reference (v_AIController)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1_a_i_1_1v___a_i_controller.html)
- [Invector API Reference (vDamage)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_damage.html)
- [Invector API Reference (vMeleeWeapon)](https://ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_melee_1_1v_melee_weapon.html)
- [Invector API Reference (vMeleeAttackObject)](https://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_melee_1_1v_melee_attack_object.html)
- [Invector API Reference (vDamageReceiver)](https://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_damage_receiver.html)
- [Invector API Reference (vCharacter)](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_character.html)
- [Invector Melee Combat Documentation (PDFCoffee)](https://pdfcoffee.com/invector-documentation-meleecombat-pdf-free.html)
- [Invector Damage Modifier System (Forum)](https://invector.proboards.com/thread/3993/simple-damage-modifier-invector-controller)
- [Invector SpellSystem Addon (GitHub)](https://github.com/ShadesOfInsomnia/SpellSystem)
- [Invector FSM AI Template (Asset Store)](https://assetstore.unity.com/packages/tools/behavior-ai/invector-fsm-ai-template-123618)
- [Emerald AI - Invector Integration Tutorial](https://github.com/Black-Horizon-Studios/Emerald-AI/wiki/Invector-Integration-Tutorial)
- [Easy Multiplayer Invector - Damage Data](https://cyberbulletgames.com/easy-multiplayer-invector-ui-docs/HelpfulCode/getting_damage_data/)
- [Invector vThirdPersonController Source (GitHub LITE)](https://github.com/ReForge-Mode/Unity_VRoid_3D_Character_Controller_Invector_Free)
