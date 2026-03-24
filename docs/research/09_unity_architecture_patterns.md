# Unity Game Architecture Deep Research
## Patterns & Best Practices for Arena Combat / Action Games
### Compiled: 2026-03-22 | 25+ Unique References

---

## Table of Contents
1. [Game Architecture Patterns Overview](#1-game-architecture-patterns-overview)
2. [ScriptableObject Architecture](#2-scriptableobject-architecture)
3. [Event System & Decoupling](#3-event-system--decoupling)
4. [Singleton Pattern](#4-singleton-pattern)
5. [Observer Pattern](#5-observer-pattern)
6. [Command Pattern & Input Buffering](#6-command-pattern--input-buffering)
7. [Strategy Pattern & Ability Systems](#7-strategy-pattern--ability-systems)
8. [State Machine Pattern & Enemy AI](#8-state-machine-pattern--enemy-ai)
9. [Dependency Injection](#9-dependency-injection)
10. [Coroutines vs Async/Await](#10-coroutines-vs-asyncawait)
11. [Scene Management](#11-scene-management)
12. [Game State Machine (Pause/Restart)](#12-game-state-machine-pauserestart)
13. [Save System](#13-save-system)
14. [Audio Manager Architecture](#14-audio-manager-architecture)
15. [Camera System (Third Person)](#15-camera-system-third-person)
16. [New Input System](#16-new-input-system)
17. [Physics Layers & Collision Matrix](#17-physics-layers--collision-matrix)
18. [NavMesh & Runtime Obstacles](#18-navmesh--runtime-obstacles)
19. [Performance Profiling & Optimization](#19-performance-profiling--optimization)
20. [Project Structure & Organization](#20-project-structure--organization)
21. [Applicability to The Scorpion](#21-applicability-to-the-scorpion)

---

## 1. Game Architecture Patterns Overview

### Core Architectural Approaches for Unity

**MVC (Model-View-Controller):** Separates data (Model), UI (View), and logic (Controller). The Controller processes game data and calculates value changes at runtime. Requires adaptation for Unity's component model.

**MVP (Model-View-Presenter):** Evolution of MVC where the View handles user input instead of the Controller. The Presenter formats Model data for display.

**MVVM (Model-View-ViewModel):** Decouples view logic from business logic. ViewModel converts Model data for View presentation through property bindings.

**ECS (Entity Component System):** Data-oriented approach where Entities are IDs, Components are pure data, and Systems contain logic. Unity's DOTS implementation prioritizes cache-friendly data layouts.

**Component-Oriented (Unity Default):** Unity's native pattern where GameObjects are containers and MonoBehaviours are components. Best practice: each component solves ONE problem.

### Three Pillars of Game Engineering (Unity Official)
1. **Modular design** - Components work independently
2. **Ease of change/editing** - Runtime-editable, data-driven
3. **Ease of debugging** - Observable systems, Inspector-friendly

### Key Principle
> "Unity initially gives a component-oriented approach, which means that some patterns will have to be adapted to the game engine." Thinking about architecture at the earliest stages reduces future refactoring.

**Sources:**
- [Unity Open Project 1 - Game Architecture Overview](https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview)
- [Organizing Architecture for Games on Unity (DEV Community)](https://dev.to/devsdaddy/organizing-architecture-for-games-on-unity-laying-out-the-important-things-that-matter-4d4p)
- [Game Design Patterns Complete Guide](https://generalistprogrammer.com/tutorials/game-design-patterns-complete-guide)

---

## 2. ScriptableObject Architecture

### Pattern 1: Variables as Data Assets

Create shared data containers that decouple systems. Instead of direct references, systems read/write to shared ScriptableObject variables.

```csharp
[CreateAssetMenu(menuName = "Variables/Float Variable")]
public class FloatVariable : ScriptableObject
{
    public float value;

    // Runtime copy to avoid modifying disk value
    [System.NonSerialized] public float runtimeValue;

    public void OnEnable()
    {
        runtimeValue = value;
    }
}
```

**Use case:** PlayerHP FloatVariable referenced by Player (writes), HealthBar UI (reads), Music system (reads), Enemy AI (reads) -- all without knowing about each other.

### Pattern 2: Event Channels (see Section 3)

### Pattern 3: Systems as ScriptableObjects

Move system logic from MonoBehaviour singletons into ScriptableObjects:
- No Transform or Update overhead
- State persists between scene loads without DontDestroyOnLoad
- Easy to swap implementations (test vs. production)

**Example:** Inventory system as a ScriptableObject asset. Player and UI both reference it directly -- no singleton needed.

### Design Recommendations
- **Data-driven design**: Store game balance, entity definitions, and configurations in ScriptableObjects
- **Runtime editability**: Design systems to be editable in Inspector during Play mode
- **Component focus**: Each component solves one problem; combine components for emergent behavior

**Sources:**
- [Unity Official: Architect Code with ScriptableObjects](https://unity.com/how-to/architect-game-code-scriptable-objects)
- [Unity E-book: Create Modular Architecture with ScriptableObjects](https://unity.com/resources/create-modular-game-architecture-with-scriptable-objects-ebook)
- [ScriptableObject Architecture GitHub](https://github.com/DanielEverland/ScriptableObject-Architecture)
- [Unity PaddleGameSO Demo](https://github.com/UnityTechnologies/PaddleGameSO)

---

## 3. Event System & Decoupling

### ScriptableObject Event Channels (Complete Implementation)

**Base Void Event Channel:**
```csharp
[CreateAssetMenu(menuName = "Events/Void Event Channel")]
public class VoidEventChannelSO : ScriptableObject
{
    public UnityAction OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
```

**Generic Typed Event Channel:**
```csharp
public abstract class GenericEventChannelSO<T> : ScriptableObject
{
    public UnityAction<T> OnEventRaised;

    public void RaiseEvent(T value)
    {
        OnEventRaised?.Invoke(value);
    }
}

[CreateAssetMenu(menuName = "Events/Float Event Channel")]
public class FloatEventChannelSO : GenericEventChannelSO<float> { }

[CreateAssetMenu(menuName = "Events/Int Event Channel")]
public class IntEventChannelSO : GenericEventChannelSO<int> { }

[CreateAssetMenu(menuName = "Events/Bool Event Channel")]
public class BoolEventChannelSO : GenericEventChannelSO<bool> { }
```

**Broadcasting (Raising Events):**
```csharp
public class TriggerEvent : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO m_EventChannel;

    public void TriggerEventMethod()
    {
        m_EventChannel.RaiseEvent();
    }
}
```

**Listening for Events:**
```csharp
public class EventListener : MonoBehaviour
{
    [SerializeField] private VoidEventChannelSO m_EventChannel;

    private void OnEnable()
    {
        m_EventChannel.OnEventRaised += HandleEvent;
    }

    private void OnDisable()
    {
        m_EventChannel.OnEventRaised -= HandleEvent;
    }

    private void HandleEvent()
    {
        // Response logic here
    }
}
```

### Key Architecture Benefits
- **Loose coupling**: Systems communicate without direct references
- **Cross-scene**: Event channels are project-level assets, work across scenes
- **Designer-friendly**: Wire up in Inspector, no code needed for listeners
- **Testable**: Can raise events from Inspector buttons or test scripts

### Unity Open Project 1 Architecture
The official Unity demo uses Event Channels as the backbone:
- Connects GameObjects, Managers, and across scenes
- Replaces traditional singletons
- All communication flows through ScriptableObject event assets

**Sources:**
- [Unity Official: ScriptableObject Event Channels](https://unity.com/how-to/scriptableobjects-event-channels-game-code)
- [Decoupled Unity Events with ScriptableObjects (Wayline)](https://www.wayline.io/blog/decoupled-unity-events-scriptable-objects)
- [Unity ScriptableObject Game Events GitHub](https://github.com/chark/unity-scriptable-objects)
- [Cross-Scene Event System GitHub](https://github.com/sticmac/unity-event-system)

---

## 4. Singleton Pattern

### Basic Singleton with Duplicate Prevention
```csharp
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }
}
```

### Persistent Singleton (Survives Scene Loads)
```csharp
public class PersistentSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}
```

### Master Singleton (Service Locator Pattern)
```csharp
public class Singleton : MonoBehaviour
{
    public static Singleton Instance { get; private set; }

    public AudioManager AudioManager { get; private set; }
    public UIManager UIManager { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        AudioManager = GetComponentInChildren<AudioManager>();
        UIManager = GetComponentInChildren<UIManager>();
    }
}
```

### When to Use vs. Avoid
**Use for:** Global managers (Audio, Camera), single-player Player reference, system controllers
**Avoid when:** Multiple instances might be needed, testability matters, tight coupling would harm maintainability

### Better Alternative: ScriptableObject Systems
Instead of singletons, use ScriptableObject assets as shared references. Any script can reference the SO asset directly, making dependencies explicit, swappable, and testable.

**Sources:**
- [Singletons in Unity Done Right (Game Dev Beginner)](https://gamedevbeginner.com/singletons-in-unity-the-right-way/)
- [Unity Game Manager Best Practices (CLIMB)](https://climbtheladder.com/10-unity-game-manager-best-practices/)
- [UnitySingleton GitHub](https://github.com/UnityCommunity/UnitySingleton)

---

## 5. Observer Pattern

### C# Event-Based Observer
```csharp
// Subject (Publisher)
using System;

public class Subject : MonoBehaviour
{
    public event Action ThingHappened;

    public void DoThing()
    {
        ThingHappened?.Invoke();
    }
}

// Observer (Subscriber)
public class Observer : MonoBehaviour
{
    [SerializeField] private Subject subjectToObserve;

    private void OnThingHappened()
    {
        Debug.Log("Observer responds");
    }

    private void Awake()
    {
        if (subjectToObserve != null)
            subjectToObserve.ThingHappened += OnThingHappened;
    }

    private void OnDestroy()
    {
        if (subjectToObserve != null)
            subjectToObserve.ThingHappened -= OnThingHappened;
    }
}
```

### Static Event Bus (Global Events)
```csharp
public static class GameEvents
{
    public static event Action<int> OnEnemyKilled;
    public static event Action<float> OnPlayerDamaged;
    public static event Action OnWaveCompleted;
    public static event Action OnGameOver;

    public static void EnemyKilled(int points) => OnEnemyKilled?.Invoke(points);
    public static void PlayerDamaged(float damage) => OnPlayerDamaged?.Invoke(damage);
    public static void WaveCompleted() => OnWaveCompleted?.Invoke();
    public static void GameOver() => OnGameOver?.Invoke();
}
```

### Key Rules
- Always unsubscribe in `OnDestroy()` or `OnDisable()` to prevent memory leaks
- Use `?.Invoke()` for null-safe event raising
- Static events add abstraction but harder to debug; SO event channels are more Inspector-friendly

**Sources:**
- [Unity Learn: Observer Pattern](https://learn.unity.com/tutorial/create-modular-and-maintainable-code-with-the-observer-pattern)
- [Observer Pattern in Unity (Medium/CodeX)](https://medium.com/codex/understanding-the-observer-pattern-event-systems-in-unity-a92cdec26d7f)
- [Observer Pattern with C# Events (One Wheel Studio)](https://onewheelstudio.com/blog/2020/7/24/observer-pattern-c-events)

---

## 6. Command Pattern & Input Buffering

### ICommand Interface with Undo/Redo
```csharp
public interface ICommand
{
    void Execute();
    void Undo();
}
```

### Command Invoker (Stack-Based Undo/Redo)
```csharp
public class CommandInvoker
{
    private static Stack<ICommand> _undoStack = new Stack<ICommand>();
    private static Stack<ICommand> _redoStack = new Stack<ICommand>();

    public static void ExecuteCommand(ICommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();
    }

    public static void UndoCommand()
    {
        if (_undoStack.Count > 0)
        {
            ICommand activeCommand = _undoStack.Pop();
            _redoStack.Push(activeCommand);
            activeCommand.Undo();
        }
    }

    public static void RedoCommand()
    {
        if (_redoStack.Count > 0)
        {
            ICommand activeCommand = _redoStack.Pop();
            _undoStack.Push(activeCommand);
            activeCommand.Execute();
        }
    }
}
```

### Concrete Move Command
```csharp
public class MoveCommand : ICommand
{
    PlayerMover playerMover;
    Vector3 movement;

    public MoveCommand(PlayerMover player, Vector3 moveVector)
    {
        this.playerMover = player;
        this.movement = moveVector;
    }

    public void Execute() => playerMover.Move(movement);
    public void Undo() => playerMover.Move(-movement);
}
```

### Input Buffer for Combat Systems
```csharp
public class InputBuffer : MonoBehaviour
{
    public Queue<string> inputQueue = new Queue<string>();
    public float actionDelay = 0.2f;
    private float timeSinceLastAction = 0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            inputQueue.Enqueue("Attack");
        if (Input.GetKeyDown(KeyCode.LeftShift))
            inputQueue.Enqueue("Dodge");
    }

    void FixedUpdate()
    {
        timeSinceLastAction += Time.deltaTime;
        if (inputQueue.Count > 0 && timeSinceLastAction >= actionDelay)
        {
            string action = inputQueue.Dequeue();
            ProcessAction(action);
            timeSinceLastAction = 0f;
        }
    }

    void ProcessAction(string action)
    {
        switch (action)
        {
            case "Attack":
                // Trigger attack animation
                break;
            case "Dodge":
                // Trigger dodge animation
                break;
        }
    }
}
```

### Combat Input Buffer Best Practices
- Limit buffer size to 2-3 actions to prevent action queuing
- Clear the buffer during state changes (stuns, knockdowns, death)
- Use animation events to trigger attack logic at correct frames
- Implement cooldowns to prevent infinite input loops
- For combo systems: store directional inputs and check for valid sequences within time windows

**Sources:**
- [Unity Learn: Command Pattern](https://learn.unity.com/course/design-patterns-unity-6/tutorial/use-the-command-pattern-for-flexible-and-extensible-game-systems)
- [Unity Combat Input Buffer (Wayline)](https://www.wayline.io/blog/unity-combat-input-buffer)
- [Unity Pattern Combo GitHub](https://github.com/homemech/unity-pattern-combo)
- [InputBuffer for Unity GitHub](https://github.com/madeyellow/InputBuffer)

---

## 7. Strategy Pattern & Ability Systems

### ScriptableObject-Based Strategy Pattern

**Abstract Ability Base:**
```csharp
public abstract class Attack : ScriptableObject, IAttack
{
    public abstract void Execute(Player player);
}

interface IAttack
{
    void Execute(Player player);
}
```

**Concrete Ability Implementations:**
```csharp
[CreateAssetMenu(menuName = "Custom/Attacks/Melee Attack")]
class MeleeAttack : Attack
{
    public int Damage = 1;
    public override void Execute(Player player)
    {
        // Melee attack logic - damage nearby enemies
    }
}

[CreateAssetMenu(menuName = "Custom/Attacks/Crossbow Attack")]
class CrossbowAttack : Attack
{
    public int Damage = 3;
    public float Range = 20f;
    public int Arrows = 5;
    public override void Execute(Player player)
    {
        // Ranged attack logic
    }
}
```

**Player Using Strategy:**
```csharp
class Player : MonoBehaviour
{
    public Attack currentAttack; // Assign in Inspector or at runtime

    public void PerformAttack()
    {
        currentAttack.Execute(this);
    }

    public void SwitchAttack(Attack newAttack)
    {
        currentAttack = newAttack;
    }
}
```

### Applicability to Element System
The Strategy pattern is ideal for The Scorpion's Fire/Lightning element system:
- Each element's abilities are ScriptableObject assets
- Switching elements swaps the active strategy
- New elements can be added without modifying core code
- Designers can tweak damage, cooldowns, and effects in Inspector

**Sources:**
- [Strategy Pattern with ScriptableObjects (DEV Community)](https://dev.to/eriksk/implementing-the-strategy-design-pattern-using-scriptable-objects-in-unity-292i)
- [Unity Learn: Strategy Pattern](https://learn.unity.com/tutorial/strategy-pattern)
- [Strategy Pattern Ability System (Dev Genius)](https://blog.devgenius.io/strategy-pattern-in-unity-b82065aaa969)
- [Strategy Pattern in Unity GitHub](https://github.com/sharpaccent/Strategy-Pattern-in-Unity)

---

## 8. State Machine Pattern & Enemy AI

### Approach 1: Enum-Based FSM (Simple, Good for Prototyping)

```csharp
public enum EnemyState { Idle, Patrol, Chase, Attack }

public class EnemyAI : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Idle;
    public float chaseDistance = 10f;
    public float attackDistance = 2f;
    public Transform player;

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case EnemyState.Idle:
                // Wait, look around
                if (dist < chaseDistance)
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Patrol:
                // Move between waypoints
                if (dist < chaseDistance)
                    currentState = EnemyState.Chase;
                break;

            case EnemyState.Chase:
                transform.position = Vector3.MoveTowards(
                    transform.position, player.position, Time.deltaTime * 5f);
                if (dist < attackDistance)
                    currentState = EnemyState.Attack;
                if (dist > chaseDistance * 1.5f)
                    currentState = EnemyState.Patrol;
                break;

            case EnemyState.Attack:
                // Deal damage
                if (dist > attackDistance)
                    currentState = EnemyState.Chase;
                break;
        }
    }
}
```

### Approach 2: Interface-Based State Pattern (Scalable)

```csharp
public interface IState
{
    void OnEnter(StateController controller);
    void UpdateState(StateController controller);
    void OnHurt(StateController controller);
    void OnExit(StateController controller);
}

public class StateController : MonoBehaviour
{
    IState currentState;

    // Pre-allocated states
    public PatrolState patrolState = new PatrolState();
    public ChaseState chaseState = new ChaseState();
    public AttackState attackState = new AttackState();
    public HurtState hurtState = new HurtState();

    void Start() => ChangeState(patrolState);

    void Update()
    {
        currentState?.UpdateState(this);
    }

    public void ChangeState(IState newState)
    {
        currentState?.OnExit(this);
        currentState = newState;
        currentState.OnEnter(this);
    }
}

public class PatrolState : IState
{
    float timeBeforeSleep;

    public void OnEnter(StateController sc) { timeBeforeSleep = 20; }

    public void UpdateState(StateController sc)
    {
        if (Physics.Raycast(sc.transform.position, sc.transform.forward))
            sc.ChangeState(sc.chaseState);
        if (timeBeforeSleep < 0)
            sc.ChangeState(sc.sleepState);
        timeBeforeSleep -= Time.deltaTime;
    }

    public void OnHurt(StateController sc) { sc.ChangeState(sc.hurtState); }
    public void OnExit(StateController sc) { }
}
```

### Approach 3: Hierarchical FSM (Complex AI)

```csharp
// Using UnityHFSM library
var fsm = new StateMachine();

// Top-level states
fsm.AddState("Idle");
fsm.AddState("Combat", new StateMachine()); // Nested FSM
fsm.AddState("Dead");

// Combat sub-states
var combatFsm = (StateMachine)fsm.GetState("Combat");
combatFsm.AddState("Chase", onLogic: state => MoveTowardsPlayer(speed));
combatFsm.AddState("Attack", onLogic: state => PerformAttack());
combatFsm.AddState("Retreat", onLogic: state => MoveAwayFromPlayer());

// Transitions
combatFsm.AddTransition("Chase", "Attack", t => DistanceToPlayer < attackRange);
combatFsm.AddTransition("Attack", "Chase", t => DistanceToPlayer > attackRange);
combatFsm.AddTransition("Attack", "Retreat", t => Health < retreatThreshold);

fsm.AddTransition("Idle", "Combat", t => DistanceToPlayer < detectionRange);
fsm.AddTransition("Combat", "Idle", t => DistanceToPlayer > detectionRange * 1.5f);

fsm.SetStartState("Idle");
fsm.Init();
```

### Super-States for Shared Behavior
```csharp
// Shared hurt response for all vulnerable states
public abstract class VulnerableState : State
{
    protected override void OnHurt()
    {
        sc.ChangeState(sc.hurtState); // All vulnerable states respond to damage
    }
}

// Specific states inherit shared behavior
public class ChaseState : VulnerableState { /* chase logic */ }
public class PatrolState : VulnerableState { /* patrol logic */ }
```

**Sources:**
- [State Machines in Unity (Game Dev Beginner)](https://gamedevbeginner.com/state-machines-in-unity-how-and-when-to-use-them/)
- [UnityHFSM Library GitHub](https://github.com/Inspiaaa/UnityHFSM)
- [Unity AI FSM Tutorial (Toptal)](https://www.toptal.com/developers/unity/unity-ai-development-finite-state-machine-tutorial)
- [Simple Enemy AI with FSM (Wayline)](https://www.wayline.io/blog/unity-enemy-ai-finite-state-machine)
- [Enemy Behaviour with Delegates (Faramira)](https://faramira.com/enemy-behaviour-with-finite-state-machine-using-csharp-delegates-in-unity/)

---

## 9. Dependency Injection

### Zenject (Most Popular Unity DI)
- Lightweight DI framework specifically for Unity
- Uses **Project Context** for cross-scene dependencies (global services)
- Uses **Scene Context** for scene-specific dependencies
- Supports constructor, method, property, and field injection

### VContainer (Faster Alternative)
- 5-10x faster than Zenject
- Minimum GC allocation (zero allocation without spawned instances)
- Allows pure C# classes as entry points (not MonoBehaviour)
- Separates control flow from MonoBehaviour view components

### Basic DI Concepts for Unity
```csharp
// Without DI (tight coupling)
public class Enemy : MonoBehaviour
{
    void Start()
    {
        var player = GameManager.Instance.Player; // Singleton dependency
    }
}

// With DI (loose coupling)
public class Enemy : MonoBehaviour
{
    [Inject] private IPlayerService _playerService;

    void Start()
    {
        var playerPos = _playerService.GetPosition();
    }
}
```

### Pragmatic Approach for Small Teams
For a tight-deadline project, full DI may be overkill. Instead:
- Use ScriptableObject references for shared data (lightweight DI)
- Use event channels for communication (decoupling without DI framework)
- Reserve proper DI for large, multi-team projects

**Sources:**
- [VContainer Documentation](https://vcontainer.hadashikick.jp/)
- [Zenject GitHub](https://github.com/modesttree/Zenject)
- [VContainer GitHub](https://github.com/hadashiA/VContainer)
- [DI with ECS for Scalable Game Development](https://gamedev.center/how-to-use-dependency-injection-with-ecs-for-scalable-game-development/)

---

## 10. Coroutines vs Async/Await

### Performance Benchmarks

| Approach | 10 Objects | 1000 Objects |
|----------|-----------|-------------|
| Standard method | ~65ms (12 FPS) | ~150ms (7 FPS) |
| Async Task | ~4ms (200 FPS) | ~50ms (30 FPS) |
| Coroutine | ~6ms (90 FPS) | - |
| C# Job System | ~6ms (90 FPS) | ~30ms (30 FPS) |
| Burst Compiled Job | ~3ms (150 FPS) | ~6ms (80-90 FPS) |

### Coroutine Example
```csharp
IEnumerator SpawnWaveWithDelay(float delay)
{
    Debug.Log("Wave starting...");
    yield return new WaitForSeconds(delay);
    SpawnEnemies();
    yield return new WaitUntil(() => enemiesAlive == 0);
    Debug.Log("Wave complete!");
}

// Start: StartCoroutine(SpawnWaveWithDelay(3f));
```

### Async/Await Example
```csharp
async Task LoadGameAsync()
{
    Debug.Log("Loading...");
    await Task.Delay(3000);
    Debug.Log("Done!");
}
```

### Cancellation Token (Critical for Async in Unity)
```csharp
public class AsyncExample : MonoBehaviour
{
    CancellationTokenSource cts;

    async void Start()
    {
        cts = new CancellationTokenSource();
        try
        {
            await Task.Delay(5000, cts.Token);
            Debug.Log("Completed");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Cancelled");
        }
    }

    void OnDestroy()
    {
        cts?.Cancel();
        cts?.Dispose();
    }
}
```

### Critical Difference: Lifetime
- **Coroutines** stop when the owning GameObject is destroyed
- **Async functions** continue running after object destruction (must manually cancel!)

### Recommendations for Action Games
| Use Case | Recommended |
|----------|-------------|
| Timed delays, cooldowns | Coroutine |
| Wave spawn sequences | Coroutine |
| Animation-synced events | Coroutine |
| Background file I/O | Async/Await |
| Heavy computation (AI pathfinding) | Job System + Burst |
| Network operations | Async/Await |
| Frame-dependent timing | Coroutine |

**UniTask** is recommended for GC-free async operations in Unity (struct-based, no allocation).

**Sources:**
- [Async in Unity (Game Dev Beginner)](https://gamedevbeginner.com/async-in-unity/)
- [Performance: Async vs Coroutines vs Job System (LogRocket)](https://blog.logrocket.com/performance-unity-async-await-tasks-coroutines-c-job-system-burst-compiler/)
- [Benchmarking Async/Await, Coroutine, UniTask (Medium)](https://prasetion.medium.com/benchmarking-async-await-coroutine-and-unitask-in-unity-which-one-is-best-1-59beec0fb53a)

---

## 11. Scene Management

### Additive Scene Loading Architecture (Unity Open Project 1)

```
Scene Hierarchy:
├── Initialization Scene (loads PersistentManagers, then unloads itself)
├── PersistentManagers Scene (NEVER unloaded - audio, scene transitions)
├── Gameplay Scene (loaded during play - gameplay UI, managers)
└── Location Scenes (loaded/unloaded dynamically)
```

### Bootstrap Pattern
```csharp
public class SceneBootstrapper : MonoBehaviour
{
    [SerializeField] private string persistentManagersScene = "PersistentManagers";
    [SerializeField] private string firstScene = "MainMenu";

    async void Start()
    {
        // Load persistent managers additively
        await SceneManager.LoadSceneAsync(persistentManagersScene, LoadSceneMode.Additive);

        // Load first real scene
        await SceneManager.LoadSceneAsync(firstScene, LoadSceneMode.Additive);

        // Unload bootstrap scene
        await SceneManager.UnloadSceneAsync(gameObject.scene);
    }
}
```

### SceneData ScriptableObject
```csharp
[CreateAssetMenu(menuName = "Scene Data/Scene Data")]
public class SceneDataSO : ScriptableObject
{
    public string sceneName;
    public string description;
    // Unity scene reference
}
```

### Key Principles
- **Persistent managers** in their own scene (never unloaded)
- **Additive loading** for all game content (UI, levels, etc.)
- **EditorInitialization** prefab for testing individual scenes
- Use **Addressable Assets** for efficient asset management

**Sources:**
- [Unity Open Project 1 Game Architecture](https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview)
- [Scene Management Complete Guide (Outscal)](https://outscal.com/blog/unity-scene-management-complete-guide)
- [Managing Multiple Scenes (Outscal)](https://outscal.com/blog/unity-scene-management-guide)

---

## 12. Game State Machine (Pause/Restart)

### Game Flow State Machine
```csharp
public enum GameState
{
    Boot,
    MainMenu,
    Loading,
    Playing,
    Paused,
    GameOver,
    Victory
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        ExitState(CurrentState);
        CurrentState = newState;
        EnterState(newState);
        OnGameStateChanged?.Invoke(newState);
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                // Show game over UI
                break;
        }
    }

    private void ExitState(GameState state)
    {
        // Cleanup for previous state
    }
}
```

### Pause as Sub-State
Recommended approach: "In Progress" as a superstate containing "Running" and "Paused" sub-states:
- Pause trigger: Running -> Paused
- Resume trigger: Paused -> Running
- This keeps pause/resume logic contained within gameplay

**Sources:**
- [State Machines in Unity (Game Dev Beginner)](https://gamedevbeginner.com/state-machines-in-unity-how-and-when-to-use-them/)
- [QuizU: State Pattern for Game Flow (Unity Discussions)](https://discussions.unity.com/t/quizu-state-pattern-for-game-flow-post-3/309255)
- [GameManager with FSM in Unity 6 (Unity Discussions)](https://discussions.unity.com/t/a-right-way-to-do-a-gamemanager-with-fsm-in-unity-6/1581169)

---

## 13. Save System

### Three Main Approaches

**1. PlayerPrefs** (Simple key-value, settings only):
```csharp
PlayerPrefs.SetFloat("MasterVolume", 0.8f);
float volume = PlayerPrefs.GetFloat("MasterVolume", 1f);
```

**2. JSON Serialization** (Flexible, human-readable):
```csharp
[System.Serializable]
public class SaveData
{
    public int currentWave;
    public float playerHealth;
    public int totalKills;
    public float bestTime;
}

public static class SaveSystem
{
    private static string SavePath => Path.Combine(
        Application.persistentDataPath, "savegame.json");

    public static void Save(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
    }

    public static SaveData Load()
    {
        if (!File.Exists(SavePath)) return new SaveData();
        string json = File.ReadAllText(SavePath);
        return JsonUtility.FromJson<SaveData>(json);
    }
}
```

**3. Binary Serialization** (Compact, harder to tamper):
```csharp
public static void SaveBinary(SaveData data)
{
    BinaryFormatter formatter = new BinaryFormatter();
    using (FileStream stream = new FileStream(SavePath, FileMode.Create))
    {
        formatter.Serialize(stream, data);
    }
}
```

### Best Practices
- Always use `Application.persistentDataPath` for cross-platform compatibility
- Create a centralized SaveManager class
- Mark data classes with `[System.Serializable]`
- For arena combat games: save high scores, unlocks, settings (not mid-wave state)

**Sources:**
- [Unity Binary Serialization Save System (Outscal)](https://outscal.com/blog/unity-binary-serialization-save-system)
- [Persistent Data in Unity (Outscal)](https://outscal.com/blog/persistent-data-saving-unity)
- [Save & Load with JSON (UhiyamaLab)](https://uhiyama-lab.com/en/notes/unity/unity-save-load-json-serialization-guide/)

---

## 14. Audio Manager Architecture

### ScriptableObject-Based Audio System
```csharp
[CreateAssetMenu(menuName = "Audio/Sound Effect")]
public class SoundEffectSO : ScriptableObject
{
    public AudioClip[] clips;
    [Range(0f, 1f)] public float volume = 1f;
    [Range(0.1f, 3f)] public float pitch = 1f;
    [Range(0f, 0.3f)] public float pitchVariation = 0.1f;
    public bool loop = false;
}
```

### Audio Source Pooling
```csharp
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private int poolSize = 10;
    private Queue<AudioSource> audioPool = new Queue<AudioSource>();

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.SetParent(transform);
            var source = go.AddComponent<AudioSource>();
            audioPool.Enqueue(source);
        }
    }

    public void PlaySFX(SoundEffectSO sfx, Vector3 position = default)
    {
        if (audioPool.Count == 0) return;

        var source = audioPool.Dequeue();
        source.transform.position = position;
        source.clip = sfx.clips[Random.Range(0, sfx.clips.Length)];
        source.volume = sfx.volume;
        source.pitch = sfx.pitch + Random.Range(-sfx.pitchVariation, sfx.pitchVariation);
        source.loop = sfx.loop;
        source.Play();

        if (!sfx.loop)
            StartCoroutine(ReturnToPool(source, source.clip.length));
    }

    IEnumerator ReturnToPool(AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);
        source.Stop();
        audioPool.Enqueue(source);
    }
}
```

### Key Design Points
- Use AudioMixer groups for volume categories (Master, SFX, Music, UI)
- Slight pitch/volume modulation makes repeated sounds feel natural
- Pool AudioSources to avoid create/destroy overhead
- Use ScriptableObjects for sound definitions (designer-friendly)

**Sources:**
- [Unity Audio Manager with ScriptableObjects GitHub](https://github.com/Rellac-Rellac/unity-audio-manager)
- [Audio Pooling Unity GitHub](https://github.com/GCodergr/Audio-Pooling-Unity)
- [Unity3D Sound Manager GitHub](https://github.com/baratgabor/Unity3D-SoundManager)
- [Unity Audio Pooling (adammyhre) GitHub](https://github.com/adammyhre/Unity-Audio-Pooling)

---

## 15. Camera System (Third Person)

### Cinemachine Third Person Follow Setup

**Rig Architecture:** 4 pivot points:
1. **Origin (A):** Target position, rotates horizontally
2. **Shoulder (B):** Offset for over-the-shoulder view
3. **Hand (C):** Vertical offset, rotates vertically around shoulder
4. **Camera (D):** Behind hand, always parallel to target rotation

**Key Properties:**

| Property | Typical Value | Purpose |
|----------|---------------|---------|
| Shoulder Offset | (0.7, 0.3, -0.5) | Over-the-shoulder positioning |
| Vertical Arm Length | 0.5 | Screen position during vertical rotation |
| Camera Side | 1 (right) | Which shoulder |
| Camera Distance | 2-3 | Distance from character |
| Camera Radius | 0.2 | Minimum obstacle distance |
| Damping | (0.1, 0.5, 0.3) | Camera smoothing per axis |

### Lock-On Camera for Combat
For arena combat, use **State-Driven Camera** that changes based on:
- Normal exploration: Free-look follow camera
- Lock-on target: Orbit camera centered between player and enemy
- Boss fights: Wider FOV, higher angle

### Integration Notes for Invector
Invector has its own camera system. Cinemachine can work alongside it:
- Use Cinemachine for cinematic moments and special cameras
- Use Invector's camera for gameplay if already configured
- Or replace Invector camera entirely with Cinemachine for more control

**Sources:**
- [Cinemachine Third Person Follow (3.1.5 Docs)](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html)
- [Cinemachine 3rd Person Follow (2.8.9 Docs)](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.8/manual/Cinemachine3rdPersonFollow.html)
- [3rd Person Camera Angles with Cinemachine (Medium)](https://medium.com/@austinjy13/standard-3rd-person-camera-angles-unity-cinemachine-61a7e89ee160)

---

## 16. New Input System

### Action Maps Architecture
Action Maps define contextual input sets. Only one active at a time.

**Typical Action Maps for Arena Combat:**
- **Gameplay** - Movement, attack, dodge, element switch, ultimate
- **UI** - Menu navigation, confirm, cancel
- **Cutscene** - Skip only

### Setup Steps
1. Install Input System package from Unity Registry
2. Create Input Action Asset (`.inputactions` file)
3. Define Action Maps with Actions and Bindings
4. Add `PlayerInput` component to player GameObject
5. Set default Action Map

### Switching Action Maps
```csharp
public static class InputManager
{
    private static InputActionAsset inputActions;

    public static void Initialize(InputActionAsset asset)
    {
        inputActions = asset;
    }

    public static void ToggleActionMap(InputActionMap actionMap)
    {
        if (actionMap.enabled) return;
        inputActions.Disable(); // Disable all maps
        actionMap.Enable();     // Enable requested map
    }
}
```

### Integration with Invector
Invector uses the legacy Input Manager by default. Options:
- Keep Invector's input system (simplest, works out of the box)
- Bridge new Input System to Invector's expected inputs
- For custom systems (Element switching, Ultimate), use new Input System independently

**Sources:**
- [Unity New Input System Complete Guide (Game Dev Beginner)](https://gamedevbeginner.com/input-in-unity-made-easy-complete-guide-to-the-new-system/)
- [Changing Action Maps (One Wheel Studio)](https://onewheelstudio.com/blog/2021/6/27/changing-action-maps-with-unitys-new-input-system)
- [Unity New Input System (Zero to Mastery)](https://zerotomastery.io/blog/unity-new-input-system/)

---

## 17. Physics Layers & Collision Matrix

### Recommended Layers for Arena Combat

| Layer # | Name | Collides With |
|---------|------|---------------|
| 0 | Default | Everything |
| 8 | Player | Enemy, EnemyProjectile, Environment, Pickup |
| 9 | Enemy | Player, PlayerProjectile, Environment |
| 10 | PlayerProjectile | Enemy, Environment |
| 11 | EnemyProjectile | Player, Environment |
| 12 | Environment | Everything except Pickup |
| 13 | Pickup | Player only |
| 14 | DeadEnemy | Environment only (ragdoll) |
| 15 | Trigger | Nothing (trigger zones only) |

### Setup
1. Edit > Project Settings > Tags and Layers > Define custom layers
2. Edit > Project Settings > Physics > Collision Matrix > Uncheck unnecessary pairs

### Performance Benefits
- Reduces broad-phase collision calculations per frame
- Projectiles skip other projectiles
- Dead enemies stop colliding with live entities
- Player weapon doesn't damage player

### Best Practices
- Enable only necessary interactions
- Use clear naming conventions
- Disable unused layer pairs
- Revisit periodically as project grows
- Test thoroughly after changes

**Sources:**
- [Unity Manual: Layer Collision Matrix](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization-cpu-collision-layers.html)
- [Unity Layer Collision Matrix Guide (Medium)](https://medium.com/@Brian_David/how-to-use-unitys-layer-collision-matrix-to-prevent-collisions-and-optimize-game-mechanics-4014e2aba19e)
- [Understanding Collision Layers (Medium)](https://medium.com/@tmaurodot/understanding-and-working-with-collision-layers-in-unity-1802aaec5ab4)

---

## 18. NavMesh & Runtime Obstacles

### Static NavMesh Baking
Bake at edit time for the arena floor. Mark floor and walls as Navigation Static.

### Runtime NavMesh Obstacles
```
NavMeshObstacle component:
├── Shape: Box or Capsule
├── Carve: true (cuts holes in NavMesh at runtime)
├── Carve Only Stationary: true (prevents constant rebaking)
└── Move Threshold: 0.1 (minimum distance to trigger re-carve)
```

### Key Behaviors
- **Carving enabled + stationary**: Cuts a hole in NavMesh, agents pathfind around it
- **Carving enabled + moving**: Acts as obstacle (avoidance), no NavMesh modification
- **Carving disabled**: Only collision avoidance, agents may try to path through

### Arena Combat Considerations
- Bake NavMesh for the 25x25m arena at edit time
- Use NavMeshObstacle with carving for dynamic arena hazards (fire zones, barriers)
- NavMeshAgent for enemy pathfinding to player
- Set agent avoidance priority: Boss (highest), Heavy (high), Fast (medium), Basic (low)
- Use `NavMeshAgent.SetDestination()` for chase behavior
- Use obstacle avoidance radius appropriately per enemy size

### Runtime Baking (if needed)
```csharp
// For procedurally generated content
NavMeshSurface surface = GetComponent<NavMeshSurface>();
surface.BuildNavMesh(); // Rebuilds entire NavMesh
```

**Sources:**
- [Unity Learn: NavMesh Baking](https://learn.unity.com/tutorial/navmesh-baking)
- [Dynamic NavMesh Obstacles (Medium)](https://medium.com/@fulton_shaun/unity-navmesh-part-2-dynamic-obstacles-recalculating-paths-17a2d3ce0f9b)
- [Using NavMesh Obstacles in Unity (Medium)](https://medium.com/geekculture/using-navmesh-obstacles-in-unity-da50dd76f385)

---

## 19. Performance Profiling & Optimization

### Frame Budget Targets

| Target FPS | Budget per Frame | Mobile (with 35% headroom) |
|-----------|-----------------|---------------------------|
| 30 fps | 33.33 ms | 21.66 ms |
| 60 fps | 16.66 ms | 10.83 ms |

### Profiling Workflow
1. **Profile before** changes (establish baseline)
2. **Profile during** development (track regressions)
3. **Profile after** optimization (validate improvements)

### Common Bottleneck Categories

**CPU-Bound Issues:**
- Physics calculations
- MonoBehaviour Update() calls
- Garbage collection spikes
- Camera culling
- Draw call batching
- UI rebuilds
- Animation processing

**GPU-Bound Issues:**
- Full-screen post-processing (AO, Bloom)
- Complex fragment shaders
- Overdraw (transparent objects, particles)
- High resolution without LOD
- Uncompressed textures

### Action Game Specific Optimizations

**Object Pooling (Critical for wave spawning):**
```csharp
public class ObjectPool<T> where T : MonoBehaviour
{
    private Queue<T> pool = new Queue<T>();
    private T prefab;
    private Transform parent;

    public ObjectPool(T prefab, int initialSize, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
        {
            T obj = Object.Instantiate(prefab, parent);
            obj.gameObject.SetActive(false);
            pool.Enqueue(obj);
        }
    }

    public T Get()
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
```

**Batching Solutions:**
- SRP Batching: Persistent GPU memory (best for URP)
- GPU Instancing: Multiple identical meshes in one draw call
- Static Batching: Non-moving meshes sharing materials

### Profiling Tools
- **Unity Profiler**: CPU timeline and hierarchy
- **Memory Profiler**: Memory budget compliance
- **Frame Debugger**: Draw call analysis
- **Profile Analyzer**: Performance over many frames

**Sources:**
- [Unity: Best Practices for Profiling](https://unity.com/how-to/best-practices-for-profiling-game-performance)
- [Ultimate Guide to Profiling Unity Games](https://unity.com/resources/ultimate-guide-to-profiling-unity-games)
- [Unity Profiler: Identifying Performance Issues (Wayline)](https://www.wayline.io/blog/unity-profiler-identifying-and-fixing-performance-issues)
- [Unity Optimization Tips 2026](https://makaka.org/unity-tutorials/optimization)

---

## 20. Project Structure & Organization

### Recommended Folder Structure for The Scorpion

```
Assets/
├── _Project/                          # Custom game code (underscore keeps it at top)
│   ├── Scripts/
│   │   ├── Core/                      # GameManager, EventChannels, Singletons
│   │   ├── Player/                    # Element system, Ultimate, HUD hooks
│   │   ├── Enemy/                     # AI states, enemy types, spawning
│   │   ├── Boss/                      # Boss AI, phases, special attacks
│   │   ├── Combat/                    # Damage system, input buffer, combos
│   │   ├── Wave/                      # WaveManager, spawn configurations
│   │   ├── UI/                        # HUD, menus, health bars
│   │   ├── Audio/                     # Audio manager, sound definitions
│   │   ├── Camera/                    # Camera extensions, lock-on
│   │   └── Utility/                   # Object pool, extensions, helpers
│   ├── ScriptableObjects/
│   │   ├── Events/                    # Event channel assets
│   │   ├── Variables/                 # Shared variable assets
│   │   ├── WaveData/                  # Wave composition definitions
│   │   ├── EnemyData/                 # Enemy stat definitions
│   │   ├── AbilityData/              # Fire/Lightning ability configs
│   │   └── Audio/                     # Sound effect definitions
│   ├── Prefabs/
│   │   ├── Enemies/
│   │   ├── VFX/
│   │   ├── UI/
│   │   └── Projectiles/
│   ├── Scenes/
│   ├── Art/
│   ├── Audio/
│   ├── Animations/
│   └── Materials/
├── Invector-3rdPersonController/      # DO NOT MODIFY
├── Plugins/                           # Third-party assets
└── TextMesh Pro/
```

### Naming Conventions
- Scripts: PascalCase (`WaveManager.cs`, `EnemyAI.cs`)
- Assets: PascalCase (`FireAbility.asset`, `OnWaveComplete.asset`)
- Prefabs: PascalCase (`HollowMonk.prefab`)
- Folders: PascalCase (`ScriptableObjects/`)
- No spaces in file or folder names
- Use underscores for variants (`enemy_lvl1_dungeon.fbx`)

### Key Principles
- Separate Art, Scripts, Prefabs, Scenes, and Audio at top level
- Break work into Prefabs (avoid large scene files)
- Feature-based sub-organization within Scripts folder
- Keep third-party assets in their own folders
- Document conventions in a style guide

**Sources:**
- [Unity: Best Practices for Organizing Your Project](https://unity.com/how-to/organizing-your-project)
- [Unity 6 Folder Structure Guide (Anchorpoint)](https://www.anchorpoint.app/blog/unity-folder-structure)
- [Unity Project Structure GitHub](https://github.com/themorfeus/unity_project_structure)
- [7 Ways to Keep Unity Project Organized (Juego Studios)](https://www.juegostudio.com/blog/7-ways-to-keep-unity-project-organized-unity3d-best-practices)

---

## 21. Applicability to The Scorpion

### Recommended Architecture Stack

Based on this research, here is the recommended architecture for The Scorpion arena combat game:

| System | Pattern | Rationale |
|--------|---------|-----------|
| **GameManager** | Singleton + State Machine | Manages game flow (Menu/Playing/Paused/GameOver/Victory) |
| **Element System** | Strategy Pattern + ScriptableObjects | Fire/Lightning as swappable strategy assets |
| **Event Communication** | SO Event Channels | Decouples all systems (wave events, damage, kills, UI updates) |
| **Enemy AI** | Interface-based State Pattern | Extensible per enemy type (Hollow Monk, Shadow Acolyte, Stone Sentinel) |
| **Boss AI** | Hierarchical State Machine | 3-phase boss with sub-states per phase |
| **Wave Manager** | ScriptableObject Data + Coroutines | Wave configs as SO assets, coroutines for spawn timing |
| **Input Handling** | Invector (combat) + Command Pattern (custom) | Buffer custom inputs, let Invector handle core combat |
| **Object Pooling** | Generic Pool<T> | Pre-pool enemies, projectiles, VFX for all 10 waves |
| **HUD/UI** | Observer Pattern (SO Events) | UI listens to events, never directly references game systems |
| **Audio** | SO-based + Audio Pooling | Sound definitions as assets, pooled AudioSources |
| **Camera** | Invector Camera + Cinemachine (boss) | Use Invector default, Cinemachine for cinematic boss transitions |
| **Save/Settings** | PlayerPrefs (settings) + JSON (high scores) | Minimal save needs for arena game |

### Priority Order (for 1-week deadline)
1. GameManager state machine + Wave spawning
2. Element System (Strategy Pattern)
3. Enemy AI (3 types with FSM)
4. Object pooling for enemies/VFX
5. HUD via event channels
6. Boss AI (3 phases)
7. Audio system
8. Polish (save scores, settings)

### Key Integration Points with Invector
- Extend `vHealthController` for element damage types
- Hook into `vMeleeManager` events for adrenaline gain on hit/kill
- Use `vMeleeAI` as base for custom enemy AI states
- Let Invector handle core combat, layer custom systems on top via events

---

## Complete Source Reference List

1. [Unity Open Project 1 - Game Architecture](https://github.com/UnityTechnologies/open-project-1/wiki/Game-architecture-overview)
2. [Unity: Architect Code with ScriptableObjects](https://unity.com/how-to/architect-game-code-scriptable-objects)
3. [Unity: ScriptableObject Event Channels](https://unity.com/how-to/scriptableobjects-event-channels-game-code)
4. [Unity E-book: Modular Architecture with ScriptableObjects](https://unity.com/resources/create-modular-game-architecture-with-scriptable-objects-ebook)
5. [Singletons in Unity Done Right (Game Dev Beginner)](https://gamedevbeginner.com/singletons-in-unity-the-right-way/)
6. [Unity Learn: Observer Pattern](https://learn.unity.com/tutorial/create-modular-and-maintainable-code-with-the-observer-pattern)
7. [Unity Learn: Command Pattern (Unity 6)](https://learn.unity.com/course/design-patterns-unity-6/tutorial/use-the-command-pattern-for-flexible-and-extensible-game-systems)
8. [Unity Combat Input Buffer (Wayline)](https://www.wayline.io/blog/unity-combat-input-buffer)
9. [Strategy Pattern with ScriptableObjects (DEV Community)](https://dev.to/eriksk/implementing-the-strategy-design-pattern-using-scriptable-objects-in-unity-292i)
10. [State Machines in Unity (Game Dev Beginner)](https://gamedevbeginner.com/state-machines-in-unity-how-and-when-to-use-them/)
11. [UnityHFSM Hierarchical FSM Library](https://github.com/Inspiaaa/UnityHFSM)
12. [VContainer DI Framework](https://vcontainer.hadashikick.jp/)
13. [Zenject DI Framework](https://github.com/modesttree/Zenject)
14. [Async in Unity (Game Dev Beginner)](https://gamedevbeginner.com/async-in-unity/)
15. [Performance: Async vs Coroutines vs Jobs (LogRocket)](https://blog.logrocket.com/performance-unity-async-await-tasks-coroutines-c-job-system-burst-compiler/)
16. [Scene Management Complete Guide (Outscal)](https://outscal.com/blog/unity-scene-management-complete-guide)
17. [Unity: Best Practices for Profiling](https://unity.com/how-to/best-practices-for-profiling-game-performance)
18. [Unity: Organizing Your Project](https://unity.com/how-to/organizing-your-project)
19. [Cinemachine Third Person Follow Docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html)
20. [Unity New Input System Guide (Game Dev Beginner)](https://gamedevbeginner.com/input-in-unity-made-easy-complete-guide-to-the-new-system/)
21. [Unity Layer Collision Matrix Docs](https://docs.unity3d.com/6000.3/Documentation/Manual/physics-optimization-cpu-collision-layers.html)
22. [NavMesh Dynamic Obstacles (Medium)](https://medium.com/@fulton_shaun/unity-navmesh-part-2-dynamic-obstacles-recalculating-paths-17a2d3ce0f9b)
23. [Game Design Patterns Complete Guide](https://generalistprogrammer.com/tutorials/game-design-patterns-complete-guide)
24. [Organizing Architecture for Unity Games (DEV)](https://dev.to/devsdaddy/organizing-architecture-for-games-on-unity-laying-out-the-important-things-that-matter-4d4p)
25. [Unity Folder Structure Guide (Anchorpoint)](https://www.anchorpoint.app/blog/unity-folder-structure)
26. [ScriptableObject Architecture GitHub](https://github.com/DanielEverland/ScriptableObject-Architecture)
27. [Observer Pattern with C# Events (One Wheel Studio)](https://onewheelstudio.com/blog/2020/7/24/observer-pattern-c-events)
28. [Unity Pattern Combo GitHub](https://github.com/homemech/unity-pattern-combo)
