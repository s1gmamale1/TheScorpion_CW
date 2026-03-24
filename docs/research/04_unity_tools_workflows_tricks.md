# Unity Tools, Workflows & Tricks

Practical tools and workflows that speed up 3D game development. Focused on a dark-themed arena combat game.

---

## 1. Editor Productivity Tips

- **Lock the Inspector** on a specific GameObject — stays put while selecting others.
- **Search by Type** in Hierarchy: `t:Collider`, `t:Light` to find all GameObjects with a component.
- **Tool hotkeys** Q/W/E/R/T/Y match toolbar order left-to-right.
- Unity 6 has AI-assisted workflows with project-aware context and profiler-informed insights.

## 2. ProBuilder for Arena Level Design

Your 25x25m arena:
- Install via Package Manager (`Window > Package Manager > ProBuilder`).
- **Shape Tool** for base floor (25x25), extrude walls, pillars, raised platforms in-scene.
- **Bevel edges** on walls/pillars for polished blockout.
- **ProGrids** for consistent grid snapping — critical for symmetric arena.
- Play-test immediately after geometry changes.
- Replace with final art later or UV-unwrap/texture ProBuilder meshes directly.

## 3. ScriptableObjects for Game Architecture

Directly applicable to ElementSystem, WaveManager, EnemyAI config:

- **Data containers** for enemy stats (HP, speed, damage, weakness), wave compositions, ability definitions. Tweak without touching code.
- **Event channels** to decouple systems: `OnWaveComplete`, `OnElementSwitch`, `OnBossPhaseChange` — any system subscribes without direct references.
- **Runtime sets** — ScriptableObject holding list of active enemies. WaveManager/HUD query counts without `FindObjectsOfType`.
- Unity's **PaddleGameSO** demo on GitHub has working examples.
- Free e-book: "Create Modular Game Architecture with ScriptableObjects" (updated Unity 6).

## 4. Custom Editor Tools

- **Wave Editor Window** (`EditorWindow`) to visually configure wave compositions, enemy counts, spawn delays.
- `[CustomEditor]` for BossAI phases — show thresholds, attacks, summons in readable layout.
- Editor scripts in `Assets/Scripts/Editor/` (excluded from builds).
- `MenuItem` attribute for quick menu actions: "Tools > Reset Wave Data", "Tools > Spawn Test Enemies".

## 5. Cinemachine Third-Person Camera

- Install Cinemachine 3.x via Package Manager.
- **CinemachineCamera** with **ThirdPersonFollow** body component.
- Key settings:
  - **Shoulder Offset**: X: 0.5-1.0, Y: 1.5-2.0 (over-shoulder)
  - **Camera Distance**: 2-4m for melee visibility
  - **Damping**: 0.1-0.3 for responsive feel
- **Built-in collision resolution** — auto-adjusts near walls (critical for enclosed arena).
- Lock-on: use **Target Group** to frame player + locked enemy.
- Boss intro: separate CinemachineCamera with dolly track, blend via Timeline.

## 6. Post-Processing / URP Effects

Add **Global Volume** (`GameObject > Volume > Global Volume`):

| Effect | Settings | Purpose |
|--------|----------|---------|
| **Bloom** | Intensity 1-2, Threshold ~0.9 | Fire/lightning elemental glow |
| **Color Grading** | Shadows → cool blue/purple, desaturated mids | Grim atmosphere |
| **Vignette** | Intensity 0.3-0.4 | Darken edges, focus center |
| **SSAO** | Default | Depth in corners/crevices |
| **Motion Blur** | Intensity 0.1-0.2 | Sell melee attack speed |

- Custom effects: `Assets > Create > Rendering > URP Post-processing Effect` (e.g., red flash on damage).
- Ensure URP Renderer has "Post Processing" enabled.

## 7. Mixamo Character Import

1. Download character as **FBX for Unity**, **With Skin**, 30 FPS, no keyframe reduction.
2. Animations: download as **FBX Without Skin** (saves file size).
3. In Unity: `Assets/Art/Mixamo/Characters/` and `Assets/Art/Mixamo/Animations/`.
4. Character FBX → Rig tab → **Humanoid** → Apply.
5. Animation FBXs → Humanoid → **copy Avatar from character**.
6. For Invector: use **Animator Override Controller** to map Mixamo clips to Invector's expected states.

## 8. Prefab Workflow

- **Base enemy prefab** with shared components (health, NavMeshAgent, Animator).
- **Prefab Variants** per enemy type (Hollow Monk, Shadow Acolyte, Stone Sentinel) — override specific values.
- **Nested prefabs** for arena: pillar → wall → arena. Changes propagate.
- **Edit in Prefab Mode** (double-click) for isolated editing.
- Reference prefab assets, not instances. Keep nesting 2-3 levels max.

## 9. Debugging Tips

- **NullReferenceException**: Check `GetComponent<>()` returns, unassigned serialized fields. Use null-checks in `Awake()`.
- **Script Execution Order**: `Edit > Project Settings > Script Execution Order`. Or use `Awake()` for init, `Start()` for usage.
- **Breakpoint Debugging**: Visual Studio/Rider → "Attach to Unity" → step through code. Better than Debug.Log for boss phases.
- **IndexOutOfRange**: Check collection size before access. Use `List<T>` for dynamic sizes.
- **Conditional Compilation**: `#if UNITY_EDITOR` for debug-only code.

## 10. Free Asset Store Recommendations

- **Medieval Game Assets** category for characters, monsters, environments.
- Monthly **free asset packs** from Unity.
- **Top Free Assets** page for popular downloads.
- Free VFX packs for fire/lightning particles.
- Free dark ambient and combat music packs.

## 11. Timeline for Boss Intro

1. Create Timeline asset, add PlayableDirector to empty GameObject.
2. **Cinemachine Track**: Blend gameplay → cinematic dolly camera around boss.
3. **Animation Track**: Boss intro animation (roar, weapon slam).
4. **Audio Track**: Boss theme sting.
5. **Signal Track**: End signal tells GameManager to start fight.
6. Disable player input at cutscene start via Signal.

## 12. Shader Graph — Fire & Lightning

- **Fire**: Noise node (Gradient/Voronoi) scrolling upward × fire gradient (orange→yellow) → Emission HDR intensity 3-5 → triggers Bloom.
- **Lightning**: Voronoi noise high density + Step node for sharp arcs. Animate seed with Time. Bright cyan/white HDR emission.
- **Fresnel glow** on player: elemental aura (red=Fire, blue=Lightning). Script-exposed property for intensity.
- **VFX Graph**: Fire embers (spawn rate, velocity, color over lifetime). Lightning bolts via line renderers or strip particles.

## Priority for Sub-1-Week Deadline

1. **Highest**: ScriptableObjects for data, ProBuilder arena blockout, Post-processing Global Volume
2. **High**: Prefab Variants for enemies, Cinemachine camera, Shader Graph Fresnel glow
3. **If time**: Timeline boss intro, custom editor tools, polished VFX Graph effects
