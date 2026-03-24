# Deep Research: Invector Third Person Controller - Melee Combat Template (v2.6.5)

> Compiled: 2026-03-22 | 20+ references across official docs, API references, forums, GitHub repos, and community guides

---

## Table of Contents

1. [Class Hierarchy & Architecture](#1-class-hierarchy--architecture)
2. [Getting Started: Character Setup](#2-getting-started-character-setup)
3. [Input System & GenericInput](#3-input-system--genericinput)
4. [Melee Combat System](#4-melee-combat-system)
5. [vMeleeManager API Reference](#5-vmeleemanager-api-reference)
6. [Animator Controller & Custom Animations](#6-animator-controller--custom-animations)
7. [Extending vMeleeCombatInput with Custom Attacks](#7-extending-vmeleecombatinput-with-custom-attacks)
8. [Damage System & Custom Effects](#8-damage-system--custom-effects)
9. [Health Controller & Custom UI/HUD](#9-health-controller--custom-uihud)
10. [Enemy AI System](#10-enemy-ai-system)
11. [Lock-On Target System](#11-lock-on-target-system)
12. [Weapon Setup](#12-weapon-setup)
13. [Ragdoll & Death System](#13-ragdoll--death-system)
14. [Spell System Addon (Community)](#14-spell-system-addon-community)
15. [Common Pitfalls & Forum Solutions](#15-common-pitfalls--forum-solutions)
16. [Integration Patterns for The Scorpion](#16-integration-patterns-for-the-scorpion)

---

## 1. Class Hierarchy & Architecture

### Full Inheritance Chain (Player)

```
MonoBehaviour
  └── vHealthController          (Health/Stamina, TakeDamage, OnDead, OnReceiveDamage)
        └── vCharacter            (Animator, Ragdoll, DeathBy enum, Action triggers)
              └── vThirdPersonMotor   (Rigidbody, Movement, Stamina, Ground detection, Physics)
                    └── vThirdPersonAnimator  (Animator parameters, animation control)
                          └── vThirdPersonController  (Virtual movement methods)
                                └── vMeleeCombatInput  (Combat input handling, MeleeManager ref)
```

### Interfaces Implemented
- `vIHealthController` - Health management contract
- `vIDamageReceiver` - Damage receiving contract
- `vICharacter` - Character identity contract
- `vIMeleeFighter` - Melee combat contract

### Key Design Principle
**Invector uses a deep inheritance chain.** Every class builds on the one below it. Custom systems MUST extend or hook via events/callbacks -- never replace base classes. The recommended pattern is:

1. **Events/Callbacks** (preferred) - Subscribe to `onReceiveDamage`, `onDead`, `onDamageHit`
2. **Partial class extension** - Create new MonoBehaviours that reference Invector components
3. **Virtual method override** - Only when absolutely necessary, and in a subclass

### Source References
- API: `http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/`
- vHealthController: `Invector.vHealthController`
- vCharacter: `Invector.vCharacterController.vCharacter`
- vThirdPersonMotor: `Invector.vCharacterController.vThirdPersonMotor`

---

## 2. Getting Started: Character Setup

### Step-by-Step Character Creation

1. **Import the Package**: Import Invector Melee Combat Template from Asset Store
2. **Set Input Mode**: Edit > Project Settings > Player > Configuration > Active Input Handling > set to **"Both"** (Old + New)
3. **Import Project Settings**: Invector > Import ProjectSettings (imports required Input axes)
4. **Prepare Your FBX Model**:
   - Must be fully rigged humanoid
   - Set the FBX Rig > Animation Type to **Humanoid** in import settings
   - Scale Factor should be **1** for proper ragdoll behavior
5. **Create the Controller**:
   - Go to **Invector > Melee Combat > Create Melee Controller**
   - Assign your FBX Model to the "FBX Model" field
   - Click **Create**

### What the Character Creator Does Automatically
- Creates ThirdPersonController GameObject with all required components
- Adds Capsule Collider, Rigidbody, all Invector scripts
- Sets up correct Layers and Tags
- Creates ThirdPersonCamera
- Creates UI Canvas with HUD (health, stamina display)
- Configures animator controller with all melee combat layers

### Layer/Tag Requirements
- Player layer: **Player**
- Enemy layer: **Enemy**
- These must exist in your project's layer/tag settings
- The Character Creator adds them automatically

### Source
- [Invector Third Person Controller Guide (Scribd)](https://www.scribd.com/document/471108297/Invector-Third-Person-Controller-Basic-Locomotion-Guide)
- [Unity Asset Store - Melee Combat Template](https://assetstore.unity.com/packages/tools/game-toolkits/invector-third-person-controller-melee-combat-template-44227)

---

## 3. Input System & GenericInput

### GenericInput Class

Invector uses its own `GenericInput` class wrapping keyboard, gamepad, and mobile inputs:

```csharp
public GenericInput myNewInput = new GenericInput("keyboardButtonName",
    "joystickButtonName", "mobileButtonName");
```

Three parameters: keyboard key name, joystick button name, mobile button name.

### Adding Custom Inputs to vThirdPersonInput

```csharp
// 1. Declare input in your input script (or a class extending vThirdPersonInput)
public GenericInput elementSwitchInput = new GenericInput("Q", "JoystickButton4", "");

// 2. Create handler method
protected virtual void ElementSwitchMethod()
{
    if (elementSwitchInput.GetButtonDown())
    {
        // Trigger your custom system
        cc.animator.CrossFadeInFixedTime("ElementSwitch", 0.1f);
    }
}

// 3. Call from InputHandle()
protected override void InputHandle()
{
    base.InputHandle();
    ElementSwitchMethod();
}
```

### Key Input Methods
- `GetButton()` - Held down
- `GetButtonDown()` - First frame pressed
- `GetButtonUp()` - Released
- `GetDoubleButtonDown()` - Rapid double-tap (built-in to GenericInput)

### Double Button Detection (for Dodge)
```csharp
if (myNewInput.GetDoubleButtonDown() && !actions)
    cc.animator.CrossFadeInFixedTime("DodgeAnimation", 0.1f);
```

### Important: CrossFadeInFixedTime vs Bool Parameters
Modern Invector versions use `cc.animator.CrossFadeInFixedTime("AnimationName", 0.1f)` to trigger animations directly, **NOT** boolean animator parameters. This is a change from v1.0.

### New Input System Compatibility
As of v2.6.5, Invector does **NOT** natively support Unity's New Input System. Their input system is "buried deep into our core" (official developer statement, Nov 2021). Workaround: Set Active Input Handling to "Both" and layer custom New Input System scripts on top. Community members report basic movement works but inventory integration requires custom work.

### Source
- [Invector Forum: Adding inputs and animation](https://invector.proboards.com/thread/366/adding-inputs-animation-custom-controller)
- [Invector Forum: New Input System](https://invector.proboards.com/thread/4223/new-input-system)

---

## 4. Melee Combat System

### Core Architecture

The melee combat system consists of:

| Component | Role |
|-----------|------|
| `vMeleeCombatInput` | Handles player input for attacks, defense, combos |
| `vMeleeManager` | Manages weapons, hitboxes, damage calculation, body members |
| `vMeleeWeapon` | Weapon component with damage, moveset, attack/defense config |
| `vMeleeAttackBehaviour` | Animator StateMachineBehaviour for attack states |
| `vHitBox` | Collider-based hit detection on character body parts |
| `vBodyMember` | Maps body parts to hitboxes |

### Combat Flow
1. Player presses attack input
2. `vMeleeCombatInput` reads input and triggers animator
3. Animator enters attack state with `vMeleeAttackBehaviour`
4. `vMeleeAttackBehaviour` tells `vMeleeManager` which body parts/hitboxes to activate
5. Active hitboxes detect collisions with damageable targets
6. `vMeleeManager` calculates damage and calls `TakeDamage()` on the target
7. `onDamageHit` event fires

### Attack Configuration via Animator
- Each attack animation state has a `vMeleeAttackBehaviour` attached
- The behaviour specifies:
  - **HitboxFrom**: LeftArm, RightArm, LeftLeg, RightLeg, BothArms, BothLegs, etc.
  - **Damage type**: String identifier (e.g., "Fire", "Lightning")
  - **Attack ID**: Integer linking to weapon movesets
  - **Recoil ID**: Animation reaction on the target
  - **Damage multiplier**: Scales weapon base damage

### Combo System
- Combos are handled through **animator state transitions**
- Each attack state can transition to the next attack state on input
- Timing windows are controlled by animator transition conditions
- A blend tree uses unarmed animations as base, overlaying weapon-specific animations
- Combo detection uses sequential button monitoring within time windows (default 0.3s)

### Weapon Moveset System
- Each weapon has a **Moveset ID**
- The animator controller has blend trees or sub-state machines per moveset
- Changing weapons changes the active moveset, which changes the attack animation set
- Weapons can be set to: Attack, Defense, or Both

### Source
- [Invector Forum: Melee Combat](https://invector.proboards.com/thread/84/released-melee-combat-v2-0c)
- [Invector Forum: Attack Combo System](https://invector.proboards.com/thread/106/attack-combo-system)
- [Invector Forum: Adding New Attacks](https://invector.proboards.com/thread/437/adding-new-attacks)

---

## 5. vMeleeManager API Reference

### Class: `Invector.vMelee.vMeleeManager`

#### Public Properties/Fields

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `Members` | `List<vBodyMember>` | new list | Body part hitbox mappings |
| `defaultDamage` | `vDamage` | 10 | Base damage when no weapon |
| `defaultAttackDistance` | `float` | 1f | Unarmed attack range |
| `defaultStaminaCost` | `float` | 20f | Stamina per unarmed attack |
| `defaultStaminaRecoveryDelay` | `float` | 1f | Delay before stamina regen after attack |
| `defaultDefenseRate` | `int` | 50 | Unarmed defense percentage |
| `defaultDefenseRange` | `float` | 90 | Defense angle in degrees |
| `hitProperties` | `HitProperties` | - | Hit detection configuration |
| `leftWeapon` | `vMeleeWeapon` | - | Currently equipped left weapon |
| `rightWeapon` | `vMeleeWeapon` | - | Currently equipped right weapon |
| `onDamageHit` | `vOnHitEvent` | - | Event: damage dealt to target |
| `fighter` | `vIMeleeFighter` | - | Reference to the fighter interface |

#### Public Methods

```csharp
// Attack activation
void SetActiveAttack(List<string> bodyParts, vAttackType type, ...)
void SetActiveAttack(string bodyPart, vAttackType type, ...)

// Query methods
int GetAttackID()
float GetAttackDistance()          // Ideal distance for current attack
float GetAttackStaminaCost()
float GetAttackStaminaRecoveryDelay()
int GetDefenseID()
int GetDefenseRate()               // Defense damage reduction percentage
int GetDefenseRecoilID()           // Animation ID when defense breaks
int GetMoveSetID()                 // Current weapon moveset

// Validation
bool CanBlockAttack(Vector3 attackPoint)   // Check if block is possible from angle
bool CanBreakAttack()                       // Check if defense can break the attack

// Event handlers
void OnDamageHit(vHitInfo hitInfo)   // Damage event listener
void OnRecoilHit(vHitInfo hitInfo)   // Recoil event listener
void OnDefense()                      // Defense event trigger

// Weapon management
void SetLeftWeapon(GameObject weaponObj)
void SetLeftWeapon(vMeleeWeapon weapon)
void SetRightWeapon(GameObject weaponObj)
void SetRightWeapon(vMeleeWeapon weapon)
```

### Critical Integration Point for The Scorpion

To add custom damage effects (Fire DoT, Lightning stun), hook into `onDamageHit`:

```csharp
var meleeManager = GetComponent<vMeleeManager>();
meleeManager.onDamageHit.AddListener(OnMeleeDamageHit);

void OnMeleeDamageHit(vHitInfo hitInfo)
{
    // hitInfo.attackObject - the weapon/attack source
    // hitInfo.targetCollider - what was hit
    // hitInfo.attackObject.damage - the vDamage object
    // Apply elemental effects here
}
```

### Source
- [vMeleeManager API](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_melee_1_1v_melee_manager.html)
- [Emerald AI Integration Tutorial](https://github.com/Black-Horizon-Studios/Emerald-AI/wiki/Invector-Integration-Tutorial)

---

## 6. Animator Controller & Custom Animations

### Animator Layer Structure

Invector's melee animator uses multiple layers with AvatarMasks:
- **Base Layer** - Locomotion (movement blend trees)
- **Left Arm** - Left arm overrides (AvatarMask)
- **Right Arm** - Right arm overrides (AvatarMask)
- **Upper Body** - Upper body actions (AvatarMask)
- **Full Body** - Full body actions (attacks, rolls, etc.)

### Adding Custom Animation States

1. Create a **SubStateMachine** in the appropriate layer
2. Add your animation state inside it
3. Configure **transition to Exit** with Exit Time enabled
4. Create **transition from SubStateMachine back to Locomotion**

This ensures proper flow: Locomotion -> Custom Action -> back to Locomotion.

### Triggering Custom Animations

```csharp
// Modern approach (v2.x+): Direct CrossFade
cc.animator.CrossFadeInFixedTime("YourAnimationState", 0.1f);

// Alternative: Set animator trigger
cc.animator.SetTrigger("YourTrigger");
```

### Attack Animation Setup

For each attack animation state:
1. Attach `vMeleeAttackBehaviour` StateMachineBehaviour
2. Configure:
   - **HitboxFrom**: Which body part (LeftArm, RightArm, BothLegs, etc.)
   - **Damage Type**: String identifier for elemental/effect matching
   - **Attack ID**: Links to weapon moveset configuration
   - **Recoil ID**: What reaction the target plays
3. Set transition conditions for combo chains

### Moveset System

Weapons define a Moveset ID. The animator uses this to select which attack blend tree/sub-state machine to use:
- Moveset 0: Unarmed
- Moveset 1: Sword
- Moveset 2: Dual Blades (configure for The Scorpion)
- etc.

### Source
- [Invector Forum: Adding Animations](https://invector.proboards.com/thread/1504/adding-animations)
- [Invector Forum: Weapon Animation State Change](https://invector.proboards.com/thread/2795/help-weapon-animation-state-change)

---

## 7. Extending vMeleeCombatInput with Custom Attacks

### Approach 1: Add Custom Input to Existing System

```csharp
using Invector.vCharacterController;
using Invector.vMelee;

public class vCustomMeleeCombatInput : vMeleeCombatInput
{
    // New inputs for elemental abilities
    public GenericInput fireAbilityInput = new GenericInput("Alpha1", "JoystickButton5", "");
    public GenericInput lightningAbilityInput = new GenericInput("Alpha2", "JoystickButton6", "");
    public GenericInput elementSwitchInput = new GenericInput("Q", "JoystickButton4", "");

    protected override void InputHandle()
    {
        base.InputHandle(); // Process all default melee inputs

        ElementalInputHandle();
    }

    protected virtual void ElementalInputHandle()
    {
        if (fireAbilityInput.GetButtonDown())
        {
            // Trigger fire ability animation
            cc.animator.CrossFadeInFixedTime("FireAbility", 0.1f);
            // Notify element system
            SendMessage("OnFireAbilityUsed", SendMessageOptions.DontRequireReceiver);
        }

        if (lightningAbilityInput.GetButtonDown())
        {
            cc.animator.CrossFadeInFixedTime("LightningAbility", 0.1f);
            SendMessage("OnLightningAbilityUsed", SendMessageOptions.DontRequireReceiver);
        }

        if (elementSwitchInput.GetButtonDown())
        {
            SendMessage("OnElementSwitch", SendMessageOptions.DontRequireReceiver);
        }
    }
}
```

### Approach 2: Separate Component (Recommended for The Scorpion)

Instead of subclassing, create a standalone component that reads Invector state:

```csharp
using UnityEngine;
using Invector.vMelee;
using Invector.vCharacterController;

[RequireComponent(typeof(vMeleeCombatInput))]
public class ElementCombatInput : MonoBehaviour
{
    private vMeleeCombatInput meleeInput;
    private vMeleeManager meleeManager;

    void Start()
    {
        meleeInput = GetComponent<vMeleeCombatInput>();
        meleeManager = GetComponent<vMeleeManager>();

        // Subscribe to damage events
        meleeManager.onDamageHit.AddListener(OnDamageHit);
    }

    void OnDamageHit(vHitInfo hitInfo)
    {
        // Apply elemental effects based on current element
        // Access damage type: hitInfo.attackObject.damage.damageType
    }
}
```

### Approach 3: Adding New Attack Types (AOE, Projectile)

For attacks that are NOT standard melee swings:
- **Animation-integrated AOE**: Add a 360-degree attack animation, use existing hitbox system with wider colliders
- **Separate collision geometry**: Spawn a trigger collider during the animation, manually call `TakeDamage()` on targets
- **Projectile attacks**: Requires either the Shooter Template or custom projectile scripts that call `TakeDamage()` on hit

### Key Method: RollConditions()

The `RollConditions()` method in `vMeleeCombatInput` controls when dodge/roll is allowed. Override to add custom conditions:

```csharp
protected override bool RollConditions()
{
    // Add custom conditions (e.g., not during ability cast)
    return base.RollConditions() && !isCastingAbility;
}
```

### Source
- [Invector Forum: Interrupt Attacks Rolling](https://invector.proboards.com/thread/4675/interrupt-attacks-when-rolling)
- [Invector Forum: Adding New Attacks](https://invector.proboards.com/thread/437/adding-new-attacks)
- [Emerald AI Invector Integration](https://github.com/Black-Horizon-Studios/Emerald-AI/wiki/Invector-Integration-Tutorial)

---

## 8. Damage System & Custom Effects

### vDamage Class Properties

```csharp
vDamage damage = new vDamage(damageAmount);

// Key properties:
damage.damageValue      // int: Amount of damage
damage.sender           // Transform: Who dealt the damage (root = attacker GO)
damage.receiver         // Transform: Hit body part transform (root = target GO)
damage.hitPosition      // Vector3: World position of hit
damage.reaction_id      // int: Animation reaction to play
damage.damageType       // string: "Fire", "Lightning", "Physical", etc.
damage.hitReaction       // bool: Whether target should play hit reaction
```

### Applying Damage Externally

```csharp
// Create and apply damage to any object with vHealthController
vDamage damage = new vDamage(25);
damage.sender = transform;
damage.hitPosition = targetTransform.position;
damage.damageType = "Fire";
damage.reaction_id = 1;

// Apply to target
var healthController = target.GetComponent<vHealthController>();
if (healthController != null)
    healthController.TakeDamage(damage);
```

### Custom Damage Modifier System (Community Solution)

A community-developed system for elemental damage resistance/vulnerability:

```csharp
// DamageModifier.cs - Attach to characters
public class DamageModifier : MonoBehaviour
{
    // Damage types: Fire=0, Ice=1, Impact=2, Lightning=3, Poison=4,
    //               Acid=5, Radiant=6, Shadow=7, Necrotic=8
    public int[] damageModifiers = new int[9]; // -100 (immune) to +100 (vulnerable)

    public float GetModifier(string damageType)
    {
        int index = GetDamageTypeIndex(damageType);
        return damageModifiers[index] / 100f;
    }

    private int GetDamageTypeIndex(string type)
    {
        switch(type)
        {
            case "Fire": return 0;
            case "Ice": return 1;
            case "Impact": return 2;
            case "Lightning": return 3;
            case "Poison": return 4;
            default: return 2; // Impact as default
        }
    }
}
```

### Integration Point: Adding to MeleeAttackObject.cs

To apply modifiers before damage calculation:
```csharp
// In MeleeAttackObject's ApplyDamage method, add:
DamageModifier dmgModifier = targetCollider.GetComponentInParent<DamageModifier>();
if (dmgModifier != null)
{
    float mod = dmgModifier.GetModifier(damage.damageType);
    damage.damageValue = Mathf.RoundToInt(damage.damageValue * (1f + mod));
}
```

### Custom Hit Particles by Damage Type

Using `vHitDamageParticle`:
1. Add `vHitDamageParticle` component to the character receiving damage
2. Expand `CustomDamageEffect` array
3. Set `DamageType` string to match your damage source EXACTLY (string comparison)
4. Assign particle prefab to `Effects Prefab`

**Damage type origins:**
- Melee weapons: Set via `vMeleeAttackBehaviour` > Damage Type parameter in animator
- Projectiles: Set in the projectile prefab
- Environmental: Set in `vObjectDamage` > Damage Options

### Accessing Damage Data via Events

```csharp
// Subscribe to damage events
var healthController = GetComponent<vHealthController>();
healthController.onReceiveDamage.AddListener(OnDamaged);

void OnDamaged(vDamage damage)
{
    Debug.Log($"Received {damage.damageValue} {damage.damageType} damage from {damage.sender.root.name}");
    Debug.Log($"Hit body part: {damage.receiver.name}");

    // Headshot detection
    bool isHeadshot = damage.receiver.name == "Head";
}
```

### Source
- [Invector Forum: Simple Damage Modifier](https://invector.proboards.com/thread/3993/simple-damage-modifier-invector-controller)
- [Invector Forum: Custom Hit Damage Particle](https://invector.proboards.com/thread/2013/custom-hit-damage-particle)
- [Easy Multiplayer Invector: Getting Damage Data](https://cyberbulletgames.com/easy-multiplayer-invector-ui-docs/HelpfulCode/getting_damage_data/)

---

## 9. Health Controller & Custom UI/HUD

### vHealthController API

```csharp
// Properties
int MaxHealth { get; protected set; }       // Default: 100
float currentHealth { get; protected set; }
bool isDead { get; set; }
bool canRecoverHealth { get; }              // Virtual

// Fields
int maxHealth = 100;
float healthRecovery = 0f;
float healthRecoveryDelay = 0f;

// Methods
void ChangeHealth(int value)        // Modify current health
void ChangeMaxHealth(int value)     // Modify max health
void TakeDamage(vDamage damage)     // Apply damage

// Events
OnReceiveDamage onReceiveDamage;    // Fires when damage received
OnDead onDead;                       // Fires on death
```

### vThirdPersonMotor Stamina API

```csharp
// Fields
int maxStamina = 200;
float staminaRecovery = 1.2f;
float sprintStamina = 30f;      // Cost per second while sprinting
float jumpStamina = 30f;        // Cost per jump
float rollStamina = 25f;        // Cost per roll
UnityEvent OnStaminaEnd;        // Fired when stamina hits 0

// Methods
void ChangeStamina(int value)
void ChangeMaxStamina(int value)
void ReduceStamina(float value, bool accumulative)
void StaminaRecovery()
```

### Custom HUD Integration

The default HUD uses `Image.fillAmount` for health/stamina bars. To create a custom HUD:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Invector.vCharacterController;

public class CustomHUDController : MonoBehaviour
{
    public Image healthBar;
    public Image staminaBar;
    public Image adrenalineBar;      // Custom for The Scorpion
    public Image elementIndicator;   // Custom for The Scorpion

    private vThirdPersonMotor motor;

    void Start()
    {
        motor = FindObjectOfType<vThirdPersonMotor>();

        // Subscribe to events
        motor.onReceiveDamage.AddListener(OnPlayerDamaged);
        motor.onDead.AddListener(OnPlayerDead);
    }

    void Update()
    {
        if (motor == null) return;

        // Health bar
        healthBar.fillAmount = motor.currentHealth / motor.MaxHealth;

        // Stamina bar (access via reflection or public property)
        // Note: currentStamina may need protection level change or
        // access through the motor's public interface
    }

    void OnPlayerDamaged(vDamage damage)
    {
        // Flash damage indicator, screen shake, etc.
    }

    void OnPlayerDead(GameObject player)
    {
        // Show death screen
    }
}
```

### Known Issue: Protected Access
`vHealthController` has some properties with `protected set`. If you need write access from external scripts, either:
1. Create a subclass
2. Modify line 38 of `vHealthController.cs` to remove `protected` (NOT recommended - modifies Invector source)
3. Use `ChangeHealth()` / `ChangeMaxHealth()` public methods instead

### Namespace
Always use: `using Invector.vCharacterController;` (NOT the deprecated `CharacterController` namespace)

### Source
- [Invector Forum: Customizing UI](https://invector.proboards.com/thread/4649/customizing-ui)
- [vHealthController API](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_health_controller.html)

---

## 10. Enemy AI System

### Simple Melee AI Components

The melee template includes a Simple Melee AI system. Key components:

| Component | Purpose |
|-----------|---------|
| `vControlAIMelee` | Main AI controller for melee enemies |
| `vMeleeManager` | Handles AI weapon/hitbox/damage (same as player) |
| `vAIHeadTrack` | Head look-at target tracking |
| `NavMeshAgent` | Unity navigation for pathfinding |

### AI Setup Steps

1. **Create AI**: Invector > Melee Combat > Create Melee AI
2. **Configure Tags/Layers**:
   - Set Unity Tag to **"Enemy"**
   - Set Unity Layer to **"Enemy"**
3. **Configure Detection**:
   - Set "Tags To Detect" to include "Player"
   - Set "Hit Damage Tags" for what the AI can damage
4. **Configure vMeleeManager**:
   - Open vMeleeManager on the AI
   - Press "Damage Layers" button
   - Add **Enemy** layer to the Layer Mask (so player can damage enemies)
5. **NavMesh**: Bake NavMesh in your scene (Window > AI > Navigation)

### AI Behavioral States
- **Idle**: Default state, standing still
- **Patrol**: Following waypoints
- **Chase**: Pursuing detected target (configurable chase distance)
- **Attack**: Executing attack animations near target
- **Block**: Defensive stance

### Waypoint Patrol Setup
1. Create 2+ waypoint GameObjects in the scene
2. Connect them: Waypoint1.nextTarget = Waypoint2, Waypoint2.nextTarget = Waypoint3
3. Assign first waypoint to AI's patrol configuration

### AI Combat Configuration
- **Attack Distance**: How close AI must be to attack
- **Chase Distance**: How far AI will pursue (increase if AI circles instead of attacking)
- **Detection Range**: How far AI can see targets
- **Field of View**: Angular detection range

### Critical: AI Target Recognition
The AI recognizes targets through `vCharacterStandalone` component. For custom targets:
- Must have: `vCharacterStandalone`, Capsule Collider, Rigidbody, Ragdoll
- Without ragdoll: damage applies once but animations don't trigger

### FSM AI (Separate Product)
Invector also sells an FSM AI Template with a visual node editor. The melee template's "Simple AI" is simpler but functional for basic enemy types.

### Source
- [Invector Forum: Melee AI Can't Attack](https://invector.proboards.com/thread/3250/melee-ai-enemy-attack-player)
- [Invector Forum: Enemy AI](https://invector.proboards.com/thread/2334/enemy-ai)
- [Emerald AI Integration](https://github.com/Black-Horizon-Studios/Emerald-AI/wiki/Invector-Integration-Tutorial)

---

## 11. Lock-On Target System

### Overview
Invector includes a lock-on targeting system similar to Dark Souls. The player can lock onto nearby enemies and strafe around them.

### Configuration Points
- **Input**: Default Tab key to toggle lock-on
- **Target Tags**: Enemies must have the correct tag to be targetable
- **Target Layers**: Configure which layers are valid lock-on targets
- **Obstacle Detection**: Lock-on should break when target is behind walls (configure with raycasts/layers)
- **Switch Target**: Can switch between multiple targets while locked on

### Key Behaviors When Locked On
- Camera focuses on target
- Player movement switches to **strafe mode** (circle-strafing)
- Attacks are directed toward the locked target
- `isStrafing` property becomes true on the motor

### Common Issues & Solutions
- **Locking onto enemies behind walls**: Add obstacle layer checks to lock-on raycast
- **Lock-on in topdown mode**: Requires custom configuration as default is designed for third-person camera
- **Auto-lock for melee**: Community members have created auto-lock systems that engage when attacking

### Source
- [Invector Forum: Lock On Obstacles](https://invector.proboards.com/thread/3185/lock-on-target-obstacles)
- [Invector Forum: Auto Lock Melee](https://invector.proboards.com/thread/2740/create-auto-lock-melee-system)

---

## 12. Weapon Setup

### Creating a Custom Melee Weapon

1. **Use the Weapon Creator**: Invector > Melee Combat > Create Melee Weapon
   - Select Attack, Defense, or Both
2. **Configure the Weapon Prefab**:
   - Add `vMeleeWeapon` component
   - Set damage values, moveset ID, attack/defense IDs
   - Configure hitbox colliders on the weapon mesh
3. **Handler/Equip Point Setup**:
   - On the character: ItemManager > EquipPoints > add custom handler
   - In ItemListData: set the handler name to **exactly match** the EquipPoint name
4. **Moveset Configuration**:
   - Set Moveset ID on the weapon
   - This ID maps to animator blend trees/sub-state machines
   - Different weapons can share movesets or have unique ones

### Weapon Properties (vMeleeWeapon)

- **Damage**: Base damage value
- **Damage Type**: String (e.g., "Physical", "Fire")
- **Moveset ID**: Which animator moveset to use
- **Attack ID**: Specific attack configuration
- **Defense ID**: Specific defense configuration
- **Recoil ID**: Hit reaction on targets
- **Stamina Cost**: Override per weapon
- **Attack Distance**: Ideal range for this weapon

### Dual Wielding (Critical for The Scorpion)

The system supports dual wielding through `leftWeapon` and `rightWeapon` on vMeleeManager. Configure:
- Left hand holder/equip point
- Right hand holder/equip point
- Both weapons active simultaneously
- HitboxFrom in animator can specify BothArms for dual attacks

### Common Weapon Setup Issue
**Problem**: Weapon doesn't appear in character's hands despite being in inventory
**Solution**: Ensure the EquipPoint name on the character's ItemManager matches EXACTLY the handler name in the ItemListData item configuration.

### Source
- [Invector Forum: Create Custom Melee Weapon](https://invector.proboards.com/thread/3495/create-equip-custom-melee-weapon)
- [Invector Forum: Melee Questions](https://invector.proboards.com/thread/894/melee)

---

## 13. Ragdoll & Death System

### Creating a Ragdoll

Go to **Invector > Basic Locomotion > Components > Ragdoll**:
1. Select your character in Hierarchy (fields auto-fill)
2. Click **Create**
3. Keep **Enable Projection** and **Proportional Mass** enabled
4. Use **Scale Factor 1** on your FBX Model

**Best practice**: Add ragdoll BEFORE adding other components to the character.

### Death Configuration

On the Controller Inspector, at the top there is a **"Death By"** dropdown:

```csharp
public enum DeathBy
{
    Animation,              // Play death animation only
    AnimationWithRagdoll,   // Death animation then ragdoll (limited limb movement)
    Ragdoll                 // Immediate ragdoll physics
}
```

### vCharacter Death Methods

```csharp
// Enable ragdoll manually
character.EnableRagdoll();

// Reset from ragdoll state
character.ResetRagdoll();

// Subscribe to death event
character.onDead.AddListener(OnCharacterDied);

// Check death state
if (character.isDead) { /* cleanup */ }

// Remove components after death (configurable)
character.removeComponentsAfterDie = true;
```

### Ragdoll for AI Enemies
Same setup applies to AI characters. Ragdoll is REQUIRED for proper damage reactions on AI. Without it:
- Damage applies but hit animations don't trigger
- Death behavior is broken

### Source
- [Invector Forum: Death by Ragdoll](https://invector.proboards.com/thread/339/add-death-ragdoll)
- [Invector Forum: Ragdoll Problem Solved](https://invector.proboards.com/thread/1433/ragdoll-problem-solved)

---

## 14. Spell System Addon (Community)

### ShadesOfInsomnia SpellSystem

An open-source addon (CC BY-SA 4.0) for adding magic to Invector. Highly relevant as a reference for The Scorpion's elemental system.

**GitHub**: https://github.com/ShadesOfInsomnia/SpellSystem

### Architecture

The addon extends Invector **without modifying core classes** through:

1. **Data Abstraction Layer**: SQLite/EasySave2 persistence for spell data
2. **Character Integration**: Custom creation wizards for magic-enabled player/AI
3. **Pooling System**: Centralized spawn manager for spells, particles, effects
4. **Animator-Based Casting**: Multi-layered spell casting using animator states

### Damage & Resistance System
- Multiple damage types with corresponding resist mechanics
- Damage mitigation scales based on character attributes
- Damage Over Time (DoT) effects
- Status effects: burning, poisoned, frozen
- Magic weapons/armor link damage/resist types to progression

### Key Features
- Spell Book Wizard: centralized spell creation, auto-applied to animators
- Homing projectiles with advanced targeting
- Physics-based spells
- Ranged AI for Invector Core AI (spell casting + arrow shooting)
- Leveling system with point allocation and attribute system
- Custom hand effects during casting

### Installation
1. Download release from GitHub
2. Extract and copy entire `invector` folder into project Assets
3. Accept overwrite of two item enum files (for spell items)

### Relevance to The Scorpion
This addon demonstrates the proven pattern for adding elemental abilities on top of Invector. Key takeaways:
- Use the animator for ability casting (not just code)
- Implement damage types as strings that match across weapons, spells, and resistance systems
- Use a pooling system for spawned effects
- Extend AI without modifying core Invector AI classes

### Source
- [SpellSystem GitHub](https://github.com/ShadesOfInsomnia/SpellSystem)
- [SpellSystem README](https://github.com/ShadesOfInsomnia/SpellSystem/blob/master/README.md)

---

## 15. Common Pitfalls & Forum Solutions

### 1. Input Manager Not Imported
**Symptom**: Character doesn't respond to input
**Fix**: Invector > Import ProjectSettings (imports required Input axes for melee combat)

### 2. Active Input Handling Wrong
**Symptom**: Errors about input axes
**Fix**: Edit > Project Settings > Player > Configuration > Active Input Handling > **"Both"**

### 3. Damage Layers Not Configured
**Symptom**: Attacks pass through enemies, no damage dealt
**Fix**: On the player's vMeleeManager, press "Damage Layers" and add the **Enemy** layer. On AI's vMeleeManager, add the **Player** layer.

### 4. AI Circles Around Player Without Attacking
**Symptom**: Enemy approaches but spins around without engaging
**Fix**: Increase the AI's **Chase Distance** / adjust **Attack Distance** so the AI stops at the correct range

### 5. Weapon Not Appearing in Hands
**Symptom**: Weapon shows in inventory but not equipped visually
**Fix**: EquipPoint name on ItemManager must **exactly match** handler name in ItemListData

### 6. Ragdoll Not Working
**Symptom**: Character falls through ground or doesn't ragdoll on death
**Fix**: Create ragdoll FIRST before adding other components. Use Scale Factor 1 on FBX. Enable Projection and Proportional Mass.

### 7. Camera Goes Off-Tilt
**Symptom**: Camera rotates wildly during custom actions/conversations
**Fix**: Ensure Invector gameplay camera is tagged **MainCamera**. Set custom cameras to higher Depth values.

### 8. Cursor Lock Issues
**Symptom**: Mouse cursor not clickable on custom menus
**Fix**: Call `vShooterMeleeInput.ShowCursor()` / `LockCursor()` via events, or use the ShowCursorOnEnable script on panels

### 9. Hit Reactions Not Playing on AI
**Symptom**: Damage applies but AI doesn't flinch
**Fix**: AI must have ragdoll component. Without it, hit reactions won't trigger.

### 10. Namespace Errors
**Symptom**: `CharacterController` reference ambiguous
**Fix**: Use `Invector.vCharacterController` namespace, not Unity's `CharacterController`

### 11. Custom Scripts Can't Access Health
**Symptom**: `currentHealth` is inaccessible
**Fix**: Use `ChangeHealth()` public method, or access via interface `vIHealthController`. Don't modify Invector source to change access modifiers.

### 12. AI Not Detecting Custom Targets
**Symptom**: AI ignores custom objects/characters
**Fix**: Target needs `vCharacterStandalone` component + correct tag + correct layer + capsule collider + rigidbody

### Source
- [Invector Forum Home](https://invector.proboards.com/forum)
- [Invector Forum: Common Problems](https://invector.proboards.com/thread/4688/problems-solve-after-tester-release)
- [Pixel Crushers Forum: Invector Issues](https://www.pixelcrushers.com/phpbb/viewtopic.php?t=5120)

---

## 16. Integration Patterns for The Scorpion

Based on all research, here are the recommended integration patterns for each custom system:

### ElementSystem (Fire/Lightning)

```
Pattern: Separate MonoBehaviour + onDamageHit event subscription
- Create ElementSystem.cs as standalone component on player
- Subscribe to vMeleeManager.onDamageHit to apply elemental effects on hit
- Use vDamage.damageType strings: "Fire", "Lightning"
- Switch element changes the damageType on the active weapon at runtime
- Energy management is entirely custom (not in Invector)
```

### UltimateSystem (Adrenaline Rush)

```
Pattern: Separate MonoBehaviour + onDamageHit + onDead event subscriptions
- Track adrenaline via custom float (not Invector stamina)
- +2 per hit: subscribe to onDamageHit
- +5 per kill: subscribe to enemy onDead events
- +10 per combo finisher: detect via animator state callbacks
- Time-slow: modify Time.timeScale (exclude player with unscaledDeltaTime)
- Damage boost: temporarily modify weapon damage values
```

### WaveManager

```
Pattern: Standalone GameManager system
- Manages spawn points (N/S/E/W at arena edges)
- Instantiates AI prefabs configured with Invector's Create Melee AI
- Each enemy type = different AI prefab with different stats/animations
- Track kills via vHealthController.onDead on each spawned enemy
- Wave progression is entirely custom logic
```

### Enemy Types

```
Hollow Monk (Basic): Standard vControlAIMelee, default settings
Shadow Acolyte (Fast): vControlAIMelee with higher speed, lower health
Stone Sentinel (Heavy): vControlAIMelee with higher health, slower speed, more damage

All use:
- vMeleeManager for combat
- vHealthController for health
- NavMeshAgent for pathfinding
- Ragdoll for death
- Custom DamageModifier for elemental vulnerabilities
```

### BossAI (The Fallen Guardian)

```
Pattern: Custom FSM on top of Invector AI base
- Extend vControlAIMelee or create custom MonoBehaviour
- 3 phases based on vHealthController.currentHealth percentage
- Phase transitions: subscribe to onReceiveDamage, check health thresholds
- Summon mechanic: instantiate enemy prefabs (reuse WaveManager spawn logic)
- Fire aura (Phase 2): use vObjectDamage on trigger zone around boss
- Custom attack patterns: animator-driven with multiple vMeleeAttackBehaviour states
```

### HUDController

```
Pattern: Custom UI reading Invector properties
- Read motor.currentHealth / motor.MaxHealth for health bar
- Custom adrenaline/element UI is entirely independent
- Subscribe to onReceiveDamage for damage flash effects
- Subscribe to onDead for death screen
- Wave counter from WaveManager (not Invector)
```

### Critical Rules
1. **NEVER modify** files in `Assets/Invector-3rdPersonController/`
2. **Always extend** via events, callbacks, or separate components
3. **Use vDamage.damageType** strings for all elemental identification
4. **Hook into onDamageHit** on vMeleeManager for post-hit effects
5. **Hook into onReceiveDamage** on vHealthController for pre-damage modifications
6. **Use the animator** for ability animations -- don't bypass the animation system

---

## Reference Index

### Official Resources
1. [Invector Official Site](https://www.invector.xyz/)
2. [Invector Third Person Documentation](https://www.invector.xyz/thirdpersondocumentation)
3. [Invector AI Documentation](https://www.invector.xyz/aidocumentation)
4. [Invector Release Notes](https://www.invector.xyz/release-notes)
5. [Unity Asset Store - Melee Combat Template](https://assetstore.unity.com/packages/tools/game-toolkits/invector-third-person-controller-melee-combat-template-44227)

### API References
6. [vHealthController API](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_health_controller.html)
7. [vCharacter API](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_character.html)
8. [vThirdPersonMotor API](https://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_character_controller_1_1v_third_person_motor.html)
9. [vMeleeManager API](http://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/class_invector_1_1v_melee_1_1v_melee_manager.html)
10. [vIDamageReceiver Interface](https://www.ricardoferro.com/jorgenegreiros/vAPI/API/html/interface_invector_1_1v_i_damage_receiver.html)

### Community & Forums
11. [Invector Community Forum](https://invector.proboards.com/forum)
12. [Forum: Adding Inputs & Animations](https://invector.proboards.com/thread/366/adding-inputs-animation-custom-controller)
13. [Forum: Simple Damage Modifier System](https://invector.proboards.com/thread/3993/simple-damage-modifier-invector-controller)
14. [Forum: Custom Hit Damage Particles](https://invector.proboards.com/thread/2013/custom-hit-damage-particle)
15. [Forum: Create Custom Melee Weapon](https://invector.proboards.com/thread/3495/create-equip-custom-melee-weapon)
16. [Forum: Death by Ragdoll](https://invector.proboards.com/thread/339/add-death-ragdoll)
17. [Forum: Melee Combat Helper Add-On](https://invector.proboards.com/thread/4186/melee-combat-helper-add-wip)
18. [Forum: Customizing UI](https://invector.proboards.com/thread/4649/customizing-ui)

### Integration Guides & Addons
19. [SpellSystem Addon (GitHub)](https://github.com/ShadesOfInsomnia/SpellSystem)
20. [Emerald AI - Invector Integration Tutorial](https://github.com/Black-Horizon-Studios/Emerald-AI/wiki/Invector-Integration-Tutorial)
21. [Easy Multiplayer Invector - Damage Data](https://cyberbulletgames.com/easy-multiplayer-invector-ui-docs/HelpfulCode/getting_damage_data/)
22. [Pixel Crushers - Dialogue System Invector Support](https://www.pixelcrushers.com/dialogue_system/manual2x/html/invector.html)
23. [Malbers Animal Controller - Invector Integration](https://malbersanimations.gitbook.io/animal-controller/annex/integrations/invector-templates)
24. [vThirdPersonController Source (GitHub LITE)](https://github.com/ReForge-Mode/Unity_VRoid_3D_Character_Controller_Invector_Free)

### Guides
25. [Invector Basic Locomotion Guide (Scribd)](https://www.scribd.com/document/471108297/Invector-Third-Person-Controller-Basic-Locomotion-Guide)
26. [Invector Melee Combat Documentation (Scribd)](https://www.scribd.com/document/471110141/Invector-Documentation-MeleeCombat)
27. [Unity Forum: Third Person Templates by Invector](https://forum.unity.com/threads/third-person-templates-by-invector.349124/)
