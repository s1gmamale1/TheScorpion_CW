using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Player
{
    public class ScorpionInputHandler : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ElementSystem elementSystem;
        [SerializeField] private UltimateSystem ultimateSystem;

        private Invector.vHealthController playerHealth;

        private void Start()
        {
            playerHealth = GetComponent<Invector.vHealthController>();
        }

        private void Update()
        {
            // Don't process input if player is dead
            if (playerHealth != null && playerHealth.isDead) return;

            // No input during PreGame (start screen handles it)
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.PreGame) return;

            // Pause handling works during Playing and Paused
            HandlePauseInput();

            if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

            // These always work — even during attacks, mid-combo, while blocking
            HandleElementInput();
            HandleAbilityInput();
            HandleProjectileInput();
            HandleDashInput();
            HandleUltimateInput();
        }

        private void HandleElementInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.Q))
                elementSystem.SwitchToPreviousElement();

            if (Input.GetKeyDown(KeyCode.E))
                elementSystem.SwitchToNextElement();
        }

        private void HandleAbilityInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.F))
                elementSystem.UseAbility1();

            if (Input.GetKeyDown(KeyCode.R))
                elementSystem.UseAbility2();
        }

        private void HandleProjectileInput()
        {
            if (elementSystem == null) return;

            if (Input.GetKeyDown(KeyCode.C))
                elementSystem.FireProjectile();
        }

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

                // Use Invector's built-in Roll which handles physics, animation, and i-frames
                controller.Roll();
                dashCooldownTimer = DASH_COOLDOWN;
                Debug.Log("[Scorpion] Evasive dash!");
            }
        }

        private void HandleUltimateInput()
        {
            if (ultimateSystem == null) return;

            if (Input.GetKeyDown(KeyCode.V))
                ultimateSystem.TryActivateUltimate();
        }

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
