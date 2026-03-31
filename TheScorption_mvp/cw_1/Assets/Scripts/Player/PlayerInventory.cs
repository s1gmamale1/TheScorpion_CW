using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Player
{
    /// <summary>
    /// Simple inventory for consumables.
    /// Press 1 to use a health potion (heals 30 HP).
    /// Potions are collected by walking into HealPotionPickup objects.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Potion Settings")]
        [SerializeField] private int healAmount = 30;

        private int healthPotionCount;
        private Invector.vHealthController healthController;

        public int HealthPotionCount => healthPotionCount;

        private void Start()
        {
            healthController = GetComponent<Invector.vHealthController>();
        }

        private void Update()
        {
            if (healthController != null && healthController.isDead) return;
            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Press 1 to use health potion
            if (Input.GetKeyDown(KeyCode.Alpha1))
                UseHealthPotion();
        }

        public void AddHealthPotion(int count = 1)
        {
            healthPotionCount += count;
            Debug.Log($"[Inventory] Picked up health potion! Total: {healthPotionCount}");
        }

        public void UseHealthPotion()
        {
            if (healthPotionCount <= 0)
            {
                Debug.Log("[Inventory] No health potions!");
                return;
            }

            if (healthController == null) return;

            // Don't waste if already full HP
            if (healthController.currentHealth >= healthController.MaxHealth)
            {
                Debug.Log("[Inventory] Already at full health!");
                return;
            }

            healthPotionCount--;
            healthController.AddHealth(healAmount);

            Debug.Log($"[Inventory] Used health potion! +{healAmount} HP | Potions left: {healthPotionCount}");

            // Heal VFX — green flash
            SpawnHealVFX();
        }

        private void SpawnHealVFX()
        {
            var go = new GameObject("PotionHealVFX");
            go.transform.position = transform.position + Vector3.up * 1f;

            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 1f, 0.4f);
            light.range = 5f;
            light.intensity = 5f;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
            main.startColor = new Color(0.2f, 1f, 0.4f, 0.9f);
            main.maxParticles = 25;
            main.gravityModifier = -0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 25));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.2f, 1f, 0.4f);

            Destroy(go, 1.5f);
        }
    }
}
