using UnityEngine;
using UnityEngine.UI;
using TheScorpion.Core;
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

        // Settings
        private Text volumeValueText, sensitivityValueText;
        private bool settingsOpenedFromPause;

        private ElementSystem elementSystem;

        private void Start()
        {
            HideInvectorWatermark();
            CreateCanvas();
            CreateWaveAnnouncement();
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
            gameOverPanel.SetActive(state == GameState.GameOver);
            victoryPanel.SetActive(state == GameState.Victory);

            if (state == GameState.GameOver)
            {
                int wave = WaveManager.Instance != null ? WaveManager.Instance.CurrentWave : 0;
                int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
                string time = GameManager.Instance != null ? GameManager.Instance.GetFormattedTime() : "00:00";
                gameOverWaveText.text = $"Wave Reached: {wave}";
                gameOverKillsText.text = $"Enemies Slain: {kills}";
                gameOverTimeText.text = $"Time: {time}";
            }
            else if (state == GameState.Victory)
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

            var bg = gameOverPanel.GetComponent<Image>();
            bg.color = new Color(0.15f, 0, 0, 0.85f);

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

        private GameState lastKnownState = GameState.PreGame;

        private void Update()
        {
            if (elementSystem == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    elementSystem = player.GetComponent<ElementSystem>();
            }

            // Fallback: poll GameManager state in case event didn't fire
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != lastKnownState)
            {
                lastKnownState = GameManager.Instance.CurrentState;
                RefreshPanels();
            }

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
