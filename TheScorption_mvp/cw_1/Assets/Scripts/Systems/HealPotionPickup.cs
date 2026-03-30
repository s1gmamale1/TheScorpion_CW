using UnityEngine;
using TheScorpion.Player;

namespace TheScorpion.Systems
{
    /// <summary>
    /// Heal potion that can be picked up by the player on contact.
    /// Spawned by WaveManager at the start of each wave.
    /// Bobs up and down and rotates for visual appeal.
    /// </summary>
    public class HealPotionPickup : MonoBehaviour
    {
        private float bobSpeed = 2f;
        private float bobHeight = 0.3f;
        private float rotateSpeed = 90f;
        private Vector3 startPos;
        private bool collected;

        private void Start()
        {
            startPos = transform.position;

            // Add green point light glow
            var light = gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.2f, 1f, 0.4f);
            light.range = 3f;
            light.intensity = 2f;

            // Make sure we have a trigger collider
            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.radius = 0.8f;
                sphere.isTrigger = true;
            }
            else
            {
                col.isTrigger = true;
            }

            // Rigidbody for trigger detection (kinematic)
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            }
        }

        private void Update()
        {
            if (collected) return;

            // Bob up and down
            float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(startPos.x, newY, startPos.z);

            // Rotate
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected) return;

            // Only player can collect
            if (!other.CompareTag("Player") && !other.transform.root.CompareTag("Player"))
                return;

            var inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null)
                inventory = other.transform.root.GetComponent<PlayerInventory>();

            if (inventory != null)
            {
                collected = true;
                inventory.AddHealthPotion();

                // Pickup VFX — green sparkle burst
                SpawnPickupVFX();

                Destroy(gameObject);
            }
        }

        private void SpawnPickupVFX()
        {
            var go = new GameObject("PickupVFX");
            go.transform.position = transform.position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(0.2f, 1f, 0.4f, 1f);
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBurst(0, new ParticleSystem.Burst(0f, 20));

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
            renderer.material.color = new Color(0.2f, 1f, 0.4f);

            Destroy(go, 1f);
        }
    }
}
