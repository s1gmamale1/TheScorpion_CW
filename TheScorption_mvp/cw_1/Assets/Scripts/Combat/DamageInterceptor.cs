using UnityEngine;
using Invector.vMelee;
using TheScorpion.Core;
using TheScorpion.Player;
using TheScorpion.UI;
using TheScorpion.VFX;

namespace TheScorpion.Combat
{
    public class DamageInterceptor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ElementSystem elementSystem;
        [SerializeField] private UltimateSystem ultimateSystem;

        [Header("Combo Tracking")]
        [SerializeField] private float comboWindowTime = 0.6f;
        [SerializeField] private int finisherThreshold = 3;

        [Header("Event Channels")]
        [SerializeField] private DamageEventChannelSO onDamageDealtEvent;
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;

        [Header("Combo Regen Bonus")]
        [SerializeField] private float baseEnergyRegen = 3f;
        [SerializeField] private float comboRegenBonusPerHit = 0.5f;
        [SerializeField] private float maxComboRegenMultiplier = 3f;

        private vMeleeManager meleeManager;
        private StyleMeter styleMeter;
        private Animator playerAnimator;
        private Invector.vCharacterController.vThirdPersonController playerMotor;

        // Combo state
        private int comboCounter;
        private float comboTimer;
        private int lastAttackID = -1;
        private bool comboActive; // true once 3+ hits reached

        public int ComboCounter => comboCounter;
        public bool IsComboActive => comboActive;
        public float ComboDamageMultiplier => comboActive ? 1.05f : 1f; // +5% during combo
        public float ComboRegenMultiplier => Mathf.Min(1f + comboCounter * comboRegenBonusPerHit, maxComboRegenMultiplier);

        private void Start()
        {
            meleeManager = GetComponent<vMeleeManager>();
            styleMeter = GetComponent<StyleMeter>();
            playerAnimator = GetComponent<Animator>();
            playerMotor = GetComponent<Invector.vCharacterController.vThirdPersonController>();

            if (meleeManager != null)
                meleeManager.onDamageHit.AddListener(OnPlayerDealtDamage);

            var healthController = GetComponent<Invector.vHealthController>();
            if (healthController != null)
                healthController.onReceiveDamage.AddListener(OnPlayerTookDamage);
        }

        private void Update()
        {
            // Decay combo window
            if (comboTimer > 0f)
            {
                comboTimer -= Time.unscaledDeltaTime;
                if (comboTimer <= 0f)
                {
                    EndCombo();
                }
            }

            // Combo regen bonus — higher combo = faster MP and stamina recovery
            if (comboCounter > 0 && elementSystem != null)
            {
                float bonusRegen = (ComboRegenMultiplier - 1f) * baseEnergyRegen * Time.deltaTime;
                if (bonusRegen > 0f)
                    elementSystem.GainEnergy(bonusRegen);
            }

            // Boost stamina recovery based on combo
            if (comboCounter > 0 && playerMotor != null)
            {
                float staminaBonus = comboCounter * 0.3f * Time.deltaTime;
                playerMotor.ChangeStamina((int)(staminaBonus * 10f));
            }
        }

        private void OnPlayerDealtDamage(vHitInfo hitInfo)
        {
            if (hitInfo.attackObject == null || hitInfo.attackObject.damage == null) return;

            var damage = hitInfo.attackObject.damage;

            // Store original base damage so we don't permanently modify the weapon
            float baseDamage = damage.damageValue;

            // === 1. Apply active element to melee damage ===
            if (elementSystem != null && elementSystem.ActiveElement != ElementType.None)
                damage.damageType = elementSystem.ActiveElement.ToString();

            // === 2. Calculate final damage from base (don't compound multipliers) ===
            float finalDamage = baseDamage;

            if (ultimateSystem != null && ultimateSystem.IsUltimateActive)
                finalDamage *= ultimateSystem.GetDamageMultiplier();

            if (comboActive)
                finalDamage *= ComboDamageMultiplier;

            damage.damageValue = Mathf.RoundToInt(finalDamage);

            // === 3. Fire Aura burn on melee hit ===
            if (elementSystem != null && elementSystem.IsAbility2Active && elementSystem.ActiveElement == ElementType.Fire)
            {
                var targetStatus = hitInfo.targetCollider?.GetComponentInParent<Enemy.EnemyStatusEffects>();
                if (targetStatus != null && !targetStatus.IsBurning)
                {
                    float burnDmg = elementSystem.GetAttackBonusDamage();
                    if (burnDmg > 0f)
                        targetStatus.ApplyBurn(burnDmg, 2f);
                }
            }

            // === 4. Energy gain on hit ===
            if (elementSystem != null)
                elementSystem.GainEnergy(5f);

            // === 5. Combo tracking + finisher detection ===
            int currentAttackID = meleeManager.GetAttackID();
            comboCounter++;
            comboTimer = comboWindowTime;

            // Activate combo at 3+ hits
            if (!comboActive && comboCounter >= finisherThreshold)
            {
                comboActive = true;
                Debug.Log($"[Scorpion] COMBO ACTIVE! +5% damage bonus");
            }

            bool isFinisher = comboCounter >= finisherThreshold && currentAttackID != lastAttackID;
            lastAttackID = currentAttackID;

            // === 6. Adrenaline gain ===
            if (ultimateSystem != null)
            {
                float styleMultiplier = styleMeter != null ? styleMeter.GetMultiplier() : 1f;

                if (isFinisher)
                    ultimateSystem.AddAdrenalineForFinisher(styleMultiplier);
                else
                    ultimateSystem.AddAdrenalineForHit(styleMultiplier);
            }

            // === 7. Feed style meter ===
            if (styleMeter != null)
            {
                ElementType elem = elementSystem != null ? elementSystem.ActiveElement : ElementType.None;
                styleMeter.OnHitLanded(currentAttackID, elem);
            }

            // === 8. Raise damage event ===
            if (onDamageDealtEvent != null)
            {
                var data = new DamageEventData
                {
                    Sender = transform,
                    Receiver = hitInfo.targetCollider?.transform,
                    DamageValue = damage.damageValue,
                    Element = elementSystem != null ? elementSystem.ActiveElement : ElementType.None,
                    HitPoint = hitInfo.hitPoint,
                    IsFinisher = isFinisher
                };
                onDamageDealtEvent.RaiseEvent(data);
            }

            // === 9. Damage popup ===
            if (hitInfo.targetCollider != null)
            {
                Vector3 popupPos = hitInfo.targetCollider.transform.root.position;
                string elem = damage.damageType ?? "";
                DamagePopup.Spawn(popupPos, (int)damage.damageValue, elem);
            }

            // === 10. Camera shake ===
            if (CameraShakeController.Instance != null)
            {
                if (isFinisher)
                    CameraShakeController.Instance.ShakeHeavy();
                else
                    CameraShakeController.Instance.ShakeOnAttack();
            }

            // === 11. Restore base damage so weapon isn't permanently modified ===
            damage.damageValue = baseDamage;
        }

        private void OnPlayerTookDamage(Invector.vDamage damage)
        {
            // Comeback adrenaline
            if (ultimateSystem != null)
                ultimateSystem.AddAdrenalineForDamageTaken();

            // Drop style rank on hit
            if (styleMeter != null)
                styleMeter.OnHitTaken();

            // Reset combo (no stamina refill — you got hit)
            comboCounter = 0;
            comboTimer = 0f;
            comboActive = false;

            // Camera shake
            if (CameraShakeController.Instance != null)
                CameraShakeController.Instance.ShakeOnHit();
        }

        private void EndCombo()
        {
            if (comboActive)
            {
                // Refill stamina on successful combo completion
                if (playerMotor != null)
                    playerMotor.ChangeStamina((int)playerMotor.maxStamina);

                // Heal 10 HP if below 50% health
                var health = GetComponent<Invector.vHealthController>();
                if (health != null)
                {
                    float healthPct = health.currentHealth / health.MaxHealth;
                    if (healthPct < 0.5f)
                    {
                        health.AddHealth(10);
                        Debug.Log($"[Scorpion] Combo heal! +10 HP (was {healthPct * 100f:F0}%)");
                    }
                }

                Debug.Log($"[Scorpion] Combo ended ({comboCounter} hits) — stamina refilled!");
            }
            comboCounter = 0;
            comboActive = false;
        }

        private void OnDestroy()
        {
            if (meleeManager != null)
                meleeManager.onDamageHit.RemoveListener(OnPlayerDealtDamage);
        }
    }
}
