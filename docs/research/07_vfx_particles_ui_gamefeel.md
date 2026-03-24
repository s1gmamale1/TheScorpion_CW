# Research 07: VFX, Particle Systems, HUD/UI, and Game Feel/Juice

## Compiled Research from 20+ Sources — Full Implementations

---

## TABLE OF CONTENTS

1. [Fire Particle Effects](#1-fire-particle-effects)
2. [Lightning / Electric Arc VFX](#2-lightning--electric-arc-vfx)
3. [Shader Graph Glow & Emission (URP)](#3-shader-graph-glow--emission-urp)
4. [VFX Graph Combat Effects (Slash/Hit)](#4-vfx-graph-combat-effects-slashhit)
5. [Screen Shake — Cinemachine Impulse](#5-screen-shake--cinemachine-impulse)
6. [Post Processing — Dark Atmosphere (URP)](#6-post-processing--dark-atmosphere-urp)
7. [Health Bar UI (Slider)](#7-health-bar-ui-slider)
8. [Cooldown Ability Icon UI](#8-cooldown-ability-icon-ui)
9. [Damage Numbers / Floating Text](#9-damage-numbers--floating-text)
10. [Element Indicator UI](#10-element-indicator-ui)
11. [Wave Counter UI](#11-wave-counter-ui)
12. [Adrenaline / Ultimate Meter UI](#12-adrenaline--ultimate-meter-ui)
13. [Time Slow Motion Effect](#13-time-slow-motion-effect)
14. [Chromatic Aberration Hit Effect](#14-chromatic-aberration-hit-effect)
15. [Blood/Hit Splash Particles](#15-bloodhit-splash-particles)
16. [Trail Renderer — Weapon Swing](#16-trail-renderer--weapon-swing)
17. [Ground Slam Shockwave Effect](#17-ground-slam-shockwave-effect)
18. [Elemental Aura Effect (Shader)](#18-elemental-aura-effect-shader)
19. [UI Animation with DOTween](#19-ui-animation-with-dotween)
20. [Game Feel / Juice — Master Reference](#20-game-feel--juice--master-reference)
21. [Custom Post Processing in URP (Full Code)](#21-custom-post-processing-in-urp-full-code)
22. [Dissolve + Glow Edge Shader (URP)](#22-dissolve--glow-edge-shader-urp)
23. [Energy Shield / Hologram Shader (URP)](#23-energy-shield--hologram-shader-urp)
24. [Feel Framework — 150+ Feedback Reference](#24-feel-framework--150-feedback-reference)

---

## 1. Fire Particle Effects

**Sources:**
- [GameDev Academy — Fire Particle Tutorial](https://gamedevacademy.org/fire-particle-unity-tutorial/)
- [Unity Learn — Playing with Fire](https://learn.unity.com/tutorial/playing-with-fire-what-are-particle-systems)
- [80.lv — Magic Fire Effect Breakdown](https://80.lv/articles/breakdown-magic-fire-effect-in-unity)

### Architecture: Multi-System Fire
A convincing fire effect uses **4 child particle systems** working together:
- `Particles_Fire` — main flame body
- `Particles_FireAdd` — additive glow layer
- `Particles_Sparks` — flying ember particles
- `Particles_FireGlow` — ambient light halo

### Fire Particle System Configuration

**Main Module:**
| Parameter | Value | Notes |
|-----------|-------|-------|
| Start Lifetime | Random: 0.5–1.5s | Organic variation |
| Start Speed | Random: 1–3 | Natural flickering |
| Start Size | Random: 0.3–0.8 | Size variation |
| Start Color | Gradient: Red → Orange | Fire palette |
| Simulation Space | World | Particles independent of emitter |
| Gravity Modifier | -0.1 to -0.3 | Upward drift |
| Max Particles | 50–100 | Performance balance |

**Emission Module:**
| Parameter | Value |
|-----------|-------|
| Rate over Time | 20–40 |
| Bursts | Optional: 5–10 particles every 0.5s |

**Shape Module:**
| Parameter | Value |
|-----------|-------|
| Shape | Cone |
| Angle | 15–25° |
| Radius | 0.2–0.5 |
| Emit From | Surface (not base) |

**Color over Lifetime:**
- 0%: White/Yellow (hot core) → 30%: Orange → 60%: Red → 100%: Transparent Black
- Alpha: Full at 0%, fade to 0 by 100%

**Size over Lifetime:**
- Curve: Start at 0.5, grow to 1.0 at 30%, shrink to 0 by 100%

**Velocity over Lifetime:**
- Y: Small positive value (0.5–1.0) for upward drift
- X/Z: Small random noise (-0.2 to 0.2)

**Noise Module (for Sparks):**
- Strength: 0.5–1.0
- Frequency: 2–4
- Scroll Speed: 1–2

**Renderer:**
- Render Mode: Billboard
- Material: Additive particle shader with soft fire texture
- Sort Mode: By Distance

### Magic Fire (Hades-style) Technique
1. Create 4 fire sprite variations in one sprite sheet
2. Basic particle emitter distributes sprites
3. Custom dissolve shader (using Custom Vertex Streams) adds dissolving effect
4. Duplicate emitters at different sizes for depth
5. Add overexposure emitters + small trailing particles
6. Noise-based scrolling shader between fingers for energy leak effect

---

## 2. Lightning / Electric Arc VFX

**Sources:**
- [ArmanDoesStuff — Lightning/Shock Aura Effect](https://www.armandoesstuff.com/tutorial/lightning-shock-aura-effect)
- [Gabriel Aguiar — Electric Arc VFX Graph](https://www.artstation.com/artwork/nELXe6)
- [Blender.fi — Electric Arc Tutorial](https://blender.fi/2022/05/20/unity-vfx-graph-electric-arc-tutorial/)
- [Keijiro SpektrLightning](https://github.com/keijiro/SpektrLightning)

### Method A: ShaderGraph + VFX Graph Electric Aura

**Shader Setup (Unlit ShaderGraph):**
- Surface Type: Transparent
- Blend Mode: Additive
- Shadow Casting: Disabled
- Receive Shadows: Off

**Node Chain:**
1. `Time` node → `Tiling and Offset` (moves UVs along bolt texture)
2. `Sample Texture 2D` with bolt/lightning texture
3. `Step` node for threshold (controls bolt visibility)
4. Multiply by `Color` property (HDR for bloom)
5. Output to `Base Color` and `Alpha`

**VFX Graph Configuration:**
- **Spawn**: Low constant rate + burst spawning with randomized delays
- **Initialize**: Set lifetime, size; position particles on sphere around character
- **Update**: Sample curves mapped to particle lifetime; adjust step strength via curve
- **Output**: `Output Particle Mesh` using custom bolt mesh + shader; render in Local space
- **Orient**: Face particles toward center of character

**Mesh Preparation:**
- Create thin bolt geometry in Blender
- UV map filled for texture sampling
- Export as FBX

### Method B: VFX Graph Bezier Curve Lightning

**Structure (from Gabriel Aguiar tutorial):**
1. Particles positioned along a Bezier curve
2. Particle strip configuration for continuous arc
3. `Noise 3D` node for procedural electricity jitter
4. Spark particles at endpoints
5. Controls for Bezier curve start/end points

**Key VFX Graph Nodes:**
- `Set Position (Bezier)` — positions particles along curve
- `Output Particle Strip` — renders connected particles as strip
- `Noise 3D` — displaces strip points for electrical look
- `Turbulence` — adds randomized force

### Method C: Line Renderer Lightning (Simple)

```csharp
public class LightningBolt : MonoBehaviour
{
    public Transform startPoint, endPoint;
    public LineRenderer lineRenderer;
    public int segments = 10;
    public float offsetAmount = 0.5f;
    public float updateInterval = 0.05f;

    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0;
            UpdateBolt();
        }
    }

    void UpdateBolt()
    {
        lineRenderer.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 point = Vector3.Lerp(startPoint.position, endPoint.position, t);

            if (i != 0 && i != segments - 1)
            {
                point += Random.insideUnitSphere * offsetAmount;
            }

            lineRenderer.SetPosition(i, point);
        }
    }
}
```

**LineRenderer Settings:**
- Width: 0.05–0.15
- Material: Additive with glow texture
- Color: Cyan/Blue HDR for bloom
- Corner Vertices: 3–5
- End Cap Vertices: 3–5

---

## 3. Shader Graph Glow & Emission (URP)

**Sources:**
- [Daniel Ilett — Glitter/Glow Effect](https://danielilett.com/2021-11-06-tut5-19-glitter/)
- [Daniel Ilett — Energy Shield](https://danielilett.com/2023-02-09-tut6-3-energy-shield/)
- [Daniel Ilett — Dissolve Effect](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/)

### Critical Rule: Bloom Required
**All glow/emission effects require a Bloom post-processing override on the Volume to be visible.** Without Bloom, HDR emission colors will appear as solid flat colors.

### Glow Properties Setup (Lit Shader Graph)

| Property | Type | Default | Purpose |
|----------|------|---------|---------|
| Glitter Color | Color (HDR) | White, Intensity 3+ | Emission sparkle color |
| Fresnel Power | Float | 2–5 | Edge glow falloff |
| Fresnel Color | Color (HDR) | Cyan, Intensity 2+ | Edge glow tint |
| Edge Width | Float | 0.05 | Glow border thickness |

### Fresnel Glow Node Chain
1. `Fresnel Effect` node (Power = Fresnel Power property)
2. `Multiply` by Fresnel Color (HDR)
3. Add other emission sources
4. Output → `Emission` pin on PBR/Lit Master

### Glitter/Sparkle Effect Node Chain
1. `Sample Texture 2D` (noise texture, 256x256)
2. `Tiling and Offset` (Noise Scale)
3. `Hue` node (Offset = Noise Rotation Speed x Time)
4. Subtract `Glitter Offset` threshold
5. `Normalize` result
6. `View Direction` (World) → `One Minus` → `Normalize`
7. `Dot Product` between steps 5 and 6
8. `Saturate` → `Multiply` by Glitter Color (HDR)
9. Output → add to Emission

### Intersection Glow (Energy Shield)
- Compare `Scene Depth` with fragment depth
- Difference drives glow intensity at intersections
- Requires `_CameraDepthTexture` enabled in URP settings

### Emissive Edge Glow (Dissolve)
1. `Simple Noise` → `Remap` to (-Noise Strength, +Noise Strength)
2. Add to `Cutoff Height`
3. `Position` (World) → `Split` Y → first `Step` for pixel culling
4. Second `Step` with offset = `Edge Width` added to Y
5. Subtract first Step from second → isolates edge band
6. Multiply by HDR `Edge Color` → `Emission` output

---

## 4. VFX Graph Combat Effects (Slash/Hit)

**Sources:**
- [z4gon/ground-slash-vfx-unity (GitHub)](https://github.com/z4gon/ground-slash-vfx-unity)
- [Gabriel Aguiar — Sword Slash VFX](https://www.artstation.com/artwork/G82XxB)
- [Blender.fi — Ground Slash Tutorial](https://blender.fi/2021/12/29/unity-vfx-graph-ground-slash-tutorial/)

### Sword Slash VFX (3 Texture Approach)
**Textures Needed:**
1. Circular texture — gives slash its shape
2. Noise/caustic texture — detail inside slash
3. Gradient texture — highlights specific areas; also used for dissolution

**VFX Graph Setup:**
- `Output Particle Mesh` — renders slash mesh
- Set culling mode to `Both` (render all sides)
- Rotate/adjust mesh to face attack direction
- `Spawn Over Distance` — spawns particles as VFX moves through world
- `Change Space` node with Target Space = World

### Ground Slash VFX Components

**1. Slash Mesh:**
- Create Bezier curve in Blender, extrude to 3D shape
- UV map for gradient texture
- Export FBX
- Use `Output Particle Mesh`, culling = Both

**2. Ground Decals:**
- `Spawn Over Distance` component
- `Output Particle Forward Decal` node
- Dual decals: black scorch + orange fire trail

**3. Debris System:**
- `Output Particle URP Lit Mesh` node
- Add gravity and plane collision
- Random colors, sizes, rotations

**Triggering from Code:**
```csharp
public class SlashVFXTrigger : MonoBehaviour
{
    public VisualEffect slashVFX;

    public void TriggerSlash(Vector3 direction)
    {
        slashVFX.SetVector3("Direction", direction);
        slashVFX.SendEvent("OnSlash");
    }
}
```

---

## 5. Screen Shake — Cinemachine Impulse

**Sources:**
- [Unity Docs — Cinemachine Impulse 3.1](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulse.html)
- [Unity Docs — Impulse Source 2.3](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/CinemachineImpulseSource.html)

### Setup Steps

**1. Add Impulse Source to damage-dealing objects:**
- Component: `CinemachineImpulseSource`
- Configure Raw Signal (6D Noise Profile or Fixed Signal)
- Set Amplitude Gain (strength multiplier)
- Set Frequency Gain (vibration speed)

**2. Add Impulse Listener to virtual camera:**
- Select CinemachineVirtualCamera
- Extensions → Add Extension → `CinemachineImpulseListener`
- Adjust `Gain` property (amplifies all impulses)

### Impulse Source Properties

| Property | Description | Recommended |
|----------|-------------|-------------|
| Amplitude Gain | Multiplies signal strength | 0.5–2.0 |
| Frequency Gain | Multiplies vibration speed | 1.0–2.0 |
| Attack | Time to reach full amplitude | 0.05s |
| Sustain Time | Duration at full amplitude | 0.1–0.3s |
| Decay | Fade-out duration | 0.2–0.5s |
| Impact Radius | Full-strength area | 0 (infinite) or scene-appropriate |
| Dissipation Mode | Falloff type | Linear or Soft Decay |
| Dissipation Distance | Fade distance beyond radius | 100 |

### Triggering from Script

```csharp
using Cinemachine;

public class CombatScreenShake : MonoBehaviour
{
    [SerializeField] private CinemachineImpulseSource impulseSource;

    // Light hit
    public void OnLightHit()
    {
        impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 0.3f;
        impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = 0.1f;
        impulseSource.GenerateImpulse(Vector3.down * 0.5f);
    }

    // Heavy hit
    public void OnHeavyHit()
    {
        impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 1.0f;
        impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = 0.2f;
        impulseSource.GenerateImpulse(Vector3.down * 1.5f);
    }

    // Ultimate activation
    public void OnUltimateActivation()
    {
        impulseSource.m_ImpulseDefinition.m_AmplitudeGain = 2.0f;
        impulseSource.m_ImpulseDefinition.m_TimeEnvelope.m_SustainTime = 0.5f;
        impulseSource.GenerateImpulse(Vector3.one * 2.0f);
    }
}
```

### Impulse Channel System
- Works like Camera Layers
- Listeners filter which sources they respond to
- Use to separate UI shake from combat shake

---

## 6. Post Processing — Dark Atmosphere (URP)

**Sources:**
- [Febucci — Custom Post Processing URP Guide](https://blog.febucci.com/2022/05/custom-post-processing-in-urp/)
- [GameDev Dustin — Post Processing in URP](https://gamedevdustin.medium.com/post-processing-in-urp-for-unity-2020-20f560816231)
- [Unity Docs — Post Processing in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-in-urp.html)

### Quick Setup for Dark Arena Atmosphere

**1. Create Global Volume:**
- GameObject → Volume → Global Volume
- Create new Profile

**2. Add Overrides:**

**Bloom:**
| Parameter | Value | Purpose |
|-----------|-------|---------|
| Threshold | 0.8–1.0 | Only bright areas glow |
| Intensity | 1.0–2.5 | Glow strength |
| Scatter | 0.6–0.7 | Atmospheric haze |
| Tint | Warm orange or cool blue | Mood setting |

**Vignette:**
| Parameter | Value | Purpose |
|-----------|-------|---------|
| Color | Dark purple/black | Edge darkness |
| Center | (0.5, 0.5) | Screen center |
| Intensity | 0.3–0.5 | Darkness amount |
| Smoothness | 0.3–0.5 | Fade softness |

**Color Grading:**
| Parameter | Value | Purpose |
|-----------|-------|---------|
| Post Exposure | -0.5 to 0 | Overall darkness |
| Contrast | 20–40 | Dramatic shadows |
| Saturation | -10 to 10 | Desaturate slightly |
| Color Filter | Slight warm/cool tint | Mood |
| Shadows (Lift) | Dark blue/purple | Shadow color |
| Highlights (Gain) | Warm orange | Light color |

**Tonemapping:**
- Mode: ACES (filmic, cinematic look)

**Film Grain:**
| Parameter | Value |
|-----------|-------|
| Type | Thin |
| Intensity | 0.1–0.2 |
| Response | 0.8 |

**3. Enable on Camera:**
- Camera component → Rendering → Post Processing = ON

---

## 7. Health Bar UI (Slider)

**Sources:**
- [VionixStudio — Health Bar Tutorial](https://vionixstudio.com/2022/02/07/how-to-make-a-health-bar-in-unity/)
- [Unity Learn — Health Bar with UI Toolkit](https://learn.unity.com/tutorial/make-health-bar-with-UItoolkit)
- [MakeUseOf — Design and Code Health Bars](https://www.makeuseof.com/unity3d-health-bars-design-code/)

### UI Hierarchy Setup
```
Canvas
└── HealthBar (Empty GameObject + Slider component)
    ├── Background (Image — dark bar outline)
    ├── Fill Area
    │   └── Fill (Image — colored health fill)
    └── Handle Slide Area (disable/delete for non-interactive)
```

### Slider Configuration
- **Interactable**: OFF (read-only display)
- **Transition**: None
- **Min Value**: 0
- **Max Value**: 100 (or 1.0 for normalized)
- **Whole Numbers**: OFF (for smooth animation)
- **Fill Image Type**: Filled, Horizontal Fill Method

### Complete Health Bar Script

```csharp
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HealthBarController : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image fillImage;
    public Slider damageSlider; // Delayed damage indicator (white bar behind red)

    [Header("Settings")]
    public float maxHealth = 100f;
    public float lerpSpeed = 5f;
    public float damageIndicatorDelay = 0.5f;

    [Header("Color Thresholds")]
    public Color highHealthColor = Color.green;
    public Color midHealthColor = Color.yellow;
    public Color lowHealthColor = Color.red;
    public float midThreshold = 0.5f;
    public float lowThreshold = 0.25f;

    private float currentHealth;
    private float targetHealth;
    private Coroutine damageCoroutine;

    void Start()
    {
        currentHealth = maxHealth;
        targetHealth = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = maxHealth;
        if (damageSlider != null)
        {
            damageSlider.maxValue = maxHealth;
            damageSlider.value = maxHealth;
        }
    }

    void Update()
    {
        // Smooth health bar fill
        healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealth, Time.deltaTime * lerpSpeed);

        // Color based on health percentage
        float healthPercent = healthSlider.value / maxHealth;
        if (healthPercent > midThreshold)
            fillImage.color = Color.Lerp(midHealthColor, highHealthColor,
                (healthPercent - midThreshold) / (1f - midThreshold));
        else if (healthPercent > lowThreshold)
            fillImage.color = Color.Lerp(lowHealthColor, midHealthColor,
                (healthPercent - lowThreshold) / (midThreshold - lowThreshold));
        else
            fillImage.color = lowHealthColor;
    }

    public void TakeDamage(float damage)
    {
        targetHealth = Mathf.Max(0, targetHealth - damage);

        // Delayed damage indicator
        if (damageSlider != null)
        {
            if (damageCoroutine != null) StopCoroutine(damageCoroutine);
            damageCoroutine = StartCoroutine(UpdateDamageIndicator());
        }
    }

    IEnumerator UpdateDamageIndicator()
    {
        yield return new WaitForSeconds(damageIndicatorDelay);
        while (damageSlider.value > targetHealth + 0.1f)
        {
            damageSlider.value = Mathf.Lerp(damageSlider.value, targetHealth, Time.deltaTime * lerpSpeed);
            yield return null;
        }
        damageSlider.value = targetHealth;
    }

    public void Heal(float amount)
    {
        targetHealth = Mathf.Min(maxHealth, targetHealth + amount);
        if (damageSlider != null)
            damageSlider.value = targetHealth;
    }
}
```

---

## 8. Cooldown Ability Icon UI

**Sources:**
- [Medium — Creating a Cooldown System in Unity (Shakhboz Nabiev)](https://medium.com/@240153_78160/creating-a-cooldown-system-in-unity-6df2915008fc)
- [Medium — Cooldown System (SENDONET)](https://sendonet.medium.com/creating-a-cooldown-system-in-unity-f7b692d866e4)
- [Unity Discussions — Radial Progress](https://discussions.unity.com/t/how-to-best-to-do-a-radial-progress-circle/854069)

### UI Setup for Radial Cooldown

```
Canvas
└── AbilitySlot (Empty)
    ├── AbilityIcon (Image — ability artwork)
    ├── CooldownOverlay (Image — dark semi-transparent)
    │   Image Type: Filled
    │   Fill Method: Radial 360
    │   Fill Origin: Top
    │   Clockwise: true
    │   Color: (0, 0, 0, 0.7)
    └── CooldownText (TextMeshPro — countdown seconds)
```

### Complete Cooldown System

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityCooldown : MonoBehaviour
{
    [Header("UI References")]
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public Image abilityIcon;

    [Header("Settings")]
    public float cooldownDuration = 5f;
    public KeyCode activationKey = KeyCode.Alpha1;
    public Color readyColor = Color.white;
    public Color onCooldownColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private float cooldownTimer = 0f;
    private bool isOnCooldown = false;

    void Start()
    {
        cooldownOverlay.fillAmount = 0f;
        cooldownText.gameObject.SetActive(false);
        abilityIcon.color = readyColor;
    }

    void Update()
    {
        if (Input.GetKeyDown(activationKey) && !isOnCooldown)
        {
            ActivateAbility();
        }

        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;

            // Update radial fill (1 = full overlay, 0 = no overlay)
            cooldownOverlay.fillAmount = cooldownTimer / cooldownDuration;

            // Update countdown text
            cooldownText.text = Mathf.CeilToInt(cooldownTimer).ToString();

            if (cooldownTimer <= 0f)
            {
                EndCooldown();
            }
        }
    }

    void ActivateAbility()
    {
        isOnCooldown = true;
        cooldownTimer = cooldownDuration;
        cooldownOverlay.fillAmount = 1f;
        cooldownText.gameObject.SetActive(true);
        abilityIcon.color = onCooldownColor;

        // TODO: Trigger actual ability effect here
    }

    void EndCooldown()
    {
        isOnCooldown = false;
        cooldownOverlay.fillAmount = 0f;
        cooldownText.gameObject.SetActive(false);
        abilityIcon.color = readyColor;

        // Optional: Flash/pulse to indicate ready
    }

    // Called externally to start cooldown
    public void StartCooldown(float duration)
    {
        cooldownDuration = duration;
        ActivateAbility();
    }

    public bool IsReady() => !isOnCooldown;
}
```

---

## 9. Damage Numbers / Floating Text

**Sources:**
- [Code Monkey — Damage Popup Text](https://unitycodemonkey.com/video.php?v=iD1_JczQcFY)
- [Dusk Sharp — Floating Text Guide](https://dusksharp.medium.com/unity-floating-text-damage-popup-implementation-guide-222c98576d46)
- [GitHub — unity-easy-damage-numbers](https://github.com/bryjch/unity-easy-damage-numbers)

### Prefab Setup
1. Create a Canvas (World Space) or use 3D TextMeshPro
2. Add TextMeshProUGUI child with desired font
3. Save as prefab

### Complete Damage Popup System

```csharp
using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    public TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;
    private Vector3 moveVector;
    private static int sortingOrder;

    public static DamagePopup Create(Vector3 position, int damageAmount, bool isCritical)
    {
        // Instantiate from prefab (assign in a manager or Resources.Load)
        GameObject prefab = Resources.Load<GameObject>("DamagePopup");
        GameObject instance = Instantiate(prefab, position, Quaternion.identity);
        DamagePopup popup = instance.GetComponent<DamagePopup>();
        popup.Setup(damageAmount, isCritical);
        return popup;
    }

    public void Setup(int damageAmount, bool isCritical)
    {
        textMesh.SetText(damageAmount.ToString());

        if (isCritical)
        {
            textMesh.fontSize = 8f;
            textColor = new Color(1f, 0.5f, 0f); // Orange for crits
        }
        else
        {
            textMesh.fontSize = 5f;
            textColor = Color.yellow;
        }

        textMesh.color = textColor;
        disappearTimer = 1f;

        // Random horizontal offset for visual variety
        moveVector = new Vector3(Random.Range(-0.7f, 0.7f), 1f, 0f) * 3f;

        // Sorting order to prevent overlap
        sortingOrder++;
        textMesh.sortingOrder = sortingOrder;
    }

    void Update()
    {
        // Move upward and decelerate
        transform.position += moveVector * Time.deltaTime;
        moveVector -= moveVector * 8f * Time.deltaTime;

        // Scale: Grow then shrink
        if (disappearTimer > 0.5f)
        {
            float scaleAmount = 1f;
            transform.localScale += Vector3.one * scaleAmount * Time.deltaTime;
        }
        else
        {
            float scaleAmount = 1f;
            transform.localScale -= Vector3.one * scaleAmount * Time.deltaTime;
        }

        disappearTimer -= Time.deltaTime;
        if (disappearTimer < 0)
        {
            // Fade out
            textColor.a -= 3f * Time.deltaTime;
            textMesh.color = textColor;

            if (textColor.a < 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
```

### Object Pooling Version (Better Performance)

```csharp
using UnityEngine;
using UnityEngine.Pool;
using TMPro;

public class DamagePopupPool : MonoBehaviour
{
    public static DamagePopupPool Instance;
    public GameObject popupPrefab;
    private ObjectPool<GameObject> pool;

    void Awake()
    {
        Instance = this;
        pool = new ObjectPool<GameObject>(
            createFunc: () => Instantiate(popupPrefab),
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            defaultCapacity: 20,
            maxSize: 50
        );
    }

    public void SpawnPopup(Vector3 position, int damage, bool isCritical)
    {
        GameObject popup = pool.Get();
        popup.transform.position = position + Vector3.up * 1.5f;
        var dm = popup.GetComponent<DamagePopup>();
        dm.Setup(damage, isCritical);
        // Return to pool after lifetime
        StartCoroutine(ReturnAfterDelay(popup, 1.5f));
    }

    System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        pool.Release(obj);
    }
}
```

---

## 10. Element Indicator UI

**Sources:**
- [mob-sakai/UIEffect (GitHub)](https://github.com/mob-sakai/UIEffect)
- [Unity Docs — UI Effect Components](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/comp-UIEffects.html)

### Element Indicator Design for Fire/Lightning Switching

```
Canvas
└── ElementIndicator (HorizontalLayoutGroup)
    ├── FireIcon (Image + outline glow when active)
    ├── SwitchArrow (Image — animated rotation)
    └── LightningIcon (Image + outline glow when active)
```

### Complete Element Indicator Script

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ElementIndicatorUI : MonoBehaviour
{
    [Header("Icons")]
    public Image fireIcon;
    public Image lightningIcon;
    public Image activeGlow; // Positioned behind active element

    [Header("Colors")]
    public Color fireActiveColor = new Color(1f, 0.4f, 0f);
    public Color lightningActiveColor = new Color(0.3f, 0.7f, 1f);
    public Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    [Header("Energy")]
    public Image energyFill;
    public TextMeshProUGUI energyText;

    [Header("Animation")]
    public float switchAnimDuration = 0.3f;
    private bool isFire = true;

    public void SetElement(bool fire)
    {
        isFire = fire;
        if (isFire)
        {
            fireIcon.color = fireActiveColor;
            lightningIcon.color = inactiveColor;
            activeGlow.color = fireActiveColor;
            // Move glow behind fire icon
            activeGlow.rectTransform.anchoredPosition = fireIcon.rectTransform.anchoredPosition;
            energyFill.color = fireActiveColor;
        }
        else
        {
            fireIcon.color = inactiveColor;
            lightningIcon.color = lightningActiveColor;
            activeGlow.color = lightningActiveColor;
            // Move glow behind lightning icon
            activeGlow.rectTransform.anchoredPosition = lightningIcon.rectTransform.anchoredPosition;
            energyFill.color = lightningActiveColor;
        }
    }

    public void UpdateEnergy(float current, float max)
    {
        energyFill.fillAmount = current / max;
        energyText.text = Mathf.FloorToInt(current) + "/" + Mathf.FloorToInt(max);
    }
}
```

---

## 11. Wave Counter UI

**Sources:**
- [Medium — UI Elements For Wave System (Derek Anderson)](https://medium.com/@derekanderson-dev/ui-wave-system-unity-developer-8daf51db522f)
- [Medium — Creating a Wave System (Brian Stong)](https://medium.com/@stonger44/creating-and-enemy-wave-system-in-unity-a2054c1af9ec)
- [Medium — Wave Management (Brian David)](https://medium.com/@Brian_David/wave-management-in-unity-game-development-managing-enemy-waves-4e1adb84cec1)

### UI Structure
```
Canvas
└── WaveUI (Panel at top of screen)
    ├── WaveCountText (TMP — "WAVE 3 / 10")
    ├── EnemyCountText (TMP — "Enemies: 12")
    └── WaveAnnouncement (TMP — Large centered "WAVE 3" that fades)
```

### Complete Wave UI Controller

```csharp
using UnityEngine;
using TMPro;
using System.Collections;

public class WaveUIController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI waveCountText;
    public TextMeshProUGUI enemyCountText;
    public TextMeshProUGUI waveAnnouncementText;
    public CanvasGroup announcementGroup;

    [Header("Animation Settings")]
    public float announcementDuration = 2f;
    public float announcementFadeSpeed = 2f;
    public float announcementScale = 1.5f;

    public void UpdateWave(int currentWave, int totalWaves, int enemyCount)
    {
        waveCountText.text = $"WAVE {currentWave} / {totalWaves}";
        enemyCountText.text = $"Enemies: {enemyCount}";
        StartCoroutine(ShowWaveAnnouncement(currentWave));
    }

    public void UpdateEnemyCount(int remaining)
    {
        enemyCountText.text = $"Enemies: {remaining}";
    }

    IEnumerator ShowWaveAnnouncement(int waveNumber)
    {
        waveAnnouncementText.text = $"WAVE {waveNumber}";
        announcementGroup.alpha = 0;
        announcementGroup.gameObject.SetActive(true);

        // Scale up animation
        RectTransform rect = waveAnnouncementText.rectTransform;
        Vector3 startScale = Vector3.one * 0.5f;
        Vector3 endScale = Vector3.one * announcementScale;

        // Fade in
        float timer = 0;
        while (timer < 0.3f)
        {
            timer += Time.deltaTime;
            announcementGroup.alpha = timer / 0.3f;
            rect.localScale = Vector3.Lerp(startScale, endScale, timer / 0.3f);
            yield return null;
        }

        announcementGroup.alpha = 1;
        yield return new WaitForSeconds(announcementDuration);

        // Fade out
        timer = 0;
        while (timer < 0.5f)
        {
            timer += Time.deltaTime;
            announcementGroup.alpha = 1 - (timer / 0.5f);
            yield return null;
        }

        announcementGroup.gameObject.SetActive(false);
    }

    public void ShowWaveComplete()
    {
        StartCoroutine(ShowAnnouncementText("WAVE COMPLETE!", Color.green));
    }

    public void ShowAllWavesCleared()
    {
        StartCoroutine(ShowAnnouncementText("ALL WAVES CLEARED!", Color.yellow));
    }

    IEnumerator ShowAnnouncementText(string text, Color color)
    {
        waveAnnouncementText.text = text;
        waveAnnouncementText.color = color;
        announcementGroup.alpha = 1;
        announcementGroup.gameObject.SetActive(true);

        yield return new WaitForSeconds(2f);

        float timer = 0;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            announcementGroup.alpha = 1 - timer;
            yield return null;
        }
        announcementGroup.gameObject.SetActive(false);
    }
}
```

---

## 12. Adrenaline / Ultimate Meter UI

**Sources:**
- [Unity Discussions — Adrenaline Meter](https://discussions.unity.com/t/how-to-make-an-adrenaline-meter-to-execute-special-moves-in-unity/677717)
- [Unity Learn — Health Bar with UI Toolkit](https://learn.unity.com/tutorial/make-health-bar-with-UItoolkit)
- [Nanimo Labs — Fill Effect with Sprites](https://nanaimolabs.itch.io/hungry-ghost/devlog/54934/creating-a-fill-effect-with-sprites-in-unity)

### UI Structure
```
Canvas
└── AdrenalineMeter
    ├── Background (Image — dark bar frame)
    ├── FillBar (Image — Filled type, vertical fill)
    ├── GlowOverlay (Image — additive, pulsing when full)
    └── ReadyText (TMP — "ULTIMATE READY!" flashing)
```

### Complete Adrenaline Meter Script

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class AdrenalineMeterUI : MonoBehaviour
{
    [Header("UI References")]
    public Image fillBar;
    public Image glowOverlay;
    public TextMeshProUGUI readyText;
    public Image frameImage;

    [Header("Settings")]
    public float maxAdrenaline = 100f;
    public float fillLerpSpeed = 3f;

    [Header("Colors")]
    public Color emptyColor = new Color(0.3f, 0.1f, 0.1f);
    public Color midColor = new Color(0.8f, 0.4f, 0f);
    public Color fullColor = new Color(1f, 0.2f, 0.2f);
    public Color glowColor = new Color(1f, 0.3f, 0.3f, 0.5f);

    private float currentAdrenaline;
    private float displayAdrenaline;
    private bool isUltimateReady;
    private float glowPulseTimer;

    void Start()
    {
        fillBar.fillAmount = 0;
        glowOverlay.gameObject.SetActive(false);
        readyText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Smooth fill
        displayAdrenaline = Mathf.Lerp(displayAdrenaline, currentAdrenaline, Time.deltaTime * fillLerpSpeed);
        fillBar.fillAmount = displayAdrenaline / maxAdrenaline;

        // Color gradient
        float percent = displayAdrenaline / maxAdrenaline;
        if (percent < 0.5f)
            fillBar.color = Color.Lerp(emptyColor, midColor, percent / 0.5f);
        else
            fillBar.color = Color.Lerp(midColor, fullColor, (percent - 0.5f) / 0.5f);

        // Glow pulse when full
        if (isUltimateReady)
        {
            glowPulseTimer += Time.deltaTime * 3f;
            float alpha = (Mathf.Sin(glowPulseTimer) + 1f) / 2f * 0.5f + 0.2f;
            Color c = glowColor;
            c.a = alpha;
            glowOverlay.color = c;
        }
    }

    public void AddAdrenaline(float amount)
    {
        currentAdrenaline = Mathf.Min(maxAdrenaline, currentAdrenaline + amount);

        if (currentAdrenaline >= maxAdrenaline && !isUltimateReady)
        {
            isUltimateReady = true;
            glowOverlay.gameObject.SetActive(true);
            readyText.gameObject.SetActive(true);
            StartCoroutine(FlashReadyText());
        }
    }

    public bool TryActivateUltimate()
    {
        if (!isUltimateReady) return false;

        currentAdrenaline = 0;
        isUltimateReady = false;
        glowOverlay.gameObject.SetActive(false);
        readyText.gameObject.SetActive(false);
        return true;
    }

    IEnumerator FlashReadyText()
    {
        while (isUltimateReady)
        {
            readyText.alpha = 1f;
            yield return new WaitForSeconds(0.5f);
            readyText.alpha = 0.3f;
            yield return new WaitForSeconds(0.5f);
        }
    }

    // Per GDD: +2 per hit, +5 per kill, +10 per combo finisher
    public void OnEnemyHit() => AddAdrenaline(2f);
    public void OnEnemyKill() => AddAdrenaline(5f);
    public void OnComboFinisher() => AddAdrenaline(10f);
}
```

---

## 13. Time Slow Motion Effect

**Sources:**
- [SharpCoder — Slow Motion Effect Script](https://www.sharpcoderblog.com/blog/unity-3d-slow-motion-effect-script)
- [Ketra Games — Slow Motion Tutorial](https://www.ketra-games.com/2020/10/slow-motion-effect-unity-game-tutorial.html)
- [Unity Docs — Time.timeScale](https://docs.unity3d.com/ScriptReference/Time-timeScale.html)

### Key Rules
1. **Always scale fixedDeltaTime** when changing timeScale
2. **Adjust AudioSource pitch** proportionally
3. **Use unscaledDeltaTime** for UI animations during slow-mo
4. **Rigidbody Interpolation** should be "Interpolate" or "Extrapolate"

### Complete Slow Motion System (with Gradual Transition)

```csharp
using UnityEngine;
using System.Collections;

public class SlowMotionController : MonoBehaviour
{
    [Header("Settings")]
    public float slowMotionTimeScale = 0.3f;
    public float transitionDuration = 0.5f;
    public float ultimateDuration = 8f; // Per GDD: 8 seconds

    [Header("Audio")]
    public AudioSource slowMoStartSFX;
    public AudioSource slowMoEndSFX;

    private float defaultTimeScale = 1f;
    private float defaultFixedDeltaTime;
    private bool isSlowMo = false;
    private Coroutine slowMoCoroutine;

    // Audio sources cache
    private AudioSource[] allAudioSources;

    void Start()
    {
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void ActivateUltimateSlowMo()
    {
        if (slowMoCoroutine != null)
            StopCoroutine(slowMoCoroutine);
        slowMoCoroutine = StartCoroutine(UltimateSlowMotion());
    }

    IEnumerator UltimateSlowMotion()
    {
        // Play SFX
        if (slowMoStartSFX) slowMoStartSFX.Play();

        // Gradually slow down
        yield return StartCoroutine(LerpTimeScale(defaultTimeScale, slowMotionTimeScale, transitionDuration));

        // Hold for ultimate duration (use unscaledTime)
        float elapsed = 0f;
        while (elapsed < ultimateDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Play end SFX
        if (slowMoEndSFX) slowMoEndSFX.Play();

        // Gradually return to normal
        yield return StartCoroutine(LerpTimeScale(slowMotionTimeScale, defaultTimeScale, transitionDuration));
    }

    IEnumerator LerpTimeScale(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float newScale = Mathf.Lerp(from, to, t);
            SetTimeScale(newScale);
            yield return null;
        }
        SetTimeScale(to);
    }

    void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * scale;

        // Adjust all audio sources
        allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (var audio in allAudioSources)
        {
            if (audio != null)
                audio.pitch = scale;
        }
    }

    public void ForceNormalTime()
    {
        if (slowMoCoroutine != null)
            StopCoroutine(slowMoCoroutine);
        SetTimeScale(defaultTimeScale);
    }

    void OnDestroy()
    {
        // Safety: always restore time on destroy
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
    }
}
```

---

## 14. Chromatic Aberration Hit Effect

**Sources:**
- [Unity Docs — Chromatic Aberration](https://docs.unity3d.com/560/Documentation/Manual/PostProcessing-ChromaticAberration.html)
- [Unity Learn — Chromatic Aberration](https://learn.unity.com/tutorial/post-processing-effects-chromatic-aberration)
- [GitHub — Unity Post Processing Wiki](https://github.com/Unity-Technologies/PostProcessing/wiki/Chromatic-Aberration)

### How It Works
Chromatic aberration splits RGB channels at image edges, simulating lens distortion. When pulsed briefly on hit, it creates a powerful damage feedback effect.

### URP Volume Override Setup
1. Add `Chromatic Aberration` override to Volume Profile
2. Default Intensity: 0 (off)
3. Pulse to 0.5–1.0 on hit, decay back to 0

### Complete Hit Feedback Controller

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class HitFeedbackController : MonoBehaviour
{
    [Header("Volume Reference")]
    public Volume postProcessVolume;

    [Header("Chromatic Aberration")]
    public float chromaticIntensity = 0.8f;
    public float chromaticDecaySpeed = 3f;

    [Header("Vignette")]
    public float vignetteHitIntensity = 0.5f;
    public float vignetteDecaySpeed = 2f;
    public Color vignetteHitColor = Color.red;

    [Header("Color Adjustments")]
    public float saturationDrop = -30f;

    private ChromaticAberration chromatic;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private float currentChromatic;
    private float currentVignette;
    private float currentSaturation;
    private float defaultVignetteIntensity;
    private Color defaultVignetteColor;

    void Start()
    {
        postProcessVolume.profile.TryGet(out chromatic);
        postProcessVolume.profile.TryGet(out vignette);
        postProcessVolume.profile.TryGet(out colorAdjustments);

        if (vignette != null)
        {
            defaultVignetteIntensity = vignette.intensity.value;
            defaultVignetteColor = vignette.color.value;
        }
    }

    void Update()
    {
        // Decay chromatic aberration
        if (currentChromatic > 0.01f)
        {
            currentChromatic = Mathf.Lerp(currentChromatic, 0, Time.deltaTime * chromaticDecaySpeed);
            if (chromatic != null) chromatic.intensity.value = currentChromatic;
        }

        // Decay vignette
        if (currentVignette > defaultVignetteIntensity + 0.01f)
        {
            currentVignette = Mathf.Lerp(currentVignette, defaultVignetteIntensity, Time.deltaTime * vignetteDecaySpeed);
            if (vignette != null)
            {
                vignette.intensity.value = currentVignette;
                vignette.color.value = Color.Lerp(defaultVignetteColor, vignetteHitColor,
                    (currentVignette - defaultVignetteIntensity) / (vignetteHitIntensity - defaultVignetteIntensity));
            }
        }

        // Decay saturation
        if (currentSaturation < -0.5f)
        {
            currentSaturation = Mathf.Lerp(currentSaturation, 0, Time.deltaTime * 4f);
            if (colorAdjustments != null) colorAdjustments.saturation.value = currentSaturation;
        }
    }

    public void OnPlayerHit(float damagePercent = 1f)
    {
        // Scale effect intensity with damage
        currentChromatic = chromaticIntensity * damagePercent;
        currentVignette = defaultVignetteIntensity + (vignetteHitIntensity * damagePercent);
        currentSaturation = saturationDrop * damagePercent;

        if (chromatic != null) chromatic.intensity.value = currentChromatic;
        if (vignette != null) vignette.intensity.value = currentVignette;
        if (colorAdjustments != null) colorAdjustments.saturation.value = currentSaturation;
    }

    // Heavy hit: also add brief screen flash
    public void OnHeavyHit()
    {
        OnPlayerHit(1f);
        StartCoroutine(ScreenFlash());
    }

    IEnumerator ScreenFlash()
    {
        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 2f;
            yield return new WaitForSeconds(0.05f);
            float timer = 0;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                colorAdjustments.postExposure.value = Mathf.Lerp(2f, 0f, timer / 0.2f);
                yield return null;
            }
            colorAdjustments.postExposure.value = 0f;
        }
    }
}
```

---

## 15. Blood/Hit Splash Particles

**Sources:**
- [Medium — Blood Splatter Effects (Niklas Bergstrand)](https://bergstrand-niklas.medium.com/how-to-add-blood-splatter-effects-to-enemies-in-unity-ad1f436cc4f)
- [Unity Discussions — Instantiate Blood Particles](https://discussions.unity.com/t/how-do-i-instantiate-a-blood-particle-system-when-i-attack-an-enemy/205887)
- [Unity Discussions — Spawn at Sword Hit Point](https://discussions.unity.com/t/spawn-blood-particle-system-prefab-where-sword-hits-enemy/244735)

### Hit Splash Particle Configuration

**Main Module:**
| Parameter | Value |
|-----------|-------|
| Duration | 0.5s |
| Looping | OFF |
| Start Lifetime | 0.3–0.8 |
| Start Speed | 3–8 |
| Start Size | 0.1–0.3 |
| Start Color | Dark Red / Crimson |
| Gravity Modifier | 1–2 |
| Simulation Space | World |
| Play On Awake | ON |
| Stop Action | Destroy |

**Emission:**
| Parameter | Value |
|-----------|-------|
| Rate over Time | 0 |
| Burst | 1 burst, 15–30 particles |

**Shape:**
| Parameter | Value |
|-----------|-------|
| Shape | Hemisphere |
| Radius | 0.1 |

**Color over Lifetime:**
- Red at 0% → Dark red at 50% → Black (transparent) at 100%

**Size over Lifetime:**
- Curve: 1.0 → 0.3 (shrink as they fall)

**Renderer:**
- Material: Default particle with blood splat texture
- Render Mode: Billboard

### Spawning at Hit Point

```csharp
public class HitSplashSpawner : MonoBehaviour
{
    public GameObject hitSplashPrefab;
    public float hitNormalOffset = 0.1f;

    public void SpawnHitEffect(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Offset slightly along normal to prevent clipping inside enemy
        Vector3 spawnPos = hitPoint + hitNormal * hitNormalOffset;
        Quaternion rotation = Quaternion.LookRotation(hitNormal);

        GameObject splash = Instantiate(hitSplashPrefab, spawnPos, rotation);
        Destroy(splash, 2f); // Cleanup after particles finish
    }

    // For melee: use collision contact point
    public void SpawnMeleeHitEffect(Collision collision)
    {
        ContactPoint contact = collision.GetContact(0);
        SpawnHitEffect(contact.point, contact.normal);
    }

    // For raycast hits
    public void SpawnRaycastHitEffect(RaycastHit hit)
    {
        SpawnHitEffect(hit.point, hit.normal);
    }
}
```

---

## 16. Trail Renderer — Weapon Swing

**Sources:**
- [Unity Community Wiki — MeleeWeaponTrail](https://wiki.unity3d.com/index.php/MeleeWeaponTrail)
- [Unity Discussions — Sword Trail Effect](https://discussions.unity.com/t/how-to-make-sword-trail-effect/241125)
- [Unity Discussions — Sword Attack Trails](https://discussions.unity.com/t/sword-attack-trails/383080)
- [Invector Forum — Particles for Sword](https://invector.proboards.com/thread/2960/particles-sword)

### Method A: Trail Renderer (Simple)

**Setup:**
1. Create empty child on weapon tip
2. Add `TrailRenderer` component

**Trail Renderer Settings:**
| Parameter | Value |
|-----------|-------|
| Time | 0.15–0.3s |
| Min Vertex Distance | 0.01 |
| Width | Curve: 0.15 at start → 0.0 at end |
| Color | HDR white/element color → transparent |
| Material | Additive/Unlit particle shader |
| Emitting | OFF by default, enable during swing |
| Autodestruct | OFF |
| Shadow Casting | OFF |

### Method B: Animation Event Control

```csharp
public class WeaponTrailController : MonoBehaviour
{
    public TrailRenderer trail;
    public ParticleSystem swingParticles; // Optional particle burst

    void Start()
    {
        trail.emitting = false;
    }

    // Called from animation event at swing start frame
    public void StartTrail()
    {
        trail.Clear();
        trail.emitting = true;
        if (swingParticles != null) swingParticles.Play();
    }

    // Called from animation event at swing end frame
    public void StopTrail()
    {
        trail.emitting = false;
        if (swingParticles != null) swingParticles.Stop();
    }
}
```

### Method C: Enhanced Weapon Trail with Color

```csharp
public class ElementalWeaponTrail : MonoBehaviour
{
    public TrailRenderer trail;
    public Material fireTrailMat;
    public Material lightningTrailMat;

    [Header("Fire Settings")]
    public Gradient fireGradient;
    public float fireWidth = 0.2f;

    [Header("Lightning Settings")]
    public Gradient lightningGradient;
    public float lightningWidth = 0.1f;

    public void SetElement(bool isFire)
    {
        if (isFire)
        {
            trail.material = fireTrailMat;
            trail.colorGradient = fireGradient;
            trail.widthMultiplier = fireWidth;
        }
        else
        {
            trail.material = lightningTrailMat;
            trail.colorGradient = lightningGradient;
            trail.widthMultiplier = lightningWidth;
        }
    }
}
```

### Integration with Invector
The Invector forum thread confirms: attach trail effects as children of weapon bones, control via animation events synced to Invector's attack animations. Invector's `vMeleeManager` triggers `OnDamageHit` events where you can spawn hit particles.

---

## 17. Ground Slam Shockwave Effect

**Sources:**
- [GitHub — abitofgamedev/earth_slam_vfx](https://github.com/abitofgamedev/earth_slam_vfx)
- [80.lv — Earth Slam Attack VFX](https://80.lv/articles/creating-earth-slam-attack-vfx-in-unity)
- [Unity Discussions — Shockwave Particle Effect](https://discussions.unity.com/t/shockwave-particle-effect/404423)

### Shockwave Ring Particle System

**Main Module:**
| Parameter | Value |
|-----------|-------|
| Duration | 0.5s |
| Looping | OFF |
| Start Lifetime | 0.5–1.0 |
| Start Speed | 0 |
| Start Size | 0.5 |
| Play On Awake | OFF |

**Emission:**
- Burst: 1 particle (the ring)

**Shape:**
- Shape: Sphere, Radius: 0 (single point)

**Size over Lifetime:**
- Curve: 0 → 10–15 (expanding ring)

**Color over Lifetime:**
- White (full alpha) → Transparent over lifetime

**Renderer:**
- Render Mode: Billboard or Mesh (torus)
- Material: Additive ring texture

### Earth Slam VFX Components
1. **Surface Cracks**: Depth mask + crack textures, animated in stages
2. **Rock Emergence**: Mesh particles rising from ground with physics
3. **Dust Cloud**: Cone-shaped particle burst
4. **Shockwave Ring**: Expanding torus/ring
5. **Debris**: Small rock meshes with gravity + plane collision

### Shockwave Shader (Distortion)

```csharp
// Script to trigger expanding shockwave with screen distortion
public class ShockwaveEffect : MonoBehaviour
{
    public Material shockwaveMaterial;
    public float maxRadius = 1f;
    public float speed = 2f;
    public float distortionStrength = 0.1f;

    private float currentRadius;
    private bool isPlaying;

    public void TriggerShockwave(Vector3 worldPosition)
    {
        isPlaying = true;
        currentRadius = 0;
        Vector3 screenPos = Camera.main.WorldToViewportPoint(worldPosition);
        shockwaveMaterial.SetVector("_Center", new Vector4(screenPos.x, screenPos.y, 0, 0));
    }

    void Update()
    {
        if (!isPlaying) return;

        currentRadius += speed * Time.deltaTime;
        shockwaveMaterial.SetFloat("_Radius", currentRadius);
        shockwaveMaterial.SetFloat("_Distortion", distortionStrength * (1f - currentRadius / maxRadius));

        if (currentRadius >= maxRadius)
        {
            isPlaying = false;
            shockwaveMaterial.SetFloat("_Distortion", 0);
        }
    }
}
```

---

## 18. Elemental Aura Effect (Shader)

**Sources:**
- [ArmanDoesStuff — Lightning Aura](https://www.armandoesstuff.com/tutorial/lightning-shock-aura-effect)
- [Medium — Anime Aura Shader](https://shahriyarshahrabi.medium.com/anime-aura-shader-in-unity-be0a9ebd3353)
- [Asset Store — Character Auras (15 types)](https://unityunreal.com/unity-assets-free-download-2/vfx-particles/4888-character-auras.html)
- [Asset Store — Stylized Fire Flames Shader Graph](https://assetstore.unity.com/packages/vfx/shaders/stylized-fire-flames-vfx-shader-graph-urp-215912)

### Fire Aura (Particle System Approach)

**Configuration:**
| Parameter | Fire Aura | Notes |
|-----------|-----------|-------|
| Shape | Sphere (around character) | Radius: 0.8–1.2 |
| Emission Rate | 30–50 | Dense flame look |
| Start Speed | 1–3 (upward) | Flames rise |
| Start Size | 0.3–0.6 | |
| Color over Lifetime | Yellow → Orange → Red → Transparent | |
| Noise | Strength 1.5, Freq 3 | Flame flicker |
| Gravity | -0.5 | Upward pull |

**Parent to character root. Enable/disable on element switch.**

### Lightning Aura (ShaderGraph Approach)

**Unlit ShaderGraph Setup:**
1. Surface: Transparent, Additive blend
2. Shadows: OFF
3. `Time` → `Tiling and Offset` (scroll bolt texture)
4. `Sample Texture 2D` (bolt/arc texture)
5. `Step` (threshold for bolt visibility, animated)
6. Multiply by HDR Cyan color
7. Output to Base Color + Alpha

**VFX Graph Layer:**
- Spawn bolt meshes on sphere around character
- Sample lifetime curve for fade in/out
- Orient toward center
- Add spark particles at random positions

### Switching Between Elements

```csharp
public class ElementAuraController : MonoBehaviour
{
    [Header("Fire Aura")]
    public ParticleSystem fireAura;
    public Light fireLight;

    [Header("Lightning Aura")]
    public ParticleSystem lightningAura;
    public Light lightningLight;

    [Header("Transition")]
    public float transitionDuration = 0.3f;

    public void SetElement(bool isFire)
    {
        if (isFire)
        {
            StartCoroutine(TransitionAura(lightningAura, fireAura, lightningLight, fireLight));
        }
        else
        {
            StartCoroutine(TransitionAura(fireAura, lightningAura, fireLight, lightningLight));
        }
    }

    System.Collections.IEnumerator TransitionAura(
        ParticleSystem fadeOut, ParticleSystem fadeIn,
        Light lightOut, Light lightIn)
    {
        // Fade out current
        var emOut = fadeOut.emission;
        float startRate = emOut.rateOverTime.constant;
        float timer = 0;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            emOut.rateOverTime = Mathf.Lerp(startRate, 0, t);
            lightOut.intensity = Mathf.Lerp(1, 0, t);
            yield return null;
        }

        fadeOut.Stop();
        emOut.rateOverTime = startRate;

        // Fade in new
        fadeIn.Play();
        var emIn = fadeIn.emission;
        float targetRate = emIn.rateOverTime.constant;
        emIn.rateOverTime = 0;
        timer = 0;

        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            emIn.rateOverTime = Mathf.Lerp(0, targetRate, t);
            lightIn.intensity = Mathf.Lerp(0, 1, t);
            yield return null;
        }
    }
}
```

---

## 19. UI Animation with DOTween

**Sources:**
- [DOTween Documentation](https://dotween.demigiant.com/documentation.php)
- [DeepWiki — DOTween UI Animations](https://deepwiki.com/Demigiant/dotween/3.2-ui-component-animations)
- [Medium — DOTween Basics (Faruk Yolcu)](https://medium.com/@farukyolcu/unity-dotween-basics-part-1-93df8504eaba)

### Installation
1. Import DOTween from Asset Store (free version) or via Package Manager
2. Tools → Demigiant → DOTween Utility Panel → Setup DOTween

### Complete API Reference for Game UI

**Transform Animations:**
```csharp
// Scale
transform.DOScale(2f, 1f);                          // Uniform scale
transform.DOScale(new Vector3(2, 3, 1), 1f);        // Per-axis
transform.DOScaleX(1.5f, 0.5f);                     // X only

// Punch (spring back to original)
transform.DOPunchScale(new Vector3(0.2f, 0.2f, 0), 0.5f, 5, 1f);
// params: punch amount, duration, vibrato, elasticity (0-1)

transform.DOPunchPosition(Vector3.up * 5f, 0.5f, 10, 1f);
transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.3f, 10, 1f);

// Shake (random vibrations)
transform.DOShakePosition(0.5f, 0.5f, 10, 90f);
// params: duration, strength, vibrato, randomness (0-180)

transform.DOShakeScale(0.5f, 0.3f, 10, 90f);
transform.DOShakeRotation(0.3f, 15f, 10, 90f, true);
```

**UI Image Animations:**
```csharp
image.DOColor(Color.yellow, 1f);                     // Color change
image.DOFade(0f, 1f);                                // Fade out
image.DOFillAmount(1f, 2f);                          // Fill animation
```

**UI Text Animations:**
```csharp
textMesh.DOColor(Color.red, 0.5f);
textMesh.DOFade(0f, 1f);
textMesh.DOFontSize(48, 0.5f);
textMesh.DOText("WAVE COMPLETE!", 1f);               // Typewriter effect
```

**RectTransform Animations:**
```csharp
rectTransform.DOAnchorPos(new Vector2(100, 0), 0.5f);
rectTransform.DOSizeDelta(new Vector2(200, 50), 0.3f);
rectTransform.DOPunchAnchorPos(Vector2.up * 20f, 0.3f, 5, 1f);
rectTransform.DOShakeAnchorPos(0.3f, 10f, 10, 90f);
```

**CanvasGroup:**
```csharp
canvasGroup.DOFade(0f, 1f);                          // Fade entire group
```

**Slider:**
```csharp
slider.DOValue(0.5f, 1f);                            // Animate slider value
```

### Sequences (Chained Animations)
```csharp
Sequence seq = DOTween.Sequence();
seq.Append(transform.DOMoveX(5f, 1f));               // Step 1
seq.Append(transform.DOScale(2f, 0.5f));             // Step 2
seq.Join(transform.DORotate(Vector3.up * 180, 0.5f)); // Simultaneous with step 2
seq.Insert(0.5f, image.DOFade(1f, 0.5f));            // At 0.5s mark
seq.AppendInterval(1f);                               // 1s pause
seq.AppendCallback(() => Debug.Log("Done"));          // Callback
```

### Practical Game UI Patterns

**Health Bar Hit Feedback:**
```csharp
public void OnDamage()
{
    healthSlider.DOValue(newHealth, 0.3f).SetEase(Ease.OutQuad);
    healthFill.DOColor(Color.white, 0.05f)
        .OnComplete(() => healthFill.DOColor(GetHealthColor(), 0.2f));
    healthBar.DOPunchScale(new Vector3(0.05f, 0.1f, 0), 0.2f, 5, 0.5f);
}
```

**Wave Announcement:**
```csharp
public void ShowWaveAnnouncement(string text)
{
    waveText.text = text;
    Sequence seq = DOTween.Sequence();
    seq.Append(waveText.rectTransform.DOScale(1.5f, 0.3f).SetEase(Ease.OutBack));
    seq.Join(waveText.DOFade(1f, 0.2f));
    seq.AppendInterval(1.5f);
    seq.Append(waveText.DOFade(0f, 0.5f));
    seq.Join(waveText.rectTransform.DOScale(0.5f, 0.5f));
}
```

**Ability Ready Flash:**
```csharp
public void FlashAbilityReady(Image icon)
{
    icon.DOColor(Color.white, 0.1f)
        .SetLoops(4, LoopType.Yoyo)
        .OnComplete(() => icon.color = Color.white);
    icon.rectTransform.DOPunchScale(Vector3.one * 0.3f, 0.5f, 3, 0.5f);
}
```

**Damage Number Pop:**
```csharp
public void AnimateDamageNumber(RectTransform numberRect, CanvasGroup group)
{
    Sequence seq = DOTween.Sequence();
    seq.Append(numberRect.DOScale(1.5f, 0.1f).SetEase(Ease.OutBack));
    seq.Append(numberRect.DOScale(1f, 0.2f));
    seq.Join(numberRect.DOAnchorPos(numberRect.anchoredPosition + Vector2.up * 50, 0.8f)
        .SetEase(Ease.OutQuad));
    seq.Insert(0.5f, group.DOFade(0f, 0.3f));
}
```

### Key Settings
```csharp
// Easing: Ease.OutBack, Ease.InOutQuad, Ease.OutBounce, Ease.OutElastic
// Loops: SetLoops(-1, LoopType.Yoyo) for infinite ping-pong
// Unscaled time (for slow-mo): .SetUpdate(true)
// Kill on destroy: DOTween.Kill(transform)
// Pooling: DOTween.SetTweensCapacity(500, 50) in Init
```

---

## 20. Game Feel / Juice — Master Reference

**Sources:**
- [Jason Tu / Nucleartide — 3 Juice Techniques](https://nucleartide.com/juice-techniques)
- [GameDev Academy — Game Feel Tutorial](https://gamedevacademy.org/game-feel-tutorial/)
- [GameAnalytics — Squeezing Juice](https://www.gameanalytics.com/blog/squeezing-more-juice-out-of-your-game-design)
- [Feel Framework](https://feel.moremountains.com/)

### The 5 Principles of Game Feel

1. **Emphasize Success** — confetti, sounds, brief pauses (hitstop)
2. **Touch Emotions** — slow-mo, dramatic camera, ambient audio
3. **Add Randomness** — varied sounds, unpredictable particles, AI variety
4. **Show Permanence** — debris, corpses, environmental destruction
5. **Enhance Control** — responsive input, wall interactions, coyote time

### Complete "Juice Stack" for Combat Actions

**On Player Attack Swing:**
1. Weapon trail (TrailRenderer)
2. Swing sound effect (randomized from 3-4 variants)
3. Slight camera zoom forward
4. Brief animation speed-up at start (anticipation)

**On Hit Connect:**
1. Hit particles (blood/sparks at contact point)
2. Screen shake (Cinemachine Impulse)
3. Hitstop/freeze frame (0.05–0.1s)
4. Hit sound effect
5. Damage number popup
6. Enemy flash white (material swap for 1 frame)
7. Chromatic aberration pulse
8. Controller rumble (if supported)

**On Enemy Death:**
1. Death particles (explosion burst)
2. Ragdoll or death animation
3. Camera zoom/slow-mo (brief, 0.2s)
4. Death sound
5. Adrenaline meter increase
6. Score/XP popup

**On Element Switch:**
1. UI indicator transition
2. Aura particle swap
3. Weapon trail color change
4. Sound effect (whoosh + element)
5. Brief screen flash in element color

**On Ultimate Activation:**
1. Time slow (0.3x for 8 seconds)
2. Heavy screen shake
3. Full-screen flash
4. Dramatic sound effect
5. Aura intensifies
6. Post-processing shift (bloom up, saturation boost)
7. Chromatic aberration burst

### Hitstop (Freeze Frame) Implementation

```csharp
using UnityEngine;
using System.Collections;

public class HitStop : MonoBehaviour
{
    private bool isWaiting = false;

    public void Freeze(float duration)
    {
        if (isWaiting) return;
        StartCoroutine(DoFreeze(duration));
    }

    IEnumerator DoFreeze(float duration)
    {
        isWaiting = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(duration);

        Time.timeScale = originalTimeScale;
        isWaiting = false;
    }
}
```

### Enemy Flash White on Hit

```csharp
public class EnemyHitFlash : MonoBehaviour
{
    public Renderer[] renderers;
    public Material flashMaterial; // Pure white unlit material
    private Material[] originalMaterials;

    void Start()
    {
        originalMaterials = new Material[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].material;
    }

    public void Flash(float duration = 0.05f)
    {
        StartCoroutine(DoFlash(duration));
    }

    System.Collections.IEnumerator DoFlash(float duration)
    {
        foreach (var r in renderers)
            r.material = flashMaterial;

        yield return new WaitForSeconds(duration);

        for (int i = 0; i < renderers.Length; i++)
            renderers[i].material = originalMaterials[i];
    }
}
```

### The 3 Core Juice Techniques (Jason Tu)

1. **Sound Effects** — Biggest ROI. Use freesound.org. Trigger AudioSource on game actions.
2. **Easing Curve Animations** — Squash and stretch via easing functions (see easings.net). DOTween handles this.
3. **Particle Systems** — Expanding rings, spark trails, dust clouds for every impact and motion.

**Layer all three** for maximum impact. A single action with sound + easing + particles is exponentially more satisfying than any one alone.

---

## 21. Custom Post Processing in URP (Full Code)

**Source:** [Febucci — Custom Post Processing URP Guide](https://blog.febucci.com/2022/05/custom-post-processing-in-urp/)

### Complete 5-File Implementation

**File 1: VolumeComponent** (`CustomEffectComponent.cs`)
```csharp
using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/CustomEffectComponent",
    typeof(UniversalRenderPipeline))]
public class CustomEffectComponent : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity =
        new ClampedFloatParameter(value: 0, min: 0, max: 1, overrideState: true);
    public NoInterpColorParameter overlayColor = new NoInterpColorParameter(Color.cyan);

    public bool IsActive() => intensity.value > 0;
    public bool IsTileCompatible() => true;
}
```

**File 2: Shader** (`CustomEffect.shader`)
```hlsl
Shader "Hidden/Custom/CustomEffect"
{
    Properties { _MainTex ("Main Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #pragma vertex vert
            #pragma fragment frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Intensity;
            float4 _OverlayColor;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float2 uv : TEXCOORD0; float4 vertex : SV_POSITION; UNITY_VERTEX_OUTPUT_STEREO };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                VertexPositionInputs vInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.vertex = vInput.positionCS;
                output.uv = input.uv;
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return lerp(color, _OverlayColor, _Intensity);
            }
            ENDHLSL
        }
    }
}
```

**File 3: Renderer Feature** (`CustomPostProcessRenderer.cs`)
```csharp
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class CustomPostProcessRenderer : ScriptableRendererFeature
{
    CustomPostProcessPass pass;
    public override void Create() { pass = new CustomPostProcessPass(); }
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData data)
    {
        renderer.EnqueuePass(pass);
    }
}
```

**File 4: Render Pass** (`CustomPostProcessPass.cs`)
```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class CustomPostProcessPass : ScriptableRenderPass
{
    RenderTargetIdentifier source;
    RenderTargetIdentifier destinationA, destinationB, latestDest;
    readonly int tmpRTIdA = Shader.PropertyToID("_TempRT");
    readonly int tmpRTIdB = Shader.PropertyToID("_TempRTB");

    public CustomPostProcessPass()
    {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var desc = renderingData.cameraData.cameraTargetDescriptor;
        desc.depthBufferBits = 0;
        source = renderingData.cameraData.renderer.cameraColorTarget;
        cmd.GetTemporaryRT(tmpRTIdA, desc, FilterMode.Bilinear);
        destinationA = new RenderTargetIdentifier(tmpRTIdA);
        cmd.GetTemporaryRT(tmpRTIdB, desc, FilterMode.Bilinear);
        destinationB = new RenderTargetIdentifier(tmpRTIdB);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.isSceneViewCamera) return;
        var materials = CustomPostProcessingMaterials.Instance;
        if (materials == null) return;

        CommandBuffer cmd = CommandBufferPool.Get("Custom Post Processing");
        var stack = VolumeManager.instance.stack;

        void BlitTo(Material mat, int pass = 0)
        {
            var first = latestDest;
            var last = first == destinationA ? destinationB : destinationA;
            Blit(cmd, first, last, mat, pass);
            latestDest = last;
        }

        latestDest = source;

        var customEffect = stack.GetComponent<CustomEffectComponent>();
        if (customEffect.IsActive())
        {
            var mat = materials.customEffect;
            mat.SetFloat("_Intensity", customEffect.intensity.value);
            mat.SetColor("_OverlayColor", customEffect.overlayColor.value);
            BlitTo(mat);
        }

        Blit(cmd, latestDest, source);
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(tmpRTIdA);
        cmd.ReleaseTemporaryRT(tmpRTIdB);
    }
}
```

**File 5: Materials Manager** (`CustomPostProcessingMaterials.cs`)
```csharp
using UnityEngine;

[System.Serializable, CreateAssetMenu(fileName = "CustomPostProcessingMaterials",
    menuName = "Game/CustomPostProcessingMaterials")]
public class CustomPostProcessingMaterials : ScriptableObject
{
    public Material customEffect;
    static CustomPostProcessingMaterials _instance;
    public static CustomPostProcessingMaterials Instance
    {
        get
        {
            if (_instance != null) return _instance;
            _instance = Resources.Load("CustomPostProcessingMaterials") as CustomPostProcessingMaterials;
            return _instance;
        }
    }
}
```

**Setup Steps:**
1. Create ScriptableObject: Right-click → Create → Game → CustomPostProcessingMaterials
2. Create Material with "Hidden/Custom/CustomEffect" shader
3. Assign material to ScriptableObject
4. Save ScriptableObject in `Assets/Resources/`
5. Add CustomPostProcessRenderer to URP Renderer asset
6. Add Volume with CustomEffectComponent override
7. Adjust intensity to test

---

## 22. Dissolve + Glow Edge Shader (URP)

**Source:** [Daniel Ilett — Dissolve Effect](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/)

### Properties
| Name | Type | Default |
|------|------|---------|
| Albedo | Color | White |
| Noise Scale | Float | 10 |
| Noise Strength | Float | 0.5 |
| Cutoff Height | Float | 0 |
| Edge Width | Float | 0.05 |
| Edge Color | Color (HDR) | Orange, Intensity 3 |

### Node Graph
1. `Simple Noise` (Scale = Noise Scale) → `Remap` (0,1 → -NoiseStrength, +NoiseStrength)
2. `Position` (World) → `Split` → Y component
3. Y + remapped noise = threshold value
4. `Step` (Cutoff Height, threshold) → `Alpha Clip Threshold` (0.5) — clips pixels below height
5. Y + Edge Width → second `Step` (Cutoff Height, threshold+width)
6. Second Step - First Step = edge band mask
7. Edge band x Edge Color (HDR) → `Emission` output

### Runtime Script
```csharp
material.SetFloat("_CutoffHeight", Mathf.Sin(Time.time) * 2f);
```

---

## 23. Energy Shield / Hologram Shader (URP)

**Source:** [Daniel Ilett — Energy Shield](https://danielilett.com/2023-02-09-tut6-3-energy-shield/)

### 9 Modular Components (Shader Keywords)

1. **PBR Base** — Lit, Transparent surface
2. **Edge Glow** — UV threshold with X/Y thickness; prominent mesh edge highlighting
3. **Intersection Glow** — Screen depth comparison; objects passing through shield glow
4. **Collision Ripples** — Script-driven UV distance ripples from impact points (requires MeshCollider)
5. **Surface Animation** — Scrolling noise pulse for energy feel
6. **Hexagon Pattern** — Texture-based hex overlay with heightmap
7. **Large Scanlines** — Sweeping light band
8. **Small Scanlines** — Detail pattern with speed control
9. **Transparency** — Overall alpha control

### Requirements
- URP 12+
- Shader Keywords (Local scope)
- `_CameraDepthTexture` enabled for intersection detection
- MeshCollider on shield for ripple hit UVs

---

## 24. Feel Framework — 150+ Feedback Reference

**Source:** [Feel — More Mountains](https://feel.moremountains.com/)

### Key Feedback Types for The Scorpion

**Combat:**
- Camera Shake (duration, amplitude, frequency)
- Freeze Frame (brief time pause)
- Chromatic Aberration (pulse intensity over time)
- Bloom (flash on hit)
- Vignette (damage indicator)
- Post Exposure flash
- Particle Play/Instantiate
- Material color flicker (enemy flash)
- Time Scale modifier (slow-mo ultimate)

**UI:**
- CanvasGroup alpha fade
- Image fill amount
- Image color change
- RectTransform size/position/anchor
- Text content/color/size
- TMP character spacing
- Floating text spawner

**Audio:**
- AudioSource play with pitch/volume variation
- Sound manager integration
- Audio filter effects (distortion, echo)

**Transform:**
- Position/Rotation/Scale shake
- Punch animations
- Spring-based movement
- Squash and stretch

**All feedbacks** support:
- Duration control
- Timing curves
- Delay and cool-down
- Channel filtering
- Runtime preview in Inspector

---

## QUICK REFERENCE: What To Implement for The Scorpion MVP

### Priority 1 (Essential — Do First)
- [x] Health Bar (Slider-based, Section 7)
- [x] Adrenaline Meter (Section 12)
- [x] Element Indicator (Section 10)
- [x] Wave Counter (Section 11)
- [x] Cooldown Icons (Section 8)
- [x] Screen Shake (Cinemachine Impulse, Section 5)
- [x] Slow Motion (Section 13)

### Priority 2 (High Impact — Do Next)
- [x] Damage Numbers (Section 9)
- [x] Hit Splash Particles (Section 15)
- [x] Weapon Trail (Section 16)
- [x] Hitstop on Hit (Section 20)
- [x] Post Processing Atmosphere (Section 6)
- [x] Chromatic Aberration Hit (Section 14)

### Priority 3 (Polish — If Time Permits)
- [x] Fire Aura Particles (Sections 1, 18)
- [x] Lightning Aura (Sections 2, 18)
- [x] Ground Slam Shockwave (Section 17)
- [x] DOTween UI Animations (Section 19)
- [x] Shader Graph Glow (Section 3)
- [x] VFX Graph Slash (Section 4)
- [x] Enemy Flash White (Section 20)

---

## ALL SOURCES

1. [GameDev Academy — Fire Particle Tutorial](https://gamedevacademy.org/fire-particle-unity-tutorial/)
2. [Unity Learn — Playing with Fire: Particle Systems](https://learn.unity.com/tutorial/playing-with-fire-what-are-particle-systems)
3. [80.lv — Magic Fire Effect Breakdown](https://80.lv/articles/breakdown-magic-fire-effect-in-unity)
4. [ArmanDoesStuff — Lightning/Shock Aura Effect](https://www.armandoesstuff.com/tutorial/lightning-shock-aura-effect)
5. [Gabriel Aguiar — Electric Arc VFX Graph](https://www.artstation.com/artwork/nELXe6)
6. [Blender.fi — Electric Arc Tutorial](https://blender.fi/2022/05/20/unity-vfx-graph-electric-arc-tutorial/)
7. [Keijiro SpektrLightning (GitHub)](https://github.com/keijiro/SpektrLightning)
8. [Daniel Ilett — Glitter/Glow Shader Graph](https://danielilett.com/2021-11-06-tut5-19-glitter/)
9. [Daniel Ilett — Dissolve + Glow Edge](https://danielilett.com/2020-04-15-tut5-4-urp-dissolve/)
10. [Daniel Ilett — Energy Shield Hologram](https://danielilett.com/2023-02-09-tut6-3-energy-shield/)
11. [z4gon Ground Slash VFX (GitHub)](https://github.com/z4gon/ground-slash-vfx-unity)
12. [Gabriel Aguiar — Sword Slash VFX](https://www.artstation.com/artwork/G82XxB)
13. [Unity Docs — Cinemachine Impulse 3.1](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineImpulse.html)
14. [Unity Docs — Impulse Source 2.3](https://docs.unity3d.com/Packages/com.unity.cinemachine@2.3/manual/CinemachineImpulseSource.html)
15. [Febucci — Custom Post Processing URP](https://blog.febucci.com/2022/05/custom-post-processing-in-urp/)
16. [Unity Docs — Post Processing in URP](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/post-processing-in-urp.html)
17. [VionixStudio — Health Bar Tutorial](https://vionixstudio.com/2022/02/07/how-to-make-a-health-bar-in-unity/)
18. [Code Monkey — Damage Popup Text](https://unitycodemonkey.com/video.php?v=iD1_JczQcFY)
19. [GitHub — unity-easy-damage-numbers](https://github.com/bryjch/unity-easy-damage-numbers)
20. [Medium — Wave System UI (Derek Anderson)](https://medium.com/@derekanderson-dev/ui-wave-system-unity-developer-8daf51db522f)
21. [Medium — Wave System (Brian Stong)](https://medium.com/@stonger44/creating-and-enemy-wave-system-in-unity-a2054c1af9ec)
22. [Unity Discussions — Adrenaline Meter](https://discussions.unity.com/t/how-to-make-an-adrenaline-meter-to-execute-special-moves-in-unity/677717)
23. [SharpCoder — Slow Motion Script](https://www.sharpcoderblog.com/blog/unity-3d-slow-motion-effect-script)
24. [Ketra Games — Slow Motion Tutorial](https://www.ketra-games.com/2020/10/slow-motion-effect-unity-game-tutorial.html)
25. [Unity Docs — Chromatic Aberration](https://docs.unity3d.com/560/Documentation/Manual/PostProcessing-ChromaticAberration.html)
26. [Unity Learn — Chromatic Aberration](https://learn.unity.com/tutorial/post-processing-effects-chromatic-aberration)
27. [Medium — Blood Splatter Effects](https://bergstrand-niklas.medium.com/how-to-add-blood-splatter-effects-to-enemies-in-unity-ad1f436cc4f)
28. [Unity Wiki — MeleeWeaponTrail](https://wiki.unity3d.com/index.php/MeleeWeaponTrail)
29. [Invector Forum — Particles for Sword](https://invector.proboards.com/thread/2960/particles-sword)
30. [GitHub — earth_slam_vfx](https://github.com/abitofgamedev/earth_slam_vfx)
31. [DOTween Documentation](https://dotween.demigiant.com/documentation.php)
32. [DeepWiki — DOTween UI Animations](https://deepwiki.com/Demigiant/dotween/3.2-ui-component-animations)
33. [Nucleartide — 3 Juice Techniques](https://nucleartide.com/juice-techniques)
34. [GameDev Academy — Game Feel Tutorial](https://gamedevacademy.org/game-feel-tutorial/)
35. [Feel Framework — More Mountains](https://feel.moremountains.com/)
36. [mob-sakai/UIEffect (GitHub)](https://github.com/mob-sakai/UIEffect)
37. [Asset Store — Stylized Fire Flames Shader Graph](https://assetstore.unity.com/packages/vfx/shaders/stylized-fire-flames-vfx-shader-graph-urp-215912)
