using UnityEngine;
using UnityEngine.UI;

namespace TheScorpion.UI
{
    /// <summary>
    /// Spawns floating damage numbers above enemies when the player deals damage.
    /// Numbers float upward and fade out. Color based on element type.
    /// Call DamagePopup.Spawn() from anywhere to show damage.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        private static Canvas worldCanvas;

        private Text text;
        private RectTransform rect;
        private float lifetime;
        private float timer;
        private Vector3 worldPos;
        private Color startColor;

        public static void Spawn(Vector3 position, int damage, string element = "")
        {
            EnsureCanvas();

            var go = new GameObject("DmgPopup");
            go.transform.SetParent(worldCanvas.transform, false);
            var popup = go.AddComponent<DamagePopup>();
            popup.Init(position, damage, element);
        }

        private static void EnsureCanvas()
        {
            if (worldCanvas != null) return;

            var canvasGO = new GameObject("DamagePopup_Canvas");
            DontDestroyOnLoad(canvasGO);
            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            worldCanvas.sortingOrder = 15;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        private void Init(Vector3 position, int damage, string element)
        {
            worldPos = position + Vector3.up * 2f;
            lifetime = 2f; // slower fade (was 1.2)
            timer = 0f;

            // Pick color based on element
            if (element == "Fire")
                startColor = new Color(1f, 0.4f, 0.1f);
            else if (element == "Lightning")
                startColor = new Color(0.3f, 0.7f, 1f);
            else
                startColor = Color.white;

            // Bigger text
            int fontSize = damage >= 30 ? 48 : damage >= 15 ? 38 : 30;

            rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 40);

            text = gameObject.AddComponent<Text>();
            text.text = damage.ToString();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = startColor;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontStyle = FontStyle.Bold;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            var shadow = gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.9f);
            shadow.effectDistance = new Vector2(1, -1);

            // Random horizontal offset so numbers don't stack
            worldPos += new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
        }

        private void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            float progress = timer / lifetime;

            // Float upward (slower)
            worldPos += Vector3.up * 0.8f * Time.unscaledDeltaTime;

            // Convert world position to screen position
            var cam = UnityEngine.Camera.main;
            if (cam == null) { Destroy(gameObject); return; }

            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            // Behind camera — hide
            if (screenPos.z < 0)
            {
                text.enabled = false;
                return;
            }
            text.enabled = true;

            // Convert screen position to canvas position
            rect.position = screenPos;

            // Scale punch at start, then shrink
            float scale = progress < 0.1f ? Mathf.Lerp(1.3f, 1f, progress / 0.1f) : 1f;
            rect.localScale = Vector3.one * scale;

            // Fade out in last 40%
            float alpha = progress > 0.6f ? Mathf.Lerp(1f, 0f, (progress - 0.6f) / 0.4f) : 1f;
            text.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
        }
    }
}
