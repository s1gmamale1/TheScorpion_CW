using UnityEngine;

/// <summary>
/// CameraController — third-person follow camera with mouse look and combat zoom.
/// Attach to the Main Camera. Assign player transform in inspector.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;

    [Header("Follow")]
    public Vector3 offset = new Vector3(0f, 4f, -6f);
    public float followSpeed = 10f;
    public float rotationSpeed = 3f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -20f;
    public float maxVerticalAngle = 60f;

    [Header("Combat Zoom")]
    public float normalFOV = 60f;
    public float combatFOV = 55f;
    public float ultimateFOV = 70f;
    public float fovLerpSpeed = 5f;

    [Header("Screen Shake")]
    public float shakeDecay = 5f;

    private float yaw;
    private float pitch;
    private Camera cam;
    private float targetFOV;

    // Shake
    private float shakeMagnitude;
    private float shakeTimer;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        targetFOV = normalFOV;

        if (player == null)
        {
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }

        // Initialize rotation from current angles
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameManager.GameState.Paused)
            return;

        HandleMouseLook();
        FollowPlayer();
        HandleFOV();
        HandleShake();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
    }

    void FollowPlayer()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = player.position + rotation * offset;

        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(player.position + Vector3.up * 1.5f); // Look at chest height
    }

    void HandleFOV()
    {
        // Check if ultimate is active for FOV change
        var ultimate = player.GetComponent<UltimateSystem>();
        if (ultimate != null && ultimate.IsUltimateActive)
            targetFOV = ultimateFOV;
        else
            targetFOV = normalFOV;

        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, fovLerpSpeed * Time.deltaTime);
    }

    #region Screen Shake

    /// <summary>
    /// Call this from anywhere: CameraController.Shake(0.3f, 0.2f)
    /// </summary>
    public void TriggerShake(float magnitude, float duration)
    {
        shakeMagnitude = magnitude;
        shakeTimer = duration;
    }

    void HandleShake()
    {
        if (shakeTimer > 0f)
        {
            Vector3 shakeOffset = Random.insideUnitSphere * shakeMagnitude;
            transform.position += shakeOffset;

            shakeTimer -= Time.deltaTime;
            shakeMagnitude = Mathf.Lerp(shakeMagnitude, 0f, shakeDecay * Time.deltaTime);
        }
    }

    /// <summary>
    /// Static helper to shake from anywhere.
    /// Usage: CameraController.Shake(0.2f, 0.3f);
    /// </summary>
    public static void Shake(float magnitude, float duration)
    {
        var cam = Camera.main?.GetComponent<CameraController>();
        cam?.TriggerShake(magnitude, duration);
    }

    #endregion
}
