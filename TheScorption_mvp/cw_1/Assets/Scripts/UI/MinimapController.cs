using UnityEngine;
using UnityEngine.UI;

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

            // Border
            var borderGO = new GameObject("Minimap_Border");
            borderGO.transform.SetParent(canvasGO.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(1, 1);
            borderRect.anchorMax = new Vector2(1, 1);
            borderRect.pivot = new Vector2(1, 1);
            borderRect.anchoredPosition = new Vector2(-15, -15);
            borderRect.sizeDelta = new Vector2(uiSize + 4, uiSize + 4);
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

            // Minimap display
            var mapGO = new GameObject("Minimap_Display");
            mapGO.transform.SetParent(canvasGO.transform, false);
            var mapRect = mapGO.AddComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(1, 1);
            mapRect.anchorMax = new Vector2(1, 1);
            mapRect.pivot = new Vector2(1, 1);
            mapRect.anchoredPosition = new Vector2(-17, -17);
            mapRect.sizeDelta = new Vector2(uiSize, uiSize);
            var rawImg = mapGO.AddComponent<RawImage>();
            rawImg.texture = renderTexture;

            // Player indicator dot (center of minimap)
            var dotGO = new GameObject("Minimap_PlayerDot");
            dotGO.transform.SetParent(canvasGO.transform, false);
            var dotRect = dotGO.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(1, 1);
            dotRect.anchorMax = new Vector2(1, 1);
            dotRect.pivot = new Vector2(0.5f, 0.5f);
            dotRect.anchoredPosition = new Vector2(-17 - uiSize / 2, -17 - uiSize / 2);
            dotRect.sizeDelta = new Vector2(8, 8);
            var dotImg = dotGO.AddComponent<Image>();
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

        private void OnDestroy()
        {
            if (renderTexture != null)
                renderTexture.Release();
        }
    }
}
