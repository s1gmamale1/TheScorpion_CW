using UnityEngine;
using System.Reflection;

namespace TheScorpion.Camera
{
    /// <summary>
    /// Fixes a bug where Invector's vThirdPersonCamera.selfRigidbody getter tries to
    /// AddComponent<Rigidbody>() but fails when one already exists on the GameObject.
    /// This script runs in Awake (before Invector's Start/Init) and pre-assigns the
    /// existing Rigidbody to Invector's private _selfRigidbody field via reflection.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before Invector
    public class CameraRigidbodyFix : MonoBehaviour
    {
        private void Awake()
        {
            var cam = GetComponent<Invector.vCamera.vThirdPersonCamera>();
            if (cam == null) return;

            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            // Pre-assign the private _selfRigidbody field so Invector's getter
            // finds it already set and skips the AddComponent call
            var field = typeof(Invector.vCamera.vThirdPersonCamera)
                .GetField("_selfRigidbody", BindingFlags.NonPublic | BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(cam, rb);
                // Ensure correct settings
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.None;
                Debug.Log("[Scorpion] CameraRigidbodyFix: Pre-assigned existing Rigidbody to vThirdPersonCamera");
            }
            else
            {
                Debug.LogWarning("[Scorpion] CameraRigidbodyFix: Could not find _selfRigidbody field");
            }
        }
    }
}
