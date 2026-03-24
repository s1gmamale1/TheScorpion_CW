using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HUDController — binds to player systems and updates all UI elements.
/// Attach to a Canvas. Assign UI references in inspector.
/// 
/// Required UI hierarchy (create in Unity Editor):
/// Canvas
/// ├── HealthBar (Slider)
/// ├── AdrenalineBar (Slider)
/// ├── WaveText (TextMeshProUGUI)
/// ├── ElementIndicator (Image)
/// ├── EnergyBar (Slider)
/// ├── Ability1Icon (Image + CooldownOverlay Image)
/// ├── Ability2Icon (Image + CooldownOverlay Image)
/// ├── ComboText (TextMeshProUGUI)
/// ├── GameOverPanel (Panel, disabled by default)
/// └── VictoryPanel (Panel, disabled by default)
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Health")]
    public Slider healthBar;
    public Image healthFill;

    [Header("Adrenaline")]
    public Slider adrenalineBar;
    public Image adrenalineFill;

    [Header("Wave")]
    public TextMeshProUGUI waveText;

    [Header("Element")]
    public Image elementIndicator;
    public Slider energyBar;
    public Color fireColor = new Color(1f, 0.3f, 0f);
    public Color lightningColor = new Color(0.3f, 0.6f, 1f);

    [Header("Abilities")]
    public Image ability1CooldownOverlay;
    public Image ability2CooldownOverlay;
    public TextMeshProUGUI ability1CooldownText;
    public TextMeshProUGUI ability2CooldownText;

    [Header("Combo")]
    public TextMeshProUGUI comboText;
    public float comboDisplayDuration = 2f;

    [Header("Panels")]
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public GameObject pausePanel;

    // Cached references
    private PlayerHealth playerHealth;
    private UltimateSystem ultimateSystem;
    private ElementSystem elementSystem;
    private PlayerCombat playerCombat;
    private WaveManager waveManager;

    private float comboDisplayTimer;

    void Start()
    {
        // Find player components
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
            ultimateSystem = player.GetComponent<UltimateSystem>();
            elementSystem = player.GetComponent<ElementSystem>();
            playerCombat = player.GetComponent<PlayerCombat>();
        }

        waveManager = FindObjectOfType<WaveManager>();

        // Subscribe to events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHealthBar;
            playerHealth.OnDeath += ShowGameOver;
        }

        if (ultimateSystem != null)
            ultimateSystem.OnAdrenalineChanged += UpdateAdrenalineBar;

        if (elementSystem != null)
        {
            elementSystem.OnElementChanged += UpdateElementIndicator;
            elementSystem.OnEnergyChanged += UpdateEnergyBar;
            elementSystem.OnAbilityCooldownChanged += UpdateAbilityCooldown;
        }

        if (playerCombat != null)
            playerCombat.ComboChanged += UpdateCombo;

        if (waveManager != null)
        {
            waveManager.OnWaveChanged += UpdateWaveText;
            waveManager.OnAllWavesComplete += ShowVictory;
        }

        // Subscribe to game state
        if (GameManager.Instance != null)
            GameManager.Instance.GameStateChanged += OnGameStateChanged;

        // Initialize UI
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (comboText != null) comboText.gameObject.SetActive(false);
    }

    void Update()
    {
        // Combo text fade
        if (comboDisplayTimer > 0f)
        {
            comboDisplayTimer -= Time.deltaTime;
            if (comboDisplayTimer <= 0f && comboText != null)
                comboText.gameObject.SetActive(false);
        }

        // Adrenaline bar glow when full
        if (adrenalineFill != null && ultimateSystem != null)
        {
            if (ultimateSystem.CurrentAdrenaline >= ultimateSystem.maxAdrenaline)
            {
                float pulse = Mathf.PingPong(Time.time * 3f, 1f);
                adrenalineFill.color = Color.Lerp(Color.yellow, Color.white, pulse);
            }
            else
            {
                adrenalineFill.color = Color.yellow;
            }
        }
    }

    #region Event Handlers

    void UpdateHealthBar(float current, float max)
    {
        if (healthBar == null) return;
        healthBar.maxValue = max;
        healthBar.value = current;
    }

    void UpdateAdrenalineBar(float current, float max)
    {
        if (adrenalineBar == null) return;
        adrenalineBar.maxValue = max;
        adrenalineBar.value = current;
    }

    void UpdateElementIndicator(ElementType element)
    {
        if (elementIndicator == null) return;

        switch (element)
        {
            case ElementType.Fire:
                elementIndicator.color = fireColor;
                break;
            case ElementType.Lightning:
                elementIndicator.color = lightningColor;
                break;
        }
    }

    void UpdateEnergyBar(float current, float max)
    {
        if (energyBar == null) return;
        energyBar.maxValue = max;
        energyBar.value = current;
    }

    void UpdateAbilityCooldown(int slot, float remaining)
    {
        if (slot == 1)
        {
            if (ability1CooldownOverlay != null)
                ability1CooldownOverlay.fillAmount = remaining > 0 ? remaining / 15f : 0f;
            if (ability1CooldownText != null)
                ability1CooldownText.text = remaining > 0 ? Mathf.CeilToInt(remaining) + "s" : "";
        }
        else if (slot == 2)
        {
            if (ability2CooldownOverlay != null)
                ability2CooldownOverlay.fillAmount = remaining > 0 ? remaining / 15f : 0f;
            if (ability2CooldownText != null)
                ability2CooldownText.text = remaining > 0 ? Mathf.CeilToInt(remaining) + "s" : "";
        }
    }

    void UpdateWaveText(int current, int total)
    {
        if (waveText != null)
            waveText.text = $"WAVE {current}/{total}";
    }

    void UpdateCombo(int count)
    {
        if (comboText == null) return;

        if (count >= 2)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = $"{count}x COMBO";
            comboDisplayTimer = comboDisplayDuration;

            // Scale punch effect
            comboText.transform.localScale = Vector3.one * 1.3f;
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void ShowVictory()
    {
        if (victoryPanel != null)
            victoryPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGameStateChanged(GameManager.GameState state)
    {
        if (pausePanel != null)
            pausePanel.SetActive(state == GameManager.GameState.Paused);

        if (state == GameManager.GameState.Paused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (state == GameManager.GameState.Playing)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    #endregion

    #region Button Callbacks (Assign in Unity Editor)

    public void OnRestartClicked()
    {
        GameManager.Instance?.RestartLevel();
    }

    public void OnResumeClicked()
    {
        GameManager.Instance?.ResumeGame();
    }

    #endregion

    void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHealthBar;
            playerHealth.OnDeath -= ShowGameOver;
        }
        if (ultimateSystem != null)
            ultimateSystem.OnAdrenalineChanged -= UpdateAdrenalineBar;
        if (elementSystem != null)
        {
            elementSystem.OnElementChanged -= UpdateElementIndicator;
            elementSystem.OnEnergyChanged -= UpdateEnergyBar;
            elementSystem.OnAbilityCooldownChanged -= UpdateAbilityCooldown;
        }
        if (playerCombat != null)
            playerCombat.ComboChanged -= UpdateCombo;
    }
}
