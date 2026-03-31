# Deep Research: Unity Enemy AI, State Machines & Boss Fight Systems

> Compiled 2026-03-22 | 20+ unique sources | Full implementations extracted

---

## TABLE OF CONTENTS

1. [Enemy AI State Machine - Complete Architecture](#1-enemy-ai-state-machine)
2. [Finite State Machine Patterns (ScriptableObject-Based)](#2-fsm-patterns-scriptableobject-based)
3. [Behavior Tree vs State Machine Comparison](#3-behavior-tree-vs-state-machine)
4. [NavMesh Agent Enemy Pathfinding](#4-navmesh-agent-pathfinding)
5. [Enemy Attack Patterns Implementation](#5-enemy-attack-patterns)
6. [Wave Spawner System](#6-wave-spawner-system)
7. [Object Pooling for Enemy Spawning](#7-object-pooling)
8. [Boss Fight Multiple Phases](#8-boss-fight-phases)
9. [Boss Attack Patterns with ScriptableObjects](#9-boss-attack-patterns-scriptableobjects)
10. [Enemy Flanking & Tactical Positioning](#10-flanking--tactical-positioning)
11. [Aggro System & Threat Management](#11-aggro--threat-management)
12. [Telegraph / Attack Warning System](#12-telegraph--attack-warnings)
13. [AI Combat Manager - Coordinated Attacks](#13-combat-manager--coordinated-attacks)
14. [Enemy Spawn Animation Effects](#14-spawn-animation-effects)
15. [Difficulty Scaling Wave System](#15-difficulty-scaling)
16. [Enemy Type Design Patterns](#16-enemy-type-design-patterns)
17. [Minion Summoning Boss Mechanic](#17-minion-summoning)
18. [Stagger / Stun / Poise System](#18-stagger--stun--poise)
19. [ScriptableObject Enemy Data Configuration](#19-scriptableobject-enemy-data)
20. [Arena Combat Enemy Positioning](#20-arena-combat-positioning)

---

## 1. ENEMY AI STATE MACHINE

### Source: [Faramira - FSM with C# Delegates](https://faramira.com/enemy-behaviour-with-finite-state-machine-using-csharp-delegates-in-unity/)

**Architecture**: Delegate-based FSM with 5 states: IDLE, CHASE, ATTACK, DAMAGE, DIE

#### Core Data Structure
```csharp
public float mMaxSpeed = 3.0f;
public float mWalkSpeed = 1.5f;
public float mViewingDistance = 10.0f;
public float mViewingAngle = 60.0f;
public float mAttackDistance = 2.0f;
public float mTurnRate = 500.0f;
public string[] mEnemyTags;
public float Gravity = -30.0f;
public Transform mEyeLookAt;
[HideInInspector] public float mDistanceToNearestEnemy;
[HideInInspector] public GameObject mNearestEnemy;
Animator mAnimator;
CharacterController mCharacterController;
int mDamageCount = 0;
private Vector3 mVelocity = new Vector3(0.0f, 0.0f, 0.0f);
public FSM mFsm;
public int mMaxNumDamages = 5;
```

#### Movement System
```csharp
public virtual void Move(float speed) {
    Vector3 forward = transform.TransformDirection(Vector3.forward).normalized;
    mAnimator.SetFloat("PosZ", speed / mMaxSpeed);
    mVelocity = forward * speed;
    mVelocity.y += Gravity * Time.deltaTime;
    mCharacterController.Move(mVelocity * Time.deltaTime);
    if (mCharacterController.isGrounded && mVelocity.y < 0)
        mVelocity.y = 0f;
}

public bool MoveTowards(Vector3 tpos, float speed) {
    float dist = Distance(gameObject, tpos);
    if (dist > 1.5f) {
        Vector3 mpos = transform.position;
        Vector3 desiredDirection = (tpos - mpos).normalized;
        Vector3 forward = Vector3.Scale(desiredDirection,
            new Vector3(1, 0, 1)).normalized;
        transform.rotation = Quaternion.RotateTowards(transform.rotation,
            Quaternion.LookRotation(forward), mTurnRate * Time.deltaTime);
        Move(speed);
        return true;
    }
    return false;
}
```

#### Enemy Detection with Viewing Angle
```csharp
public GameObject GetNearestEnemyInSight(out float distance,
    float viewableDistance, bool useViewingAngle = false) {
    distance = viewableDistance;
    GameObject nearest = null;
    for (int t = 0; t < mEnemyTags.Length; ++t) {
        GameObject[] gos = GameObject.FindGameObjectsWithTag(mEnemyTags[t]);
        for (int i = 0; i < gos.Length; ++i) {
            GameObject player = gos[i];
            Vector3 diff = player.transform.position - transform.position;
            float curDistance = diff.magnitude;
            if (curDistance < distance) {
                diff.y = 0.0f;
                if (useViewingAngle) {
                    float angleH = Vector3.Angle(diff, GetEyeForwardVector());
                    if (angleH <= mViewingAngle) {
                        distance = curDistance;
                        nearest = player;
                    }
                } else {
                    distance = curDistance;
                    nearest = player;
                }
            }
        }
    }
    return nearest;
}
```

#### State Initialization (Delegate Pattern)
```csharp
void Start() {
    mFsm = new FSM();
    mFsm.Add((int)StateTypes.IDLE, new NPCState(mFsm, StateTypes.IDLE, this));
    mFsm.Add((int)StateTypes.CHASE, new NPCState(mFsm, StateTypes.CHASE, this));
    mFsm.Add((int)StateTypes.ATTACK, new NPCState(mFsm, StateTypes.ATTACK, this));
    mFsm.Add((int)StateTypes.DAMAGE, new NPCState(mFsm, StateTypes.DAMAGE, this));
    mFsm.Add((int)StateTypes.DIE, new NPCState(mFsm, StateTypes.DIE, this));

    Init_IdleState();
    Init_AttackState();
    Init_DieState();
    Init_DamageState();
    Init_ChaseState();
    mFsm.SetCurrentState(mFsm.GetState((int)StateTypes.IDLE));
}

// IDLE: scans for threats
void Init_IdleState() {
    NPCState state = (NPCState)mFsm.GetState((int)StateTypes.IDLE);
    state.OnUpdateDelegate += delegate () {
        mNearestEnemy = GetNearestEnemyInSight(out mDistanceToNearestEnemy, mViewingDistance);
        if (mNearestEnemy) {
            if (mDistanceToNearestEnemy > mAttackDistance)
                SetState(StateTypes.CHASE);
            else
                SetState(StateTypes.ATTACK);
            return;
        }
        PlayAnimation(StateTypes.IDLE);
    };
}

// CHASE: pursues target
void Init_ChaseState() {
    NPCState state = (NPCState)mFsm.GetState((int)StateTypes.CHASE);
    state.OnUpdateDelegate += delegate () {
        mNearestEnemy = GetNearestEnemyInSight(out mDistanceToNearestEnemy, mViewingDistance);
        if (!mNearestEnemy) { SetState(StateTypes.IDLE); return; }
        if (mDistanceToNearestEnemy < mAttackDistance) { SetState(StateTypes.ATTACK); return; }
        MoveTowards(mNearestEnemy.transform.position, mWalkSpeed);
    };
}

// ATTACK: engages enemy
void Init_AttackState() {
    NPCState state = (NPCState)mFsm.GetState((int)StateTypes.ATTACK);
    state.OnUpdateDelegate += delegate () {
        mNearestEnemy = GetNearestEnemyInSight(out mDistanceToNearestEnemy, mViewingDistance);
        if (mNearestEnemy && IsAlive()) {
            if (mDistanceToNearestEnemy < mAttackDistance)
                PlayAnimation(StateTypes.ATTACK);
            else if (mDistanceToNearestEnemy < mViewingDistance)
                SetState(StateTypes.CHASE);
        } else {
            SetState(StateTypes.IDLE);
        }
    };
}

// DAMAGE: handles injury, tracks hit count
void Init_DamageState() {
    NPCState state = (NPCState)mFsm.GetState((int)StateTypes.DAMAGE);
    state.OnEnterDelegate += delegate () { mDamageCount++; };
    state.OnUpdateDelegate += delegate () {
        if (mDamageCount == mMaxNumDamages) { SetState(StateTypes.DIE); return; }
        PlayAnimation(StateTypes.DAMAGE);
        SetState(StateTypes.IDLE);
    };
}
```

**State Transition Diagram:**
```
IDLE <-> CHASE <-> ATTACK
  |        |         |
  +---> DAMAGE <-----+
  +----> DIE (terminal)
```

### Source: [Game Dev Beginner - Enemy AI](https://gamedevbeginner.com/enemy-ai-in-unity/)

#### Switch-Statement AI Pattern (Simplest)
```csharp
public class SwitchExample : MonoBehaviour
{
    public State currentState = State.Idle;
    void Update()
    {
        switch (currentState)
        {
            case State.Idle:    Debug.Log("Waiting..."); break;
            case State.Attack:  Debug.Log("Attacking!"); break;
            case State.Retreat: Debug.Log("Run Away!"); break;
        }
    }
}
[System.Serializable]
public enum State { Idle, Attack, Retreat }
```

#### Projectile Prediction System
```csharp
public GameObject projectile;
public Transform target;
public float marginOfError = 1.5f;
float projectileSpeed = 25;
Vector3 targetLastPosition;

Vector3 Trajectory() {
    Vector3 trajectory = (target.position - targetLastPosition) / Time.deltaTime;
    targetLastPosition = target.position;
    return trajectory;
}

float TimeToReach() {
    float distance = Vector3.Distance(transform.position, target.position);
    return distance / projectileSpeed;
}

void FireProjectile(Vector3 targetPosition) {
    GameObject newProjectile = Instantiate(projectile, transform.position, Quaternion.identity);
    newProjectile.transform.LookAt(targetPosition + Random.insideUnitSphere * marginOfError);
}
```

---

## 2. FSM PATTERNS (SCRIPTABLEOBJECT-BASED)

### Source: [Pav Creations - ScriptableObject FSM](https://pavcreations.com/finite-state-machine-for-ai-enemy-controller-in-2d/2/)

**Architecture**: Pluggable FSM using ScriptableObjects for States, Activities, Transitions, and Decisions.

#### Core Framework

```csharp
// Base State
public class BaseState : ScriptableObject
{
    public virtual void Enter(BaseStateMachine machine) { }
    public virtual void Execute(BaseStateMachine machine) { }
    public virtual void Exit(BaseStateMachine machine) { }
}

// State Machine MonoBehaviour
public class BaseStateMachine : MonoBehaviour
{
    [SerializeField] private BaseState _initialState;
    public BaseState CurrentState { get; set; }
    private void Awake() { CurrentState = _initialState; }
    private void Start() { CurrentState.Enter(this); }
    private void Update() { CurrentState.Execute(this); }
}

// Concrete State (holds activities + transitions)
[CreateAssetMenu(menuName = "AI/FSM/State")]
public sealed class State : BaseState
{
    public List<Activity> Activities = new List<Activity>();
    public List<Transition> Transitions = new List<Transition>();

    public override void Enter(BaseStateMachine machine) {
        foreach (var activity in Activities) activity.Enter(machine);
    }
    public override void Execute(BaseStateMachine machine) {
        foreach (var activity in Activities) activity.Execute(machine);
        foreach (var transition in Transitions) transition.Execute(machine);
    }
    public override void Exit(BaseStateMachine machine) {
        foreach (var activity in Activities) activity.Exit(machine);
    }
}
```

#### Activity System
```csharp
public abstract class Activity : ScriptableObject
{
    public abstract void Enter(BaseStateMachine stateMachine);
    public abstract void Execute(BaseStateMachine stateMachine);
    public abstract void Exit(BaseStateMachine stateMachine);
}

// Patrol Activity
[CreateAssetMenu(menuName = "AI/FSM/Activity/PatrolActivity")]
public class PatrolActivity : Activity
{
    public float speed = 1;
    public override void Enter(BaseStateMachine stateMachine) {
        var PatrolPoints = stateMachine.GetComponent<PatrolPoints>();
        stateMachine.GetComponent<Animator>().SetBool("isWalk", true);
    }
    public override void Execute(BaseStateMachine stateMachine) {
        var PatrolPoints = stateMachine.GetComponent<PatrolPoints>();
        var RigidBody = stateMachine.GetComponent<Rigidbody2D>();
        float x = PatrolPoints.GetTargetPointDirection().x;
        Vector2 position = RigidBody.position +
            new Vector2(x * speed * Time.fixedDeltaTime, RigidBody.position.y);
        RigidBody.MovePosition(position);
    }
    public override void Exit(BaseStateMachine stateMachine) {
        stateMachine.GetComponent<PatrolPoints>().SetNextTargetPoint();
    }
}

// Chase Activity
[CreateAssetMenu(menuName = "AI/FSM/Activity/ChaseActivity")]
public class ChaseActivity : Activity
{
    GameObject target;
    public string targetTag;
    public float speed = 1;
    public override void Enter(BaseStateMachine stateMachine) {
        target = GameObject.FindWithTag(targetTag);
    }
    public override void Execute(BaseStateMachine stateMachine) {
        var RigidBody = stateMachine.GetComponent<Rigidbody2D>();
        Vector2 dir = (target.transform.position - stateMachine.transform.position).normalized;
        RigidBody.velocity = new Vector2(dir.x * speed, RigidBody.velocity.y);
    }
    public override void Exit(BaseStateMachine stateMachine) { }
}
```

#### Transition & Decision System
```csharp
// Transition: links a decision to true/false states
[CreateAssetMenu(menuName = "AI/FSM/Transition")]
public sealed class Transition : ScriptableObject
{
    public Decision decision;
    public BaseState TrueState;
    public BaseState FalseState;

    public void Execute(BaseStateMachine stateMachine) {
        if (decision.Decide(stateMachine) && !(TrueState is RemainInState)) {
            stateMachine.CurrentState.Exit(stateMachine);
            stateMachine.CurrentState = TrueState;
            stateMachine.CurrentState.Enter(stateMachine);
        } else if (!(FalseState is RemainInState)) {
            stateMachine.CurrentState.Exit(stateMachine);
            stateMachine.CurrentState = FalseState;
            stateMachine.CurrentState.Enter(stateMachine);
        }
    }
}

// RemainInState sentinel
[CreateAssetMenu(menuName = "AI/FSM/Remain In State")]
public sealed class RemainInState : BaseState { }

// Abstract Decision
public abstract class Decision : ScriptableObject
{
    public abstract bool Decide(BaseStateMachine stateMachine);
}

// Line of Sight Decision
[CreateAssetMenu(menuName = "AI/FSM/Decisions/InLineOfSightDecision")]
public class InLineOfSightDecision : Decision
{
    public LayerMask layerMask;
    public float distanceThreshold = 3f;
    public override bool Decide(BaseStateMachine stateMachine) {
        Vector3 dir = (stateMachine.transform.position - prevPosition).normalized;
        RaycastHit2D hit = Physics2D.Raycast(stateMachine.transform.position,
            dir, distanceThreshold, layerMask);
        return hit.collider != null;
    }
}

// Distance Decision
[CreateAssetMenu(menuName = "AI/FSM/Decisions/DistanceDecision")]
public class DistanceDecision : Decision
{
    public string targetTag;
    public float distanceThreshold = 3f;
    public override bool Decide(BaseStateMachine stateMachine) {
        if (target == null) target = GameObject.FindWithTag(targetTag);
        return Vector3.Distance(stateMachine.transform.position,
            target.transform.position) >= distanceThreshold;
    }
}
```

### Source: [Outscal - Scriptable FSM](https://outscal.com/blog/scriptable-finite-state-machine-unity)

#### Cleaner 3D Implementation
```csharp
// Base State
public abstract class State : ScriptableObject
{
    public virtual void OnEnter(StateMachine machine) { }
    public abstract void OnUpdate(StateMachine machine);
    public virtual void OnFixedUpdate(StateMachine machine) { }
    public virtual void OnExit(StateMachine machine) { }
}

// State Machine
public class StateMachine : MonoBehaviour
{
    [SerializeField] private State _currentState;
    public NavMeshAgent Agent { get; private set; }
    void Awake() { Agent = GetComponent<NavMeshAgent>(); }
    void Start() { _currentState?.OnEnter(this); }
    void Update() { _currentState?.OnUpdate(this); }

    public void TransitionToState(State nextState) {
        _currentState?.OnExit(this);
        _currentState = nextState;
        _currentState?.OnEnter(this);
    }
}

// Patrol State
[CreateAssetMenu(menuName = "AI/States/Patrol")]
public class PatrolState : State
{
    public float detectionRadius = 10f;
    public State chaseState;
    private int _waypointIndex = 0;

    public override void OnUpdate(StateMachine machine) {
        if (machine.Agent.remainingDistance < machine.Agent.stoppingDistance) {
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            machine.Agent.SetDestination(waypoints[_waypointIndex].position);
        }
    }
    public override void OnFixedUpdate(StateMachine machine) {
        Collider[] colliders = Physics.OverlapSphere(machine.transform.position, detectionRadius);
        foreach (var collider in colliders) {
            if (collider.CompareTag("Player")) {
                machine.TransitionToState(chaseState);
                return;
            }
        }
    }
}

// Chase State
[CreateAssetMenu(menuName = "AI/States/Chase")]
public class ChaseState : State
{
    public float loseSightDistance = 15f;
    public State patrolState;
    private Transform _player;

    public override void OnEnter(StateMachine machine) {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }
    public override void OnUpdate(StateMachine machine) {
        machine.Agent.SetDestination(_player.position);
        if (Vector3.Distance(machine.transform.position, _player.position) > loseSightDistance)
            machine.TransitionToState(patrolState);
    }
}
```

### Source: [NTU 50.033 - Pluggable State Machine](https://natalieagus.github.io/50033/docs/teen/fsm/)

#### Advanced Pluggable FSM with Event Actions
```csharp
// State with setup/exit/event actions
[CreateAssetMenu(menuName = "PluggableSM/State")]
public class State : ScriptableObject
{
    public Action[] setupActions;   // Run once on enter
    public Action[] actions;        // Run every Update
    public EventAction[] eventTriggeredActions; // Run on specific events
    public Action[] exitActions;    // Run once on exit
    public Transition[] transitions;

    public void UpdateState(StateController controller) {
        DoActions(controller);
        CheckTransitions(controller);
    }
    protected void CheckTransitions(StateController controller) {
        controller.transitionStateChanged = false;
        for (int i = 0; i < transitions.Length; ++i) {
            if (controller.transitionStateChanged) break;
            bool decisionSucceded = transitions[i].decision.Decide(controller);
            if (decisionSucceded)
                controller.TransitionToState(transitions[i].trueState);
            else
                controller.TransitionToState(transitions[i].falseState);
        }
    }
}

// StateController with timer utility
public abstract class StateController : MonoBehaviour
{
    public State startState;
    public State previousState;
    public State currentState;
    public State remainState;
    public bool transitionStateChanged = false;
    [HideInInspector] public float stateTimeElapsed;

    public void TransitionToState(State nextState) {
        if (nextState == remainState) return;
        OnExitState();
        previousState = currentState;
        currentState = nextState;
        transitionStateChanged = true;
        OnSetupState();
    }

    public bool CheckIfCountDownElapsed(float duration) {
        stateTimeElapsed += Time.deltaTime;
        return stateTimeElapsed >= duration;
    }
}
```

---

## 3. BEHAVIOR TREE VS STATE MACHINE

### Source: [PeerDH - Comparison](https://peerdh.com/blogs/programming-insights/comparing-state-machines-and-behavior-trees-for-ai-decision-making-efficiency-in-unity)

#### State Machine Interface
```csharp
public class StateMachine
{
    private IState currentState;
    public void ChangeState(IState newState) {
        currentState?.Exit();
        currentState = newState;
        currentState?.Enter();
    }
    public void Update() { currentState?.Update(); }
}

public interface IState
{
    void Enter();
    void Update();
    void Exit();
}
```

#### Behavior Tree Core
```csharp
public abstract class Node
{
    public abstract bool Execute();
}

public class Selector : Node  // OR logic - tries until one succeeds
{
    private List<Node> children;
    public Selector(List<Node> children) { this.children = children; }
    public override bool Execute() {
        foreach (var child in children)
            if (child.Execute()) return true;
        return false;
    }
}

public class Sequence : Node  // AND logic - runs until one fails
{
    private List<Node> children;
    public Sequence(List<Node> children) { this.children = children; }
    public override bool Execute() {
        foreach (var child in children)
            if (!child.Execute()) return false;
        return true;
    }
}
```

### Source: [Coffee Brain Games](https://coffeebraingames.wordpress.com/2014/02/23/finite-state-machine-vs-behaviour-tree-a-true-story/) | [Queen of Squiggles](https://queenofsquiggles.github.io/guides/fsm-vs-bt/)

**Decision Matrix:**

| Criteria | State Machine | Behavior Tree |
|----------|:------------:|:-------------:|
| Simple AI (<=3 behaviors) | BEST | Overkill |
| Complex AI (5+ behaviors) | Spaghetti risk | BEST |
| Non-programmer friendly | Yes | No |
| Runtime behavior swapping | Difficult | Easy (hot-swap branches) |
| Memory footprint | Lower | Higher |
| Debugging | Easy state tracking | Visual flow |
| Modularity | Low (transitions multiply) | High (plug in leaf nodes) |
| Learning curve | Low | Steep |

**Key Insight**: "If your AI has 3 or fewer behaviors and they don't interrupt each other, use an FSM. Anything more complex? Behavior trees will save you pain later."

**For The Scorpion**: Use FSM for basic enemies (Hollow Monk, Shadow Acolyte, Stone Sentinel) since each has simple patrol/chase/attack loops. Consider a lightweight BT only for the Boss if the 3-phase complexity demands it.

---

## 4. NAVMESH AGENT PATHFINDING

### Source: [DevSourceHub - NavMesh Guide](https://devsourcehub.com/unity-navmesh-in-depth-guide-to-pathfinding-for-npcs/)

#### Advanced Patrol + Chase System
```csharp
using UnityEngine;
using UnityEngine.AI;

public class AdvancedNPCController : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float patrolWaitTime = 3f;
    public float chaseRange = 10f;
    public Transform player;

    private NavMeshAgent agent;
    private int currentPatrolIndex;
    private float waitTimer;
    private bool isWaiting;
    private bool isChasing;

    private void Start() {
        agent = GetComponent<NavMeshAgent>();
        if (patrolPoints.Length > 0) SetNextDestination();
    }

    private void Update() {
        if (isChasing) ChasePlayer();
        else if (isWaiting) Wait();
        else Patrol();

        isChasing = Vector3.Distance(transform.position, player.position) <= chaseRange;
    }

    private void Patrol() {
        if (agent.remainingDistance < 0.1f) {
            isWaiting = true;
            waitTimer = patrolWaitTime;
        }
    }

    private void Wait() {
        waitTimer -= Time.deltaTime;
        if (waitTimer <= 0) { isWaiting = false; SetNextDestination(); }
    }

    private void ChasePlayer() { agent.SetDestination(player.position); }

    private void SetNextDestination() {
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
}
```

#### Batched Agent Updates (Performance)
```csharp
public class NavMeshAgentManager : MonoBehaviour
{
    public List<NavMeshAgent> agents = new List<NavMeshAgent>();
    public int agentsPerFrame = 5;
    private int currentAgentIndex = 0;

    private void Update() {
        for (int i = 0; i < agentsPerFrame; i++) {
            if (currentAgentIndex >= agents.Count) { currentAgentIndex = 0; break; }
            UpdateAgent(agents[currentAgentIndex]);
            currentAgentIndex++;
        }
    }
    private void UpdateAgent(NavMeshAgent agent) { /* per-agent logic */ }
}
```

#### NPC Interception (Predict Target Position)
```csharp
public class NPCInterceptor : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform target;
    public float predictionTime = 2f;

    private void Update() {
        Vector3 targetVelocity = target.GetComponent<Rigidbody>().velocity;
        Vector3 predicted = target.position + targetVelocity * predictionTime;
        agent.SetDestination(predicted);
    }
}
```

#### Off-Mesh Link Jump
```csharp
private System.Collections.IEnumerator JumpCoroutine() {
    isJumping = true;
    OffMeshLinkData data = agent.currentOffMeshLinkData;
    Vector3 startPos = agent.transform.position;
    Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
    float duration = Vector3.Distance(startPos, endPos) / jumpSpeed;
    float time = 0f;
    while (time < duration) {
        float t = time / duration;
        agent.transform.position = Vector3.Lerp(startPos, endPos, t) +
            Vector3.up * jumpCurve.Evaluate(t);
        time += Time.deltaTime;
        yield return null;
    }
    agent.CompleteOffMeshLink();
    isJumping = false;
}
```

**Performance Tips:**
- Stagger agent destination updates across frames (don't update all 30 enemies every frame)
- Use NavMeshObstacle.carving sparingly (causes expensive rebakes)
- Simplify pathfinding for distant NPCs
- Use `NavMesh.SamplePosition()` to validate spawn points are on the mesh

---

## 5. ENEMY ATTACK PATTERNS

### Source: [Pav Creations - Melee Combat AI](https://pavcreations.com/melee-attacks-and-ai-combat-mechanic-in-2d-unity-games/2/)

#### Attack Activity (ScriptableObject)
```csharp
[CreateAssetMenu(menuName = "FSM/Activity/AI/AttackActivity")]
public class AttackActivity : Activity
{
    public override void Enter(BaseStateMachine stateMachine) {
        stateMachine.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
        stateMachine.GetComponent<Animator>().Play("Attack");
    }
    public override void Execute(BaseStateMachine stateMachine) { }
    public override void Exit(BaseStateMachine stateMachine) {
        stateMachine.GetComponent<Animator>().Play("Idle");
    }
}
```

#### Enemy Attack Execution with Hitbox
```csharp
public class EnemyState : MonoBehaviour
{
    public LayerMask enemyLayer;
    public int HP = 3;
    public bool isHit = false, isDead = false;
    public float attackRange = .35f;
    int executionNumber = 0;

    public void ExecuteAttack() {
        GetComponent<AudioSource>().PlayOneShot(swingClip);
        executionNumber++;
        if (executionNumber == 1) {
            GetComponent<Rigidbody2D>().velocity = Vector2.zero;
            Collider2D[] hitTargets = Physics2D.OverlapCircleAll(
                transform.GetChild(0).position, attackRange, enemyLayer);
            if (hitTargets.Length > 0 && hitTargets[0] != null)
                GameManager.instance.subtractHP(1);
        }
    }
    public void ResetAttack() { executionNumber = 0; }
}
```

**Key Pattern**: Melee attacks use animated colliders + OverlapCircle for hit detection. The `executionNumber` prevents multi-hit on single swing.

---

## 6. WAVE SPAWNER SYSTEM

### Source: [Wayline - Dynamic Enemy Spawns](https://www.wayline.io/blog/crafting-dynamic-enemy-spawns-in-unity)

#### Wave Data Structure
```csharp
[Serializable]
public class Wave
{
    public GameObject enemyPrefab;
    public int enemyCount;
    public float spawnInterval;
}
```

#### Complete Wave Management System
```csharp
public class EnemySpawner : MonoBehaviour
{
    public List<Wave> waves;
    public List<Transform> spawnZones;
    private int currentWave = 0;

    void Start() { StartCoroutine(StartWaves()); }

    IEnumerator StartWaves() {
        while (currentWave < waves.Count) {
            Wave wave = waves[currentWave];
            for (int i = 0; i < wave.enemyCount; i++) {
                SpawnEnemy(wave.enemyPrefab);
                yield return new WaitForSeconds(wave.spawnInterval);
            }
            currentWave++;
            // Wait until all enemies defeated
            while (GameObject.FindGameObjectsWithTag("Enemy").Length > 0)
                yield return null;
        }
        Debug.Log("All waves complete!");
    }

    void SpawnEnemy(GameObject enemyPrefab) {
        Transform selectedZone = spawnZones[Random.Range(0, spawnZones.Count)];
        Bounds zoneBounds = selectedZone.GetComponent<Collider>().bounds;
        Vector3 spawnPosition = new Vector3(
            Random.Range(zoneBounds.min.x, zoneBounds.max.x),
            selectedZone.position.y,
            Random.Range(zoneBounds.min.z, zoneBounds.max.z));
        Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
    }
}
```

#### Spawn State Enum Pattern (from GitHub Gist)
```csharp
public enum SpawnState { SPAWNING, WAITING, COUNTING }
// Use to track: is a wave actively spawning? waiting for clear? counting down to next wave?
```

**For The Scorpion**: Use 4 fixed spawn point Transforms (N/S/E/W) instead of random zones. Track enemies with a List<GameObject> instead of FindGameObjectsWithTag for performance.

---

## 7. OBJECT POOLING

### Source: [Yarsa Labs - ObjectPool with IObjectPool](https://blog.yarsalabs.com/object-pooling-in-unity-with-iobjectpool/)

#### Unity's Built-in ObjectPool<T>
```csharp
public class Spawner : MonoBehaviour
{
    public Object prefab;
    private IObjectPool<Object> objectPool;

    void Start() {
        objectPool = new ObjectPool<Object>(
            InstantiateObject,  // createFunc
            OnObject,           // actionOnGet
            OnReleased          // actionOnRelease
        );
    }

    private Object InstantiateObject() {
        Object obj = Instantiate(prefab);
        obj.SetPool(objectPool);
        return obj;
    }
    public void OnObject(Object obj) { obj.gameObject.SetActive(true); }
    public void OnReleased(Object obj) { obj.gameObject.SetActive(false); }
}

public class Object : MonoBehaviour
{
    private IObjectPool<Object> objectPool;
    public void SetPool(IObjectPool<Object> pool) { objectPool = pool; }
    void OnCollisionEnter(Collision col) { objectPool.Release(this); }
}
```

**Critical Warning**: Always reset enemy state (health, position, animation, physics) when retrieving from pool. Without Reset(), enemies spawn already damaged or in wrong state.

**Performance**: Object pooling improves performance by 80-90% when spawning >10-20 objects/second and eliminates GC spikes.

---

## 8. BOSS FIGHT PHASES

### Source: [Unibear Studio - State Machines and Boss Fights](https://www.unibearstudio.com/tutorial/state-machines-and-boss-fights)

#### Coroutine-Based Boss State Machine
```csharp
enum BossState {
    FirstAttack,
    SecondAttack,
    Move,
    COUNT  // Auto-counts states
}

BossState currentState = BossState.Move;

void Start() { GoToNextState(); }

void GoToNextState() {
    BossState nextState = (BossState)Random.Range(0, (int)BossState.COUNT);
    string nextStateString = nextState.ToString() + "State";
    string lastStateString = currentState.ToString() + "State";
    currentState = nextState;
    StopCoroutine(lastStateString);
    StartCoroutine(nextStateString);
}

IEnumerator MoveState() {
    float timeToMove = 3f;
    while (timeToMove > 0f) {
        timeToMove -= Time.deltaTime;
        transform.Translate(transform.right * Time.deltaTime);
        yield return null;
    }
    GoToNextState();
}

IEnumerator FirstAttackState() {
    yield return null;
    Debug.Log("First Attack");
    GoToNextState();
}
```

**Rule**: Every BossState enum value MUST have a matching IEnumerator named `{EnumValue}State`.

### Boss Phase Transition Pattern (Synthesized from multiple sources)

```csharp
// Recommended architecture for The Scorpion's boss
public class BossController : MonoBehaviour
{
    public enum BossPhase { Phase1, Phase2, Phase3, Dead }

    [Header("Phase Thresholds")]
    public float phase2Threshold = 0.6f;  // 60% HP
    public float phase3Threshold = 0.3f;  // 30% HP

    private BossPhase currentPhase = BossPhase.Phase1;
    private float maxHealth;
    private float currentHealth;

    void Update() {
        float healthPercent = currentHealth / maxHealth;

        BossPhase targetPhase;
        if (healthPercent <= 0f) targetPhase = BossPhase.Dead;
        else if (healthPercent <= phase3Threshold) targetPhase = BossPhase.Phase3;
        else if (healthPercent <= phase2Threshold) targetPhase = BossPhase.Phase2;
        else targetPhase = BossPhase.Phase1;

        if (targetPhase != currentPhase) {
            ExitPhase(currentPhase);
            currentPhase = targetPhase;
            EnterPhase(currentPhase);
        }

        ExecutePhase(currentPhase);
    }

    void EnterPhase(BossPhase phase) {
        switch (phase) {
            case BossPhase.Phase2:
                // Enable fire aura, change attack set
                break;
            case BossPhase.Phase3:
                // Enrage: speed boost, aggression increase
                break;
        }
    }
}
```

---

## 9. BOSS ATTACK PATTERNS (SCRIPTABLEOBJECTS)

### Source: [Bronson Zgeb - Command Pattern with ScriptableObjects](https://bronsonzgeb.com/index.php/2021/09/25/the-command-pattern-with-scriptable-objects/)

#### Command Pattern for Boss Attacks
```csharp
// Base Command
public abstract class Command : ScriptableObject
{
    public abstract void Execute(Monster owner, Monster target);
}

// Attack Command
[CreateAssetMenu]
public class AttackCommand : Command
{
    public string ConsoleMessage;
    public override void Execute(Monster owner, Monster target) {
        var attackDamage = Mathf.CeilToInt(owner.Attack *
            (target.IsWeakAgainst(owner.Type) ? 1.5f : 1f));
        target.Health -= attackDamage;
    }
}

// Charge Command (accumulates over turns)
[CreateAssetMenu]
public class ChargeCommand : Command
{
    public override void Execute(Monster owner, Monster target) {
        if (!owner.TryGetComponent(out ChargeData data))
            data = owner.AddComponent<ChargeData>();
        ++data.Level;
    }
}

// MonsterType holds available commands
[CreateAssetMenu]
public class MonsterType : ScriptableObject
{
    public int StartingHealth;
    public int BaseSpeed;
    public int BaseAttack;
    public MonsterType[] Weaknesses;
    public Command[] Commands;  // Drag-drop attack list in Inspector
}
```

**For The Scorpion Boss**: Create ScriptableObject commands for each boss attack (SwordCombo, SummonMinions, FireWaveAttack, EnragedSlam). Assign different Command[] arrays per phase.

---

## 10. FLANKING & TACTICAL POSITIONING

### Source: [Envato Tuts+ / Tricky Fast Studios](https://www.trickyfast.com/2017/10/09/building-an-attack-slot-system-in-unity/)

Tactical positioning uses the **Attack Slot System** (see Section 13) combined with offset calculations. The core idea:

1. Calculate positions around the player using angle offsets
2. Assign enemies to slots NOT directly in front of the player
3. Use NavMesh to validate flank positions are reachable

```csharp
// Flanking position calculation
Vector3 GetFlankPosition(Transform player, Transform enemy, float distance) {
    // Get player's forward direction
    Vector3 playerForward = player.forward;
    // Calculate perpendicular direction (left or right)
    Vector3 flankDir = Vector3.Cross(playerForward, Vector3.up).normalized;
    // Choose side based on enemy's current position
    float dot = Vector3.Dot(flankDir, (enemy.position - player.position).normalized);
    if (dot < 0) flankDir = -flankDir;
    return player.position + flankDir * distance;
}
```

---

## 11. AGGRO & THREAT MANAGEMENT

### Source: [Unity Discussions - Dynamic Aggro System](https://discussions.unity.com/t/dynamic-aggro-system-using-lists/845670) | [Medium - Enemy Aggro with Colliders](https://medium.com/geekculture/how-to-code-enemy-aggro-with-colliders-b1a089f00798)

**Architecture for Arena Combat Aggro:**

```csharp
// Simplified aggro system for single-player arena
public class AggroSystem : MonoBehaviour
{
    public float aggroRange = 15f;
    public float deaggroRange = 25f;
    private Transform player;
    private bool isAggro = false;

    void Update() {
        float dist = Vector3.Distance(transform.position, player.position);
        if (!isAggro && dist <= aggroRange) {
            isAggro = true;
            OnAggro();
        } else if (isAggro && dist > deaggroRange) {
            isAggro = false;
            OnDeaggro();
        }
    }

    // Chain aggro: alert nearby enemies
    void OnAggro() {
        Collider[] nearby = Physics.OverlapSphere(transform.position, aggroRange * 0.5f);
        foreach (var col in nearby) {
            var otherAggro = col.GetComponent<AggroSystem>();
            if (otherAggro != null && !otherAggro.isAggro)
                otherAggro.ForceAggro();
        }
    }
}
```

**For The Scorpion**: In a 25x25m arena, all enemies will be in aggro range constantly. Use a simpler approach: all enemies aggro on spawn, no deaggro needed. Focus combat coordination through the Combat Manager instead.

---

## 12. TELEGRAPH / ATTACK WARNINGS

### Source: [Gamedeveloper.com - Enemy Attacks and Telegraphing](https://www.gamedeveloper.com/design/enemy-attacks-and-telegraphing)

**Core Design Philosophy:**
> Players cannot engage with gameplay they don't understand. Every enemy attack must answer: "Can you avoid my damage?"

**Telegraph Implementation Methods:**

1. **Pre-Attack Animations**: Deliberate wind-up delay before strike. Signals "here I come"
2. **Visual Effects**: Particle accumulation, flash effects, ground indicators
3. **Audio Cues**: Charge-up sounds, verbal warnings, weapon-specific sounds
4. **Multi-Modal**: Best results combine animation + VFX + audio simultaneously

**Implementation Pattern:**
```csharp
public class EnemyAttack : MonoBehaviour
{
    [Header("Telegraph")]
    public float telegraphDuration = 0.8f;  // Time before attack lands
    public GameObject telegraphVFX;          // Warning particle/decal
    public AudioClip telegraphSound;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRadius = 2f;

    IEnumerator PerformAttack() {
        // TELEGRAPH PHASE
        var vfx = Instantiate(telegraphVFX, attackOrigin.position, Quaternion.identity);
        GetComponent<AudioSource>().PlayOneShot(telegraphSound);
        GetComponent<Animator>().SetTrigger("WindUp");

        yield return new WaitForSeconds(telegraphDuration);

        // ATTACK PHASE
        Destroy(vfx);
        GetComponent<Animator>().SetTrigger("Strike");

        // Hit detection
        Collider[] hits = Physics.OverlapSphere(attackOrigin.position, attackRadius);
        foreach (var hit in hits) {
            var health = hit.GetComponent<HealthController>();
            if (health != null) health.TakeDamage(attackDamage);
        }

        // RECOVERY PHASE
        yield return new WaitForSeconds(recoveryDuration);
    }
}
```

**Telegraph Timing Guidelines:**
- Fast enemies (Shadow Acolyte): 0.3-0.5s telegraph
- Basic enemies (Hollow Monk): 0.6-0.8s telegraph
- Heavy enemies (Stone Sentinel): 1.0-1.5s telegraph
- Boss attacks: 0.8-2.0s depending on power

---

## 13. COMBAT MANAGER / COORDINATED ATTACKS

### Source: [Tricky Fast - Attack Slot System](https://www.trickyfast.com/2017/10/09/building-an-attack-slot-system-in-unity/)

#### Complete Slot Manager
```csharp
public class SlotManager : MonoBehaviour
{
    private List<GameObject> slots;
    public int count = 6;
    public float distance = 2f;

    void Start() {
        slots = new List<GameObject>();
        for (int index = 0; index < count; ++index)
            slots.Add(null);
    }

    public Vector3 GetSlotPosition(int index) {
        float degreesPerIndex = 360f / count;
        var pos = transform.position;
        var offset = new Vector3(0f, 0f, distance);
        return pos + (Quaternion.Euler(new Vector3(0f, degreesPerIndex * index, 0f)) * offset);
    }

    public int Reserve(GameObject attacker) {
        var bestPosition = transform.position;
        var offset = (attacker.transform.position - bestPosition).normalized * distance;
        bestPosition += offset;
        int bestSlot = -1;
        float bestDist = 99999f;

        for (int index = 0; index < slots.Count; ++index) {
            if (slots[index] != null) continue;
            var dist = (GetSlotPosition(index) - bestPosition).sqrMagnitude;
            if (dist < bestDist) { bestSlot = index; bestDist = dist; }
        }

        if (bestSlot != -1) slots[bestSlot] = attacker;
        return bestSlot;
    }

    public void Release(int slot) { slots[slot] = null; }

    void OnDrawGizmosSelected() {
        for (int index = 0; index < count; ++index) {
            Gizmos.color = (slots == null || slots.Count <= index || slots[index] == null)
                ? Color.black : Color.red;
            Gizmos.DrawWireSphere(GetSlotPosition(index), 0.5f);
        }
    }
}
```

#### Enemy Using Slots
```csharp
public class EnemyController : MonoBehaviour
{
    GameObject target;
    float pathTime = 0f;
    int slot = -1;

    void Update() {
        pathTime += Time.deltaTime;
        if (pathTime > 0.5f) {
            pathTime = 0f;
            var slotManager = target.GetComponent<SlotManager>();
            if (slot == -1) slot = slotManager.Reserve(gameObject);
            if (slot == -1) return;  // No slot available, wait
            GetComponent<NavMeshAgent>().destination = slotManager.GetSlotPosition(slot);
        }
    }
}
```

### Source: [Envato Tuts+ - Battle Circle AI](https://code.tutsplus.com/battle-circle-ai-let-your-player-feel-like-theyre-fighting-lots-of-enemies--gamedev-13535t)

**Battle Circle Architecture:**
1. Enemies approach player until within **danger radius**
2. In danger zone, enemies maintain distance from each other
3. Enemies query player for **attack permission** (token system)
4. `simultaneousAttackers` variable limits concurrent attackers (1 = cinematic, 3+ = dangerous)
5. `attackRate` + `attackRateFluctuation` controls attack frequency

**Pressure Spectrum:**
- **Cinematic** (Batman/Assassin's Creed): `simultaneousAttackers = 1`, patient waiting
- **Dangerous** (Dark Souls): `simultaneousAttackers = 3+`, aggressive positioning

**For The Scorpion**: Use `simultaneousAttackers = 2` for regular waves, `simultaneousAttackers = 1` during boss fight (so boss + 1 minion max attack at once).

---

## 14. SPAWN ANIMATION EFFECTS

### Sources: Various Unity VFX tutorials

**Implementation Pattern:**
```csharp
public class EnemySpawnEffect : MonoBehaviour
{
    public GameObject spawnVFXPrefab;
    public float spawnDelay = 1.0f;  // Time VFX plays before enemy appears
    public AnimationCurve scaleIn;   // 0->1 over spawnDelay

    IEnumerator SpawnWithEffect(GameObject enemyPrefab, Vector3 position) {
        // 1. Play VFX at spawn point
        var vfx = Instantiate(spawnVFXPrefab, position, Quaternion.identity);

        // 2. Wait for VFX to play
        yield return new WaitForSeconds(spawnDelay);

        // 3. Spawn enemy (scaled from 0 to 1)
        var enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.transform.localScale = Vector3.zero;

        float t = 0;
        while (t < 1f) {
            t += Time.deltaTime / 0.3f;  // 0.3s scale-in
            enemy.transform.localScale = Vector3.one * scaleIn.Evaluate(t);
            yield return null;
        }

        // 4. Cleanup
        Destroy(vfx, 2f);
        enemy.GetComponent<EnemyAI>().enabled = true;
    }
}
```

**Dissolve Effect**: Use shader with `_DissolveAmount` property (0 = fully visible, 1 = dissolved). Animate from 1 to 0 on spawn, 0 to 1 on death.

---

## 15. DIFFICULTY SCALING

### Sources: [Multiple Medium articles on wave management](https://medium.com/@phiktional/implementing-a-wave-system-for-enemy-spawning-in-unity-ebf820e7a936)

**Scaling Strategies:**

```csharp
[System.Serializable]
public class WaveConfig
{
    public int waveNumber;
    public EnemySpawnEntry[] enemies;
    public float timeBetweenSpawns;
    public float healthMultiplier = 1f;
    public float damageMultiplier = 1f;
    public float speedMultiplier = 1f;
}

[System.Serializable]
public class EnemySpawnEntry
{
    public GameObject prefab;
    public int count;
    public int spawnPointIndex;  // N=0, S=1, E=2, W=3
}
```

**Progressive Difficulty Pattern for 10 Waves:**

| Wave | Hollow Monk | Shadow Acolyte | Stone Sentinel | HP Multi | Notes |
|------|:-----------:|:--------------:|:--------------:|:--------:|-------|
| 1 | 3 | 0 | 0 | 1.0x | Tutorial wave |
| 2 | 4 | 1 | 0 | 1.0x | Introduce fast type |
| 3 | 3 | 2 | 0 | 1.0x | Mixed composition |
| 4 | 2 | 2 | 1 | 1.1x | Introduce heavy type |
| 5 | 3 | 3 | 1 | 1.1x | Mid-game spike |
| 6 | 4 | 2 | 2 | 1.2x | Heavy emphasis |
| 7 | 2 | 4 | 1 | 1.2x | Speed emphasis |
| 8 | 3 | 3 | 2 | 1.3x | Full mix |
| 9 | 4 | 4 | 2 | 1.3x | Pre-boss gauntlet |
| 10 | 0 | 0 | 0 | 1.5x | BOSS + summons |

---

## 16. ENEMY TYPE DESIGN PATTERNS

### Source: [Multiple - Strategy Pattern + ScriptableObjects](https://medium.com/@manishkumarbeck/level-up-your-game-mastering-design-patterns-for-powerful-unity-projects-25ed53faafef)

**Strategy Pattern for Enemy Attacks:**
```csharp
public interface IAttackStrategy
{
    void Execute(Transform attacker, Transform target);
}

public class MeleeAttackStrategy : IAttackStrategy
{
    public float damage = 10f;
    public float range = 2f;
    public void Execute(Transform attacker, Transform target) {
        if (Vector3.Distance(attacker.position, target.position) <= range) {
            target.GetComponent<HealthController>().TakeDamage(damage);
        }
    }
}

public class RangedAttackStrategy : IAttackStrategy
{
    public GameObject projectilePrefab;
    public float speed = 15f;
    public void Execute(Transform attacker, Transform target) {
        var proj = Object.Instantiate(projectilePrefab, attacker.position, Quaternion.identity);
        proj.GetComponent<Rigidbody>().velocity =
            (target.position - attacker.position).normalized * speed;
    }
}
```

**For The Scorpion's 3 Enemy Types:**

| Type | AI Pattern | Attack Style | Movement | Special |
|------|-----------|-------------|----------|---------|
| Hollow Monk | Simple chase + attack | Melee swing (0.7s telegraph) | Walk speed | None |
| Shadow Acolyte | Flank + hit-and-run | Quick double-strike (0.3s telegraph) | 2x speed | Dodge back after attack |
| Stone Sentinel | Slow advance + guard | Heavy slam (1.2s telegraph) | 0.5x speed | Poise/armor, knockback attack |

---

## 17. MINION SUMMONING

### Sources: [Multiple - Boss fight mechanics](https://medium.com/@derekanderson-dev/adding-a-boss-fight-part-2-boss-spawn-position-unity-developer-28c915389454)

**Implementation Pattern:**
```csharp
public class BossSummonAbility : MonoBehaviour
{
    public GameObject[] minionPrefabs;
    public Transform[] summonPoints;
    public int maxMinions = 3;
    public float summonCooldown = 15f;

    private List<GameObject> activeMinions = new List<GameObject>();
    private float lastSummonTime;

    public void TrySummon() {
        // Clean dead minions
        activeMinions.RemoveAll(m => m == null);

        if (activeMinions.Count >= maxMinions) return;
        if (Time.time - lastSummonTime < summonCooldown) return;

        lastSummonTime = Time.time;
        StartCoroutine(SummonSequence());
    }

    IEnumerator SummonSequence() {
        // Boss plays summon animation
        GetComponent<Animator>().SetTrigger("Summon");
        yield return new WaitForSeconds(1.5f);

        // Spawn minions at summon points
        int toSummon = Mathf.Min(2, maxMinions - activeMinions.Count);
        for (int i = 0; i < toSummon; i++) {
            Transform point = summonPoints[Random.Range(0, summonPoints.Length)];
            GameObject minion = Instantiate(
                minionPrefabs[Random.Range(0, minionPrefabs.Length)],
                point.position, Quaternion.identity);
            activeMinions.Add(minion);
            // Spawn VFX
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Call when boss dies to kill all minions
    public void KillAllMinions() {
        foreach (var minion in activeMinions)
            if (minion != null) Destroy(minion);
        activeMinions.Clear();
    }
}
```

**Design Note**: Killing the boss should auto-kill all minions (instant win condition). Minions in Phase 1 should be weaker versions of normal enemies.

---

## 18. STAGGER / STUN / POISE

### Source: [Pav Creations - Stunned Activity](https://pavcreations.com/melee-attacks-and-ai-combat-mechanic-in-2d-unity-games/2/) | Dark Souls poise analysis

#### ScriptableObject Stun Activity
```csharp
[CreateAssetMenu(menuName = "FSM/Activity/AI/StunnedActivity")]
public class StunnedActivity : Activity
{
    public AudioClip hitClip;
    public float hitInvincibilityTime = 1;

    public override void Enter(BaseStateMachine stateMachine) {
        stateMachine.GetComponent<AudioSource>().PlayOneShot(hitClip);
        var enemyState = stateMachine.GetComponent<EnemyState>();
        stateMachine.GetComponent<Rigidbody2D>().AddForce(
            new Vector2(enemyState.hitDir.x * 2.5f, 0), ForceMode2D.Impulse);
    }
    public override void Execute(BaseStateMachine stateMachine) {
        var enemyState = stateMachine.GetComponent<EnemyState>();
        if (enemyState.isHit) {
            enemyState.hitTimer += Time.deltaTime;
            if (enemyState.hitTimer >= hitInvincibilityTime) {
                enemyState.HP--;
                enemyState.hitTimer = 0;
                enemyState.isHit = false;
                if (enemyState.HP == 0) enemyState.isDead = true;
            }
        }
    }
}
```

#### Poise System (Dark Souls-inspired)
```csharp
public class PoiseSystem : MonoBehaviour
{
    [Header("Poise")]
    public float maxPoise = 30f;
    public float poiseRecoveryDelay = 5f;
    public float poiseRecoveryRate = 10f;

    private float currentPoise;
    private float lastHitTime;
    private bool isStaggered;

    void Start() { currentPoise = maxPoise; }

    void Update() {
        // Recover poise after delay
        if (!isStaggered && Time.time - lastHitTime > poiseRecoveryDelay)
            currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryRate * Time.deltaTime);
    }

    public bool TakePoiseDamage(float poiseDamage) {
        lastHitTime = Time.time;
        currentPoise -= poiseDamage;

        if (currentPoise <= 0) {
            currentPoise = maxPoise;  // Reset on stagger
            StartCoroutine(Stagger());
            return true;  // Was staggered
        }
        return false;  // Absorbed the hit
    }

    IEnumerator Stagger() {
        isStaggered = true;
        GetComponent<Animator>().SetTrigger("Stagger");
        GetComponent<NavMeshAgent>().isStopped = true;

        yield return new WaitForSeconds(1.5f);  // Stagger duration

        GetComponent<NavMeshAgent>().isStopped = false;
        isStaggered = false;
    }
}
```

**For The Scorpion Enemy Types:**

| Enemy | Max Poise | Stagger Duration | Notes |
|-------|:---------:|:----------------:|-------|
| Hollow Monk | 15 | 1.0s | Staggers easily |
| Shadow Acolyte | 10 | 0.8s | Very vulnerable but dodges |
| Stone Sentinel | 50 | 1.5s | Tanky, hard to stagger |
| Boss Phase 1 | 80 | 1.2s | Staggerable |
| Boss Phase 2 | 120 | 0.8s | Harder to stagger |
| Boss Phase 3 | 999 | 0s | Unstaggerable (enraged) |

---

## 19. SCRIPTABLEOBJECT ENEMY DATA

### Source: [Odin Inspector Blog](https://odininspector.com/blog/scriptable-objects-tutorial) | [Terresquall - Vampire Survivors](https://blog.terresquall.com/2022/12/creating-a-rogue-like-vampire-survivors-part-4/)

#### Enemy Data ScriptableObject
```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "Enemy Data")]
public class EnemyData : ScriptableObject
{
    public new string name;
    public string description;
    public GameObject enemyModel;
    public int health = 20;
    public float speed = 2f;
    public float detectRange = 10f;
    public int damage = 1;
}
```

#### Enemy Controller Loading Data
```csharp
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyControl : MonoBehaviour
{
    private NavMeshAgent navAgent;
    public EnemyData data;

    private void Start() {
        navAgent = GetComponent<NavMeshAgent>();
        if (data != null) LoadEnemy(data);
    }

    private void LoadEnemy(EnemyData _data) {
        // Clear existing visuals
        foreach (Transform child in transform) Destroy(child.gameObject);

        // Spawn model
        GameObject visuals = Instantiate(data.enemyModel);
        visuals.transform.SetParent(transform);
        visuals.transform.localPosition = Vector3.zero;

        // Apply stats
        navAgent.speed = data.speed;
    }
}
```

#### Runtime Stats Handler (separate from SO data)
```csharp
public class EnemyStats : MonoBehaviour
{
    public EnemyScriptableObject enemyData;

    // Runtime copies (don't modify the SO!)
    float currentMoveSpeed;
    float currentHealth;
    float currentDamage;

    void Awake() {
        currentMoveSpeed = enemyData.MoveSpeed;
        currentHealth = enemyData.MaxHealth;
        currentDamage = enemyData.Damage;
    }

    public void TakeDamage(float dmg) {
        currentHealth -= dmg;
        if (currentHealth <= 0) Kill();
    }
    public void Kill() { Destroy(gameObject); }
}
```

**Critical Pattern**: Store BASE stats in ScriptableObject (shared, read-only). Copy to RUNTIME variables on Awake(). Never modify the ScriptableObject at runtime -- 100 goblins referencing 1 GoblinData SO = 1 memory copy, not 100.

---

## 20. ARENA COMBAT POSITIONING

### Sources: [Unity Discussions - Enemy Surround](https://discussions.unity.com/t/make-the-enemy-surround-the-player/748327) | [GameDev.net - Tactical Circle](https://www.gamedev.net/forums/topic/701048-tactical-circle-system/)

**The "Kung Fu Circle" Pattern:**

Enemies claim positions around the player in a circle. The SlotManager (Section 13) implements this. Key additions for arena combat:

```csharp
public class ArenaPositioning : MonoBehaviour
{
    public Transform player;
    public float innerRadius = 3f;   // Minimum distance from player
    public float outerRadius = 6f;   // Maximum hover distance
    public float separationForce = 2f;

    // Keep enemies from stacking on each other
    public Vector3 GetSeparationVector(Transform enemy, List<Transform> allEnemies) {
        Vector3 separation = Vector3.zero;
        int neighbors = 0;

        foreach (var other in allEnemies) {
            if (other == enemy) continue;
            float dist = Vector3.Distance(enemy.position, other.position);
            if (dist < 2f) {  // Too close
                separation += (enemy.position - other.position).normalized / dist;
                neighbors++;
            }
        }

        if (neighbors > 0) separation /= neighbors;
        return separation * separationForce;
    }

    // Validate position is on navmesh and within arena bounds
    public bool IsValidPosition(Vector3 pos) {
        NavMeshHit hit;
        return NavMesh.SamplePosition(pos, out hit, 1f, NavMesh.AllAreas)
            && Vector3.Distance(pos, arenaCenter) < arenaRadius;
    }
}
```

**For 25x25m Arena with 4 Spawn Points:**
- Use 8 attack slots around player (every 45 degrees)
- `simultaneousAttackers = 2` (max 2 enemies attacking at once)
- Remaining enemies circle at `outerRadius` with separation
- When slot opens, nearest waiting enemy claims it

---

## SYNTHESIS: RECOMMENDED ARCHITECTURE FOR THE SCORPION

Based on all 20 research topics, here is the recommended architecture:

### Layer 1: Data (ScriptableObjects)
- `EnemyData` SO: name, health, speed, damage, poise, attackRange, telegraphTime
- `WaveConfig` SO: enemySpawnEntries[], healthMultiplier, damageMultiplier
- `BossPhaseConfig` SO: healthThreshold, attackCommands[], summonConfig, speedMultiplier

### Layer 2: AI Framework
- **Enemies**: Simple FSM (IDLE/CHASE/ATTACK/STAGGER/DEAD) -- integrates with Invector's `vMeleeAI`
- **Boss**: Extended FSM with phase transitions based on HP thresholds
- **Combat Manager**: SlotManager on player, limits simultaneous attackers

### Layer 3: Systems
- **WaveManager**: Coroutine-based, uses WaveConfig SOs, tracks active enemies via List
- **Object Pool**: For frequent enemy types (optional, optimize later if needed)
- **Poise System**: Per-enemy, determines stagger vulnerability
- **Telegraph System**: Per-attack coroutine with VFX + audio + animation windUp

### Layer 4: Integration with Invector
- Extend `vMeleeAI` for custom enemy behaviors (don't replace)
- Use `vHealthController.onReceiveDamage` to trigger poise checks
- Hook into `vMeleeManager` for combo detection (adrenaline gain)
- Use Invector's lock-on system for combat targeting

---

## SOURCES

1. [Faramira - Enemy FSM with C# Delegates](https://faramira.com/enemy-behaviour-with-finite-state-machine-using-csharp-delegates-in-unity/)
2. [Game Dev Beginner - Enemy AI in Unity](https://gamedevbeginner.com/enemy-ai-in-unity/)
3. [Pav Creations - ScriptableObject FSM (Part 2)](https://pavcreations.com/finite-state-machine-for-ai-enemy-controller-in-2d/2/)
4. [Pav Creations - Melee Combat AI](https://pavcreations.com/melee-attacks-and-ai-combat-mechanic-in-2d-unity-games/2/)
5. [Outscal - Scriptable FSM](https://outscal.com/blog/scriptable-finite-state-machine-unity)
6. [NTU 50.033 - Pluggable State Machine](https://natalieagus.github.io/50033/docs/teen/fsm/)
7. [PeerDH - State Machines vs Behavior Trees](https://peerdh.com/blogs/programming-insights/comparing-state-machines-and-behavior-trees-for-ai-decision-making-efficiency-in-unity)
8. [Coffee Brain Games - FSM vs BT Real Story](https://coffeebraingames.wordpress.com/2014/02/23/finite-state-machine-vs-behaviour-tree-a-true-story/)
9. [Queen of Squiggles - FSM vs BT Guide](https://queenofsquiggles.github.io/guides/fsm-vs-bt/)
10. [DevSourceHub - NavMesh In-Depth Guide](https://devsourcehub.com/unity-navmesh-in-depth-guide-to-pathfinding-for-npcs/)
11. [Wayline - Dynamic Enemy Spawns](https://www.wayline.io/blog/crafting-dynamic-enemy-spawns-in-unity)
12. [Yarsa Labs - Object Pooling with IObjectPool](https://blog.yarsalabs.com/object-pooling-in-unity-with-iobjectpool/)
13. [Unibear Studio - State Machines and Boss Fights](https://www.unibearstudio.com/tutorial/state-machines-and-boss-fights)
14. [Bronson Zgeb - Command Pattern with ScriptableObjects](https://bronsonzgeb.com/index.php/2021/09/25/the-command-pattern-with-scriptable-objects/)
15. [Tricky Fast - Attack Slot System](https://www.trickyfast.com/2017/10/09/building-an-attack-slot-system-in-unity/)
16. [Envato Tuts+ - Battle Circle AI](https://code.tutsplus.com/battle-circle-ai-let-your-player-feel-like-theyre-fighting-lots-of-enemies--gamedev-13535t)
17. [Gamedeveloper.com - Enemy Attacks and Telegraphing](https://www.gamedeveloper.com/design/enemy-attacks-and-telegraphing)
18. [Odin Inspector - ScriptableObjects Tutorial](https://odininspector.com/blog/scriptable-objects-tutorial)
19. [Terresquall - Enemy Stats with ScriptableObjects](https://blog.terresquall.com/2022/12/creating-a-rogue-like-vampire-survivors-part-4/)
20. [Brackeys - Boss Battle GitHub](https://github.com/Brackeys/Boss-Battle)
21. [Mix and Jam - Batman Arkham Combat](https://github.com/mixandjam/Batman-Arkham-Combat)
22. [Unity Learn - NavMesh Agents](https://learn.unity.com/tutorial/working-with-navmesh-agents)
23. [AwesomeTuts - Enemy AI Systems](https://awesometuts.com/blog/unity-enemy-ai/)
