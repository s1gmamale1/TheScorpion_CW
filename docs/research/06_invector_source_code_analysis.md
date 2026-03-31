# Invector Melee Combat Template — Source Code Analysis

Complete architecture breakdown from reading the actual C# source files in the project.

---

## 1. Directory Structure

```
Assets/Invector-3rdPersonController/
├── Basic Locomotion/
│   ├── Scripts/
│   │   ├── CharacterController/
│   │   │   ├── vThirdPersonController.cs       [Main controller]
│   │   │   ├── vThirdPersonInput.cs           [Input handling]
│   │   │   ├── vThirdPersonAnimator.cs        [Animator integration]
│   │   │   ├── vThirdPersonMotor.cs           [Physics & movement]
│   │   │   └── vICharacter.cs                 [Character interface]
│   │   ├── Generic/Health/
│   │   │   ├── vHealthController.cs           [Health & damage]
│   │   │   ├── vIHealthController.cs          [Health interface]
│   │   │   └── vIDamageReceiver.cs            [Damage receiver interface]
│   │   └── ObjectDamage/
│   │       └── vDamage.cs                     [Damage data structure]
│
├── Melee Combat/
│   ├── Scripts/
│   │   ├── CharacterController/
│   │   │   └── vMeleeCombatInput.cs           [Melee input]
│   │   ├── MeleeWeapon/
│   │   │   ├── vMeleeManager.cs               [Weapon orchestration]
│   │   │   ├── vMeleeWeapon.cs                [Weapon settings]
│   │   │   ├── vMeleeAttackObject.cs          [Attack with hitboxes]
│   │   │   ├── vMeleeAttackControl.cs         [StateMachineBehaviour]
│   │   │   ├── vHitBox.cs                     [Individual hitbox]
│   │   │   ├── vIMeleeFighter.cs              [Fighter interface]
│   │   │   └── vIAttackListener/Receiver.cs   [Attack interfaces]
│   │   └── LockOn/
│   │       └── vLockOn.cs
│   │
│   └── Simple Melee AI/Scripts/
│       ├── vSimpleMeleeAI_Controller.cs       [AI controller]
│       ├── vSimpleMeleeAI_Motor.cs            [AI movement]
│       └── vSimpleMeleeAI_SphereSensor.cs     [Target detection]
│
└── ItemManager/
```

## 2. Class Hierarchy

```
MonoBehaviour
├── vMonoBehaviour (base Invector class)
│   ├── vThirdPersonMotor
│   │   └── vThirdPersonAnimator
│   │       └── vThirdPersonController
│   │
│   ├── vThirdPersonInput : vIAnimatorMoveReceiver
│   │   └── vMeleeCombatInput : vIMeleeFighter
│   │
│   ├── vHealthController : vIHealthController
│   │
│   ├── vMeleeManager : IWeaponEquipmentListener
│   │
│   └── vMeleeAttackObject
│       └── vMeleeWeapon

StateMachineBehaviour
└── vMeleeAttackControl (triggers damage timing)

vSimpleMeleeAI_Motor
└── vSimpleMeleeAI_Animator
    └── vSimpleMeleeAI_Controller : vIMeleeFighter
```

## 3. Key Interfaces

### vIMeleeFighter (Most Important)
```csharp
public interface vIMeleeFighter : vIAttackReceiver, vIAttackListener
{
    void BreakAttack(int breakAtkID);
    void OnRecoil(int recoilID);
    bool isAttacking { get; }
    bool isArmed { get; }
    bool isBlocking { get; }
    vICharacter character { get; }
}
```
Implemented by: vMeleeCombatInput, vSimpleMeleeAI_Controller

### vIAttackListener
```csharp
public interface vIAttackListener
{
    void OnEnableAttack();          // Attack animation starts
    void OnDisableAttack();         // Attack damage window ends
    void ResetAttackTriggers();     // Reset animator triggers
}
```

### vIAttackReceiver
```csharp
public interface vIAttackReceiver
{
    void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker);
}
```

### vIHealthController
```csharp
public interface vIHealthController : vIDamageReceiver
{
    OnDead onDead { get; }
    float currentHealth { get; }
    int MaxHealth { get; }
    bool isDead { get; set; }
    void AddHealth(int value);
    void ChangeHealth(int value);
    void ChangeMaxHealth(int value);
    void ResetHealth();
}
```

## 4. Damage Flow Architecture

```
Attack Initiated
    ↓
vMeleeCombatInput.MeleeWeakAttackInput() / MeleeStrongAttackInput()
    ↓ Sets animator triggers
Animator State Machine
    ↓
vMeleeAttackControl (StateMachineBehaviour)
    OnStateEnter → vIAttackListener.OnEnableAttack()
    OnStateUpdate → At startDamage% time: SetActiveAttack(true)
    ↓
vMeleeManager.SetActiveAttack(bodyPart, type, active, damageMultiplier, ...)
    ↓ Activates hitboxes
vMeleeAttackObject.SetActiveDamage(true)
    ↓ Enables trigger colliders
vHitBox (OnTriggerEnter)
    ↓
vMeleeAttackObject.OnHit(vHitBox, Collider)
    ↓ Validates hit (tags, layers)
vMeleeManager.OnDamageHit(ref vHitInfo)  ← HOOK HERE
    ↓
Target.ApplyDamage(damage, fighter)
    ↓
vIAttackReceiver.OnReceiveAttack(damage, attacker)  ← HOOK HERE
    ↓
vHealthController.TakeDamage(vDamage)
    ↓ Reduces currentHealth
Events: onStartReceiveDamage → onReceiveDamage → onDead (if HP ≤ 0)
```

**Key Events:**
- `vMeleeManager.onDamageHit` — when attack hits
- `vHealthController.onStartReceiveDamage` — before damage applied
- `vHealthController.onReceiveDamage` — after damage applied
- `vHealthController.onDead` — when HP reaches 0

## 5. Combo System

No built-in combo system — combos are animation-driven:

- Each attack animation transitions to next attack in Animator
- `vMeleeCombatInput` checks `isAttacking` to prevent overlaps
- Attack IDs: `vMeleeWeapon.attackID` sets animator parameter
- Movesets: `vMeleeWeapon.movesetID` for weapon-specific animations

**Damage Timing:**
- `vMeleeAttackControl.startDamage` (0.05) — damage window opens at 5% through animation
- `vMeleeAttackControl.endDamage` (0.9) — damage window closes at 90%

## 6. Input System

**vThirdPersonInput:**
- Movement: horizontalInput, verticalInput, sprintInput, crouchInput, jumpInput, rollInput
- Camera: rotateCameraXInput, rotateCameraYInput, cameraZoomInput

**vMeleeCombatInput (extends vThirdPersonInput):**
- weakAttackInput (Mouse0/RB)
- strongAttackInput (Alpha1/RT)
- blockInput (Mouse1/LB)

**Input Locking:**
- `lockInput` — disables all input
- `lockMeleeInput` — disables combat only
- `lockMoveInput` — prevents movement
- Methods: `SetLockAllInput()`, `SetLockMeleeInput()`

## 7. Health & Stamina

**vHealthController:**
```csharp
currentHealth, maxHealth, isDead
healthRecovery          // HP/sec
healthRecoveryDelay     // Seconds before recovery

// Events
onStartReceiveDamage    // UnityEvent<vDamage>
onReceiveDamage         // UnityEvent<vDamage>
onDead                  // UnityEvent<GameObject>
onChangeHealth          // UnityEvent<float>
```

**vDamage Structure:**
```csharp
damageValue             // Primary damage
staminaBlockCost        // Stamina when blocking
ignoreDefense           // Bypass blocking
activeRagdoll           // Trigger ragdoll
hitReaction             // Play hit animation
damageType              // Custom string ("Fire", "Lightning")
sender, receiver        // Transform references
force                   // Knockback vector
```

## 8. AI System

**vSimpleMeleeAI_Controller States:**
- Idle, Chase, PatrolSubPoints, PatrolWaypoints, Wander

**Key Properties:**
```csharp
currentState, currentTarget, sphereSensor, agent (NavMeshAgent)
tagsToDetect, fieldOfView, minDetectDistance, maxDetectDistance
chanceToBlockInStrafe   // % chance to block
```

## 9. Virtual Methods for Extension

**vMeleeCombatInput (Override these):**
```csharp
virtual void MeleeWeakAttackInput()
virtual void TriggerWeakAttack()
virtual void MeleeStrongAttackInput()
virtual void BlockingInput()
virtual void OnEnableAttack()
virtual void OnDisableAttack()
virtual void OnReceiveAttack(vDamage damage, vIMeleeFighter attacker)
```

**vHealthController:**
```csharp
virtual void TakeDamage(vDamage damage)
virtual void AddHealth(int value)
```

**vMeleeManager:**
```csharp
virtual void OnDamageHit(ref vHitInfo hitInfo)
virtual void OnRecoilHit(vHitInfo hitInfo)
virtual int GetAttackID()
```

## 10. Extension Patterns

**Pattern 1: Extend vMeleeCombatInput**
```csharp
class ScorpionCombatInput : vMeleeCombatInput
{
    override void OnEnableAttack() { /* element effects + base */ }
    override void OnReceiveAttack() { /* custom reactions */ }
}
```

**Pattern 2: Subscribe to Events**
```csharp
meleeManager.onDamageHit.AddListener((vHitInfo hitInfo) => {
    // Element damage, adrenaline gain, etc.
});
```

**Pattern 3: Health Events**
```csharp
healthController.onReceiveDamage.AddListener((vDamage damage) => {
    // Status effects, damage numbers, etc.
});
```

**Pattern 4: Custom Damage Type**
```csharp
// Use vDamage.damageType for element identification
// Set damageType = "Fire" or "Lightning"
// Check in receiver for element-specific effects
```

## 11. Integration Points for The Scorpion

| System | Hook Into |
|--------|-----------|
| **ElementSystem** | vMeleeManager.onDamageHit → apply Fire/Lightning effects |
| **UltimateSystem** | vHealthController.onReceiveDamage → +2 adrenaline per hit |
| **Kill Tracking** | vHealthController.onDead → +5 adrenaline, wave counter |
| **WaveManager** | Listen to onDead on all enemies → trigger next wave |
| **HUD** | Subscribe to health, stamina, custom element/adrenaline events |
| **BossAI** | Extend vSimpleMeleeAI_Controller, override for phases |
| **Custom Damage** | vDamage.damageType = "Fire"/"Lightning" for element ID |

## 12. Important Notes

- Invector requires: Animator with specific states, RigidBody, CapsuleCollider
- Damage is completely event-driven — subscribe to events, don't modify core
- AI uses NavMesh — requires baked NavMesh
- All public methods are virtual — always extensible
- Stamina is in vThirdPersonMotor (not vHealthController)
- Never modify Invector source directly — extend via inheritance
