using UnityEngine;
using UnityEngine.UI;

namespace TheScorpion.UI
{
    /// <summary>
    /// Floating health bar above an enemy's head.
    /// Single bar, color-coded by enemy type. Faces camera. Hides on death.
    /// </summary>
    public class EnemyHealthBar : MonoBehaviour
    {
        private Invector.vHealthController healthController;
        private Transform barTransform;
        private Image fillImage;
        private Canvas worldCanvas;
        private float maxHP;
        private Color barColor = new Color(0.8f, 0.15f, 0.1f);

        private void Start()
        {
            healthController = GetComponent<Invector.vHealthController>();
            if (healthController == null) { Destroy(this); return; }

            maxHP = healthController.MaxHealth;
            CreateUI();
        }

        private void CreateUI()
        {
            var canvasGO = new GameObject("EnemyUI");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = Vector3.up * 2.2f;
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            worldCanvas = canvasGO.AddComponent<Canvas>();
            worldCanvas.renderMode = RenderMode.WorldSpace;
            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 12);

            barTransform = canvasGO.transform;

            // Background
            var bgGO = new GameObject("BarBg");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            bgGO.AddComponent<CanvasRenderer>();
            bgGO.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            // Fill
            var fillGO = new GameObject("BarFill");
            fillGO.transform.SetParent(bgGO.transform, false);
            var fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1, 1);
            fillRect.offsetMax = new Vector2(-1, -1);
            fillGO.AddComponent<CanvasRenderer>();
            fillImage = fillGO.AddComponent<Image>();
            fillImage.color = barColor;
        }

        public void SetName(string name)
        {
            // Name is now shown via enemy model color, not text
        }

        public void SetBarColor(Color color)
        {
            barColor = color;
            if (fillImage != null)
                fillImage.color = color;
        }

        /// <summary>
        /// Tints the enemy model to visually distinguish types.
        /// </summary>
        public void SetEnemyTint(Color tint)
        {
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                foreach (var mat in r.materials)
                {
                    mat.color = tint;
                }
            }
        }

        private void LateUpdate()
        {
            if (healthController == null || barTransform == null) return;

            if (fillImage != null)
            {
                float pct = healthController.currentHealth / maxHP;
                var fillRect = fillImage.GetComponent<RectTransform>();
                fillRect.anchorMax = new Vector2(Mathf.Clamp01(pct), 1);
            }

            var cam = UnityEngine.Camera.main;
            if (cam != null)
                barTransform.forward = cam.transform.forward;

            if (healthController.isDead && worldCanvas != null)
                worldCanvas.gameObject.SetActive(false);
        }
    }
}
