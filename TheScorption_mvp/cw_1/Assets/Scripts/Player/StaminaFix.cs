using UnityEngine;
using Invector.vMelee;

namespace TheScorpion.Player
{
    /// <summary>
    /// Zeroes out all melee attack stamina costs at runtime.
    /// Normal attacks should be free — only sprint and dodge cost stamina.
    /// Runs after Invector initializes weapons.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class StaminaFix : MonoBehaviour
    {
        private vMeleeManager meleeManager;
        private bool fixed_;

        private void Start()
        {
            meleeManager = GetComponent<vMeleeManager>();
            if (meleeManager != null)
            {
                meleeManager.defaultStaminaCost = 0f;
                meleeManager.defaultStaminaRecoveryDelay = 0f;
            }
            InvokeRepeating(nameof(FixWeaponStamina), 0.5f, 2f);
        }

        private void FixWeaponStamina()
        {
            if (meleeManager == null) return;

            // Zero out equipped weapons' stamina costs
            if (meleeManager.rightWeapon != null)
            {
                meleeManager.rightWeapon.staminaCost = 0f;
                meleeManager.rightWeapon.staminaRecoveryDelay = 0f;
            }
            if (meleeManager.leftWeapon != null)
            {
                meleeManager.leftWeapon.staminaCost = 0f;
                meleeManager.leftWeapon.staminaRecoveryDelay = 0f;
            }

            // Also find any vMeleeWeapon in children (holstered weapons)
            var allWeapons = GetComponentsInChildren<vMeleeWeapon>(true);
            foreach (var w in allWeapons)
            {
                w.staminaCost = 0f;
                w.staminaRecoveryDelay = 0f;
            }

            if (!fixed_)
            {
                fixed_ = true;
                Debug.Log($"[Scorpion] StaminaFix: Zeroed stamina cost on {allWeapons.Length} weapons");
            }
        }
    }
}
