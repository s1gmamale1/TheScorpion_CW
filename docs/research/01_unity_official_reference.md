# Unity 3D Game Development Reference Guide
## For Unity 6 / 2022.3 LTS with URP — Action/Combat Game Focus

---

## Table of Contents
1. [Learning Resources](#1-learning-resources)
2. [MonoBehaviour Lifecycle](#2-monobehaviour-lifecycle)
3. [GameObjects & Components](#3-gameobjects--components)
4. [Physics System](#4-physics-system)
5. [Animation System](#5-animation-system)
6. [Input System (New)](#6-input-system-new)
7. [UI Systems](#7-ui-systems)
8. [URP (Rendering & Lighting)](#8-urp-rendering--lighting)
9. [NavMesh & AI Navigation](#9-navmesh--ai-navigation)
10. [Prefabs & Asset Management](#10-prefabs--asset-management)
11. [ScriptableObjects](#11-scriptableobjects)
12. [Coroutines](#12-coroutines)
13. [Best Practices](#13-best-practices)

---

## 1. Learning Resources

### Unity Learn (learn.unity.com) — Free Courses

| Course | Level | Relevance |
|--------|-------|-----------|
| 3D Stealth Game: Haunted House | Beginner | 3D game structure, enemy awareness |
| Game Development Pathway (12 weeks) | Beginner | Full foundation for game creation |
| UI Toolkit Fundamentals | Intermediate | Modern game HUD/UI |
| VFX Graph Fundamentals | Intermediate | Combat effects, fire/lightning VFX |
| Introduction to Unity for Industry | Beginner | Editor fundamentals |

**Quick Start**: Four beginner tutorials (10-30 min each) covering editor setup through publishing.

### Key Documentation URLs
- Manual: https://docs.unity3d.com/Manual/
- Scripting API: https://docs.unity3d.com/ScriptReference/
- URP Docs: https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest
- Input System: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.8/manual/
- AI Navigation: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/
- Best Practices: https://docs.unity3d.com/6000.0/Documentation/Manual/best-practice-guides.html

---

## 2. MonoBehaviour Lifecycle

The execution order is critical for game programming. Every script inheriting from `MonoBehaviour` follows this order:

### Initialization (once per object)
```
Awake()          → Called when script loads. Use for self-references (GetComponent on same object).
OnEnable()       → Called when object/component is enabled. Subscribe to events here.
Start()          → Called before first Update, after all Awake() calls. Use for cross-object references.
```

### Game Loop (every frame)
```
FixedUpdate()    → Fixed timestep (default 0.02s). ALL physics code goes here.
                   Runs 0-N times per frame depending on frame rate.

Update()         → Once per frame. Input handling, game logic, non-physics movement.

LateUpdate()     → Once per frame, after ALL Updates. Camera follow, post-animation adjustments.
```

### Animation Callbacks
```
OnAnimatorMove() → Called during animation update. Override root motion here.
OnAnimatorIK()   → Called during IK pass. Set IK targets here.
```

### Physics Callbacks
```
OnCollisionEnter(Collision) → First frame of collision contact
OnCollisionStay(Collision)  → Every frame while colliding
OnCollisionExit(Collision)  → Frame when collision ends

OnTriggerEnter(Collider)    → First frame inside trigger volume
OnTriggerStay(Collider)     → Every frame while inside trigger
OnTriggerExit(Collider)     → Frame when leaving trigger
```

### Cleanup
```
OnDisable()      → Object/component disabled. Unsubscribe from events here.
OnDestroy()      → Object about to be destroyed. Final cleanup.
```

### Key MonoBehaviour Methods
```csharp
// Coroutines
Coroutine handle = StartCoroutine(MyRoutine());
StopCoroutine(handle);
StopAllCoroutines();

// Delayed invocation
Invoke("MethodName", 2.0f);                    // Call after 2 seconds
InvokeRepeating("MethodName", 0.5f, 1.0f);     // Start after 0.5s, repeat every 1s
CancelInvoke("MethodName");

// Key properties
this.enabled        // Toggle update calls
this.gameObject     // The attached GameObject
this.transform      // Quick Transform access
this.isActiveAndEnabled  // Full active check
```

**Critical Rule**: You cannot rely on execution order between different GameObjects' same callbacks unless you set Script Execution Order in Project Settings.

---

## 3. GameObjects & Components

### Core Architecture
- **GameObject**: Container/entity. Does nothing alone. Every one has a Transform.
- **Component**: Behavior/data attached to a GameObject. This is the fundamental building block.
- Unity uses a **component-based architecture**, not inheritance hierarchies.

### Common Component Combinations
| Object | Components |
|--------|-----------|
| Character | Transform + MeshFilter + MeshRenderer + Animator + CapsuleCollider + Rigidbody + Scripts |
| Light | Transform + Light |
| Prop/Obstacle | Transform + MeshFilter + MeshRenderer + BoxCollider |
| Trigger Zone | Transform + BoxCollider (isTrigger) |
| Audio Source | Transform + AudioSource |

### Essential Operations
```csharp
// Getting components
Rigidbody rb = GetComponent<Rigidbody>();
Animator anim = GetComponentInChildren<Animator>();
Collider[] cols = GetComponentsInChildren<Collider>();

// Adding/removing
gameObject.AddComponent<Rigidbody>();
Destroy(GetComponent<BoxCollider>());

// Hierarchy
transform.parent = otherTransform;
transform.SetParent(otherTransform, worldPositionStays: true);
Transform child = transform.Find("ChildName");
int childCount = transform.childCount;

// Activation
gameObject.SetActive(false);  // Disables entire GameObject and children
enabled = false;               // Disables just this component

// Tags and Layers
if (other.CompareTag("Enemy")) { ... }
gameObject.layer = LayerMask.NameToLayer("Enemies");
```

---

## 4. Physics System

Unity's 3D physics uses **Nvidia PhysX**.

### Rigidbody — Key Properties
| Property | Description | Combat Game Usage |
|----------|-------------|-------------------|
| mass | Object mass (kg) | Affects knockback force |
| linearDamping (drag) | Resistance to linear movement | Higher = stops faster after knockback |
| angularDamping | Resistance to rotation | Prevent spinning after hits |
| useGravity | Gravity toggle | true for most objects |
| isKinematic | Ignore physics forces | true for animated characters (Invector handles this) |
| collisionDetectionMode | Detection accuracy | Continuous for fast-moving weapons |
| interpolation | Smooth rendering | Interpolate for player character |
| constraints | Lock position/rotation axes | FreezeRotation X/Z for characters |

### Collision Detection Modes
- **Discrete**: Default. Fast but can miss fast objects (tunneling).
- **Continuous**: Prevents tunneling against static colliders. Use for player.
- **ContinuousDynamic**: Prevents tunneling against everything. Use for projectiles.
- **ContinuousSpeculative**: Cheapest continuous mode. Good compromise.

### Collider Types
| Type | Shape | Use Case |
|------|-------|----------|
| BoxCollider | Rectangular | Walls, crates, platforms |
| SphereCollider | Sphere | Pickups, area checks |
| CapsuleCollider | Cylinder+hemispheres | Characters, humanoids |
| MeshCollider | Exact mesh shape | Complex static geometry (expensive) |

### Triggers vs Colliders
- **Collider** (isTrigger = false): Physical blocking, generates OnCollision callbacks
- **Trigger** (isTrigger = true): No physical blocking, generates OnTrigger callbacks
- **Rule**: At least one object in the interaction MUST have a Rigidbody

### Force Application
```csharp
// In FixedUpdate() only!
rb.AddForce(direction * forceMagnitude, ForceMode.Impulse);  // Instant hit (knockback)
rb.AddForce(direction * forceMagnitude, ForceMode.Force);     // Sustained push (wind)

// ForceMode enum:
// Force            — Continuous, mass-dependent (N)
// Acceleration     — Continuous, mass-independent (m/s²)
// Impulse          — Instant, mass-dependent (N·s) — USE FOR KNOCKBACK
// VelocityChange   — Instant, mass-independent (m/s)

// Kinematic movement (for animated characters)
rb.MovePosition(targetPos);    // Smooth, respects interpolation
rb.MoveRotation(targetRot);

// Raycasting for hit detection
if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, layerMask))
{
    Debug.Log($"Hit {hit.collider.name} at {hit.point}");
}

// SphereCast for melee weapon sweeps
if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit, maxDistance, layerMask))
{
    hit.collider.GetComponent<vHealthController>()?.TakeDamage(damageInfo);
}
```

---

## 5. Animation System

Unity's Mecanim system is built around three pillars: **Animation Clips**, **Animator Controller**, and **Avatar** (for humanoids).

### Animator Controller Structure
```
Animator Controller (asset)
├── Parameters (Float, Int, Bool, Trigger)
├── Layers
│   ├── Base Layer (full body)
│   │   ├── States (Idle, Walk, Run, Attack, Hit, Die)
│   │   ├── Transitions (arrows between states with conditions)
│   │   └── Blend Trees (locomotion blending)
│   ├── Upper Body Layer (with Avatar Mask)
│   │   └── Attack states (play on top of locomotion)
│   └── ...
└── Sub-State Machines (group related states)
```

### Parameter Types
| Type | Use Case | Script Method |
|------|----------|--------------|
| Float | Speed, blend values | `animator.SetFloat("Speed", value)` |
| Int | Combo index, state ID | `animator.SetInteger("ComboIndex", 2)` |
| Bool | IsGrounded, IsBlocking | `animator.SetBool("IsGrounded", true)` |
| Trigger | Attack, Dodge, Die (fire-and-forget) | `animator.SetTrigger("Attack")` |

**Trigger** auto-resets after being consumed by a transition. Use `ResetTrigger("Attack")` to cancel queued triggers.

### Controlling Animations from Scripts
```csharp
private Animator animator;

void Start()
{
    animator = GetComponent<Animator>();
}

void Update()
{
    // Locomotion blending
    animator.SetFloat("Forward", verticalInput);
    animator.SetFloat("Strafe", horizontalInput);

    // Combat
    if (attackInput)
        animator.SetTrigger("Attack");

    if (dodgeInput)
        animator.SetTrigger("Dodge");

    // State checks
    animator.SetBool("IsGrounded", isGrounded);
    animator.SetBool("IsBlocking", blockInput);

    // Read current state
    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0); // layer 0
    if (state.IsName("Attack"))
    {
        // Currently in attack animation
    }
}
```

### Blend Trees (Locomotion)
- **1D Blend Tree**: Single parameter (e.g., Speed) blends Idle → Walk → Run
- **2D Blend Trees**:
  - **Simple Directional**: One motion per direction (idle center, forward walk, strafe left/right)
  - **Freeform Directional**: Multiple motions per direction
  - **Freeform Cartesian**: Independent X/Y axes (Forward + Strafe)

**Key Rule**: Blended animations must have matching timing — footfalls should occur at the same normalized time positions.

### Animation Layers
- **Base Layer**: Full body locomotion
- **Override Layer**: Completely replaces base (e.g., upper body attacks)
- **Additive Layer**: Adds on top of base (e.g., breathing, head look)
- Use **Avatar Masks** to restrict layers to specific body parts (e.g., upper body only for attacks while legs keep running)

### Root Motion
- Drives character position from animation data rather than code
- Enable `Apply Root Motion` on Animator component
- Override in `OnAnimatorMove()` for custom blending:
```csharp
void OnAnimatorMove()
{
    // Blend root motion with navmesh or custom movement
    transform.position += animator.deltaPosition;
    transform.rotation *= animator.deltaRotation;
}
```

---

## 6. Input System (New)

Package: `com.unity.inputsystem`

### Setup
1. Install via Package Manager
2. Edit > Project Settings > Input System Package > Create default Input Actions asset

### Default Action Maps
- **Player**: Move, Look, Jump, Attack (gameplay)
- **UI**: Navigate, Submit, Cancel (menu navigation)

### Reading Input in Code
```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombatInput : MonoBehaviour
{
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction dodgeAction;
    private InputAction elementSwitchAction;
    private InputAction ultimateAction;

    void Start()
    {
        moveAction = InputSystem.actions.FindAction("Move");
        attackAction = InputSystem.actions.FindAction("Attack");
        dodgeAction = InputSystem.actions.FindAction("Jump");  // rebind to dodge
        elementSwitchAction = InputSystem.actions.FindAction("Player/ElementSwitch");
        ultimateAction = InputSystem.actions.FindAction("Player/Ultimate");
    }

    void Update()
    {
        // Continuous input (movement)
        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        // Button press (combat)
        if (attackAction.IsPressed())
        {
            // Trigger attack
        }

        if (dodgeAction.WasPressedThisFrame())
        {
            // Trigger dodge roll
        }
    }
}
```

### Key Methods
- `ReadValue<T>()` — Continuous values (Vector2 for movement, float for axes)
- `IsPressed()` — True while button is held
- `WasPressedThisFrame()` — True only on the frame the button was pressed
- `WasReleasedThisFrame()` — True only on the frame the button was released

### Multi-Map Resolution
When two Action Maps share an action name: `InputSystem.actions.FindAction("Player/Attack")`

**Note for Invector**: Invector has its own input system. Your custom systems (ElementSystem, UltimateSystem) should use either Invector's `vInput` or the new Input System consistently — do not mix both for the same actions.

---

## 7. UI Systems

Unity has three UI systems. For a new combat game HUD:

| System | Status | Best For |
|--------|--------|----------|
| **UI Toolkit** | Current/Recommended | New projects, complex UI, web-like CSS styling |
| **uGUI (Canvas)** | Legacy but stable | Runtime game UI, well-documented, huge community |
| **IMGUI** | Legacy | Editor tools only, not for game UI |

### Recommendation for The Scorpion
Use **uGUI (Canvas-based)** because:
- Invector's built-in UI uses uGUI
- More tutorials and community support for game HUDs
- Better for rapid prototyping under a tight deadline

### Canvas Render Modes
| Mode | Description | Use Case |
|------|-------------|----------|
| Screen Space - Overlay | Always on top, scales with screen | Main HUD (health, adrenaline, wave counter) |
| Screen Space - Camera | Rendered by specific camera | UI with depth/3D effects |
| World Space | Exists in 3D world | Enemy health bars above heads, damage numbers |

### HUD Elements for Combat Game
```
Canvas (Screen Space - Overlay)
├── HealthBar (Slider + Image fill)
├── AdrenalineBar (Slider + custom fill)
├── ElementIndicator (Image + Text)
├── WaveCounter (Text)
├── AbilityCooldowns (Image + radial fill)
├── BossHealthBar (Slider, hidden until boss wave)
└── DamageNumbers (pooled Text objects, World Space canvas)
```

---

## 8. URP (Rendering & Lighting)

### What URP Provides
- Optimized single-pass rendering for mobile through high-end PC
- Shader Graph for visual shader creation
- Post-processing stack (Bloom, Color Grading, Ambient Occlusion, etc.)
- Anti-aliasing: FXAA, SMAA, TAA, MSAA

### URP Asset Configuration (Key Settings for Combat Game)
```
URP Asset (ScriptableObject)
├── Rendering
│   ├── Renderer: Forward (default, good for most games)
│   ├── Depth/Opaque Texture: Enable for shader effects
│   └── HDR: Enable for bloom/glow effects (fire/lightning)
├── Quality
│   ├── Anti-aliasing: FXAA or TAA
│   └── Render Scale: 1.0 for quality, lower for performance
├── Lighting
│   ├── Main Light: Per Pixel (for directional light shadows)
│   ├── Additional Lights: Per Pixel, limit 4-8
│   └── Mixed Lighting: Subtractive or Baked Indirect
├── Shadows
│   ├── Max Distance: 50m (arena is 25x25, so this covers it)
│   ├── Cascade Count: 2-3
│   └── Soft Shadows: Enable for quality
└── Post Processing
    └── Enabled: true
```

### Post-Processing for Combat Effects
Use a **Volume** component (Global) with these profiles:
- **Bloom**: Make fire/lightning effects glow
- **Color Grading**: Set mood (warm for fire phase, cool for lightning)
- **Vignette**: Darken edges during low health
- **Chromatic Aberration**: Subtle, during hits/damage
- **Motion Blur**: Optional, during dodge rolls

---

## 9. NavMesh & AI Navigation

Package: `com.unity.ai.navigation` (v2.0)

### Components
| Component | Purpose |
|-----------|---------|
| NavMeshSurface | Bakes walkable area on geometry |
| NavMeshAgent | Moves an AI character along the NavMesh |
| NavMeshObstacle | Dynamic obstacle that carves/blocks NavMesh |
| NavMeshLink | Bridge between disconnected NavMesh areas |
| NavMeshModifier | Change area type of specific objects |
| NavMeshModifierVolume | Change area type in a volume region |

### NavMeshAgent Properties for Enemy Types

| Property | Hollow Monk (Basic) | Shadow Acolyte (Fast) | Stone Sentinel (Heavy) |
|----------|--------------------|-----------------------|------------------------|
| Speed | 3.5 | 6.0 | 2.0 |
| Angular Speed | 120 | 200 | 80 |
| Acceleration | 8 | 12 | 4 |
| Stopping Distance | 1.5 | 1.0 | 2.0 |
| Radius | 0.5 | 0.4 | 0.8 |
| Height | 2.0 | 1.8 | 2.5 |
| Priority | 50 | 50 | 30 (higher priority) |

### Basic Enemy AI Navigation
```csharp
using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform player;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindWithTag("Player").transform;
    }

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance > agent.stoppingDistance)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // In attack range — trigger attack
            agent.ResetPath();
        }
    }
}
```

### Arena Setup (25x25m)
1. Place NavMeshSurface on the arena floor
2. Mark walls/pillars as NavMeshObstacle (Carve = true)
3. Bake at edit time (or runtime for dynamic obstacles)
4. Spawn points at N/S/E/W edges, enemies navigate inward

---

## 10. Prefabs & Asset Management

### What Prefabs Are
A prefab is a stored GameObject template (with all components, children, and property values) that can be instantiated multiple times.

### Prefab Workflow
```
1. Create GameObject in scene, configure it
2. Drag from Hierarchy → Project window = creates Prefab asset
3. Scene object becomes a Prefab Instance (linked to asset)
4. Changes to Prefab asset propagate to all instances
5. Instance Overrides let you customize individual instances
```

### Key Concepts
- **Nested Prefabs**: Prefab within a prefab (e.g., Enemy prefab containing Weapon prefab)
- **Prefab Variants**: Base prefab with overrides (e.g., base Enemy → Hollow Monk variant, Shadow Acolyte variant)
- **Unpacking**: Breaks the prefab link, converts back to regular GameObject

### Runtime Instantiation (Critical for Wave Spawning)
```csharp
public class WaveSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform[] spawnPoints;

    public void SpawnEnemy(int prefabIndex, int spawnPointIndex)
    {
        GameObject enemy = Instantiate(
            enemyPrefabs[prefabIndex],
            spawnPoints[spawnPointIndex].position,
            spawnPoints[spawnPointIndex].rotation
        );

        // Configure the spawned enemy
        enemy.GetComponent<EnemyAI>().Initialize(currentWave);
    }
}
```

### Prefab Structure for The Scorpion
```
Prefabs/
├── Player/
│   └── Scorpion_Player.prefab
├── Enemies/
│   ├── HollowMonk.prefab
│   ├── ShadowAcolyte.prefab
│   ├── StoneSentinel.prefab
│   └── FallenGuardian_Boss.prefab
├── VFX/
│   ├── FireBurst.prefab
│   ├── LightningStrike.prefab
│   └── DamageNumber.prefab
├── UI/
│   └── GameHUD.prefab
└── Environment/
    └── SpawnPoint.prefab
```

---

## 11. ScriptableObjects

### What They Are
Data containers that exist as project assets (not attached to GameObjects). Ideal for shared configuration and game data.

### Creating ScriptableObjects
```csharp
[CreateAssetMenu(fileName = "NewWaveData", menuName = "TheScorpion/Wave Data")]
public class WaveData : ScriptableObject
{
    public int waveNumber;
    public int hollowMonkCount;
    public int shadowAcolyteCount;
    public int stoneSentinelCount;
    public float spawnDelay;
    public float timeBetweenSpawns;
}
```

Then right-click in Project: Create > TheScorpion > Wave Data

### Use Cases for The Scorpion
| ScriptableObject | Data |
|-----------------|------|
| WaveData | Enemy counts per wave, spawn timing |
| EnemyStats | HP, damage, speed, element weakness |
| AbilityData | Cooldown, energy cost, damage, radius |
| ElementConfig | Fire/Lightning DoT values, CC durations |
| BossPhaseData | HP thresholds, attack patterns, summon counts |

### Advantages
- **Memory efficient**: One instance shared across all references (vs duplicated MonoBehaviour data)
- **Designer friendly**: Edit values in Inspector without touching code
- **Hot reload**: Change values during play mode (but changes persist, unlike MonoBehaviour)
- **Decoupled**: Systems reference data assets, not each other

### ScriptableObject Event System Pattern
```csharp
[CreateAssetMenu(menuName = "TheScorpion/Game Event")]
public class GameEvent : ScriptableObject
{
    private List<GameEventListener> listeners = new List<GameEventListener>();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
            listeners[i].OnEventRaised();
    }

    public void Register(GameEventListener listener) => listeners.Add(listener);
    public void Unregister(GameEventListener listener) => listeners.Remove(listener);
}
```
This pattern decouples systems (WaveManager raises "WaveComplete" event, HUD listens without direct reference).

---

## 12. Coroutines

Coroutines let you spread work across multiple frames — essential for wave spawning, cooldowns, delayed effects.

### Basic Pattern
```csharp
IEnumerator SpawnWave(WaveData data)
{
    yield return new WaitForSeconds(data.spawnDelay);  // Initial delay

    for (int i = 0; i < data.hollowMonkCount; i++)
    {
        SpawnEnemy(hollowMonkPrefab);
        yield return new WaitForSeconds(data.timeBetweenSpawns);
    }

    // Continue with other enemy types...
}

// Start it
StartCoroutine(SpawnWave(currentWaveData));
```

### Yield Types
| Yield | Resumes When |
|-------|-------------|
| `yield return null` | Next frame (after Update) |
| `yield return new WaitForSeconds(t)` | After t seconds (scaled time) |
| `yield return new WaitForSecondsRealtime(t)` | After t real seconds (ignores Time.timeScale) |
| `yield return new WaitForFixedUpdate()` | Next FixedUpdate |
| `yield return new WaitForEndOfFrame()` | After rendering |
| `yield return new WaitUntil(() => condition)` | When condition becomes true |
| `yield return new WaitWhile(() => condition)` | When condition becomes false |
| `yield return StartCoroutine(Other())` | When nested coroutine finishes |

### Combat Game Patterns
```csharp
// Damage over time (Fire element)
IEnumerator ApplyBurnDamage(vHealthController health, float dps, float duration)
{
    float elapsed = 0f;
    while (elapsed < duration)
    {
        health.TakeDamage(dps * Time.deltaTime);
        elapsed += Time.deltaTime;
        yield return null;
    }
}

// Ability cooldown
IEnumerator AbilityCooldown(float cooldownTime, System.Action onReady)
{
    yield return new WaitForSeconds(cooldownTime);
    onReady?.Invoke();
}

// Time-slow ultimate (Adrenaline Rush)
IEnumerator AdrenalineRush(float duration, float timeScale)
{
    Time.timeScale = timeScale;  // e.g., 0.3 for slow-mo
    Time.fixedDeltaTime = 0.02f * timeScale;  // Scale physics too!

    yield return new WaitForSecondsRealtime(duration);  // Use REAL time, not scaled

    Time.timeScale = 1.0f;
    Time.fixedDeltaTime = 0.02f;
}
```

### Stopping Coroutines
```csharp
private Coroutine burnCoroutine;

// Start and store reference
burnCoroutine = StartCoroutine(ApplyBurnDamage(health, 5f, 3f));

// Stop specific coroutine
if (burnCoroutine != null)
    StopCoroutine(burnCoroutine);

// Stop all coroutines on this MonoBehaviour
StopAllCoroutines();
```

---

## 13. Best Practices

### From Unity's Official Best Practice Guides

**Architecture & Scripting**
- Use ScriptableObjects for modular, data-driven architecture
- Follow SOLID principles and common design patterns (Observer, State, Command)
- C# naming conventions: PascalCase for public members, camelCase for private
- Use `[SerializeField] private` instead of `public` for Inspector-exposed fields

**Performance**
- Cache component references in Awake/Start (never GetComponent in Update)
- Use object pooling for frequently spawned objects (enemies, VFX, damage numbers)
- Minimize garbage collection: avoid `new` in Update, use StringBuilder, cache arrays
- Profile with Unity Profiler before optimizing (don't guess bottlenecks)
- Physics: use layers + layer collision matrix to skip unnecessary checks

**Graphics & Rendering (URP)**
- Use Shader Graph for custom shaders (fire glow, lightning crackling)
- Set up quality tiers in URP Asset for different hardware
- Bake static lighting where possible, use realtime only for dynamic elements
- Limit realtime lights (4-8 additional lights max in forward rendering)

**DevOps**
- Use version control (Git) with proper .gitignore for Unity
- Organize project: Assets/Scripts/, Assets/Prefabs/, Assets/Materials/, etc.
- Use Assembly Definitions for faster compile times in larger projects

**Combat Game Specific**
- Physics layers: Player, Enemy, PlayerWeapon, EnemyWeapon, Environment, Trigger
- Use Physics.SphereCast or OverlapSphere for melee hit detection
- Animation events for attack timing (damage window start/end)
- State machines for enemy AI (Idle, Patrol, Chase, Attack, Stagger, Die)
- Object pool enemies rather than Instantiate/Destroy each wave

---

## Quick Reference: Key APIs

```csharp
// Finding objects
GameObject.FindWithTag("Player")
FindObjectOfType<WaveManager>()           // Expensive, cache result
FindObjectsByType<Enemy>(FindObjectsSortMode.None)  // Unity 6 preferred

// Instantiate/Destroy
Instantiate(prefab, position, rotation)
Instantiate(prefab, parent)
Destroy(gameObject)                        // End of frame
Destroy(gameObject, delay)                 // After delay seconds
DestroyImmediate(gameObject)               // Immediately (editor only)

// Scene Management
using UnityEngine.SceneManagement;
SceneManager.LoadScene("SceneName");
SceneManager.LoadSceneAsync("SceneName");  // Non-blocking
DontDestroyOnLoad(gameObject);             // Persist across scenes

// Time
Time.deltaTime          // Frame duration (scaled)
Time.fixedDeltaTime     // Physics step duration
Time.timeScale          // 1.0 normal, 0 paused, 0.3 slow-mo
Time.unscaledDeltaTime  // Frame duration (ignores timeScale)

// Math
Vector3.Distance(a, b)
Vector3.Lerp(a, b, t)           // Linear interpolation
Quaternion.LookRotation(dir)    // Face direction
Mathf.Clamp(value, min, max)
```
