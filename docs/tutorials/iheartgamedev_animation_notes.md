# iHeartGameDev - Unity Animation System: Complete Notes

> Compiled from all 13 videos in the "Unity's Animation System" playlist by iHeartGameDev (Nicky).
> Focus: Understanding Unity animation for 3D character/combat game development.

---

## Table of Contents

1. [Video 1: Mixamo Import](#video-1-importing-from-mixamo)
2. [Video 2: Animator Explained](#video-2-animator-explained)
3. [Video 3: Animation Transitions with Booleans](#video-3-animation-transitions-with-booleans)
4. [Video 4: 1D Blend Trees](#video-4-1d-blend-trees)
5. [Video 5: 2D Blend Trees](#video-5-2d-blend-trees)
6. [Video 6: Animation Retargeting](#video-6-animation-retargeting)
7. [Video 7: Animation Layers](#video-7-animation-layers)
8. [Video 8: Animations Overview (Curves & Events)](#video-8-animations-overview-curves--events)
9. [Video 9: New Input System + Root Motion](#video-9-new-input-system--root-motion)
10. [Video 10: Character Controllers](#video-10-character-controllers)
11. [Video 11: Animated Movement (Full Implementation)](#video-11-animated-movement-full-implementation)
12. [Video 12: Animation Rigging & Procedural Animation](#video-12-animation-rigging--procedural-animation)
13. [Video 13: Character Rig & State Machine for Procedural Animation](#video-13-character-rig--state-machine-for-procedural-animation)

---

## Video 1: Importing from Mixamo

**Video**: https://www.youtube.com/watch?v=-FhvQDqmgmU (6:34)

### Key Concepts

- **Mixamo** is a free Adobe platform for downloading character models and hundreds of animations.
- Sign up at mixamo.com; the X-Bot and Y-Bot characters are ideal for prototyping.
- Download idle, walk, and run animations as a starting set.

### Export Settings from Mixamo

| Setting | Recommended Value | Notes |
|---------|-------------------|-------|
| Format | FBX for Unity | Optimized texturing for Unity |
| Frames Per Second | 30 | Higher = larger file; Unity interpolates between frames anyway |
| Keyframe Reduction | None (for learning) | Removes frames with negligible changes; happens on import |
| Skin | With Skin (at least once) | Includes 3D model mesh; omit for animation-only files |
| In Place | Checked (for walk/run) | Prevents character from translating during animation playback |

### Key Definitions

- **Interpolation**: Unity estimates animation values between keyframes. If arm goes from 0 to 90 degrees across 2 frames, Unity calculates 45 degrees for the midpoint.
- **Keyframe Reduction**: Removes frames where values don't differ enough from surrounding frames. Controlled by tolerance.

### Unity Import Steps

1. Create organized folder structure: `Assets/Animations/`, `Assets/Mixamo/`
2. Drag FBX files into the Mixamo folder (or right-click > Import New Asset)
3. Files without skin will only contain animation data (no mesh preview)

### Combat Game Application

For a melee combat game, download: idle, walk, run, attack combos, dodge/roll, hit reactions, death animations. Always check "In Place" for locomotion unless using root motion.

---

## Video 2: Animator Explained

**Video**: https://www.youtube.com/watch?v=vApG8aYD5aI (7:51)

### Key Concepts

- Unity uses the **Animator component** + **Animator Controller** asset to drive animations.
- The Animator Controller is a visual state machine for creating, modifying, and connecting animation states.

### Setup Steps

1. Select character GameObject in hierarchy
2. Add Component > Animator
3. Create Animator Controller asset (right-click > Create > Animator Controller)
4. Drag controller into the Animator component's "Controller" slot

### Animation States

- Each animation (idle, walk, run) = one **animation state** in the controller
- The **Entry** node automatically connects to the **default state** (orange)
- Right-click a state > "Set as Layer Default State" to change default

### Preparing Animations for Use

1. Expand imported FBX in Project tab to see child animation clip
2. Select the animation clip > Cmd+D (Mac) / Ctrl+D (Win) to duplicate it out of the FBX
3. On the duplicate, check **Loop Time** in Inspector
4. Drag duplicated animation into the Animator Controller grid

### Animator Component Properties (5 total)

| Property | Purpose |
|----------|---------|
| **Controller** | Reference to the Animator Controller asset |
| **Avatar** | Used for animation retargeting (humanoid rigs) |
| **Apply Root Motion** | Animation-driven movement vs script-driven |
| **Update Mode** | Normal (timescale-affected), Animate Physics (FixedUpdate), Unscaled Time (UI) |
| **Culling Mode** | Always Animate, Cull Update Transforms, Cull Completely |

### Update Mode Details

- **Normal**: Animation speed scales with `Time.timeScale`. Perfect for slow-motion combat effects.
- **Animate Physics**: Runs in FixedUpdate. Use when animations interact with Rigidbody objects.
- **Unscaled Time**: Ignores timescale. Best for UI animations during slow-motion.

### Culling Mode Details

- **Always Animate**: Animates even off-screen (costly but safe)
- **Cull Update Transforms**: Calculates frames off-screen so animation is seamless when returning to view
- **Cull Completely**: Stops animation entirely when off-screen; resumes from where it left off

### Combat Game Application

For an arena combat game: Use **Normal** update mode to enable slow-motion ultimate abilities (like Adrenaline Rush). Use **Animate Physics** if attack animations need to interact with physics objects. Set enemies to **Cull Update Transforms** so they animate correctly when camera pans back to them.

---

## Video 3: Animation Transitions with Booleans

**Video**: https://www.youtube.com/watch?v=FF6kezDQZ7s (12:00)

### Key Concepts

- **Parameters** control animation state transitions (Float, Int, Bool, Trigger)
- **Transitions** connect states and use conditions based on parameters
- **Has Exit Time** forces a percentage of the current animation to complete before transitioning

### Parameter Types

| Type | Description |
|------|-------------|
| Float | Decimal number (used with blend trees) |
| Int | Whole number |
| Bool | True/False |
| Trigger | True/False but auto-resets (handled differently by Unity) |

### Setting Up Transitions

1. Right-click animation state > Make Transition > click target state
2. Select the transition arrow in the grid
3. Disable **Has Exit Time** for instant transitions
4. Add **Condition**: select parameter + required value (true/false)
5. Create return transition with opposite condition

### Transition Settings

| Setting | Purpose |
|---------|---------|
| Has Exit Time | Percentage of animation that must complete before transition (default 0.97 = 97%) |
| Transition Duration | Blend time between animations; 0 = snappy, higher = smoother |
| Transition Offset | Where to start the next animation (0-1, e.g., 0.25 = start at 25%) |
| Interruption Source | Allows switching animations mid-transition (None, Current State, Next State) |
| Solo | Only plays this transition |
| Mute | Disables this transition completely |

### Code: Controlling Booleans from Script

```csharp
public class AnimationStateController : MonoBehaviour
{
    Animator animator;
    int isWalkingHash;
    int isRunningHash;

    void Start()
    {
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
    }

    void Update()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);
        bool forwardPressed = Input.GetKey(KeyCode.W);
        bool runPressed = Input.GetKey(KeyCode.LeftShift);

        // Start walking
        if (!isWalking && forwardPressed)
            animator.SetBool(isWalkingHash, true);

        // Stop walking
        if (isWalking && !forwardPressed)
            animator.SetBool(isWalkingHash, false);

        // Start running
        if (!isRunning && forwardPressed && runPressed)
            animator.SetBool(isRunningHash, true);

        // Stop running
        if (isRunning && (!forwardPressed || !runPressed))
            animator.SetBool(isRunningHash, false);
    }
}
```

### Performance Tips

- Use `Animator.StringToHash()` to convert parameter name strings to integers -- avoids string comparison every frame.
- Only call `SetBool` when the value actually needs to change (check current value first with `GetBool`).

### Combat Game Application

For a melee combat game, booleans work well for discrete states: isAttacking, isDodging, isBlocking, isStunned. Use **Trigger** parameters for one-shot animations like attack swings. Set **Transition Duration** short (0.1-0.15s) for responsive combat feel. Use **Interruption Source = Next State** to allow attack canceling into dodge.

---

## Video 4: 1D Blend Trees

**Video**: https://www.youtube.com/watch?v=m8rGyoStfgQ (10:20)

### Key Concepts

- **Blend Trees** dynamically blend multiple animations together based on float parameters.
- A **1D Blend Tree** uses a single float parameter to interpolate between animations.
- Ideal for blending walk-to-run based on movement speed.

### Setup Steps

1. In Animator grid: right-click > Create State > From New Blend Tree
2. Create a Float parameter (e.g., "Speed" or "Velocity")
3. Double-click the Blend Tree node to enter its sub-layer
4. Add Motion Fields for each animation (walk, run)
5. Set up transitions from idle to the blend tree using the float parameter

### Blend Tree Inspector Options

| Setting | Purpose |
|---------|---------|
| Blend Type | 1D (single parameter), 2D types (two parameters), Direct |
| Parameter | Which float parameter drives the blending |
| Motion List | Animations to blend between |
| Thresholds | Parameter values where each animation has 100% weight |
| Automate Thresholds | Evenly distributes thresholds (toggle off for manual control) |
| Speed | Playback speed multiplier per animation |
| Mirror | Reflects animation (humanoid only) |

### Motion Field Sub-Properties

- **Loop Time**: Repeats the animation
- **Loop Pose**: Blends start/end keyframes for seamless looping
- **Cycle Offset**: Adjusts animation starting point

### Code: Controlling Blend Tree with Velocity

```csharp
public class AnimationStateController : MonoBehaviour
{
    Animator animator;
    float velocity = 0f;
    public float acceleration = 2f;
    public float deceleration = 2f;
    int velocityHash;

    void Start()
    {
        animator = GetComponent<Animator>();
        velocityHash = Animator.StringToHash("Velocity");
    }

    void Update()
    {
        bool forwardPressed = Input.GetKey(KeyCode.W);

        // Accelerate
        if (forwardPressed && velocity < 1f)
            velocity += Time.deltaTime * acceleration;

        // Decelerate
        if (!forwardPressed && velocity > 0f)
            velocity -= Time.deltaTime * deceleration;

        // Clamp
        if (velocity < 0f) velocity = 0f;

        animator.SetFloat(velocityHash, velocity);
    }
}
```

### Combat Game Application

Blend trees are essential for smooth locomotion in a combat game. Use a 1D blend tree to smoothly transition between walk and run based on analog stick pressure or acceleration over time. This gives characters a natural feel rather than snapping between discrete walk/run states.

---

## Video 5: 2D Blend Trees

**Video**: https://www.youtube.com/watch?v=_J8RPIaO2Lc (23:15)

### Key Concepts

- **2D Blend Trees** use two float parameters (e.g., VelocityX, VelocityZ) for multi-directional blending.
- Enables blending forward movement with strafing in any direction.

### 2D Blend Type Comparison

| Type | Use Case |
|------|----------|
| **2D Simple Directional** | One animation per direction only; no duplicates in same direction |
| **2D Freeform Directional** | Multiple animations in same direction (walk + run forward). Best for velocity-based movement |
| **2D Freeform Cartesian** | Animations not representing directions (e.g., aiming angles). X and Y can represent different concepts |
| **Direct** | Manual weight control per animation. Used for facial expressions |

### Setup for 7 Animations

| Animation | Position X | Position Y | Notes |
|-----------|-----------|-----------|-------|
| Idle | 0 | 0 | Origin = no velocity |
| Walk Forward | 0 | 0.5 | |
| Run Forward | 0 | 2.0 | |
| Strafe Walk Left | -0.5 | 0 | |
| Strafe Run Left | -2.0 | 0 | |
| Strafe Walk Right | 0.5 | 0 | |
| Strafe Run Right | 2.0 | 0 | |

### Code Structure (Key Patterns)

```csharp
// Ternary operator for walk/run max velocity
float currentMaxVelocity = runPressed ? maxRunVelocity : maxWalkVelocity;

// Acceleration with capping
if (forwardPressed && velocityZ < currentMaxVelocity)
    velocityZ += Time.deltaTime * acceleration;

// Deceleration
if (!forwardPressed && velocityZ > 0f)
    velocityZ -= Time.deltaTime * deceleration;

// Lock velocity to prevent overshoot
if (!forwardPressed && velocityZ < 0f)
    velocityZ = 0f;

// Smooth deceleration from run to walk speed
if (forwardPressed && velocityZ > maxWalkVelocity)
    velocityZ -= Time.deltaTime * deceleration;

// Snap to max when close enough (prevents jitter)
if (forwardPressed && velocityZ < currentMaxVelocity
    && velocityZ > currentMaxVelocity - 0.05f)
    velocityZ = currentMaxVelocity;

// Set animator floats
animator.SetFloat(velocityZHash, velocityZ);
animator.SetFloat(velocityXHash, velocityX);
```

### Code Refactoring Tips

- Extract velocity change logic into `ChangeVelocity()` function
- Extract clamping/reset logic into `LockOrResetVelocity()` function
- Use `Animator.StringToHash()` for all parameter names
- Use `KeyCode` enum instead of string keys for `Input.GetKey()`

### Combat Game Application

2D Freeform Directional blend trees are critical for a melee combat game with lock-on mechanics. When locked onto an enemy, the player strafes (left/right) while maintaining facing direction -- this requires blending forward walk, strafe walk left/right, and their run variants simultaneously. Set up a 2D blend tree with VelocityX and VelocityZ parameters for full directional movement.

---

## Video 6: Animation Retargeting

**Video**: https://www.youtube.com/watch?v=BEZHVYk6Fa4 (15:01)

### Key Concepts

- **Animation Retargeting** allows sharing animations between different character models.
- Requires converting rigs to **Humanoid** type to create an **Avatar**.
- Humanoid avatars remap bones to a standardized structure that is interchangeable.

### Why Retargeting is Needed

- Animations are tied to specific bone names and hierarchies
- Different characters have different bone names and proportions
- Simply renaming bones doesn't work if proportions differ -- results in broken deformations

### Setting Up Humanoid Rigs

1. Select imported character FBX in Project tab
2. Inspector > **Rig** tab
3. Change **Animation Type** to **Humanoid**
4. Avatar Definition: **Create From This Model**
5. Press **Apply**
6. Press **Configure** to review bone mapping

### Animation Type Options

| Type | Description |
|------|-------------|
| None | Disables rig entirely |
| Legacy | Old animation system (don't use) |
| Generic | Default for imported FBX; not retargetable |
| Humanoid | Mecanim system; enables retargeting between humanoid characters |

### Avatar Configuration

- Unity auto-maps bones by searching for standard names
- Green circles = mapped bones; dotted circles = optional
- **Mapping** dropdown: Clear, Automap, Load, Save
- **Pose** dropdown: Reset, Sample Bind Pose, Enforce T-Pose
- **Muscles & Settings** tab: Adjust range of motion constraints per joint

### Skin Weights

- Default: 4 bones per vertex (performance optimized)
- Custom amount available if model deforms unexpectedly
- Higher bone count = more accurate but less performant

### Important Notes for Retargeting

1. Download animations "without skin" but still switch their Rig type to Humanoid
2. For skinless animations: set Avatar Definition to **Copy From Other Avatar** and select the base character's avatar
3. When switching existing Generic animations to Humanoid, duplicate animations first
4. Set **Root Transform Rotation > Based Upon** to **Original** and check **Bake Into Pose**
5. All animations in an Animator Controller must be the same rig type (all Humanoid or all Generic)

### Optimize Game Object Toggle

- Removes transform hierarchy and bakes into avatar/animator
- Recommended before shipping final build
- Apply BEFORE adding Animator component, or remove character and re-add after

### Combat Game Application

Retargeting is essential for a combat game with multiple enemy types (Hollow Monk, Shadow Acolyte, Stone Sentinel). Create one set of base humanoid animations and retarget them across all enemy models. Each enemy can share common animations (idle, patrol, hit react, death) while having unique attack animations. The boss character can reuse standard humanoid locomotion but have custom attack sequences.

---

## Video 7: Animation Layers

**Video**: https://www.youtube.com/watch?v=W0eRZGS6dhQ (16:00)

### Key Concepts

- **Animation Layers** allow stacking animations that control different parts of the body.
- Combined with **Avatar Masks**, layers can override or add to specific bone groups.
- Layers are ordered by priority: lower in the list = higher priority.

### Use Cases

- Upper body aiming while lower body walks/runs
- Injured animations layered over normal movement
- Crouching using additive layers
- Weapon holding overlaid on movement

### Layer Settings (Gear Icon)

| Setting | Purpose |
|---------|---------|
| **Weight** | 0-1 influence of the layer (0 = no effect, 1 = full effect) |
| **Mask** | Avatar Mask that limits which bones the layer affects |
| **Blending** | Override or Additive |
| **Sync** | Copies state structure from another layer |
| **Timing** | When synced, allows animations to play at their own speed |
| **IK Pass** | Enables OnAnimatorIK callback for this layer |

### Avatar Masks

1. Create: right-click > Create > Avatar Mask
2. **Humanoid** section: green silhouette; click body parts to toggle red (excluded) / green (included)
3. **IK markers**: Include/exclude IK curves from animations
4. **Shadow** under character: Toggle for root transform
5. **Transform** section: Import skeleton to toggle extra bones (tail, wings, etc.)
6. Assign mask in the layer's gear menu

### Override vs Additive Blending

**Override**: Replaces animations from layers above in the list.
- Example: Aiming layer overrides arm animations from base walking layer.

**Additive**: Combines bone transform data from multiple layers.
- Applies the *difference* between the additive reference pose (default: frame 0) and the current frame.
- Example: Vertical aim + horizontal aim layers combine to aim in any direction.
- Additive layers stack until an Override layer is reached.
- Caution: Additive animations must share a similar reference pose with the base layer for correct results.

### Additive Reference Pose

- Default is frame 0 of the animation
- Adjustable in the FBX animation tab and debug inspector
- Additive animation applies the delta (difference) between reference pose and current frame

### Additive Layer Optimization Trick

For a crouch animation:
1. Delete all keyframes except frame 0 of the crouch
2. Copy frame 0 from the standing idle into the crouch as the reference pose
3. Result: Two-keyframe animation that interpolates between standing and crouching via weight

### Sync Layers

- Copy/paste entire state structure from a source layer
- Deleting a state in either layer deletes it in all synced layers
- Use case: Injured versions of idle/walk/run states
- Set layer weight based on health percentage for dynamic injured animations

### IK Pass

- Enables the `OnAnimatorIK()` callback in scripts on the same GameObject
- Basic implementation example: head look-at and hand IK targeting spheres in the scene

### Combat Game Application

Layers are critical for a melee combat game:
- **Base Layer**: Locomotion (idle, walk, run, strafe)
- **Upper Body Layer** (Override + mask): Attack combos, blocking -- only affects torso and arms while legs continue locomotion
- **Additive Injury Layer**: As player health drops, blend in injured animations (limping, holding side)
- **Element Effects Layer**: Fire/Lightning visual poses on hands
- Use **Sync layers** for health states: normal locomotion synced with injured locomotion, weight driven by health %

---

## Video 8: Animations Overview (Curves & Events)

**Video**: https://www.youtube.com/watch?v=URjXL0QXwm4 (15:18)

### Key Concepts

- The **Animation Window** lets you create and edit animations directly in Unity.
- **Animation Curves** control interpolation paths between keyframes.
- **Animation Events** call functions at specific frames during animation playback.
- Almost any component property can be animated (not just transforms).

### Animation Window Layout

- **Left panel (Menu)**: Property list, playback controls, dope sheet/curves toggle
- **Right panel (Timeline)**: Keyframes displayed as diamonds
- **Dope Sheet** view: Standard keyframe timeline
- **Curves** view: Line graph showing interpolation paths with tangent handles

### Animatable Properties (Beyond Transforms)

- Box Collider enabled/disabled (useful for dodge i-frames)
- Material colors and properties
- Any serialized component property

### Recording Animations

1. Select a GameObject with an Animator
2. Open Animation window
3. Press **Create** to make a new animation clip
4. **Add Property** > choose component > choose property
5. Press red **Record** button
6. Move to desired frame on timeline
7. Modify values in Inspector or Scene -- keyframe auto-created
8. Click and drag keyframes to reposition; select multiple to scale

### Timeline Settings (3-dot menu)

| Setting | Purpose |
|---------|---------|
| Seconds/Frames toggle | Display mode for timeline |
| Ripple | Moving keyframes also moves surrounding keyframes |
| Show Sample Rate | Reveals the Samples field |
| Samples | Frames per second (60 default; 120 = double speed, 30 = half) |
| Filter by Selection | Only show keyframes for selected bones (essential for complex rigs) |

### Animation Curves & Tangent Types

| Tangent Type | Behavior |
|-------------|----------|
| **Clamped Auto** (default) | Smooth curve, auto-adjusts tangents, avoids overshoot |
| **Auto** (legacy) | Older version; may overshoot; for backwards compat only |
| **Free Smooth** | Both tangents collinear (same direction); manual control |
| **Flat** | Horizontal tangents; ease in/out |
| **Broken** | Independent left/right tangents with sub-types: |
| - Free | Standard curve |
| - Linear | Straight line between keyframes |
| - Constant | No change until next keyframe (step function) |
| - Weighted | Most customizable; bend curve freely |

### Animation Events

- Add via the timeline marker button
- Call any **public function** on scripts attached to the same GameObject
- Can pass parameters: float, int, string, Object reference
- If function not found at runtime, Unity throws an error

**Event Setup (two methods)**:
1. Select GameObject in hierarchy, click event > dropdown of available public functions
2. Select animation in Project, click event > type function name + parameter manually

### Practical Example: Gunshot Animation

1. Duplicate the read-only FBX animation to make it editable
2. Scrub to the frame just before weapon recoil
3. Add an event calling `FireBullet()` function
4. Script instantiates bullet prefab and adds force to its Rigidbody
5. Use curves to fix arm clipping through character head
6. Add spine rotation keyframes for recoil effect via curves
7. Scale keyframes after the event closer together to speed up the impact portion

### Combat Game Application

Animation Events are essential for melee combat:
- **Attack hit detection**: Fire event at the exact frame where the blade should deal damage (enable/disable weapon collider)
- **Sound effects**: Trigger slash sounds, impact sounds at correct animation frames
- **Particle effects**: Spawn fire/lightning VFX at attack impact frames
- **Combo windows**: Use events to mark when the next combo input is accepted
- **Dodge i-frames**: Animate collider disable/enable for invincibility frames
- **Animation Curves**: Fine-tune attack animations for better "weight" and impact feel by adjusting tangents

---

## Video 9: New Input System + Root Motion

**Video**: https://www.youtube.com/watch?v=IurqiqduMVQ (16:49)

### Key Concepts

- **Unity New Input System** package simplifies multi-device input handling.
- **Root Motion**: Character movement is "baked" into the animation (animation-driven vs script-driven movement).
- Callbacks and the generated C# class provide clean input handling.

### Root Motion Setup

1. Download animations from Mixamo **without** "In Place" checked
2. Enable **Apply Root Motion** on the Animator component
3. Fix direction issues: Select FBX > Animation tab > Root Transform Rotation > set **Based Upon** to "Original" > check **Bake Into Pose**

### New Input System Setup

1. Window > Package Manager > Install "Input System"
2. Restart Unity when prompted (disables old input system)
3. Right-click in Project > Create > Input Actions > name "PlayerInput"
4. Double-click to open Input Actions editor

### Input Actions Editor Structure

| Section | Purpose |
|---------|---------|
| **Action Maps** | Collections/containers for groups of actions (e.g., "CharacterControls") |
| **Actions** | Individual inputs (e.g., "Movement", "Run") |
| **Properties** | Settings for selected action (Action Type, Interactions, Processors) |

### Action Types

| Type | Returns | Use Case |
|------|---------|----------|
| **Value** | Various types; includes disambiguation | Movement (Vector2) |
| **Button** | Float (0 or 1) | Single button presses |
| **Pass-Through** | Same as Value but no disambiguation | When disambiguation not needed |

### Key Setup: Movement Action

1. Create Action Map: "CharacterControls"
2. Create Action: "Movement" > set to Value > Control Type: Vector2
3. Add Binding > Path > Listen > move left stick (or search "Left Stick")
4. Add Processor: **Stick Deadzone** (clamps low values to 0, high to 1)

### Key Setup: Run Action

1. Create Action: "Run" > set to Button
2. Bind to left shoulder button
3. Add Interaction: **Press** > set to "Press and Release"

### Generate C# Class

1. Select PlayerInput asset in Project
2. Inspector > check **Generate C# Class** > Apply
3. This creates a class matching your asset name for easy code access

### Code: Callbacks Pattern

```csharp
using UnityEngine.InputSystem;

public class CharacterMovement : MonoBehaviour
{
    PlayerInput input;
    Animator animator;
    Vector2 currentMovement;
    bool movementPressed;
    bool runPressed;
    int isWalkingHash, isRunningHash;

    void Awake()
    {
        input = new PlayerInput();
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");

        // Movement callbacks
        input.CharacterControls.Movement.performed += ctx => {
            currentMovement = ctx.ReadValue<Vector2>();
            movementPressed = currentMovement.x != 0 || currentMovement.y != 0;
        };
        input.CharacterControls.Movement.canceled += ctx => {
            currentMovement = ctx.ReadValue<Vector2>();
            movementPressed = false;
        };

        // Run callbacks
        input.CharacterControls.Run.performed += ctx => {
            runPressed = ctx.ReadValueAsButton();
        };
        input.CharacterControls.Run.canceled += ctx => {
            runPressed = ctx.ReadValueAsButton();
        };
    }

    void OnEnable() { input.CharacterControls.Enable(); }
    void OnDisable() { input.CharacterControls.Disable(); }
}
```

### Rotation with Input

```csharp
void HandleRotation()
{
    Vector3 currentPosition = transform.position;
    Vector3 newDirection = new Vector3(currentMovement.x, 0, currentMovement.y);
    Vector3 positionToLookAt = currentPosition + newDirection;
    transform.LookAt(positionToLookAt);
}
```

### Combat Game Application

The New Input System is critical for supporting multiple input devices (keyboard + gamepad). For a combat game:
- Map attack combos to face buttons and triggers
- Use Action Maps to switch between "Combat" and "Menu" input contexts
- Root Motion is useful for heavy attack animations where the character lunges forward -- the animation itself drives the movement distance
- Use **Stick Deadzone** processor to prevent unintended drift on analog sticks
- The callback pattern (`performed`/`canceled`/`started`) maps well to press-and-hold mechanics like blocking

---

## Video 10: Character Controllers

**Video**: https://www.youtube.com/watch?v=e94KggaEAr4 (9:46)

### Key Concepts

- A **Character Controller** = component(s) providing movement + collision handling.
- Three main options: Built-In CharacterController, Dynamic Rigidbody, Kinematic Rigidbody.
- Choice depends on game requirements.

### Option Comparison

| Feature | Built-In CC | Dynamic Rigidbody | Kinematic Rigidbody |
|---------|-------------|-------------------|---------------------|
| Physics interaction | No (by default) | Full physics | Affects dynamic only |
| Gravity | Manual (SimpleMove adds it) | Automatic | Manual |
| Collision detection | Built-in collider | Needs collider component | Needs collider component |
| Slope handling | Built-in (configurable angle) | Slides by default, hard to stop | Manual |
| Step handling | Built-in | No built-in step handling | Manual |
| isGrounded | Built-in property | Must implement manually | Must implement manually |
| Movement control | Script-driven (Move/SimpleMove) | Force/velocity-based | Script-driven |
| Reacts to external forces | No | Yes (pushed by collisions) | No |
| Momentum | Must code manually | Built-in | Must code manually |

### Built-In Character Controller

- Provides a capsule collider + movement methods
- Wraps Unity's PhysX character controller
- `Move()`: Raw movement, no gravity
- `SimpleMove()`: Includes gravity automatically
- Great for: Platformers, RPGs, most action games

### Dynamic Rigidbody

- Full physics simulation (gravity, mass, drag, momentum)
- Movement via `AddForce()`, `velocity`, etc.
- Great for: Physics-heavy games (Fall Guys, Gang Beasts)
- isKinematic = false (default)

### Kinematic Rigidbody

- `isKinematic = true`
- Ignores external forces; you control all movement via code
- Still provides collision detection
- Affects other dynamic rigidbodies but isn't affected by them
- Great for: Precise character control where you want collision without physics

### Custom Character Controller

- Most complex but most flexible
- Build everything from scratch
- Full control over every behavior

### Combat Game Application

For a hack-and-slash melee combat game, the **Built-In Character Controller** or **Kinematic Rigidbody** are the best choices:
- You want precise, responsive movement (not floaty physics)
- Dodge/roll needs exact distance control
- Enemies shouldn't push the player around via physics
- Knockback can be manually applied when needed
- Built-In CC handles slopes and stairs out of the box

Note: **Invector** (the framework this project uses) uses its own character controller system built on Rigidbody. Custom systems should extend Invector's approach rather than replacing it.

---

## Video 11: Animated Movement (Full Implementation)

**Video**: https://www.youtube.com/watch?v=bXNFxQpp2qk (27:43)

### Key Concepts

This video is a complete walkthrough combining: Animator Controller setup, New Input System configuration, Built-In Character Controller movement, rotation with Quaternions, and gravity -- all in one project.

### Complete Setup Checklist

1. Download idle/walk/run animations from Mixamo (with In Place checked)
2. Import New Input System package
3. Import character model (Jammo used in tutorial)
4. Convert all animations and model to **Humanoid** rig type
5. Set animation settings: Loop Time, Bake Into Pose for Root Transform Rotation (Based Upon: Original)
6. Create Animator Controller with idle/walk/run states + transitions
7. Create Input Actions asset with Move (Vector2) and Run (Button) actions
8. Generate C# class from Input Actions
9. Attach CharacterController component to character

### Animator Controller Setup

- Three states: Idle, Walk, Run
- Two boolean parameters: isWalking, isRunning
- Transitions with Has Exit Time **disabled**
- Idle -> Walk (isWalking = true), Walk -> Idle (isWalking = false)
- Walk -> Run (isRunning = true), Run -> Walk (isRunning = false)

### New Input System: WASD + Controller

For the Move action (Vector2):
- Gamepad: Left Stick binding
- Keyboard: **Vector 2 Composite** binding (W=Up, S=Down, A=Left, D=Right)
- Processor: **Normalize Vector 2** (prevents diagonal movement from being faster)

For the Run action (Button):
- Keyboard: Left Shift
- Gamepad: Left Bumper

### Code: Complete Movement + Animation + Rotation + Gravity

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementController : MonoBehaviour
{
    PlayerInput playerInput;
    CharacterController characterController;
    Animator animator;

    int isWalkingHash, isRunningHash;
    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    bool isMovementPressed, isRunPressed;

    float rotationFactorPerFrame = 15f;
    float runMultiplier = 3f;
    float groundedGravity = -0.05f;
    float gravity = -9.8f;

    void Awake()
    {
        playerInput = new PlayerInput();
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");

        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;

        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;
    }

    void OnMovementInput(InputAction.CallbackContext ctx)
    {
        currentMovementInput = ctx.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        currentRunMovement.x = currentMovementInput.x * runMultiplier;
        currentRunMovement.z = currentMovementInput.y * runMultiplier;
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }

    void OnRun(InputAction.CallbackContext ctx)
    {
        isRunPressed = ctx.ReadValueAsButton();
    }

    void Update()
    {
        HandleAnimation();
        HandleRotation();
        HandleGravity();

        if (isRunPressed)
            characterController.Move(currentRunMovement * Time.deltaTime);
        else
            characterController.Move(currentMovement * Time.deltaTime);
    }

    void HandleAnimation()
    {
        bool isWalking = animator.GetBool(isWalkingHash);
        bool isRunning = animator.GetBool(isRunningHash);

        if (isMovementPressed && !isWalking)
            animator.SetBool(isWalkingHash, true);
        if (!isMovementPressed && isWalking)
            animator.SetBool(isWalkingHash, false);

        if (isMovementPressed && isRunPressed && !isRunning)
            animator.SetBool(isRunningHash, true);
        if ((!isMovementPressed || !isRunPressed) && isRunning)
            animator.SetBool(isRunningHash, false);
    }

    void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.y = 0f;
        positionToLookAt.z = currentMovement.z;

        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation,
                rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = groundedGravity;
            currentRunMovement.y = groundedGravity;
        }
        else
        {
            currentMovement.y = gravity;
            currentRunMovement.y = gravity;
        }
    }

    void OnEnable() { playerInput.CharacterControls.Enable(); }
    void OnDisable() { playerInput.CharacterControls.Disable(); }
}
```

### Rotation: Quaternion Methods

| Method | Purpose |
|--------|---------|
| `Quaternion.LookRotation(direction)` | Creates rotation facing a direction vector |
| `Quaternion.Slerp(from, to, t)` | Spherical interpolation; smooth rotation. t=0 to 1; multiply by Time.deltaTime for frame-independent smoothing |

### Gravity Notes

- Built-In CharacterController does NOT apply gravity automatically
- Must set Y velocity every frame
- When grounded, use small downward force (-0.05f) to keep `isGrounded` reliable
- When not grounded, use actual gravity (-9.8f)

### DRY Principle: Don't Repeat Yourself

- Extract input handling into named functions (e.g., `OnMovementInput`) and reuse across `started`, `performed`, `canceled` callbacks
- Separate concerns: `HandleAnimation()`, `HandleRotation()`, `HandleGravity()` as distinct methods

### Combat Game Application

This is the complete reference pattern for character movement in a 3D combat game. Key adaptations for The Scorpion:
- Add `HandleCombat()` for attack input processing
- Add `HandleDodge()` for dodge/roll with i-frames
- Gravity handling is essential for keeping the character grounded during combat
- `Quaternion.Slerp` rotation speed should be high for responsive combat turning (15-20)
- The `characterController.Move()` * `Time.deltaTime` pattern ensures frame-rate-independent movement

---

## Video 12: Animation Rigging & Procedural Animation

**Video**: https://www.youtube.com/watch?v=Wx1s3CJ8NHw (13:33)

### Key Concepts

- **Procedural Animation** is generated in real-time based on the environment and game state, not pre-baked.
- The **Animation Rigging Package** provides constraints, rigs, and a UI for procedural animation in Unity.
- It builds on top of Mecanim (requires Animator component).

### Standard vs Procedural Animation Pipeline

**Standard**: Model > Rig > Weight > Animate (in Blender) > Import to Unity > Play
**Procedural**: Model > Rig > Weight > Import to Unity > Add runtime rigs + constraints > Animation responds to environment

### Examples in Games

- **Uncharted**: Wall touching, foot placement, head/arm rotation (subtle procedural details)
- **Fall Guys / Gang Beasts**: Almost entirely physics-based procedural animation

### Package Setup

1. Window > Package Manager > Install **Animation Rigging**
2. Add **Animator** component to character (required)
3. Add **Rig Builder** component to same GameObject as Animator
4. (Optional) Add **Bone Renderer** component for visualizing bones in Scene view

### Component Hierarchy

```
Character (Animator + Rig Builder + Bone Renderer)
  |-- Armature (bone hierarchy)
  |-- TargetTracking (Rig component)     <-- child of character, NOT inside armature
        |-- HeadTracking (Multi-Aim Constraint)
        |-- LookAtTarget (empty GameObject)
```

### Key Rule

Rigs and constraints go OUTSIDE the bone hierarchy, as siblings to the armature -- not on the bones themselves.

### Rig Builder

- Lists all rigs as layers ordered by priority
- Activate/deactivate rigs for different situations (combat rig, climbing rig, etc.)
- Interacts with the Animator component

### Bone Renderer

- Visualizes bones in Scene view (three shape options)
- Customizable size, color, tripod axes display
- Select all bones in hierarchy > drag into "Transforms" list
- Or use Animation Rigging dropdown > auto-setup

### Constraints Available

The package includes roughly a dozen constraint types. Example covered:

**Multi-Aim Constraint**: Rotates constrained object toward a target.

| Property | Purpose |
|----------|---------|
| Weight | 0-1 influence percentage |
| Aim Axis | Which axis points at target (typically Z in Unity) |
| Up Axis | Which axis stays "up" |
| World Up Type | Direction considered "upwards"; affects roll behavior |
| Source Objects | Target transforms (multiple supported; weight blends between them) |
| Maintain Offset | Keeps original rotation until source moves |
| Constrained Axes | Which axes can rotate (X, Y, Z toggles) |
| Min/Max Limits | Rotation limits per axis (e.g., -90 to 90 for head) |

### Constraints Only Work When

- Game is in Play Mode, OR
- Preview Mode is active in Animation/Timeline window

### Multiple Rigs for Different Situations

```
Character
  |-- TargetTracking (Rig) -- head look-at, eye tracking
  |-- CombatRig (Rig) -- weapon holding, attack overrides
  |-- ClimbingRig (Rig) -- hand/foot placement on walls
  |-- PassiveInteractions (Rig) -- touching walls, leaning
```

### Combat Game Application

Procedural animation via the Animation Rigging package adds polish to a combat game:
- **Head Tracking**: Character head follows the locked-on enemy (Multi-Aim Constraint)
- **Weapon Holding**: IK constraints for dual blade positioning
- **Hit Reactions**: Procedural spine/torso rotation toward the hit direction
- **Wall Interactions**: Hands touching walls when near surfaces
- **Foot Placement**: IK for feet on uneven terrain in the arena

---

## Video 13: Character Rig & State Machine for Procedural Animation

**Video**: https://www.youtube.com/watch?v=-Oqa-iOZpIE (13:10)

### Key Concepts

- **Two-Bone IK Constraint**: Positions and rotates joints based on a target position (hand follows target, arm bends naturally).
- **Multi-Rotation Constraint**: Rotates an object to match its source object's rotation.
- Combining constraints on a single rig creates complex procedural animations.
- A **State Machine** manages when and how procedural animations activate.

### Two-Bone IK Setup

1. Create child GameObjects under the rig: "LeftArm", "RightArm"
2. Attach **Two-Bone IK Constraint** component to each
3. Set the **Tip** property to the hand bone
4. Use dropdown: **Auto Setup from Tip Transform** -- auto-fills:
   - Tip: Hand bone
   - Mid: Forearm bone
   - Root: Upper arm bone
5. Auto-creates child GameObjects: `_target` and `_hint` as source objects

### Source Objects

| Object | Purpose |
|--------|---------|
| **Target** | Where the hand (tip) tries to reach |
| **Hint** | Direction the limb should bend (place behind elbow for natural bend) |

### Visualizing Source Objects

- Select target/hint GameObjects
- Bottom-right of Scene view > press + icon > select a mesh shape
- Transparent mesh visible only in Scene view; makes working with IK much easier

### Adding Multi-Rotation Constraint for Hand Orientation

Problem: Two-Bone IK positions the hand but doesn't control palm direction.

Solution:
1. Add **Multi-Rotation Constraint** to the same LeftArm/RightArm GameObjects
2. Constrained Object: hand bone
3. Source Object: the IK target GameObject
4. Keep all three axes checked
5. Adjust **Offset** so palm faces forward:
   - Left hand: 90 degrees on Y axis (character-dependent)
   - Right hand: -90 degrees on Y axis
6. Align palm with the Z forward direction of the target

### Default Positions

- **Hint** objects: Place slightly behind and to the side of elbows
- **Target** objects: About elbow height, in front of character
- Adjust rig weight 0-1 to see procedural override blend

### State Machine Architecture for Environment Interactions

```
Scripts/
  StateMachine/
    StateManager.cs        (abstract base)
    BaseState.cs           (abstract base)
  EnvironmentInteractions/
    EnvironmentInteractionStateMachine.cs
    EnvironmentInteractionContext.cs
    EnvironmentInteractionState.cs
    SearchState.cs
    ApproachState.cs
    RiseState.cs
    TouchState.cs
    ResetState.cs
```

### State Machine Code Pattern

```csharp
using UnityEngine.Animations.Rigging;

public class EnvironmentInteractionStateMachine : StateManager<
    EnvironmentInteractionStateMachine.EEnvironmentInteractionState>
{
    public enum EEnvironmentInteractionState
    {
        Search,
        Approach,
        Rise,
        Touch,
        Reset
    }

    [SerializeField] private TwoBoneIKConstraint _leftIKConstraint;
    [SerializeField] private TwoBoneIKConstraint _rightIKConstraint;
    [SerializeField] private MultiRotationConstraint _leftMultiRotationConstraint;
    [SerializeField] private MultiRotationConstraint _rightMultiRotationConstraint;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private CapsuleCollider _rootCollider;

    void Awake()
    {
        ValidateConstraints();
    }

    void ValidateConstraints()
    {
        Assert.IsNotNull(_leftIKConstraint, "Left IK Constraint not assigned!");
        Assert.IsNotNull(_rightIKConstraint, "Right IK Constraint not assigned!");
        // ... validate all serialized fields
    }
}
```

### Assertions for Validation

- Use `UnityEngine.Assertions.Assert.IsNotNull()` to validate serialized field references
- Halts execution with a clear error message if references are missing
- Catches configuration errors immediately at runtime

### Combat Game Application

The Two-Bone IK + Multi-Rotation pattern is directly applicable to a dual-blade combat game:
- **Weapon Holding IK**: Ensure hands properly grip dual blades regardless of body animation
- **Shield/Block Positioning**: IK targets for defensive poses
- **Environment Touch**: Hands brace against walls during combat near arena edges
- **State Machine Pattern**: Use a combat state machine (Search > Approach > Attack > Recovery > Reset) to manage procedural animation states alongside AI behavior
- The state machine enum pattern (Search, Approach, Rise, Touch, Reset) maps directly to enemy AI states in a wave-based combat system

---

## Summary: Key Takeaways for Building a 3D Melee Combat Game

### Animation System Architecture

1. **Animator Controller** as the central state machine for all character animations
2. **Boolean parameters** for discrete states (isAttacking, isDodging, isBlocking)
3. **Float parameters + Blend Trees** for smooth locomotion (walk/run/strafe blending)
4. **2D Freeform Directional** blend trees for lock-on strafing movement
5. **Animation Layers + Avatar Masks** for upper body combat over lower body locomotion
6. **Animation Events** for hit detection, VFX spawning, sound triggers at precise frames
7. **Animation Curves** for fine-tuning attack weight, impact, and timing
8. **Humanoid Rigs** for retargeting animations across all character types (player + enemies + boss)

### Performance Optimizations

- `Animator.StringToHash()` for all parameter access
- Only `SetBool`/`SetFloat` when values actually change
- `Cull Update Transforms` for off-screen enemies
- `Optimize Game Object` toggle before shipping

### Input Handling

- New Input System with generated C# class
- Callbacks (`started`/`performed`/`canceled`) for responsive input
- Normalize Vector2 processor for consistent diagonal movement
- Stick Deadzone processor for controller support

### Procedural Animation for Polish

- Animation Rigging package for head tracking (lock-on target), hand IK (weapon grips), foot placement
- Separate rigs for different contexts (combat, traversal, idle)
- Multi-Aim Constraint for enemy look-at
- Two-Bone IK for precise hand positioning

### Character Controller Choice

For a combat game with Invector framework: extend Invector's existing Rigidbody-based controller rather than replacing it. The principles of gravity handling, grounded checks, and movement apply regardless of controller type.
