using UnityEngine;
using UnityEngine.UI;
using TheScorpion.Core;
using TheScorpion.Combat;
using TheScorpion.Enemy;
using TheScorpion.Systems;
using TheScorpion.Player;

namespace TheScorpion.UI
{
    public class ScorpionHUD : MonoBehaviour
    {
        private Canvas canvas;
        private Transform root;

        // Wave announcement
        private Text waveAnnounceLabel, waveAnnounceSubLabel;
        private CanvasGroup waveAnnounceGroup;
        private int lastWave;
        private Coroutine _annCR;

        // Panels
        private GameObject startPanel;
        private GameObject pausePanel;
        private GameObject gameOverPanel;
        private GameObject victoryPanel;
        private GameObject settingsPanel;
        private GameObject controlsSection;

        // Game Over / Victory stats
        private Text gameOverWaveText, gameOverKillsText, gameOverTimeText;
        private Text victoryKillsText, victoryTimeText;
        private CanvasGroup gameOverGroup;
        private Coroutine gameOverFadeCR;

        // Settings
        private Text volumeValueText, sensitivityValueText;
        private bool settingsOpenedFromPause;

        // Gameplay HUD elements
        private Text waveCounterText;
        private Text comboCounterText;
        private Text styleRankText;
        private Text elementIndicatorText;
        private Text ability1CooldownText;
        private Text ability2CooldownText;
        private RectTransform adrenalineFillRect;
        private Image adrenalineFillImage;
        private Text adrenalineReadyText;

        // Potion counter
        private Text potionCountText;
        private PlayerInventory playerInventory;

        // Boss health bar
        private GameObject bossBarContainer;
        private RectTransform bossFillRect;
        private Text bossNameText;
        private Text bossPhaseText;
        private BossController bossController;

        // Combo/style animation state
        private int prevComboCount;
        private StyleRank prevStyleRank = StyleRank.D;
        private Coroutine comboPunchCR;
        private Coroutine stylePunchCR;

        private ElementSystem elementSystem;
        private Player.UltimateSystem ultimateSystem;
        private Combat.DamageInterceptor damageInterceptor;
        private Player.StyleMeter styleMeter;

        private void Start()
        {
            HideInvectorWatermark();
            CreateCanvas();
            CreateWaveAnnouncement();
            CreateGameplayHUD();
            CreateStartScreen();
            CreatePauseMenu();
            CreateSettingsPanel();
            CreateGameOverScreen();
            CreateVictoryScreen();

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            RefreshPanels();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState state)
        {
            RefreshPanels();
        }

        private void RefreshPanels()
        {
            var state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.PreGame;

            // Hide settings when state changes (unless we're staying in same state)
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(false);
            }

            startPanel.SetActive(state == GameState.PreGame);
            pausePanel.SetActive(state == GameState.Paused);
            victoryPanel.SetActive(state == GameState.Victory);

            if (state == GameState.GameOver)
            {
                int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
                int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
                string time = GameManager.Instance != null ? GameManager.Instance.GetFormattedTime() : "00:00";
                gameOverWaveText.text = $"Wave Reached: {wave}";
                gameOverKillsText.text = $"Enemies Slain: {kills}";
                gameOverTimeText.text = $"Time: {time}";

                // Fade in the game over screen
                if (!gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(true);
                    gameOverGroup.alpha = 0f;
                    gameOverGroup.blocksRaycasts = false;
                    if (gameOverFadeCR != null) StopCoroutine(gameOverFadeCR);
                    gameOverFadeCR = StartCoroutine(FadeInGameOver());
                }
            }
            else
            {
                gameOverPanel.SetActive(false);
            }

            if (state == GameState.Victory)
            {
                int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
                string time = GameManager.Instance != null ? GameManager.Instance.GetFormattedTime() : "00:00";
                victoryKillsText.text = $"Enemies Slain: {kills}";
                victoryTimeText.text = $"Time: {time}";
            }
        }

        private void ShowSettings(bool fromPause)
        {
            settingsOpenedFromPause = fromPause;
            startPanel.SetActive(false);
            pausePanel.SetActive(false);
            settingsPanel.SetActive(true);
            if (controlsSection != null)
                controlsSection.SetActive(false); // collapsed by default
        }

        private void HideSettings()
        {
            settingsPanel.SetActive(false);
            if (settingsOpenedFromPause)
                pausePanel.SetActive(true);
            else
                startPanel.SetActive(true);
        }

        private void HideInvectorWatermark()
        {
            var allText = FindObjectsByType<Text>(FindObjectsSortMode.None);
            foreach (var t in allText)
            {
                if (t.transform.IsChildOf(transform)) continue;
                string lower = t.text.ToLower();
                if (lower.Contains("invector") || lower.Contains("melee combat") ||
                    lower.Contains("template") || lower.Contains("v2."))
                    t.gameObject.SetActive(false);
            }
        }

        private void CreateCanvas()
        {
            var go = new GameObject("ScorpionHUD_Canvas");
            go.transform.SetParent(transform);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            root = go.transform;
        }

        // ==================== START SCREEN ====================
        private void CreateStartScreen()
        {
            startPanel = CreatePanel("StartPanel");

            var bg = startPanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);

            MakeText(startPanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(700, 100),
                "THE SCORPION", 72, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

            MakeText(startPanel.transform, "Subtitle",
                new Vector2(0.5f, 0.5f), new Vector2(0, 55), new Vector2(500, 30),
                "Blade of Fire and Lightning", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.7f, 0.7f));

            CreateButton(startPanel.transform, "StartBtn",
                new Vector2(0, -20), new Vector2(260, 55),
                "START GAME", new Color(0.9f, 0.6f, 0.1f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartGame();
                    if (WaveManager.Instance != null)
                        WaveManager.Instance.StartFirstWave();
                });

            CreateButton(startPanel.transform, "SettingsBtn",
                new Vector2(0, -90), new Vector2(260, 55),
                "SETTINGS", new Color(0.35f, 0.35f, 0.4f), () =>
                {
                    ShowSettings(false);
                });

            CreateButton(startPanel.transform, "QuitBtn",
                new Vector2(0, -160), new Vector2(260, 55),
                "QUIT", new Color(0.4f, 0.4f, 0.4f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });
        }

        // ==================== PAUSE MENU ====================
        private void CreatePauseMenu()
        {
            pausePanel = CreatePanel("PausePanel");
            pausePanel.SetActive(false);

            var bg = pausePanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            MakeText(pausePanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(400, 80),
                "PAUSED", 60, TextAnchor.MiddleCenter, Color.white);

            CreateButton(pausePanel.transform, "ResumeBtn",
                new Vector2(0, 30), new Vector2(260, 55),
                "RESUME", new Color(0.2f, 0.7f, 0.3f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.TogglePause();
                });

            CreateButton(pausePanel.transform, "SettingsBtn",
                new Vector2(0, -40), new Vector2(260, 55),
                "SETTINGS", new Color(0.35f, 0.35f, 0.4f), () =>
                {
                    ShowSettings(true);
                });

            CreateButton(pausePanel.transform, "RestartBtn",
                new Vector2(0, -110), new Vector2(260, 55),
                "RESTART", new Color(0.9f, 0.6f, 0.1f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });

            CreateButton(pausePanel.transform, "QuitBtn",
                new Vector2(0, -180), new Vector2(260, 55),
                "QUIT", new Color(0.6f, 0.2f, 0.2f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });
        }

        // ==================== SETTINGS PANEL ====================
        private void CreateSettingsPanel()
        {
            settingsPanel = CreatePanel("SettingsPanel");
            settingsPanel.SetActive(false);

            var bg = settingsPanel.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.05f, 0.1f, 0.92f);

            MakeText(settingsPanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 220), new Vector2(400, 60),
                "SETTINGS", 48, TextAnchor.MiddleCenter, Color.white);

            // --- Music Volume ---
            MakeText(settingsPanel.transform, "VolumeLabel",
                new Vector2(0.5f, 0.5f), new Vector2(-120, 140), new Vector2(200, 30),
                "Music Volume", 20, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f));

            volumeValueText = MakeText(settingsPanel.transform, "VolumeValue",
                new Vector2(0.5f, 0.5f), new Vector2(180, 140), new Vector2(60, 30),
                "100", 20, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f));

            CreateSlider(settingsPanel.transform, "VolumeSlider",
                new Vector2(0, 105), new Vector2(400, 25),
                0f, 1f, AudioListener.volume,
                (val) =>
                {
                    AudioListener.volume = val;
                    if (volumeValueText != null)
                        volumeValueText.text = $"{(int)(val * 100)}";
                });

            // --- Mouse Sensitivity ---
            MakeText(settingsPanel.transform, "SensLabel",
                new Vector2(0.5f, 0.5f), new Vector2(-120, 55), new Vector2(200, 30),
                "Mouse Sensitivity", 20, TextAnchor.MiddleLeft, new Color(0.8f, 0.8f, 0.8f));

            float currentSens = GetCurrentMouseSensitivity();
            sensitivityValueText = MakeText(settingsPanel.transform, "SensValue",
                new Vector2(0.5f, 0.5f), new Vector2(180, 55), new Vector2(60, 30),
                $"{currentSens:F1}", 20, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f));

            CreateSlider(settingsPanel.transform, "SensSlider",
                new Vector2(0, 20), new Vector2(400, 25),
                1f, 30f, currentSens,
                (val) =>
                {
                    SetMouseSensitivity(val);
                    if (sensitivityValueText != null)
                        sensitivityValueText.text = $"{val:F1}";
                });

            // --- Controls Button ---
            CreateButton(settingsPanel.transform, "ControlsBtn",
                new Vector2(0, -40), new Vector2(400, 45),
                "CONTROLS", new Color(0.25f, 0.25f, 0.35f), () =>
                {
                    if (controlsSection != null)
                        controlsSection.SetActive(!controlsSection.activeSelf);
                });

            // --- Controls List (hidden by default) ---
            controlsSection = new GameObject("ControlsSection");
            controlsSection.transform.SetParent(settingsPanel.transform, false);
            var csRect = controlsSection.AddComponent<RectTransform>();
            csRect.anchorMin = csRect.anchorMax = new Vector2(0.5f, 0.5f);
            csRect.anchoredPosition = new Vector2(0, -155);
            csRect.sizeDelta = new Vector2(450, 180);

            // Controls background
            var csBg = controlsSection.AddComponent<Image>();
            csBg.color = new Color(0.08f, 0.08f, 0.15f, 0.9f);

            string controlsList =
                "LMB  —  Melee Attack\n" +
                "WASD  —  Move\n" +
                "Mouse  —  Look Around\n" +
                "Space  —  Jump\n" +
                "Q / E  —  Switch Element\n" +
                "F  —  Ability 1 (AoE)\n" +
                "R  —  Ability 2 (Buff)\n" +
                "C  —  Projectile\n" +
                "V  —  Ultimate\n" +
                "LCtrl  —  Dodge\n" +
                "Esc  —  Pause";

            MakeText(controlsSection.transform, "ControlsList",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 170),
                controlsList, 14, TextAnchor.UpperLeft, new Color(0.7f, 0.7f, 0.7f));

            controlsSection.SetActive(false);

            // --- Back Button ---
            CreateButton(settingsPanel.transform, "BackBtn",
                new Vector2(0, -270), new Vector2(260, 55),
                "BACK", new Color(0.5f, 0.3f, 0.2f), () =>
                {
                    HideSettings();
                });
        }

        // ==================== GAME OVER SCREEN ====================
        private void CreateGameOverScreen()
        {
            gameOverPanel = CreatePanel("GameOverPanel");
            gameOverPanel.SetActive(false);
            gameOverGroup = gameOverPanel.AddComponent<CanvasGroup>();
            gameOverGroup.alpha = 0f;

            // Semi-transparent — game still visible behind
            var bg = gameOverPanel.GetComponent<Image>();
            bg.color = new Color(0.05f, 0, 0, 0.6f);

            MakeText(gameOverPanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(600, 100),
                "WASTED", 80, TextAnchor.MiddleCenter, new Color(0.8f, 0.1f, 0.1f));

            gameOverWaveText = MakeText(gameOverPanel.transform, "Wave",
                new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(400, 35),
                "Wave Reached: 0", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

            gameOverKillsText = MakeText(gameOverPanel.transform, "Kills",
                new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(400, 35),
                "Enemies Slain: 0", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

            gameOverTimeText = MakeText(gameOverPanel.transform, "Time",
                new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(400, 35),
                "Time: 00:00", 24, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

            CreateButton(gameOverPanel.transform, "RestartBtn",
                new Vector2(0, -85), new Vector2(260, 55),
                "RESTART", new Color(0.9f, 0.6f, 0.1f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });

            CreateButton(gameOverPanel.transform, "QuitBtn",
                new Vector2(0, -155), new Vector2(260, 55),
                "QUIT", new Color(0.4f, 0.4f, 0.4f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });
        }

        // ==================== VICTORY SCREEN ====================
        private void CreateVictoryScreen()
        {
            victoryPanel = CreatePanel("VictoryPanel");
            victoryPanel.SetActive(false);

            var bg = victoryPanel.GetComponent<Image>();
            bg.color = new Color(0, 0.05f, 0.1f, 0.85f);

            MakeText(victoryPanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(600, 80),
                "VICTORY", 72, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));

            MakeText(victoryPanel.transform, "Subtitle",
                new Vector2(0.5f, 0.5f), new Vector2(0, 70), new Vector2(500, 30),
                "The arena has been conquered", 20, TextAnchor.MiddleCenter, new Color(0.7f, 0.8f, 0.9f));

            victoryKillsText = MakeText(victoryPanel.transform, "Kills",
                new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(400, 35),
                "Enemies Slain: 0", 24, TextAnchor.MiddleCenter, Color.white);

            victoryTimeText = MakeText(victoryPanel.transform, "Time",
                new Vector2(0.5f, 0.5f), new Vector2(0, -15), new Vector2(400, 35),
                "Time: 00:00", 24, TextAnchor.MiddleCenter, Color.white);

            CreateButton(victoryPanel.transform, "RestartBtn",
                new Vector2(0, -80), new Vector2(260, 55),
                "PLAY AGAIN", new Color(0.2f, 0.7f, 0.3f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });

            CreateButton(victoryPanel.transform, "QuitBtn",
                new Vector2(0, -150), new Vector2(260, 55),
                "QUIT", new Color(0.4f, 0.4f, 0.4f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });
        }

        // ==================== WAVE ANNOUNCEMENT ====================
        private void CreateWaveAnnouncement()
        {
            var annGO = new GameObject("WaveAnnounce");
            annGO.transform.SetParent(root, false);
            var r = annGO.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = new Vector2(0, 60);
            r.sizeDelta = new Vector2(500, 120);
            waveAnnounceGroup = annGO.AddComponent<CanvasGroup>();
            waveAnnounceGroup.alpha = 0f;

            waveAnnounceLabel = MakeText(annGO.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 15), new Vector2(400, 60),
                "WAVE 1", 52, TextAnchor.MiddleCenter, Color.white);

            waveAnnounceSubLabel = MakeText(annGO.transform, "Sub",
                new Vector2(0.5f, 0.5f), new Vector2(0, -25), new Vector2(300, 26),
                "Eliminate all enemies", 15, TextAnchor.MiddleCenter, new Color(0.6f, 0.6f, 0.6f));
        }

        // ==================== GAMEPLAY HUD ====================
        private void CreateGameplayHUD()
        {
            // Wave counter — top center
            waveCounterText = MakeText(root, "WaveCounter",
                new Vector2(0.5f, 1f), new Vector2(0, -15), new Vector2(400, 30),
                "", 18, TextAnchor.MiddleCenter, new Color(0.8f, 0.8f, 0.8f));

            // Element indicator — top center below wave
            elementIndicatorText = MakeText(root, "ElementIndicator",
                new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(200, 26),
                "FIRE", 20, TextAnchor.MiddleCenter, new Color(1f, 0.4f, 0.1f));

            // Style rank — right side
            styleRankText = MakeText(root, "StyleRank",
                new Vector2(1f, 0.5f), new Vector2(-40, 50), new Vector2(80, 70),
                "D", 56, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f));

            // Combo counter — right side below style rank
            comboCounterText = MakeText(root, "ComboCounter",
                new Vector2(1f, 0.5f), new Vector2(-70, -10), new Vector2(200, 40),
                "", 28, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
            comboCounterText.gameObject.SetActive(false);

            // Adrenaline bar — bottom center
            var adrBarContainer = new GameObject("AdrenalineBar");
            adrBarContainer.transform.SetParent(root, false);
            var adrRect = adrBarContainer.AddComponent<RectTransform>();
            adrRect.anchorMin = new Vector2(0.3f, 0f);
            adrRect.anchorMax = new Vector2(0.7f, 0f);
            adrRect.pivot = new Vector2(0.5f, 0f);
            adrRect.anchoredPosition = new Vector2(0, 20);
            adrRect.sizeDelta = new Vector2(0, 14);

            // Adr label
            MakeText(adrBarContainer.transform, "AdrLabel",
                new Vector2(0f, 0.5f), new Vector2(-35, 0), new Vector2(60, 14),
                "ULT", 10, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.2f));

            // Adr background
            var adrBg = new GameObject("AdrBg");
            adrBg.transform.SetParent(adrBarContainer.transform, false);
            var adrBgRect = adrBg.AddComponent<RectTransform>();
            adrBgRect.anchorMin = Vector2.zero;
            adrBgRect.anchorMax = Vector2.one;
            adrBgRect.offsetMin = Vector2.zero;
            adrBgRect.offsetMax = Vector2.zero;
            adrBg.AddComponent<Image>().color = new Color(0.08f, 0.05f, 0.02f, 0.9f);

            // Adr fill
            var adrFill = new GameObject("AdrFill");
            adrFill.transform.SetParent(adrBarContainer.transform, false);
            adrenalineFillRect = adrFill.AddComponent<RectTransform>();
            adrenalineFillRect.anchorMin = Vector2.zero;
            adrenalineFillRect.anchorMax = new Vector2(0f, 1f); // starts empty
            adrenalineFillRect.offsetMin = new Vector2(1, 1);
            adrenalineFillRect.offsetMax = new Vector2(-1, -1);
            adrenalineFillImage = adrFill.AddComponent<Image>();
            adrenalineFillImage.color = new Color(0.4f, 0.1f, 0.1f);

            // "ULTIMATE READY" text
            adrenalineReadyText = MakeText(adrBarContainer.transform, "ReadyText",
                new Vector2(0.5f, 0.5f), new Vector2(0, 14), new Vector2(200, 20),
                "ULTIMATE READY [V]", 12, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
            adrenalineReadyText.gameObject.SetActive(false);

            // Ability cooldowns — bottom left above energy bar area
            ability1CooldownText = MakeText(root, "Ability1CD",
                new Vector2(0f, 0f), new Vector2(30, 55), new Vector2(100, 20),
                "F: READY", 12, TextAnchor.MiddleLeft, new Color(0.2f, 1f, 0.4f));

            ability2CooldownText = MakeText(root, "Ability2CD",
                new Vector2(0f, 0f), new Vector2(30, 35), new Vector2(100, 20),
                "R: READY", 12, TextAnchor.MiddleLeft, new Color(0.2f, 1f, 0.4f));

            // Potion counter — bottom left
            potionCountText = MakeText(root, "PotionCount",
                new Vector2(0f, 0f), new Vector2(30, 15), new Vector2(120, 20),
                "[1] Potions: 0", 12, TextAnchor.MiddleLeft, new Color(0.2f, 1f, 0.4f));

            // Boss health bar — top center, hidden by default
            bossBarContainer = new GameObject("BossBar");
            bossBarContainer.transform.SetParent(root, false);
            var bossRect = bossBarContainer.AddComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0.2f, 1f);
            bossRect.anchorMax = new Vector2(0.8f, 1f);
            bossRect.pivot = new Vector2(0.5f, 1f);
            bossRect.anchoredPosition = new Vector2(0, -60);
            bossRect.sizeDelta = new Vector2(0, 20);

            // Boss name
            bossNameText = MakeText(bossBarContainer.transform, "BossName",
                new Vector2(0.5f, 1f), new Vector2(0, 12), new Vector2(400, 20),
                "THE FALLEN GUARDIAN", 16, TextAnchor.MiddleCenter, new Color(0.9f, 0.2f, 0.15f));

            // Phase text
            bossPhaseText = MakeText(bossBarContainer.transform, "BossPhase",
                new Vector2(1f, 1f), new Vector2(-5, 12), new Vector2(120, 20),
                "", 12, TextAnchor.MiddleRight, new Color(0.8f, 0.6f, 0.2f));

            // Bar bg
            var bossBg = new GameObject("BossBg");
            bossBg.transform.SetParent(bossBarContainer.transform, false);
            var bossBgRect = bossBg.AddComponent<RectTransform>();
            bossBgRect.anchorMin = Vector2.zero;
            bossBgRect.anchorMax = Vector2.one;
            bossBgRect.offsetMin = Vector2.zero;
            bossBgRect.offsetMax = Vector2.zero;
            bossBg.AddComponent<Image>().color = new Color(0.1f, 0.02f, 0.02f, 0.9f);

            // Bar fill
            var bossFill = new GameObject("BossFill");
            bossFill.transform.SetParent(bossBarContainer.transform, false);
            bossFillRect = bossFill.AddComponent<RectTransform>();
            bossFillRect.anchorMin = Vector2.zero;
            bossFillRect.anchorMax = Vector2.one;
            bossFillRect.offsetMin = new Vector2(2, 2);
            bossFillRect.offsetMax = new Vector2(-2, -2);
            bossFill.AddComponent<Image>().color = new Color(0.8f, 0.15f, 0.1f);

            bossBarContainer.SetActive(false);
        }

        private GameState lastKnownState = GameState.PreGame;

        private void Update()
        {
            // Find player system references directly by type
            if (elementSystem == null)
                elementSystem = FindAnyObjectByType<ElementSystem>();
            if (ultimateSystem == null)
                ultimateSystem = FindAnyObjectByType<UltimateSystem>();
            if (damageInterceptor == null)
                damageInterceptor = FindAnyObjectByType<DamageInterceptor>();
            if (styleMeter == null)
                styleMeter = FindAnyObjectByType<StyleMeter>();
            if (playerInventory == null)
                playerInventory = FindAnyObjectByType<PlayerInventory>();

            // Press Enter to start game from main menu
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PreGame
                && startPanel != null && startPanel.activeSelf
                && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
            {
                GameManager.Instance.StartGame();
                if (WaveManager.Instance != null)
                    WaveManager.Instance.StartFirstWave();
            }

            // Poll GameManager state
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != lastKnownState)
            {
                lastKnownState = GameManager.Instance.CurrentState;
                RefreshPanels();
            }

            // Wave announcement
            if (WaveManager.Instance != null)
            {
                int w = WaveManager.Instance.CurrentWave;
                int t = WaveManager.Instance.TotalWaves;
                if (w != lastWave && w > 0)
                {
                    lastWave = w;
                    if (_annCR != null) StopCoroutine(_annCR);
                    _annCR = StartCoroutine(AnnounceWave(w, t));
                }
            }

            UpdateGameplayHUD();
        }

        private void UpdateGameplayHUD()
        {
            // Wave counter
            if (waveCounterText != null && WaveManager.Instance != null)
            {
                int w = WaveManager.Instance.CurrentWave;
                int t = WaveManager.Instance.TotalWaves;
                int alive = WaveManager.Instance.EnemiesAlive;
                waveCounterText.text = w > 0 ? $"WAVE {w}/{t}  |  Enemies: {alive}" : "";
            }

            // Adrenaline bar
            if (adrenalineFillRect != null && ultimateSystem != null)
            {
                float fill = ultimateSystem.AdrenalineNormalized;
                adrenalineFillRect.anchorMax = new Vector2(fill, adrenalineFillRect.anchorMax.y);

                if (adrenalineFillImage != null)
                {
                    if (fill < 0.5f)
                        adrenalineFillImage.color = Color.Lerp(new Color(0.4f, 0.1f, 0.1f), new Color(0.9f, 0.5f, 0.1f), fill * 2f);
                    else
                        adrenalineFillImage.color = Color.Lerp(new Color(0.9f, 0.5f, 0.1f), new Color(1f, 0.85f, 0.2f), (fill - 0.5f) * 2f);
                }

                if (adrenalineReadyText != null)
                {
                    bool ready = ultimateSystem.IsUltimateReady;
                    adrenalineReadyText.gameObject.SetActive(ready);
                    if (ready)
                    {
                        float pulse = (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f;
                        adrenalineReadyText.color = new Color(1f, 0.85f, 0.2f, 0.6f + pulse * 0.4f);
                    }
                }
            }

            // Combo counter with punch animation
            if (comboCounterText != null && damageInterceptor != null)
            {
                int combo = damageInterceptor.ComboCounter;
                if (combo >= 3)
                {
                    comboCounterText.gameObject.SetActive(true);
                    bool isFinisher = damageInterceptor.IsComboActive;
                    comboCounterText.text = isFinisher ? $"{combo}x COMBO!" : $"{combo}x COMBO";

                    // Punch on new hit
                    if (combo != prevComboCount && combo > prevComboCount)
                    {
                        if (comboPunchCR != null) StopCoroutine(comboPunchCR);
                        float punchSize = isFinisher ? 1.8f : 1.4f;
                        comboPunchCR = StartCoroutine(PunchScale(comboCounterText.transform, punchSize, 0.2f));
                        // Flash white then back to gold
                        comboCounterText.color = Color.white;
                    }
                    else
                    {
                        // Settle to gold with subtle pulse
                        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f;
                        if (comboPunchCR == null) // don't override punch
                            comboCounterText.transform.localScale = Vector3.one * pulse;
                        comboCounterText.color = Color.Lerp(comboCounterText.color, new Color(1f, 0.85f, 0.2f), 5f * Time.unscaledDeltaTime);
                    }
                }
                else if (prevComboCount >= 3 && combo < 3)
                {
                    // Combo broke — shrink out
                    if (comboPunchCR != null) StopCoroutine(comboPunchCR);
                    comboPunchCR = StartCoroutine(ShrinkOut(comboCounterText));
                }
                prevComboCount = combo;
            }

            // Style rank with punch on rank-up
            if (styleRankText != null && styleMeter != null)
            {
                StyleRank rank = styleMeter.CurrentRank;
                Color rankColor;
                switch (rank)
                {
                    case StyleRank.C: rankColor = new Color(0.4f, 0.7f, 1f); break;
                    case StyleRank.B: rankColor = new Color(0.2f, 1f, 0.4f); break;
                    case StyleRank.A: rankColor = new Color(1f, 0.6f, 0.1f); break;
                    case StyleRank.S: rankColor = new Color(1f, 0.85f, 0.2f); break;
                    default: rankColor = new Color(0.5f, 0.5f, 0.5f); break;
                }

                styleRankText.text = rank.ToString();

                // Punch on rank up
                if (rank != prevStyleRank)
                {
                    if (rank > prevStyleRank)
                    {
                        // Ranked up — big punch + flash white
                        if (stylePunchCR != null) StopCoroutine(stylePunchCR);
                        stylePunchCR = StartCoroutine(PunchScale(styleRankText.transform, 2f, 0.3f));
                        styleRankText.color = Color.white;
                    }
                    else
                    {
                        // Ranked down — quick shrink
                        if (stylePunchCR != null) StopCoroutine(stylePunchCR);
                        stylePunchCR = StartCoroutine(PunchScale(styleRankText.transform, 0.6f, 0.15f));
                    }
                    prevStyleRank = rank;
                }
                else
                {
                    // Settle to rank color
                    styleRankText.color = Color.Lerp(styleRankText.color, rankColor, 4f * Time.unscaledDeltaTime);
                    // S rank gets a glow pulse
                    if (rank == StyleRank.S && stylePunchCR == null)
                    {
                        float glow = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f;
                        styleRankText.transform.localScale = Vector3.one * glow;
                    }
                }
            }

            // Element indicator
            if (elementIndicatorText != null && elementSystem != null)
            {
                var elem = elementSystem.ActiveElement;
                if (elem == ElementType.Fire)
                {
                    elementIndicatorText.text = "FIRE";
                    elementIndicatorText.color = new Color(1f, 0.4f, 0.1f);
                }
                else if (elem == ElementType.Lightning)
                {
                    elementIndicatorText.text = "LIGHTNING";
                    elementIndicatorText.color = new Color(0.3f, 0.7f, 1f);
                }
            }

            // Ability cooldowns
            if (elementSystem != null)
            {
                var data = elementSystem.GetActiveData();
                if (ability1CooldownText != null)
                {
                    float cd1 = elementSystem.Ability1CooldownNormalized;
                    if (cd1 > 0.01f)
                    {
                        float secs = data != null ? cd1 * data.ability1Cooldown : 0;
                        ability1CooldownText.text = $"F: {secs:F0}s";
                        ability1CooldownText.color = new Color(0.6f, 0.6f, 0.6f);
                    }
                    else
                    {
                        ability1CooldownText.text = "F: READY";
                        ability1CooldownText.color = new Color(0.2f, 1f, 0.4f);
                    }
                }
                if (ability2CooldownText != null)
                {
                    float cd2 = elementSystem.Ability2CooldownNormalized;
                    if (cd2 > 0.01f)
                    {
                        float secs = data != null ? cd2 * data.ability2Cooldown : 0;
                        ability2CooldownText.text = $"R: {secs:F0}s";
                        ability2CooldownText.color = new Color(0.6f, 0.6f, 0.6f);
                    }
                    else
                    {
                        ability2CooldownText.text = "R: READY";
                        ability2CooldownText.color = new Color(0.2f, 1f, 0.4f);
                    }
                }
            }

            // Potion counter
            if (potionCountText != null && playerInventory != null)
                potionCountText.text = $"[1] Potions: {playerInventory.HealthPotionCount}";

            // Boss health bar
            if (bossBarContainer != null)
            {
                if (bossController == null)
                    bossController = FindAnyObjectByType<BossController>();

                if (bossController != null && bossController.CurrentPhase != BossController.BossPhase.Dead)
                {
                    bossBarContainer.SetActive(true);
                    var bossHealth = bossController.GetComponent<Invector.vHealthController>();
                    if (bossHealth != null && bossFillRect != null)
                    {
                        float fill = bossHealth.currentHealth / bossHealth.MaxHealth;
                        bossFillRect.anchorMax = new Vector2(Mathf.Clamp01(fill), bossFillRect.anchorMax.y);
                    }
                    if (bossPhaseText != null)
                    {
                        switch (bossController.CurrentPhase)
                        {
                            case BossController.BossPhase.Phase1: bossPhaseText.text = "Phase 1"; break;
                            case BossController.BossPhase.Phase2: bossPhaseText.text = "Phase 2 — Fire"; bossPhaseText.color = new Color(1f, 0.4f, 0.1f); break;
                            case BossController.BossPhase.Phase3: bossPhaseText.text = "Phase 3 — ENRAGED"; bossPhaseText.color = new Color(1f, 0.15f, 0.1f); break;
                        }
                    }
                }
                else
                {
                    bossBarContainer.SetActive(false);
                }
            }
        }

        private System.Collections.IEnumerator AnnounceWave(int w, int t)
        {
            bool isFinal = w >= t;
            waveAnnounceLabel.text = isFinal ? "FINAL WAVE" : $"WAVE {w}";
            waveAnnounceLabel.color = isFinal ? new Color(1f, 0.25f, 0.15f) : Color.white;
            waveAnnounceSubLabel.text = isFinal ? "Defeat the guardian" : "Eliminate all enemies";

            float fade = 0.4f;
            for (float f = 0; f < fade; f += Time.unscaledDeltaTime)
            {
                float p = f / fade;
                waveAnnounceGroup.alpha = p;
                waveAnnounceGroup.transform.localScale = Vector3.one * (1f + 0.15f * (1f - p));
                yield return null;
            }
            waveAnnounceGroup.alpha = 1f;
            waveAnnounceGroup.transform.localScale = Vector3.one;

            yield return new WaitForSecondsRealtime(2.2f);

            for (float f = 0; f < fade; f += Time.unscaledDeltaTime)
            {
                waveAnnounceGroup.alpha = 1f - f / fade;
                yield return null;
            }
            waveAnnounceGroup.alpha = 0f;
        }

        // ==================== GAME OVER FADE ====================
        private System.Collections.IEnumerator FadeInGameOver()
        {
            // Slow fade over 3 seconds
            float duration = 3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                // Ease in — slow start, faster at end
                float alpha = t * t;
                gameOverGroup.alpha = alpha;
                yield return null;
            }

            gameOverGroup.alpha = 1f;
            gameOverGroup.blocksRaycasts = true; // enable buttons after fade completes
        }

        // ==================== UI ANIMATIONS ====================

        /// <summary>
        /// Elastic punch scale — overshoots then bounces back to 1.0.
        /// Used for combo counter hits and style rank changes.
        /// </summary>
        private System.Collections.IEnumerator PunchScale(Transform target, float punchSize, float duration)
        {
            float elapsed = 0f;
            Vector3 baseScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                // Elastic ease out: overshoots then settles
                float elastic = 1f - Mathf.Cos(t * Mathf.PI * 2.5f) * Mathf.Pow(1f - t, 3f);
                float scale = Mathf.LerpUnclamped(punchSize, 1f, elastic);

                target.localScale = baseScale * scale;
                yield return null;
            }

            target.localScale = baseScale;
            // Clear the coroutine reference
            if (target == comboCounterText?.transform) comboPunchCR = null;
            if (target == styleRankText?.transform) stylePunchCR = null;
        }

        /// <summary>
        /// Shrink and fade out — used when combo breaks.
        /// </summary>
        private System.Collections.IEnumerator ShrinkOut(Text text)
        {
            float duration = 0.25f;
            float elapsed = 0f;
            Color startColor = text.color;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                text.transform.localScale = Vector3.one * (1f - t);
                text.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }

            text.gameObject.SetActive(false);
            text.transform.localScale = Vector3.one;
            text.color = new Color(startColor.r, startColor.g, startColor.b, 1f);
            comboPunchCR = null;
        }

        // ==================== MOUSE SENSITIVITY ====================
        private float GetCurrentMouseSensitivity()
        {
            var cam = FindAnyObjectByType<Invector.vCamera.vThirdPersonCamera>();
            if (cam != null && cam.lerpState != null)
                return cam.lerpState.xMouseSensitivity;
            return 3f;
        }

        private void SetMouseSensitivity(float value)
        {
            var cam = FindAnyObjectByType<Invector.vCamera.vThirdPersonCamera>();
            if (cam == null) return;

            // Set on lerpState (the target state that currentState lerps toward)
            if (cam.lerpState != null)
            {
                cam.lerpState.xMouseSensitivity = value;
                cam.lerpState.yMouseSensitivity = value;
            }
            // Also set on currentState for immediate effect
            if (cam.currentState != null)
            {
                cam.currentState.xMouseSensitivity = value;
                cam.currentState.yMouseSensitivity = value;
            }
            // Set on all states in the list so switching states preserves the setting
            if (cam.CameraStateList != null)
            {
                foreach (var state in cam.CameraStateList.tpCameraStates)
                {
                    state.xMouseSensitivity = value;
                    state.yMouseSensitivity = value;
                }
            }
        }

        // ==================== FACTORIES ====================

        private GameObject CreatePanel(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            go.AddComponent<Image>();
            return go;
        }

        private void CreateButton(Transform parent, string name, Vector2 pos, Vector2 size,
            string label, Color bgColor, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = bgColor;

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(
                Mathf.Min(bgColor.r + 0.15f, 1f),
                Mathf.Min(bgColor.g + 0.15f, 1f),
                Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
            colors.pressedColor = new Color(bgColor.r * 0.7f, bgColor.g * 0.7f, bgColor.b * 0.7f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            MakeText(go.transform, "Label",
                new Vector2(0.5f, 0.5f), Vector2.zero, size,
                label, 22, TextAnchor.MiddleCenter, Color.white);
        }

        private Slider CreateSlider(Transform parent, string name, Vector2 pos, Vector2 size,
            float min, float max, float value, UnityEngine.Events.UnityAction<float> onChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;

            // Slider background
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(go.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.AddComponent<CanvasRenderer>();
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            // Fill area
            var fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(go.transform, false);
            var fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillGO.AddComponent<CanvasRenderer>();
            var fillImg = fillGO.AddComponent<Image>();
            fillImg.color = new Color(1f, 0.85f, 0.2f, 1f);

            // Handle area
            var handleAreaGO = new GameObject("Handle Slide Area");
            handleAreaGO.transform.SetParent(go.transform, false);
            var handleAreaRect = handleAreaGO.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            var handleGO = new GameObject("Handle");
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = handleGO.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            handleGO.AddComponent<CanvasRenderer>();
            var handleImg = handleGO.AddComponent<Image>();
            handleImg.color = Color.white;

            // Slider component
            var slider = go.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(onChanged);

            return slider;
        }

        private Text MakeText(Transform parent, string name, Vector2 anchor,
            Vector2 pos, Vector2 size, string text, int fontSize, TextAnchor align, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var r = go.AddComponent<RectTransform>();
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            var t = go.AddComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.fontStyle = FontStyle.Bold;
            var s = go.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.8f);
            s.effectDistance = new Vector2(1, -1);
            return t;
        }
    }
}
