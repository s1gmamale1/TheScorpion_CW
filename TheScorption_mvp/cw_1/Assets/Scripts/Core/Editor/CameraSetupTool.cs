using UnityEngine;
using UnityEditor;
using Invector;
using Invector.vCamera;

public class CameraSetupTool
{
    [MenuItem("Tools/Scorpion/Fix Camera Rigidbody")]
    public static void FixCameraRigidbody()
    {
        var camera = Object.FindAnyObjectByType<vThirdPersonCamera>();
        if (camera == null)
        {
            Debug.LogError("[Scorpion] No vThirdPersonCamera found!");
            return;
        }

        // Invector's vThirdPersonCamera.selfRigidbody property tries to AddComponent<Rigidbody>()
        // during Init(). If a Rigidbody already exists on the GameObject (added manually or by
        // a previous setup), AddComponent returns null → NullReferenceException → camera never
        // initializes → doesn't follow player.
        // Fix: remove existing Rigidbody so Invector can add its own.
        var existingRb = camera.GetComponent<Rigidbody>();
        if (existingRb != null)
        {
            Object.DestroyImmediate(existingRb);
            EditorUtility.SetDirty(camera.gameObject);
            Debug.Log("[Scorpion] Removed existing Rigidbody from camera — Invector will add its own on Init()");
        }
        else
        {
            Debug.Log("[Scorpion] No extra Rigidbody found on camera — already clean");
        }
    }

    [MenuItem("Tools/Scorpion/Setup ZZZ Camera Style")]
    public static void SetupZZZCamera()
    {
        var camera = Object.FindAnyObjectByType<vThirdPersonCamera>();
        if (camera == null)
        {
            Debug.LogError("[Scorpion] No vThirdPersonCamera found!");
            return;
        }

        // ── Camera State Values (per-state in the CameraStateList asset) ──
        // ZZZ camera: centered, close, snappy rotation, slight position smoothing
        if (camera.CameraStateList != null && camera.CameraStateList.tpCameraStates != null)
        {
            foreach (var state in camera.CameraStateList.tpCameraStates)
            {
                // Distance — ZZZ is close, over-the-shoulder centered
                state.defaultDistance = 3.2f;
                state.maxDistance = 4.5f;
                state.minDistance = 1.5f;

                // Framing — centered (ZZZ keeps character dead center)
                state.right = 0f;
                state.height = 1.4f;

                // Position follow — how fast camera tracks player movement
                // smooth = distance lerp speed (higher = snappier zoom/distance)
                // smoothDamp = SmoothDamp time for position (lower = tighter follow)
                state.smooth = 15f;
                state.smoothDamp = 0.05f;

                // Mouse sensitivity — ZZZ is responsive, not sluggish
                // Invector defaults ~3-5. Keep it punchy.
                state.xMouseSensitivity = 4f;
                state.yMouseSensitivity = 2.5f;

                // Vertical limits — ZZZ has moderate vertical range
                state.yMinLimit = -20f;
                state.yMaxLimit = 40f;

                // FOV — slightly cinematic
                state.fov = 55f;

                // Culling — wall avoidance
                state.cullingHeight = 0.3f;
                state.cullingMinDist = 0.2f;

                Debug.Log($"[Scorpion] Camera state '{state.Name}' → ZZZ style applied");
            }

            EditorUtility.SetDirty(camera.CameraStateList);
        }

        // ── Component-level camera properties (on vThirdPersonCamera itself) ──
        var camSO = new SerializedObject(camera);

        // Rotation smoothing — THIS is the main "lag" control
        // Quaternion.Lerp(current, target, smoothCameraRotation * dt)
        // Higher = snappier. 20 = near-instant. 8 = cinematic lag.
        // ZZZ feels ~18-22 — snappy but not raw.
        var smoothRotProp = camSO.FindProperty("_smoothCameraRotation");
        if (smoothRotProp != null)
            smoothRotProp.floatValue = 20f;

        // Smooth between camera states (transition speed)
        var smoothBetweenProp = camSO.FindProperty("_smoothBetweenState");
        if (smoothBetweenProp != null)
            smoothBetweenProp.floatValue = 8f;

        // Auto-rotate behind player when no camera input
        // ZZZ does this — camera drifts back behind character
        var autoBehindProp = camSO.FindProperty("autoBehindTarget");
        if (autoBehindProp != null)
            autoBehindProp.boolValue = true;

        // Delay before auto-rotate kicks in (seconds of no camera input)
        var behindDelayProp = camSO.FindProperty("behindTargetDelay");
        if (behindDelayProp != null)
            behindDelayProp.floatValue = 1.5f;

        // Speed of auto-rotation back behind player
        var behindSmoothProp = camSO.FindProperty("behindTargetSmoothRotation");
        if (behindSmoothProp != null)
            behindSmoothProp.floatValue = 3f;

        camSO.ApplyModifiedProperties();

        // FOV on the actual Camera component
        var mainCam = camera.GetComponentInChildren<Camera>();
        if (mainCam != null)
        {
            mainCam.fieldOfView = 55f;
            EditorUtility.SetDirty(mainCam);
        }

        EditorUtility.SetDirty(camera);
        Debug.Log("[Scorpion] ZZZ camera applied: smoothRot=20, xSens=4, ySens=2.5, dist=3.2, FOV=55, autoBehind=ON");
    }
}
