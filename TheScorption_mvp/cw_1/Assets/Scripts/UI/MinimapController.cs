using UnityEngine;
using UnityEngine.UI;
using TheScorpion.Core;

namespace TheScorpion.UI
{
    /// <summary>
    /// Creates a minimap in the top-right corner using a top-down orthographic camera
    /// rendering to a RenderTexture displayed on a RawImage.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float mapHeight = 30f;
        [SerializeField] private float mapSize = 30f; // Ortho size covers arena
        [SerializeField] private int textureSize = 256;
        [SerializeField] private float uiSize = 180f;

        private UnityEngine.Camera minimapCamera;
        private RenderTexture renderTexture;
        private Transform playerTransform;

        private void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;

            CreateMinimapCamera();
            CreateMinimapUI();
        }

        private void CreateMinimapCamera()
        {
            var camGO = new GameObject("MinimapCamera");
            camGO.transform.SetParent(transform);

            minimapCamera = camGO.AddComponent<UnityEngine.Camera>();
            minimapCamera.orthographic = true;
            minimapCamera.orthographicSize = mapSize;
            minimapCamera.transform.position = new Vector3(0, mapHeight, 0);
            minimapCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor = new Color(0.05f, 0.05f, 0.1f, 1f);
            minimapCamera.cullingMask = ~0; // Render everything
            minimapCamera.depth = -10; // Below main camera

            renderTexture = new RenderTexture(textureSize, textureSize, 16);
            minimapCamera.targetTexture = renderTexture;

            // Don't render UI layer on minimap
            minimapCamera.cullingMask &= ~(1 << 5); // Layer 5 = UI
        }

        private void CreateMinimapUI()
        {
            // Find or create canvas
            var canvasGO = new GameObject("Minimap_Canvas");
            canvasGO.transform.SetParent(transform);
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Create round circle sprite for masking
            var circleSprite = CreateCircleSprite(128);

            // Round border
            var borderGO = new GameObject("Minimap_Border");
            borderGO.transform.SetParent(canvasGO.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(1, 1);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.pivot = new Vector2(1, 1);
            borderRect.anchoredPosition = new Vector2(-13, -13);
            borderRect.sizeDelta = new Vector2(uiSize + 8, uiSize + 8);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.sprite = circleSprite;
            borderImg.type = Image.Type.Simple;
            borderImg.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);

            // Mask container — clips the minimap into a circle
            var maskGO = new GameObject("Minimap_Mask");
            maskGO.transform.SetParent(canvasGO.transform, false);
            var maskRect = maskGO.AddComponent<RectTransform>();
            maskRect.anchorMin = new Vector2(1, 1);
            maskRect.anchorMax = new Vector2(1, 1);
            maskRect.pivot = new Vector2(1, 1);
            maskRect.anchoredPosition = new Vector2(-17, -17);
            maskRect.sizeDelta = new Vector2(uiSize, uiSize);
            var maskImg = maskGO.AddComponent<Image>();
            maskImg.sprite = circleSprite;
            maskImg.type = Image.Type.Simple;
            var mask = maskGO.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Minimap display — child of mask so it gets clipped
            var mapGO = new GameObject("Minimap_Display");
            mapGO.transform.SetParent(maskGO.transform, false);
            var mapRect = mapGO.AddComponent<RectTransform>();
            mapRect.anchorMin = Vector2.zero;
            mapRect.anchorMax = Vector2.one;
            mapRect.offsetMin = Vector2.zero;
            mapRect.offsetMax = Vector2.zero;
            var rawImg = mapGO.AddComponent<RawImage>();
            rawImg.texture = renderTexture;

            // Player indicator dot (center of minimap)
            var dotGO = new GameObject("Minimap_PlayerDot");
            dotGO.transform.SetParent(maskGO.transform, false);
            var dotRect = dotGO.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = Vector2.zero;
            dotRect.sizeDelta = new Vector2(8, 8);
            var dotImg = dotGO.AddComponent<Image>();
            dotImg.sprite = circleSprite;
            dotImg.color = new Color(0f, 1f, 0.5f, 1f);
        }

        private void LateUpdate()
        {
            if (minimapCamera == null || playerTransform == null) return;

            // Follow player XZ, stay at fixed height
            minimapCamera.transform.position = new Vector3(
                playerTransform.position.x,
                mapHeight,
                playerTransform.position.z
            );
        }

        private Sprite CreateCircleSprite(int resolution)
        {
            var tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            float center = resolution * 0.5f;
            float radius = center - 1f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                        tex.SetPixel(x, y, Color.white);
                    else if (dist <= radius + 1f)
                        tex.SetPixel(x, y, new Color(1, 1, 1, radius + 1f - dist)); // anti-alias edge
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f));
        }

        private void OnDestroy()
        {
            if (renderTexture != null)
                renderTexture.Release();
        }
    }
}
