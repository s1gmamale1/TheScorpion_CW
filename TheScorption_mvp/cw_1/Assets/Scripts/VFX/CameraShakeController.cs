using UnityEngine;
using System.Collections;

namespace TheScorpion.VFX
{
    public class CameraShakeController : MonoBehaviour
    {
        public static CameraShakeController Instance { get; private set; }

        [Header("Shake Settings")]
        [SerializeField] private float attackShakeIntensity = 0.5f;
        [SerializeField] private float attackShakeDuration = 0.08f;
        [SerializeField] private float hitShakeIntensity = 1.2f;
        [SerializeField] private float hitShakeDuration = 0.15f;
        [SerializeField] private float heavyShakeIntensity = 2.0f;
        [SerializeField] private float heavyShakeDuration = 0.25f;

        private UnityEngine.Camera cam;
        private Coroutine shakeCoroutine;

        private void Awake()
        {
            Instance = this;
            // Find the actual Camera component (might be on this object or a child)
            cam = GetComponentInChildren<UnityEngine.Camera>();
        }

        public void ShakeOnAttack()
        {
            DoShake(attackShakeIntensity, attackShakeDuration);
        }

        public void ShakeOnHit()
        {
            DoShake(hitShakeIntensity, hitShakeDuration);
        }

        public void ShakeHeavy()
        {
            DoShake(heavyShakeIntensity, heavyShakeDuration);
        }

        public void DoShake(float intensity, float duration)
        {
            if (shakeCoroutine != null)
                StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeCoroutine(intensity, duration));
        }

        private IEnumerator ShakeCoroutine(float intensity, float duration)
        {
            if (cam == null) yield break;

            float elapsed = 0f;
            Quaternion originalRot = cam.transform.localRotation;

            while (elapsed < duration)
            {
                // Use rotation-based shake — doesn't conflict with Invector's position control
                float progress = elapsed / duration;
                float dampedIntensity = intensity * (1f - progress); // Fade out

                float x = Random.Range(-1f, 1f) * dampedIntensity;
                float y = Random.Range(-1f, 1f) * dampedIntensity;

                cam.transform.localRotation = originalRot * Quaternion.Euler(x, y, 0f);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            cam.transform.localRotation = originalRot;
            shakeCoroutine = null;
        }
    }
}
