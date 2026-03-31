# Invector Melee Combat Template - Core Tutorial Notes

Compiled from official Invector Studio YouTube tutorials (v2.0+ series).
Video #6 (Weapon Holder) had no English transcript available -- noted where relevant.

---

## Table of Contents

1. [#1 Basic and Detail Overview](#1-basic-and-detail-overview)
2. [#2 MeleeManager and MeleeWeapons](#2-meleemanager-and-meleeweapons)
3. [#3 Enemy and Companion NPC](#3-enemy-and-companion-npc)
4. [#4 Inventory System](#4-inventory-system)
5. [#5 Collectables](#5-collectables)
6. [#6 Weapon Holder](#6-weapon-holder)
7. [Getting Started Character Creator](#getting-started-character-creator)
8. [Unity Animator Event](#unity-animator-event)
9. [How to Create Generic Actions](#how-to-create-generic-actions)
10. [How to Trigger Simple Animation](#how-to-trigger-simple-animation)

---

## #1 Basic and Detail Overview

**Video**: https://www.youtube.com/watch?v=KQ5xha36tfE (15:05)

### Project Organization
- After importing the template, you get two folders: Basic Locomotion and Melee Combat
- **Critical**: Create your OWN project folder separate from the Invector folders
  - Example subfolders: `My 3D Models/`, `My Animator Controllers/`, `My Prefabs/`
- This lets you safely delete the Invector folder and reimport new versions without losing your work

### Creating a Character (Basic Controller)
1. Import your FBX character model
2. **Set Rig to Humanoid** in the model import settings (critical step)
3. Ensure the FBX has an Animator component (should be default)
4. Ensure no extra colliders or scripts are on the model -- it must be **clean**
5. Go to **Invector > Basic Locomotion > Create Basic Controller**
6. Drag and drop the FBX file into the character field
7. **Animator Controller**: Each character should have its own copy of the animator controller
   - Duplicate the original animator controller from Invector's Animator folder
   - Rename it (e.g., "MyCharacter_AnimController")
   - Assign your copy to the character creator window
8. Select the camera list data (use "Third Person Normal / Third Person")
9. Hit **Create**
10. Create a prefab of your character for reuse

### Creating a Melee Combat Controller
- Same process but use **Invector > Melee Combat > Create Melee Controller**
- The only difference: uses `vMeleeCombatInput` instead of `vThirdPersonInput`
- `vMeleeCombatInput` inherits from `vThirdPersonInput` and adds: attack, strong attack, block, lock-on inputs

### Key Inspector Settings

#### Health & Stamina
- **Health**: Configurable base health value
- **Health Recovery**: Set recovery rate (0 = no auto recovery)
- **Stamina**: Base stamina value
- **Stamina Recovery**: Rate of stamina regeneration

#### Death Type
- Options: Animation, Ragdoll, or Animation + Ragdoll
- If using Ragdoll, you must add a Ragdoll component to the character

#### Ground Detection
- **Ground Layer**: Set to `Default` by default
- If your floor uses a different layer, the character will float -- match the ground layer to your floor's layer

#### Stop Move Layer
- Assign a layer to walls/obstacles
- Character will stop at objects on this layer instead of walking in place
- Without it, character walks in place against walls

#### Locomotion Type
- **Only Strafe**: Character always faces camera forward direction (good for shooters)
- **Free with Strafe**: Free camera movement normally, press Tab to toggle strafe mode
  - Free mode: camera orbits independently, character moves in any direction
  - Strafe mode: character faces camera direction

#### Roll Control
- Roll direction can be controlled

#### Random Idle
- Assign random idle animations in Locomotion > Random Idle
- Set time interval (e.g., every 3 seconds triggers a random idle animation)

#### Jump Options
- **Air Control**: OFF = jump forward only, no air steering; ON = full air control like a roll
- **Jump Forward Force**: Higher values = longer forward jump distance
- **Jump Force**: Vertical jump height (can make super jumps)

#### Movement Speed
- Adjustable animation speed multiplier
- Works with both in-place and root motion animations
- Increase/decrease to fine-tune character speed

### Input Manager
- Open via **Invector > Input Manager**
- Configure default actions: move, jump, roll, strafe
- Disable inputs by unchecking "Use" checkbox
- Change keyboard/joystick bindings per action
- For joystick axes, check the "Axis" checkbox

### Gameplay Input Styles
- **Default**: Standard third-person
- **Click and Move**: Isometric/top-down style (like Diablo)
  - Use the "Top Down Cursor" prefab
  - Change camera state to "Isometric"
  - Same controller script works for both styles

### Ragdoll Setup
1. Go to **Invector > Basic Locomotion > Components > Ragdoll**
2. It automatically assigns bones
3. Hit **Create** -- generates all necessary colliders
4. Review each bone's collider (especially the head) and adjust if needed
5. Test with hotkey **B** to activate ragdoll at runtime

---

## #2 MeleeManager and MeleeWeapons

**Video**: https://www.youtube.com/watch?v=1aA_PU9-G-0 (12:44)

### Adding MeleeManager
1. Go to **Invector > Melee Combat > Components > Melee Manager**
2. Default hitboxes are created automatically on body parts (hands, feet)
3. Ensure the character uses `vMeleeCombatInput` (not `vThirdPersonInput`)

### Attack System Overview
- Animations are in the Animator's **Full Body Layer**
- Two attack types: **Weak Attacks** and **Strong Attacks**
- Default inputs:
  - Weak Attack: Left Mouse Button / RB (gamepad)
  - Strong Attack: Alpha1 key / RT (gamepad, set as Axis)

### Combo Setup (Attack IDs)
- Attack ID 0 = default combo
- Additional combos use IDs 1, 2, 3, etc.
- Default weak combo example: Left Punch > Right Punch > Left Kick

### Attack Properties (per attack in combo)
| Property | Description |
|----------|-------------|
| **Default Damage** | Base damage for this attack |
| **Distance to Attack** | NPC-only: distance at which NPC triggers attack |
| **Stamina Cost** | Player-only: stamina consumed per attack |
| **Stamina Recovery Delay** | Time before stamina begins recovering after attack |
| **Defense Rate** | How much damage is absorbed when blocking |
| **Defense Range** | Angle of the character's block defense |
| **Start Damage / End Damage** | Normalized time (0-1) when hitbox activates/deactivates |
| **Allow Movement** | Normalized time range (0-1) when player can move/rotate during attack |
| **Damage Multiplier** | Multiplies the total damage for this specific attack |
| **Recoil ID** | Animation triggered when hitting a blocking target |
| **Reaction ID** | Animation triggered on the target when hit |
| **Attack Name** | Links to Hit Damage Particle system for custom hit effects |
| **Ignore Defense** | Attack goes through even if target is blocking |
| **Activate Ragdoll** | Triggers ragdoll on target |
| **Reset Attack Trigger** | **Important**: Enable on the LAST attack of a combo to prevent input buffering from looping the combo |

### Synchronizing Hitbox Timing
- **Start Damage** and **End Damage** are normalized values (0.0 to 1.0) representing the animation timeline
- Watch the animation to determine when the strike connects
- Example: punch connects at ~30% of animation, ends at ~40% => Start: 0.30, End: 0.40
- You can preview the hitbox activation in Scene view in real-time

### Reaction Animations
- **Small Hit Reaction** (ID 0): Plays on upper body layer only, character can still walk
- **Big Hit Reaction** (ID 1): Full body reaction, character staggers back
- Directional hit detection: reactions vary based on hit direction (front, back, left, right)

### Body Part Attacks
- Choose between **Weapon** and **Body Part** attack types
- Body parts (fists, feet, head) use the hitboxes on those bones
- Can add extra body members: **Invector > Melee Combat > Components > Add Extra Body Member**
  - Select the bone (e.g., Head for bite attacks)
  - Humanoid rigs auto-assign bones
  - Customize the hitbox collider to fit

### Custom Hit Effects (Hit Damage Particle)
- Located on the character receiving damage
- Attack Name on the weapon links to Hit Damage Particle name on the receiver
- Default: "blood splash" particle
- Can add custom effects: create entry with custom name, assign particle prefab
- When an attack with matching name hits, the custom particle triggers

### Creating a Melee Weapon
1. Go to **Invector > Melee Combat > Create Melee Weapon**
2. **Must use a clean FBX/model** -- NOT an existing prefab with components
3. Assign the FBX weapon model
4. Hit **Create** -- generates a hitbox on the weapon
5. Resize the hitbox collider to fit the weapon's blade/striking area
6. Parent the hitbox inside the weapon model so it moves together
7. Weapon has its own damage options in the inspector
8. Can duplicate hitboxes for weapons with multiple striking surfaces (add to hitbox list)

### Attaching a Weapon to Character
1. Create a prefab of the weapon
2. Drag weapon into the scene
3. Drag weapon into the character's hand bone in the hierarchy (e.g., Right Hand)
4. Position and rotate to align properly
5. Turn off hitbox gizmos for easier visual alignment
6. On Play, MeleeManager automatically detects and assigns the weapon as current weapon

---

## #3 Enemy and Companion NPC

**Video**: https://www.youtube.com/watch?v=tuwg-H8vjqY (12:05)

### Layer Setup (Critical First Step)
- Invector automatically adds required layers: Stop Move, Action, Head Track, etc.
- Ensure your ground/floor objects use the **Default** layer so characters can walk
- If layers are full, you may need to remove unused layers to make room

### Creating an Enemy NPC
1. Each NPC should have its **own animator controller** (copy from original Melee Combat animator)
2. Go to **Invector > Melee Combat > Create NPC > Enemy AI**
3. Assign the FBX model and the NPC's animator controller
4. Hit **Create**
5. Add MeleeManager: **Invector > Melee Combat > Components > Melee Manager**
6. Fix hitbox alignment on body parts if needed (resize/reposition colliders)

### Enemy AI Configuration
- **Target Detection Tag**: Set who the enemy detects/chases (e.g., "Player")
- **MeleeManager "Who Can I Hit"**: Set the tag of who this NPC can damage (e.g., "Player")
- These MUST match or the enemy won't attack/damage correctly

### NavMesh Baking (Required for NPC Movement)
- Enemy NPCs use Unity's NavMesh system -- **they will not move without a baked NavMesh**
- Steps:
  1. Set ground/floor objects as **Static**
  2. Open **Window > AI > Navigation**
  3. Click **Bake**
  4. Verify the blue NavMesh overlay appears on walkable surfaces
  5. You can uncheck Static afterward if needed; the baked NavMesh remains

### Fixing Missing Animator Behaviors
- When importing into different Unity versions, Animator State Machine Behaviors can get lost
- Symptom: Enemy attacks but deals no damage
- Fix: Check the attack state in the Animator for the `vMeleeAttackControl` behavior
- If missing, use Invector's quick-fix: replace the animator controller files from the fix folder

### Creating a Companion NPC
1. Go to **Invector > Melee Combat > Create NPC > Companion AI**
2. Assign FBX model and animator controller
3. **Change detection tag** to "Enemy" (not "Player") -- companion targets enemies
4. Add MeleeManager with "Who Can I Hit" set to "Enemy"
5. Give the companion a weapon if desired (same process as player: drag weapon into hand bone)

### Tags and Layers for NPCs
- **Enemy**: Tag = "Enemy", detect tag = "Player"
- **Companion**: Tag = "CompanionAI", detect tag = "Enemy"
- Both enemy and companion need correct tags for mutual detection
- The enemy must also detect the companion tag if you want enemies to fight companions

---

## #4 Inventory System

**Video**: https://www.youtube.com/watch?v=1aXmvntzH-g (18:07)

### Adding the Item Manager
1. Go to **Invector > Melee Combat > Components > Item Manager**
2. This is a **player-only** feature
3. Creates an `vItemManager` component on the player
4. References an Inventory UI Prefab (located in `Scripts/ItemManager/Prefabs/`)

### Best Practice: Copy the Inventory Prefab
- Create a copy of the default inventory prefab in YOUR project folder
- Assign your copy to the Item Manager
- This protects against losing customizations when updating Invector

### Inventory UI Structure
- **Main Window**: Opens with **I** key (keyboard) or **Back** button (gamepad)
- **Two tabs**: Equipment and Items
- **Equipment tab**: Contains Equipment Areas (rows) with Equipment Slots
  - Default: 3 rows for melee weapons + 1 for consumables
  - Can add more rows/slots by duplicating
  - Can block slots by toggling "Is Valid"
  - Equipment slot types: Consumable, Melee Weapon, Shooter
  - Add custom types by editing the equipment type enum in code

### Equipment Picker Window
- Opens when selecting an equipment slot
- Shows all compatible items for that slot type

### Item List Data
- **Critical**: This is the master database of all items
- Located in Invector Resources
- **Create your own copy**: Right-click > Create > vItemListData
- Open the list in a detachable window for easier editing
- Each item entry contains:

| Field | Description |
|-------|-------------|
| **Item Name** | Display name |
| **Description** | Tooltip/info text |
| **Type** | Consumable, Melee Weapon, Shooter |
| **Stackable** | Can multiple stack in one slot |
| **Stack Limit** | Max stack size |
| **Icon** | Sprite shown in UI |
| **Original Object** | The actual weapon prefab (created via Create Melee Weapon) |
| **Drop Object** | The collectible prefab spawned when dropping (can be generic or specific mesh) |

### Equip Animation IDs
- Found in Animator > Upper Body Layer > Equip Weapon states
- IDs map to different equip animations:
  - **High Left** / **High Right** (e.g., drawing from back)
  - **Low Left** / **Low Right** (e.g., drawing from hip)
- Assign the correct Equip ID in the item's Custom Settings

### Custom Equip Points
- v2.0 uses **default equip points** on left arm and right arm
- Set up weapons so their pivot/forward direction is consistent
- If all weapons share the same grip orientation, one equip point works for all
- **Custom Equip Points**: Create named points (e.g., "Shield Equip Point") on specific bones
  - Go to the arm bone > Create Custom Points > name it
  - In the item data, set the equip point name to match
  - Example: Shield uses "Shield Equip Point" on left arm

### Equip Delay Time
- Adds delay before weapon instantiates in hand
- Synchronize with the equip animation timing

### Consumable Items Setup
- Set Type = Consumable
- No original object needed (unless you want a visual potion in hand)
- **Attributes**: Key-value pairs that define what the consumable does
  - Example: `health = 25` (recovers 25 HP)
- **Attribute Events**: Connect attribute names to script methods
  1. Add an attribute event entry
  2. Set the attribute name (must match the item's attribute name exactly)
  3. Drag the GameObject with the target script
  4. Select the method (e.g., `vThirdPersonController.ChangeHealth`)
  - Other examples: `ChangeMaxHealth`, `ChangeMaxStamina`

### Inventory Events
- **On Open Inventory**: Lock player input so character stops moving
  - Use `vMeleeCombatInput.SetLockAllInput(true)`
- **On Close Inventory**: Unlock player input
- **Drop Items on Death**: Enable to spawn collectibles at death location (for respawn/retrieval mechanics)

### Starting Items
- Add items to "Items to Start With" list
- Character spawns with these items already in inventory
- Can set them to auto-equip

### Equipment Switching at Runtime
- Use arrow keys (default) to cycle between equipment slots
- Each slot triggers its own equip animation based on the Equip ID

### Inventory Input Customization
- Located on the inventory prefab:
  - Change Equipment Controllers: Arrow keys for cycling weapons/consumables
  - Submit/Cancel buttons for UI navigation
  - Open Inventory input
  - Quick Remove Equipment input
- **Time Scale**: Set 0 to fully pause game when inventory is open, or 0-1 for slow motion

---

## #5 Collectables

**Video**: https://www.youtube.com/watch?v=rnwYb8kPNNc (3:30)

### Chest / Interactive Collectible Setup
- Use the Invector Chest prefab as a base (replace the 3D model as needed)
- Requirements:
  - **Animator**: Must have an animator with an "open chest" animation
  - **NavMesh Obstacle**: Add to prevent NPCs from walking through

### Item Collection Component
- **Auto Action**: If ON, triggers automatically when player enters range (good for doors/letters)
  - For chests: turn OFF so player must press a button (e.g., A/E)
- **Message**: HUD prompt text shown to the player
- **Play Animation**: Name of the animation state (e.g., "open chest")
- **Active from Forward**: Player must face the object to interact
  - Character's forward direction must align with the trigger's forward direction
- **Destroy After Collect**: Remove the collectible object after collection
- **Item List Data**: Must reference YOUR item list data (not the default if you made a copy)

### Chest Events
- Can trigger animations on the chest (e.g., lid opening)
- Can enable particle systems (e.g., sparkle/glow effect on open)
- Add a list of items the chest contains

### Prefab Examples in Project
- Different enemy prefabs with various configurations
- Portal prefab: transfers character to another scene while keeping items/equipment
- Various collectible prefabs: generic collectibles and specific weapon/item pickups
  - Generic: uses placeholder mesh, works with any item
  - Specific: uses actual weapon mesh for visual representation

---

## #6 Weapon Holder

**Video**: https://www.youtube.com/watch?v=MSAJeU0RXOs (3:14)

> **Note**: No English transcript was available for this video. Key concepts based on context from other tutorials:

### Weapon Holder Concept (from related tutorials)
- Weapon Holders allow weapons to be visually stored on the character's body when unequipped
  - Example: Sword on back, shield on back when not in hand
- Holders are bone-parented transforms where unequipped weapons rest
- When equipping, the weapon moves from the holder to the hand equip point
- When unequipping, it moves back to the holder
- Setup involves creating holder transforms on appropriate bones (spine, back, hip)

---

## Getting Started Character Creator

**Video**: https://www.youtube.com/watch?v=cUqkkowMeSg (9:05)

### Updated Character Creator Workflow (v2.5+)

#### Pre-Setup Checklist
1. **Import Project Settings**: After importing the template, import the project settings
   - A warning displays in Scene view if project settings aren't imported
2. **Create your own folder structure** -- never use the Invector folder for your assets
3. **Never use default resource files** directly (animator controller, item list data)
   - Create copies for your project
   - Originals may be overwritten on template updates
4. Check current version: **Invector > Help > Welcome Window**

#### Creating a Controller
1. Go to **Invector** menu > select controller type:
   - **Basic** (locomotion only)
   - **Melee Combat** (locomotion + melee)
   - **Shooter** (locomotion + melee + shooter -- contains ALL features)
2. Optional components in the creator window:
   - **Use Game Controller**: Handles death behavior (reload scene or respawn)
   - **Add Inventory**: Adds Item Manager + Inventory UI prefab
   - Shooter option: can include or exclude melee combat

#### Controller Templates
- Templates are prefabs with ALL necessary components pre-configured (like demo scenes)
- Inside the template prefab:
  - Empty child object called **"3D Model"** -- this is where your model goes
  - All Invector components are pre-attached and configured
- This means: handlers, holders, footsteps, head tracking are all ready

#### Model Requirements
- Change animation type from **Generic** to **Humanoid** in import settings
- Verify bone assignments in the Humanoid rig configuration
- The only component you must manually add is **Ragdoll** (a few clicks via Invector menu)

#### Snap to Body Component (New Feature)
- Works with **Body Snapping Control**
- Tells a GameObject to snap to a specific bone/transform at runtime
- Example: Set right-hand weapon handler's target = Right Hand bone
  - On Play, the handler automatically attaches to the correct hand
- **Key benefit**: Set up handlers and holders EXTERNALLY (outside the character model)
  - When you swap character models, you don't need to redo handler setup
- Provides a preview of weapon alignment in the editor

#### Replacing a 3D Model
1. Add the new model to the "3D Model" child object
2. Update the **Avatar** on the Animator component
3. Everything else auto-generates:
   - Hitboxes for MeleeManager (based on humanoid rig)
   - Sphere triggers for Footstep system (detects hand/feet positions from rig)
4. **Remember**: If old model had Ragdoll, new model needs Ragdoll added again (or remove the component)

#### Item Collectible Quick Setup
- Use the item collectible prefab
- Set items, check **Auto Equip** option
- Set **Equip Area** index for each item:
  - 0 = Weapons
  - 1 = Defense (shields)
  - 2 = Consumables
- Items auto-equip to the correct slot on collection

#### IK Adjustment
- Left-hand IK for shooter weapons needs per-character adjustment
- Each character model has different bone rotations depending on the rig
- Invector has a separate IK adjustment tutorial for this

---

## Unity Animator Event

**Video**: https://www.youtube.com/watch?v=uZn53kKsI0I (10:40)

### Overview
- Trigger particles, effects, damage, and any UnityEvent at precise animation timings
- Works on ANY object with an animator (not just the character controller)

### Setup: Playing a Custom Animation
1. Add your animation clip to the Animator Controller
   - Use the **Actions sub-state machine**
   - Create a new state, assign the animation clip
   - Name the state (e.g., "Magic Attack")
2. Add an **exit transition** so the animation returns to locomotion
3. Use `vAnimatorTag` behavior (NOT the old "Custom Action" tag)
   - Newer and faster than the legacy tag system
   - Supports multiple tags per animation state
   - Common tags: `CustomAction` (locks movement, uses root motion), `LockMovement`, `IsReloading`, `IsEquipping`, `Attack`
   - You can create custom tags for unique conditions

### Using vGenericAnimation to Trigger
1. Add `vGenericAnimation` component to the character
2. Set the animation clip name (must match the Animator state name exactly)
3. Assign an input key (e.g., keyboard key "L")
4. On press, the animation plays via the Animator

### vAnimatorEventReceiver (Core System)
1. Add `vAnimatorEventReceiver` to the character (or any child object)
2. It needs access to an Animator Controller to search animation states
3. **Get Animator in Parent**: Check this if the receiver is on a child object (e.g., a weapon instantiated at runtime) to find the parent's Animator

### Creating Animator Events
1. In the `vAnimatorEventReceiver`, create events with unique names (e.g., "Play Fire Effect", "Stop Fire Effect")
2. Set the **normalized time** (0.0 to 1.0) for when each event fires during the animation
   - Example: Fire starts at 0.3, stops at 0.8
   - **Warning**: Event time must be LESS than the exit time of the animation state
3. For each event, assign Unity Events:
   - Drag the target GameObject
   - Select the method (e.g., `ParticleSystem.Play()`, `ParticleSystem.Stop()`)

### Particle Collision Damage Setup
1. On the particle system, enable **Collision**
   - Set collision layer (e.g., "Enemy")
   - Enable **Send Collision Messages**
2. Add `vObjectDamage` component to the particle system
   - Check **On Particle Collision**
   - Set the target tag (e.g., "Enemy")
   - Set damage value (e.g., 5)
   - Set **Damage Type** name (e.g., "Fire")

### Custom Damage Effects (Hit Damage Particle)
- Damage Type on the attacker links to Hit Damage Particle on the receiver
- On the enemy:
  1. Add a particle (e.g., fire effect) as a child of a bone (e.g., Spine) so it follows movement
  2. Uncheck **Play on Awake** and **Looping**; set short duration
  3. In Hit Damage Particle list, add entry with matching name (e.g., "Fire")
  4. Either assign a prefab to instantiate OR use **Custom Trigger Event** to call `Play()` on the existing particle

### Key Takeaway
- The Animator Event system decouples effects/damage from the controller script
- Can be used on weapons, environment objects, NPCs -- anything with an Animator
- Enables complex visual + gameplay behaviors without modifying Invector's core scripts

---

## How to Create Generic Actions

**Video**: https://www.youtube.com/watch?v=hlLWnsIQz-c (5:40)

### Overview
- Available in Basic Locomotion/Melee v2.2+ and Shooter v1.1+
- Handles environment interactions: open doors, pull levers, push buttons, climb, etc.
- Replaces the old system where actions were hardcoded in the controller

### Architecture Change
- Previously: actions (step up, climb, jump over) were inside the controller script
- Now: separated into the `vGenericAction` component
  - Much more control and customization options
  - Can be added/removed independently
  - Updating the template won't break custom actions

### vGenericAction Component
- Add to the player character
- No configuration needed on this component itself -- all settings are on the **trigger**

### vTriggerGenericAction (The Trigger Object)
- Place this on the interactable object in the scene
- Key settings:

| Setting | Description |
|---------|-------------|
| **Disable Collision** | Turn off player collision during action (useful for climb-throughs) |
| **Disable Gravity** | Turn off gravity during action (useful for climbs/jumps) |
| **Match Target** | Align a specific body bone to a specific position |
| **Match Target Axes** | Y and Z for height/distance; X left free for player position |
| **Start/End Match Target** | Normalized time range to synchronize matching with animation |
| **Active from Forward** | Player must face the trigger to activate |
| **Use Trigger Rotation** | Player rotates to face the trigger's forward direction |
| **Destroy After Use** | One-time events (e.g., opening a chest) |
| **Animation Clip** | The animation state name to play |

### Door Example (Detailed)
1. Download/create a door-opening animation (e.g., from Mixamo)
2. Create a sub-state machine in the Animator called "Open Door"
3. Add the animation clip twice:
   - Normal version for one side
   - **Mirrored** version (check Mirror in state settings) for opening from the other side
4. Create a matching door animation (using Unity's Animation tool) for the door mesh itself
5. On the trigger:
   - Set animation clip name
   - Enable **Active from Forward**
   - Enable **Use Trigger Rotation**
6. Use **Events** to communicate character action with environment:
   - **On Action Event**: Trigger the door's Animator to play the open animation
   - Can add **delay** before event fires
   - Can trigger: sound effects, particles, color changes, methods on other scripts, enable/disable GameObjects

### Ladder System
- Separated from controller into its own component
- Works like Generic Action but with extra input for climbing
- Disables `vThirdPersonInput` during ladder use
- Re-enables input when action completes
- Fully independent from the main controller

### Important Note for Fresh Updates
- If you just updated, open `vGenericAction` and verify the `OnAnimatorMove` method contains the root motion lines
- This ensures root motion works properly during generic actions

### Key Principle
- Generic Actions let you create custom controller behaviors WITHOUT modifying Invector's main scripts
- This preserves update compatibility

---

## How to Trigger Simple Animation

**Video**: https://www.youtube.com/watch?v=VVqkSlQ4x2M (2:51)

### New in v2.2+ / Shooter v1.1+
- All player components are now **collapsible** in the inspector (open/edit/close)
- New **Actions** tab in Basic Locomotion components

### vGenericAnimation Component
- Simplest way to trigger a one-off animation from input
- Steps:
  1. Add `vGenericAnimation` component to the player
  2. Open Animator > Actions sub-state machine
  3. Create a new sub-state machine (e.g., "My New Animations")
  4. Add the animation clip as a state
  5. **Add exit transition** to the Exit node (so it returns to locomotion)
  6. In the Animator state, add a `vAnimatorTag` behavior:
     - `LockMovement` = locks position, rotation, and other inputs
     - `LockRotation` = still allows character rotation during animation
  7. In the `vGenericAnimation` component:
     - Type the **exact animation state name**
     - Set **Animation End** value = same as the Exit Time on the Animator transition
  8. Assign an input key (e.g., "L")

### Adding Events to Generic Animation
- **On Start**: Fires when animation begins
- **On End**: Fires when animation ends
- Example: Hide UI at start, show UI at end
- Can trigger: sounds, particles, cutscenes, other scripts, enable/disable objects

### Key Takeaway
- Quick way to add custom animations without touching controller scripts
- Serves as a starting point for more complex behaviors (which use Generic Actions)

---

## Quick Reference: Common Setup Checklist

### New Character
- [ ] FBX set to Humanoid rig
- [ ] Clean model (no extra colliders/scripts)
- [ ] Own copy of Animator Controller
- [ ] Own copy of Item List Data
- [ ] Ground Layer matches floor layer
- [ ] Ragdoll added if needed
- [ ] Prefab created

### New Enemy NPC
- [ ] Own Animator Controller copy
- [ ] Created via Create NPC > Enemy AI
- [ ] MeleeManager added
- [ ] Detection tag = "Player"
- [ ] Hit tag = "Player"
- [ ] NavMesh baked
- [ ] Animator behaviors intact (vMeleeAttackControl)

### New Weapon
- [ ] Created from clean FBX (not existing prefab)
- [ ] Hitbox resized to match weapon geometry
- [ ] Start/End Damage times synced with attack animations
- [ ] Last combo attack has Reset Attack Trigger enabled
- [ ] Prefab created
- [ ] Added to Item List Data with correct equip ID and equip point

### Inventory
- [ ] Item Manager added to player
- [ ] Own copy of Inventory UI Prefab
- [ ] Own copy of Item List Data
- [ ] Attribute Events configured (health, stamina, etc.)
- [ ] Open/Close events lock/unlock player input
- [ ] Equipment areas and slots configured for game design

### Custom Animation / Action
- [ ] Animation added to Actions sub-state machine in Animator
- [ ] Exit transition added
- [ ] vAnimatorTag behavior added with appropriate tag
- [ ] vGenericAnimation or vTriggerGenericAction configured
- [ ] Events connected for environmental interaction
