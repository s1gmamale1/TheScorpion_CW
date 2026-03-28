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

        // Game Over / Victory stats
        private Text gameOverWaveText, gameOverKillsText, gameOverTimeText;
        private Text victoryKillsText, victoryTimeText;

        private ElementSystem elementSystem;

        private void Start()
        {
            HideInvectorWatermark();
            CreateCanvas();
            CreateWaveAnnouncement();
            CreateStartScreen();
            CreatePauseMenu();
            CreateGameOverScreen();
            CreateVictoryScreen();

            // Listen to state changes
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;

            // Show correct panel for initial state
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
                new Vector2(0, -30), new Vector2(260, 55),
                "START GAME", new Color(0.9f, 0.6f, 0.1f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.StartGame();
                    if (WaveManager.Instance != null)
                        WaveManager.Instance.StartFirstWave();
                });

            CreateButton(startPanel.transform, "QuitBtn",
                new Vector2(0, -100), new Vector2(260, 55),
                "QUIT", new Color(0.4f, 0.4f, 0.4f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                });

            MakeText(startPanel.transform, "Controls",
                new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(600, 120),
                "WASD — Move    Mouse — Look    LMB — Attack    Space — Jump\n" +
                "Q/E — Switch Element    F — Ability 1    R — Ability 2\n" +
                "C — Projectile    V — Ultimate    LCtrl — Dodge    Esc — Pause",
                14, TextAnchor.MiddleCenter, new Color(0.5f, 0.5f, 0.5f));
        }

        // ==================== PAUSE MENU ====================
        private void CreatePauseMenu()
        {
            pausePanel = CreatePanel("PausePanel");
            pausePanel.SetActive(false);

            var bg = pausePanel.GetComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.7f);

            MakeText(pausePanel.transform, "Title",
                new Vector2(0.5f, 0.5f), new Vector2(0, 100), new Vector2(400, 80),
                "PAUSED", 60, TextAnchor.MiddleCenter, Color.white);

            CreateButton(pausePanel.transform, "ResumeBtn",
                new Vector2(0, 10), new Vector2(260, 55),
                "RESUME", new Color(0.2f, 0.7f, 0.3f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.TogglePause();
                });

            CreateButton(pausePanel.transform, "RestartBtn",
                new Vector2(0, -60), new Vector2(260, 55),
                "RESTART", new Color(0.9f, 0.6f, 0.1f), () =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.RestartGame();
                });

            CreateButton(pausePanel.transform, "QuitBtn",
                new Vector2(0, -130), new Vector2(260, 55),
                "QUIT", new Color(0.6f, 0.2f, 0.2f), () =>
                {
                    Application.Quit();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #endif
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
                new Vector2(0.5f, 0.5f), new Vector2(0, 130), new Vector2(500, 80),
                "DEFEATED", 68, TextAnchor.MiddleCenter, new Color(0.8f, 0.15f, 0.1f));

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
                "TRY AGAIN", new Color(0.9f, 0.6f, 0.1f), () =>
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

        private void Update()
        {
            // Lazy find player + create energy bar
            if (elementSystem == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    elementSystem = player.GetComponent<ElementSystem>();
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
