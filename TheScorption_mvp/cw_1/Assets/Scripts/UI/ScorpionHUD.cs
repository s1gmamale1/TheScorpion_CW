using UnityEngine;
using UnityEngine.UI;
using TheScorpion.Core;
using TheScorpion.Systems;

namespace TheScorpion.UI
{
    /// <summary>
    /// Custom HUD overlay — keeps Invector's HUD for HP/Stamina (it works),
    /// adds our custom elements on top: wave announcement, and future elements.
    /// Built step by step.
    /// </summary>
    public class ScorpionHUD : MonoBehaviour
    {
        private Canvas canvas;
        private Transform root;

        // Wave announcement
        private Text waveAnnounceLabel, waveAnnounceSubLabel;
        private CanvasGroup waveAnnounceGroup;
        private int lastWave;

        private void Start()
        {
            HideInvectorWatermark();
            CreateCanvas();
            CreateWaveAnnouncement();
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

        private Coroutine _annCR;

        private System.Collections.IEnumerator AnnounceWave(int w, int t)
        {
            bool isFinal = w >= t;
            waveAnnounceLabel.text = isFinal ? "FINAL WAVE" : $"WAVE {w}";
            waveAnnounceLabel.color = isFinal ? new Color(1f, 0.25f, 0.15f) : Color.white;
            waveAnnounceSubLabel.text = isFinal ? "Defeat the guardian" : "Eliminate all enemies";

            // Fade in with scale punch
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

            // Fade out
            for (float f = 0; f < fade; f += Time.unscaledDeltaTime)
            {
                waveAnnounceGroup.alpha = 1f - f / fade;
                yield return null;
            }
            waveAnnounceGroup.alpha = 0f;
        }

        // ==================== FACTORY ====================
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
