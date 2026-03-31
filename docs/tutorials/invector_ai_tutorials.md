# Invector AI Template -- Complete Tutorial Notes

Compiled from the official Invector Studio YouTube tutorial series (11 videos).
These notes cover every FSM setup step, AI behavior configuration, layer/tag requirement, and enemy behavior customization technique demonstrated in the series.

---

## Table of Contents

1. [Importing the AI Template](#1-importing-the-ai-template)
2. [Layers and Tags](#2-layers-and-tags)
3. [Setting Up Shooter AI](#3-setting-up-shooter-ai)
4. [Introduction to FSM Editor](#4-introduction-to-fsm-editor)
5. [Headtrack and LookAround FSM](#5-headtrack-and-lookaround-fsm)
6. [Noise Listener](#6-noise-listener)
7. [SendMessage and Message Receiver](#7-sendmessage-and-message-receiver)
8. [Customizing Behavior pt2 (Move To Position, Change Behavior)](#8-customizing-behavior-pt2)
9. [Generic AI pt1 -- Movement and Damage](#9-generic-ai-pt1)
10. [Generic AI pt2 -- Combat and Hitboxes](#10-generic-ai-pt2)
11. [Jump and AutoCrouch](#11-jump-and-autocrouch)

---

## 1. Importing the AI Template

**Video**: #1 Importing the AI Template (2:50)

### Key Points

- The AI Template is a **separate package** that depends on shared assets (3D models, animations, prefabs, scripts) from the Invector Third Person Controller templates.
- An **Asset Importer** script automatically checks whether you already have any Invector third-person controller template in your project.
- If no existing Invector template is found, it extracts a "Basic Melee/Shooter Essentials" package first, then imports the AI Template on top.
- After import, two main folders appear:
  - `Assets/Invector-3rdPersonController/` -- essentials only (3D models, scripts, etc., NOT the full player controller)
  - `Assets/Invector-3rdPersonController/AI Controller/` -- the AI template content
- If AI Controller content is missing after import, manually import from: `Asset Importer > Editor Resources > AI Template`.

### Setup Steps

1. Create a new Unity project (or use existing with Invector template).
2. Import the AI Template package.
3. The Asset Importer runs automatically and extracts essentials if needed.
4. Verify both `Invector-3rdPersonController` and `AI Controller` folders exist.

---

## 2. Layers and Tags

**Video**: #2 Layers & Tags (2:04)

### Critical Setup

When importing the AI Template into a **clean/new project**, layers will be missing from prefabs. The demo scenes will still run without errors, but layers won't display correctly on prefabs.

### Required Layer Configuration

Layers must be set to specific slot numbers. Reference the official documentation image:

| Layer Slot | Layer Name |
|------------|------------|
| 8 | Player |
| 9 | Enemy |
| 10 | CompanionAI |
| 11 | - |
| 12 | - |
| 13 | BodyPart |
| 14 | - |
| 15 | StopMove |
| (others) | (check Invector docs) |

### Required Tags

- `Player`
- `Enemy`
- Additional tags as needed by detection systems

### How to Set Up

1. Go to `Invector > AI Controller > Help` to access online documentation.
2. Search for "Layers and Tags" in the documentation.
3. Follow the image reference to assign layers to their correct slot numbers.
4. If you already have a full Invector Third Person Controller template imported as a complete project, layers and tags will already be set.

---

## 3. Setting Up Shooter AI

**Video**: #6 How to set up Shooter AI (7:50)

### Three AI Controller Types

The AI Template ships with three controller types:
1. **Civilian** -- basic NPC, no combat
2. **Melee Fighter** -- close-range combat
3. **Shooter** -- ranged combat with weapons

### Shooter AI Setup Steps

#### Step 1: Tags and Layers
- Set the AI GameObject tag to `Enemy`
- Set the AI GameObject layer to `Enemy`

#### Step 2: Detection System
- Go to the head bone of the AI character
- Create an empty child object called "Eyes" as the **Detection Point**
- Assign the Eyes object to the Detection Point Reference on the controller
- Turn on detection visuals for debugging
- Set detection layers to include: `Player`
- Set detection tags to include: `Player`

#### Step 3: Shooter Manager Configuration
- The Shooter Controller comes with a **Shooter Manager** and **Head Track** already attached
- In Shooter Manager, set the **Damage Layer** to include `Player` (default is only Enemy)

#### Step 4: Weapon Setup
- Use provided weapon prefabs (e.g., Shotgun, Assault Rifle) or create custom ones
- Each weapon prefab contains: renderer, muzzle, aiming reference, source point, light, particles
- To customize: replace the 3D renderer/model while keeping the functional components

#### Step 5: Weapon Handler (Right Hand)
1. Find the right hand bone in the hierarchy
2. Create an empty child called "Handler"
3. Parent the weapon prefab under the Handler
4. Reset the weapon's transform and rotation
5. **Alignment workflow** (while in Play mode with `Lock and Debug` enabled):
   - Select the Handler transform
   - Make small incremental position/rotation adjustments to align the weapon in hand
   - Copy the Handler's Transform component values
   - Exit Play mode and paste the component values

#### Step 6: Left Hand IK
- Left hand positioning is NOT per-handler but per-weapon
- Each weapon has a **Left Hand IK** reference
- In the Shooter Manager, configure:
  - **Rotational Offset** -- adjusts left hand rotation for all weapons
  - **Positional Offset** -- adjusts left hand position for all weapons
- Copy component values after adjusting in Play mode

### Controller Settings
- **Lock and Debug**: Makes the AI aim at you for weapon alignment testing
- Changes the moveset to "aiming" automatically

---

## 4. Introduction to FSM Editor

**Video**: #8 Introduction to the FSM Editor (13:59)

### What is the FSM?

The Finite State Machine (FSM) is the **brain of the AI controller**. It determines:
- What the AI does when it sees a target
- What it does when health is low
- All behavioral decisions and transitions

Invector chose FSM with ScriptableObjects over behavior trees and GOAP (Goal-Oriented Action Planning) because it uses a node editor inspired by Unity's Animator, making the learning curve faster for Unity developers.

### Opening the FSM Editor

- Select an AI controller, click the FSM button on the component
- OR: Menu > `Invector > AI Controller > Open FSM Behavior Window`

### FSM Editor Layout

| Panel | Purpose |
|-------|---------|
| **Left Panel** | FSM Components -- lists all available Actions and Decisions (similar to Animator parameters) |
| **Center Area** | Node graph -- states and transitions |
| **Inspector** | Details of the selected state |

### Creating a New Behavior

1. `File > New Behavior`
2. Name it (e.g., "Tutorial Behavior")
3. Optionally assign a custom icon
4. Click the locate button to find the behavior asset in your project

### Default States

Every new behavior automatically has:
- **Entry State** -- the first state that runs
- **Any State (End State)** -- can transition to any state at any moment

### FSM State Properties

Each state has these configurable properties:

| Property | Description |
|----------|-------------|
| **Name** | State identifier |
| **Description** | Tooltip shown on mouse-over in the editor |
| **Color** | Visual identification in the node graph |
| **Custom Speed** | Override the controller's movement speed (e.g., Idle, Walk, Run, Sprint) |
| **Reset Current Destination** | Clears the current nav path (useful when entering Flee from Patrol) |
| **Actions** | List of action ScriptableObjects that run in this state |
| **Transitions** | List of outgoing transitions with decisions |

### Actions

- Simple scripts with encapsulated methods that perform a specific action
- Each action has an **Execution Type**:
  - `On State Update` -- runs every frame while in this state
  - `On State Enter` -- runs once when entering the state
  - `On State Exit` -- runs once when leaving the state
  - `Everything` -- runs on enter, update, and exit
- Actions show a small icon if they are NOT being used anywhere

### Transitions

- Created by clicking the transition icon on a state and dragging to another state
- Each transition can have:
  - **Decisions** -- conditions that must be true/false to trigger the transition
  - **Transition Delay** -- time in seconds before the transition fires (even without a decision)
- Right-click a transition to remove or duplicate it

### Decisions (Scriptable Objects)

Available built-in decisions include:

| Decision | Purpose |
|----------|---------|
| **Can See Target** | Check if AI can see a target |
| **Check Health** | Check health thresholds |
| **Check State** | Check if AI is in a specific FSM state |
| **Check Damage** | Check if AI has received damage (filterable by damage type) |
| **Is Listening Noise** | Check if a noise was heard |
| **Is In Destination** | Check if the nav agent reached its destination |

### Example: Idle > Patrol > Flee Behavior

**Setup**:
1. Create "Idle" state -- no actions, just waits
2. Create "Patrol" state -- add `Patrol` action (On State Update), set speed to Walk
3. Create "Flee" state -- add `Flee` action (On State Update), set speed to Sprint, enable Reset Current Destination
4. Transition: Idle -> Patrol with 5-second delay (no decision needed)
5. Transition: Any State -> Flee with `Check Damage` decision (check for "any damage", if true -> flee)
6. Transition: Flee -> Idle with 10-second delay

**Assigning the Behavior**:
- Assign the behavior asset to the FSM Behavior Controller component
- Assign a Waypoint Area in the AI Controller's base settings for patrol to work

### FSM Debugger

- Open: `Invector > AI Controller > FSM Debug Window`
- Select an AI and check "Debug Mode"
- Shows:
  - Last State
  - Current State
  - Decision evaluations (true/false)
  - Debug messages (e.g., countdown timers)

---

## 5. Headtrack and LookAround FSM

**Video**: #9 Headtrack and LookAround FSM (11:32)

### Adding Head Track

- Select AI Controller > `Invector > AI Controller > Components > Head Track`
- Or add the `vAIHeadTrack` component manually

### Head Track Configuration

| Property | Description |
|----------|-------------|
| **Transform Hips** | Override if the character's bone hips don't face the same direction as the character's forward. Create an empty GameObject with correct forward orientation and assign here. |
| **Strafe Head/Body Strength** | Weight of head/body rotation in strafe locomotion |
| **Free Head/Body Strength** | Weight of head/body rotation in free locomotion |
| **Min/Max Angles** | Limits for looking up/down and sideways |
| **Keep Looking Out of Angle** | If checked, AI keeps looking at target even when outside angle limits. Useful for fleeing AI that should look back at damage source. |
| **Offset Look Height** | Vertical offset for the look target (1.5 = typical player height) |
| **Smooth** | Speed of head track interpolation |
| **Animator Tags** | Animation tags that disable head track (e.g., `CustomAction` tag prevents head track during special animations) |
| **Eyes Reference** | Assign the same detection point (Eyes) used for the detection system |

### FSM Integration: Look at Damage Sender

In the `Check Damage` decision properties:
- Enable **"Look to Damage Sender"** -- the AI will use head track to look at whoever damaged it
- Works with the head track's "Keep Looking Out of Angle" setting

### LookAround Feature

A unique AI head track feature that simulates idle looking behavior (left-right scanning).

**Configuration** (on the Head Track component, Detection tab):
- **Use Look Around** -- enable the feature
- **Look Around Angle** -- how far left/right to look
- **Look Around Curve** -- an AnimationCurve preset that controls the left-right pattern
- **Look Around Speed** -- speed of the scanning motion

**Using LookAround in FSM**:

Two methods:
1. **In Patrol Action**: The Patrol action has a built-in `Look Around on Stay` option -- AI looks around while waiting at a waypoint
2. **As a Standalone Action**: Add the `Look Around` action to any state (e.g., Idle) with execution type `On State Update`

**Detection synergy**: Since the detection point (Eyes) is parented to the head bone, when the head rotates via LookAround, the field of view cone moves too, creating realistic visual scanning behavior.

### Shooter AI Note

Shooter controllers come with Head Track pre-assigned because it's used for aiming (body weight controls spine bending for up/down aim). If the shooter AI has weird spine movement, adjust the Min/Max angles.

---

## 6. Noise Listener

**Video**: #10 Noise Listener (14:54)

### Components

Two components work together:

1. **vAINoiseListener** -- attached to the AI, lets it "hear" noises
2. **vNoiseObject** -- attached to any object that makes noise

### Adding the Noise Listener

- Menu: `Invector > AI Controller > Components > AI Noise Listener`
- Or add `vAINoiseListener` component directly
- **Listener Power**: Amplifies the effective distance of incoming noise

### Noise Object Setup

| Property | Description |
|----------|-------------|
| **Radius** | Visual radius of the noise |
| **Min Distance** | Inner range (full volume) |
| **Max Distance** | Outer range (volume falls off to zero) |
| **Noise Type** | Category: Bullets, Distraction, Rocks, etc. (filterable by listener) |
| **Duration** | How long the noise persists |
| **Trigger on Start** | Auto-trigger when enabled |
| **Looping** | Continuous noise (e.g., alarm systems) |

### Triggering Noise

The noise is triggered by calling the `TriggerNoise()` method on the vNoiseObject. This can be done via:
- Unity Events (e.g., OnTriggerEnter from a Simple Trigger)
- Weapon "On Shot" events
- Any script calling the public method

### Example: Falling Object Makes Noise

1. Create a sphere with a Collider, Rigidbody, and `vNoiseObject`
2. Add a `vSimpleTrigger` component (auto-creates a hitbox trigger collider)
3. Configure the Simple Trigger to detect the floor layer (Default) and tag (Untagged)
4. In the Simple Trigger's OnTriggerEnter event, call `vNoiseObject.TriggerNoise()`
5. Add an AudioSource (Play on Awake = false) and trigger it from the same event

### FSM Setup for Noise Response

#### Decision: Is Listening Noise

Create a decision ScriptableObject:
- Type: `Is Listening Noise`
- Filter by noise type (e.g., "Distraction") or "Any" noise

#### Two-Stage Investigation Pattern

Instead of immediately running to the noise, create a more realistic two-stage response:

**State 1: Look at Noise**
- Custom speed: Idle (character stands still)
- Action: `Go To Noise Position` with `Look to Noise Position` enabled
- Reset Current Destination: yes
- Transition: delay 4 seconds -> Investigate Noise

**State 2: Investigate Noise**
- Action: `Go To Noise Position` (character walks to noise location)
- Reset Current Destination: yes
- Transition: `Is In Destination` decision -> if true, delay 1 second -> Idle

**Any State Transition**:
- Decision: `Is Listening Noise` (heard any noise) -> if true -> Look at Noise

### Weapon Noise Integration

- Add a `vNoiseObject` to your weapon (or as a child of the weapon handler)
- In the weapon's **On Shot** event, call `TriggerNoise()`
- Different weapons can use different noise objects with varying max distances:
  - Sniper: large max distance
  - Silenced weapon: small max distance
- This makes AI detect gunfire and investigate

---

## 7. SendMessage and Message Receiver

**Video**: #11 SendMessage (19:42)

### Overview

The **Send Message** action + **Message Receiver** component is a powerful system that lets FSM states trigger Unity Events (animations, sounds, particles, method calls) without writing code.

### Components

1. **Send Message Action** (FSM Action ScriptableObject)
   - **Listener Name**: must match a listener on the Message Receiver
   - **Message**: optional string parameter passed to the event

2. **vMessageReceiver** (Component on the AI)
   - Menu: `Invector > AI Controller > Components > Message Receiver`
   - Contains a list of message listeners, each with:
     - **Listener Name**: identifier (must match the Send Message action's listener name)
     - **On Receive Message**: UnityEvent that fires when the message is received
     - Events can use the string message parameter via the `OnReceiveMessage(string)` variant

### Example: Triggering Custom Animations from FSM

**Goal**: When AI flees, it plays a "ducking/scared" animation.

#### Animator Setup
1. Duplicate the base locomotion animator controller
2. Create a sub-state machine called "Actions" > "Ducking"
3. Add three animation states: Start Ducking, Ducking (looping), End Ducking
4. End Ducking uses **negative speed (-1)** to play the start animation in reverse
5. All states tagged as `CustomAction` so the controller treats them as actions
6. Transitions: Entry -> Start Ducking -> Ducking (loop) -> End Ducking -> Exit

#### FSM Setup
1. Create "Flee" state with Flee action
2. Create "Scared" state:
   - Custom speed: Idle
   - Reset Current Destination: yes
   - Action: `Send Message` -- listener: "TriggerAnimation", message: "StartDucking"
3. Create "End Scared" state:
   - Custom speed: Idle
   - Reset Current Destination: yes
   - Action: `Send Message` -- listener: "TriggerAnimation", message: "EndDucking"
4. Transitions: Flee -> Scared (8 sec delay) -> End Scared (10 sec delay) -> Idle

#### Message Receiver Setup
1. Add `vMessageReceiver` to the AI
2. Create listener "TriggerAnimation"
3. In On Receive Message(string): drag the AI object, select `Animator.PlayInFixedTime(string)`
4. The string message ("StartDucking"/"EndDucking") is passed as the animation state name

### Triggering Sound Effects

**Method 1: Via Message Receiver**
1. Create an AudioSource child object on the AI (Play on Awake = false)
2. Add a message listener (e.g., "TriggerScreamSound")
3. In On Receive Message: drag the AudioSource object, call `AudioSource.Play()`
4. Add a Send Message action in the appropriate FSM state

**Method 2: Via Animator State (Trigger Sound by State)**
1. On the animation state in the Animator, add the `TriggerSoundByState` script
2. Configure: audio clip, audio source prefab, play randomly option
3. The sound plays automatically when the animation state is entered

### Preventing State Conflicts with Check State Decision

When using Any State transitions, prevent re-triggering flee while already scared:
1. Create a `Check State` decision (e.g., "IsInScaredState")
2. Configure it to check if current FSM state equals "Scared"
3. Add this decision to the Any State transition: only transition to Flee if NOT in Scared state (false condition)

---

## 8. Customizing Behavior pt2

**Video**: #12 Customizing Behavior pt2 (15:44)

### Move To Position Component

A component that makes the AI navigate to a specific world position.

**Adding**: `Invector > AI Controller > Components > Move To Position` (or type `vAIMoveToPosition`)

**Configuration**:
- A list of named positions, each with:
  - **Name**: identifier string (e.g., "SearchingForWeapon")
  - **Location**: reference to a Transform in the scene (empty GameObject)
  - The forward direction of the Transform determines the AI's facing direction on arrival

**Events**:
- **On Start Move**: fires when the AI begins moving. **Recommendation**: call `StopFSM()` on the FSM Behavior Controller to prevent FSM conflicts during movement.
- **On Finish Move**: fires when the AI arrives at the destination. Use to trigger animations, enable weapons, restart FSM, etc.

**Moving**:
- Call `MoveTo(string positionName)` to start navigation to a named position
- Speed can be set (walk/run/sprint)
- Can check "Move on Start" for testing

### Changing FSM Behavior at Runtime

**Action**: `Change Behavior`
- Assign a different FSM Behavior ScriptableObject
- The AI's entire behavior tree swaps at runtime
- Example: Civilian behavior -> pick up weapon -> swap to Shooter behavior

### Event With Delay Component

**Adding**: `vEventWithDelay` component

- A list of events, each with a configurable delay (in seconds)
- Public methods:
  - `DoEvents()` -- triggers ALL events in the list with their respective delays
  - `DoEvent(int index)` -- triggers a specific event by index

**Use case**: When picking up a weapon:
1. Delay 0.3s: Disable the world weapon object (`SetActive(false)`)
2. Delay 0.3s: Enable the AI's equipped weapon object (`SetActive(true)`)
- This creates a smooth visual transition during the pickup animation

### Complete Example: Civilian Picks Up Weapon and Becomes Shooter

**FSM Flow**: Flee -> Scared -> End Scared -> Search For Weapon -> (end behavior)

1. **Search For Weapon** state:
   - Action: `Send Message` to trigger `MoveTo("SearchingForWeapon")`
   - Transition to a final state that uses `Change Behavior` action to swap to Shooter FSM

2. **Move To Position** setup:
   - On Start Move: call `StopFSM()` on FSM controller
   - On Finish Move: play pickup animation (`Animator.PlayInFixedTime("PickMeUp")`)
   - On Finish Move: call `DoEvents()` on EventWithDelay (swap weapon objects)
   - On Finish Move: call `StartFSM()` on FSM controller

3. **Controller Changes**:
   - Change from Basic Controller to Shooter Controller
   - Add Head Track (required for Shooter)
   - Add Shooter Manager (set Damage Layer to Player)
   - Swap animator controller to one with shooter parameters

### NavMesh Requirement

When using Move To Position with obstacles, mark obstacle objects as **Static** and bake the NavMesh so the AI can pathfind correctly.

---

## 9. Generic AI pt1

**Video**: #13 Generic AI pt1 -- Movement and Damage (21:18)

### Humanoid vs Generic Characters

- **Humanoid**: two legs, two arms, torso, head. Unity's retargeting system allows sharing animations between any humanoid characters.
- **Generic**: custom bone structure with its own animations. Cannot use animations from other characters. Each animation must come with the model.

### Setting Up a Generic AI Character

#### Required Components

| Component | Configuration |
|-----------|--------------|
| **Animator** | Already present on imported models |
| **Capsule Collider** | Adjust height/radius to match character |
| **Rigidbody** | Add mass, **freeze rotation X and Z** to prevent falling over |
| **NavMesh Agent** | Match height with capsule collider |
| **FSM Behavior Controller** | Assign a behavior (e.g., "Civilian Simple") |
| **AI Controller** | Choose type: Basic, Combat, Melee, or Shooter |

#### Animator Controller Setup for Generic Characters

Since generic characters cannot use Invector's default humanoid animations, you must build a custom animator controller:

1. **Duplicate** an existing Invector AI animator (e.g., the melee one) as a starting base -- it has the needed parameters.
2. **Strip unnecessary states/layers**:
   - Remove jump/airborne states (if no jump animations)
   - Remove crouch states (if no crouch animations)
   - Remove extra blend trees for strafe (if character only moves forward)
   - Remove unnecessary layers (defense, equip, upper body hit, turn-on-spot)
3. **Replace blend trees with single clips**:
   - Idle: replace the blend tree with the character's idle animation clip
   - Walk/Run/Sprint: if only one walk animation exists, duplicate it and vary the **animation speed** (1.0x for walk, 1.25x for run, etc.)
4. **Set up hit reaction**:
   - Create a sub-state machine "Hit Reaction" under Actions
   - Add the get-hit animation clip, tagged `LockMovement`
   - Transition from Any State using the `TriggerReaction` parameter
   - Create an exit transition (so it doesn't loop)
5. **Set up death**:
   - Add dead animation in the same sub-state machine
   - Transition using the `isDead` bool parameter
   - **Uncheck "Can Transition to Self"** to prevent the death animation looping/freezing
   - No exit transition needed (character stays dead)
6. **Clean up parameters**: Remove unused parameters but keep:
   - `InputHorizontal`, `InputVertical`, `InputMagnitude` (movement)
   - `TurnOnSpot` (may be called by scripts)
   - `isGrounded`, `isStrafing` (used by scripts)
   - `TriggerReaction` (hit reactions)
   - `isDead` (death)
   - `WeakAttack`, `ResetState` (for combat, next tutorial)

#### Controller Settings for Generic Characters

| Setting | Value |
|---------|-------|
| **Root Motion** | **Uncheck** if the character doesn't use root motion animations |
| **Use Turn on Spot** | **Uncheck** if no turn-on-spot animations exist (otherwise character gets stuck trying to turn) |
| **Walk/Run/Sprint Speed** | Adjust in movement options or use Animator Speed settings |

#### Tags and Layers (Critical)

The AI character MUST have:
- **Tag**: `Enemy`
- **Layer**: `Enemy`

Without these, the damage system will not register hits on the AI.

---

## 10. Generic AI pt2

**Video**: #14 Generic AI pt2 -- Combat and Hitboxes (10:56)

### Controller Types Comparison

| Controller | Use Case |
|-----------|----------|
| **Basic (vControlAI)** | Civilians, non-combat NPCs |
| **Combat (vControlAICombat)** | Simple combat, no weapon swapping, limited animations |
| **Melee (vControlAIMelee)** | Full melee system with weapon-based movesets, weapon swapping |
| **Shooter (vControlAIShooter)** | Ranged combat with firearms |

**Combat vs Melee**: Combat is for simple characters (a few attack animations, no weapon changes). Melee supports weapon ID-based moveset switching (katana moveset vs sword moveset, etc.).

### Setting Up Combat AI Detection

1. Detection Point Reference is optional (can skip for simple setups)
2. Set **Detection Layer**: `Player`
3. Set **Detection Tag**: `Player`
4. Configure attack parameters:
   - **Min/Max Attack Time**: range for time between attacks
   - **Min/Max Attack Count**: number of attacks per sequence (match to available animations)
5. If character has no strafe animations: **uncheck Strafe Combat**

### Melee Manager Setup for Generic Characters

1. Add `vMeleeManager` component (menu: `Invector > AI Controller > Components > Melee Manager`)
2. For generic characters, "Create Default Body Members" will NOT work (humanoid only)
3. Click **"Add Extra Body Member"** instead:
   - Set Member Type to `Generic`
   - Name it (e.g., "RightHand", "LeftHand", "Tail", "Head")
   - Assign the bone transform from the character's hierarchy
   - Click "Create" to generate a hitbox collider on that bone
   - Adjust the hitbox collider size as needed

### Hitbox Timing Configuration

Each attack animation state needs a **vMeleeAttackControl** behavior:
- **Body Part**: select Generic > [body part name] to specify which hitbox activates
- **Start Damage**: normalized time when hitbox enables (e.g., 0.2)
- **End Damage**: normalized time when hitbox disables (e.g., 0.35)

To determine timing:
1. Scrub through the animation in the preview
2. Note when the attack motion connects
3. Set start/end damage values accordingly

### Example: Two-Hit Attack from Single Clip

If one animation clip contains two attack motions (e.g., right punch then left punch):

**Attack Control 1 (Right Hand)**:
- Body Part: Generic > Right Hand
- Start Damage: 0.2
- End Damage: 0.35

**Attack Control 2 (Left Hand)**:
- Body Part: Generic > Left Hand
- Start Damage: 0.6 (second hit starts later)
- End Damage: 0.75
- **Reset Attack Trigger**: check this on the LAST attack control in the chain

### Animator Setup for Attacks

1. Create "Attacks" sub-state machine in the base layer Actions
2. Add the attack animation clip
3. Tag the state as `Attack`
4. Add `vAnimatorStateListener` behavior to the state
5. Add transition from Any State using the `WeakAttack` trigger parameter
6. **Uncheck "Can Transition to Self"** to prevent attack looping
7. Create an exit transition so the AI returns to locomotion

### Damage Layer Configuration

In the Melee Manager, set the damage/hit layers:
- Default is `Enemy` -- change to `Player` so the AI damages the player, not other enemies

---

## 11. Jump and AutoCrouch

**Video**: #7 Jump & AutoCrouch (5:40)

### NavMesh Setup (Prerequisite)

1. Open: `Window > AI > Navigation`
2. Mark all obstacle objects as **Static** (apply to children)
3. Configure agent settings:
   - **Radius**: small enough for the AI to fit through tight spaces
   - **Agent Height**: match the character's collider height (e.g., 1.4)
4. For jumping between surfaces: create an **Off Mesh Link**
   - Set jump distance (e.g., 3)
   - This creates a navigable connection between disconnected NavMesh areas
5. **Bake** the NavMesh

### Jump Feature

Located on the AI Controller component (NOT the FSM -- it's a controller-level feature).

| Setting | Description |
|---------|-------------|
| **Jump Speed** | Horizontal speed during jump |
| **Jump Height** | Vertical height of the jump |

Customize values based on character size and rigidbody settings. The AI will automatically use Off Mesh Links to jump between NavMesh surfaces.

### Auto Crouch Feature

Also on the AI Controller component.

| Setting | Description |
|---------|-------------|
| **Use Auto Crouch** | Enable/disable |
| **Crouch Layer** | Layer of objects that trigger crouching (e.g., Default for low ceilings) |
| **Debug Sphere** | Visual indicator showing the detection sphere above the character |

The auto crouch uses a sphere cast above the character. When it detects a low ceiling (object on the specified layer), the AI automatically crouches.

### Waypoint Setup Tips

- Create a `vWaypointArea` for patrol routes
- Edit mode: click to place waypoints
- Uncheck "Random" for the AI to follow waypoints in order
- Adjust "Time to Stay" at each waypoint

---

## Quick Reference: Complete AI Setup Checklist

### For Any AI Character

1. **Layers**: Set up all Invector-required layers in Project Settings
2. **Tags**: Create Enemy, Player tags
3. **GameObject**: Set tag=Enemy, layer=Enemy
4. **Components**:
   - Animator (with appropriate controller)
   - Capsule Collider (sized to character)
   - Rigidbody (freeze rotation X/Z)
   - NavMesh Agent (match height to collider)
   - FSM Behavior Controller (assign a behavior)
   - AI Controller (choose type: Basic/Combat/Melee/Shooter)
5. **Detection**: Create Eyes detection point, assign layers/tags to detect
6. **NavMesh**: Bake the scene's NavMesh
7. **Waypoints**: Create vWaypointArea and assign to controller

### For Melee Combat AI

- Add Melee Manager
- Set up body members (humanoid: use default; generic: add extra members)
- Configure hitboxes with proper timing
- Set damage layer to Player
- Set up attack animations with vMeleeAttackControl behaviors

### For Shooter AI

- Add Shooter Manager (set damage layer to Player)
- Add Head Track (comes auto-attached)
- Set up weapon handler on right hand bone
- Configure left hand IK offsets in Shooter Manager
- Assign weapon prefab

### For Custom Behaviors (FSM)

- Create FSM Behavior asset
- Add states with actions (Patrol, Flee, Chase, Attack, etc.)
- Add transitions with decisions (Check Damage, Can See Target, Is Listening Noise, etc.)
- Use Send Message + Message Receiver for animations/sounds/events
- Use Change Behavior action to swap FSM at runtime
- Use Check State decision to prevent state conflicts

---

## Key FSM Actions Reference

| Action | Execution | Purpose |
|--------|-----------|---------|
| Patrol | Update | Follow waypoints |
| Flee | Update | Run away from threat. Config: flee distance |
| Chase | Update | Pursue target |
| Go To Noise Position | Update | Navigate to noise source. Options: find new noises, look to noise |
| Look Around | Update | Simulate idle scanning with head track |
| Send Message | Enter | Send a message to the Message Receiver component |
| Change Behavior | Enter | Swap the FSM behavior at runtime |

## Key FSM Decisions Reference

| Decision | Returns True When... |
|----------|---------------------|
| Check Damage | AI received damage (filterable by type). Option: look to damage sender |
| Can See Target | Target is within detection cone |
| Check Health | Health meets threshold condition |
| Check State | AI is in a specific named FSM state |
| Is Listening Noise | A noise was detected (filterable by noise type) |
| Is In Destination | NavMesh agent has reached its destination |

---

## Relevance to The Scorpion Project

For our three enemy types, apply these techniques:

### Hollow Monk (Basic Melee)
- Use **Combat Controller** (vControlAICombat)
- Simple melee hitboxes on hands/weapon
- FSM: Patrol -> Detect Player -> Chase -> Attack -> (on low health) Flee
- Standard head track for realism

### Shadow Acolyte (Fast Melee)
- Use **Combat Controller** with higher movement speeds
- Lighter hitboxes, faster attack timing
- FSM: Patrol -> Detect -> Sprint Chase -> Quick Attack combo -> Disengage -> Re-engage
- Fire element should apply burn + slow (counter their speed)

### Stone Sentinel (Heavy Melee)
- Use **Melee Controller** (vControlAIMelee) for heavier weapon movesets
- Large hitboxes, slower timing, higher damage
- FSM: Guard Position -> Detect -> Slow Chase -> Heavy Attack -> Brief Pause -> Resume
- Lightning element should stun but no knockback (per GDD)

### Boss: The Fallen Guardian
- Use **Melee Controller** with custom FSM per phase
- Use **Change Behavior** action to swap FSM at phase transitions (100-60%, 60-30%, 30-0%)
- Phase 2 fire aura: trigger via Send Message + Message Receiver
- Summon mechanic: use Send Message to call a spawner method
