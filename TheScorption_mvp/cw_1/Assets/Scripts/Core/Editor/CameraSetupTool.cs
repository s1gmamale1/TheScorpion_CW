using UnityEngine;
using UnityEditor;
using Invector;
using Invector.vCamera;

public class CameraSetupTool
{
    [MenuItem("Tools/Scorpion/Setup ZZZ Camera Style")]
    public static void SetupZZZCamera()
    {
        var camera = Object.FindAnyObjectByType<vThirdPersonCamera>();
        if (camera == null)
        {
            Debug.LogError("[Scorpion] No vThirdPersonCamera found!");
            return;
        }

        // Find the camera state list
        var so = new SerializedObject(camera);

        // Adjust main camera properties
        // Invector stores camera states in a list data asset
        // We need to modify the active camera states

        // Direct approach: modify the CameraStateList on the camera
        if (camera.CameraStateList != null && camera.CameraStateList.tpCameraStates != null)
        {
            foreach (var state in camera.CameraStateList.tpCameraStates)
            {
                // ZZZ-inspired: centered, medium distance, cinematic, low mouse sens
                state.defaultDistance = 3.2f;       // Medium-close distance
                state.maxDistance = 4.0f;
                state.minDistance = 2.0f;
                state.right = 0f;                   // Centered — player in middle
                state.height = 1.5f;                // Slightly above shoulder
                state.smooth = 12f;                 // Smooth follow with slight cinematic lag
                state.smoothDamp = 0.1f;            // Slight damping for cinematic feel
                state.xMouseSensitivity = 1.5f;     // Low — camera mostly follows character
                state.yMouseSensitivity = 0.8f;     // Very low vertical — stays behind
                state.yMinLimit = -10f;             // Barely look down
                state.yMaxLimit = 30f;              // Limited look up
                state.fov = 55f;                    // Cinematic FOV
                state.cullingHeight = 0.3f;
                state.cullingMinDist = 0.2f;

                Debug.Log($"[Scorpion] Camera state '{state.Name}' updated to ZZZ centered style");
            }

            EditorUtility.SetDirty(camera.CameraStateList);
        }

        // === ZZZ KEY FEATURE: Auto-rotate camera behind player ===
        // This is the core of ZZZ's camera — when the player stops moving the
        // right stick/mouse, the camera smoothly slerps back behind the character.
        var camSO = new SerializedObject(camera);

        // Enable autoBehindTarget — camera returns behind character automatically
        var autoBehindProp = camSO.FindProperty("autoBehindTarget");
        if (autoBehindProp != null)
            autoBehindProp.boolValue = true;

        // Delay before auto-rotate starts (seconds of no camera input)
        // ZZZ uses ~1.0-1.5s — fast enough to feel responsive, slow enough
        // to let player look around briefly
        var behindDelayProp = camSO.FindProperty("behindTargetDelay");
        if (behindDelayProp != null)
            behindDelayProp.floatValue = 1.2f;

        // Speed of the auto-rotation back behind player
        // Lower = smoother/slower. ZZZ feels like ~2-3
        var behindSmoothProp = camSO.FindProperty("behindTargetSmoothRotation");
        if (behindSmoothProp != null)
            behindSmoothProp.floatValue = 2.5f;

        // Smooth camera rotation — how smoothly the camera rotates in general
        // Higher = smoother. 8-12 gives that ZZZ cinematic lag feel
        var smoothRotProp = camSO.FindProperty("_smoothCameraRotation");
        if (smoothRotProp != null)
            smoothRotProp.floatValue = 10f;

        // Smooth between states — transition speed when changing camera states
        var smoothBetweenProp = camSO.FindProperty("_smoothBetweenState");
        if (smoothBetweenProp != null)
            smoothBetweenProp.floatValue = 8f;

        camSO.ApplyModifiedProperties();

        // Also adjust the main camera FOV
        var mainCam = camera.GetComponentInChildren<Camera>();
        if (mainCam != null)
        {
            mainCam.fieldOfView = 55f;
            EditorUtility.SetDirty(mainCam);
        }

        EditorUtility.SetDirty(camera);
        Debug.Log("[Scorpion] ZZZ-style camera setup complete! autoBehindTarget=ON, delay=1.2s, smooth=2.5, FOV=55, centered.");
    }
}
