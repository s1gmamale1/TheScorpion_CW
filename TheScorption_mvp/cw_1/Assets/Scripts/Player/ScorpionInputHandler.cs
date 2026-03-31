using UnityEngine;
using Invector.vMelee;
using TheScorpion.Core;

namespace TheScorpion.Player
{
    public class ScorpionInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ElementSystem elementSystem;
        [SerializeField] private UltimateSystem ultimateSystem;

        [Header("Auto Aim")]
        [SerializeField] private float abilityAimRange = 15f;
        [SerializeField] private float meleeAimPadding = 1.5f; // extra range on top of weapon reach

        private Invector.vHealthController playerHealth;
        private Invector.vCharacterController.vThirdPersonMotor motor;
        private vMeleeManager meleeManager;
        private int enemyLayerMask;

        private float MeleeAimRange => (meleeManager != null ? meleeManager.GetAttackDistance() : 1f) + meleeAimPadding;
        private bool IsPlayerMoving => motor != null && motor.inputMagnitude > 0.1f;

        private void Start()
        {
            playerHealth = GetComponent<Invector.vHealthController>();
            motor = GetComponent<Invector.vCharacterController.vThirdPersonMotor>();
            meleeManager = GetComponent<vMeleeManager>();
            enemyLayerMask = LayerMask.GetMask("Enemy");
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.isDead) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PreGame) return;

            HandlePauseInput();

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // Auto-aim: only when player is standing still
            if (Input.GetMouseButtonDown(0) && !IsPlayerMoving)
                SnapToNearestEnemy(MeleeAimRange);

            HandleElementInput();
            HandleAbilityInput();
            HandleProjectileInput();
            HandleDashInput();
            HandleUltimateInput();
        }

        // ==================== AUTO AIM ====================
        private void SnapToNearestEnemy(float range)
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, range, enemyLayerMask);
            if (hits.Length == 0) return;

            Transform best = null;
            float bestScore = float.MaxValue;

            foreach (var hit in hits)
            {
                var health = hit.GetComponentInParent<Invector.vHealthController>();
                if (health == null || health.isDead) continue;

                Transform enemyRoot = hit.transform.root;
                float dist = Vector3.Distance(transform.position, enemyRoot.position);
                float healthPct = health.currentHealth / health.MaxHealth;

                // Score: lower health = higher priority, distance as tiebreaker
                // healthPct (0-1) weighted heavily so a 10% HP enemy at 4m beats a full HP enemy at 2m
                float score = healthPct * range + dist * 0.3f;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemyRoot;
                }
            }

            if (best == null) return;

            // Hard snap rotation — no interpolation, instant turn
            Vector3 dir = best.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }

        // ==================== ELEMENT ====================
        private void HandleElementInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.Q))
                elementSystem.SwitchToPreviousElement();

            if (Input.GetKeyDown(KeyCode.E))
                elementSystem.SwitchToNextElement();
        }

        // ==================== ABILITIES ====================
        private void HandleAbilityInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.F))
                elementSystem.UseAbility1();

            if (Input.GetKeyDown(KeyCode.R))
                elementSystem.UseAbility2();
        }

        // ==================== PROJECTILE ====================
        private void HandleProjectileInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.C))
                elementSystem.FireProjectile();
        }

        // ==================== DASH ====================
        private float dashCooldownTimer;
        private const float DASH_COOLDOWN = 0.8f;

        private void HandleDashInput()
        {
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                var controller = GetComponent<Invector.vCharacterController.vThirdPersonController>();
                if (controller == null) return;

                controller.Roll();
                dashCooldownTimer = DASH_COOLDOWN;
            }
        }

        // ==================== ULTIMATE ====================
        private void HandleUltimateInput()
        {
            if (ultimateSystem == null) return;

            if (Input.GetKeyDown(KeyCode.V))
                ultimateSystem.TryActivateUltimate();
        }

        // ==================== PAUSE ====================
        private void HandlePauseInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.TogglePause();
            }
        }
    }
}
