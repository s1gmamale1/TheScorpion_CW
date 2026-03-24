using UnityEngine;
using Invector.vMelee;
using TheScorpion.Core;
using TheScorpion.Player;
using TheScorpion.VFX;

namespace TheScorpion.Combat
{
    public class DamageInterceptor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private ElementSystem elementSystem;
        [SerializeField] private UltimateSystem ultimateSystem;

        [Header("Event Channels")]
        [SerializeField] private DamageEventChannelSO onDamageDealtEvent;
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;

        private vMeleeManager meleeManager;

        private void Start()
        {
            meleeManager = GetComponent<vMeleeManager>();
            if (meleeManager != null)
            {
                meleeManager.onDamageHit.AddListener(OnPlayerDealtDamage);
            }

            var healthController = GetComponent<Invector.vHealthController>();
            if (healthController != null)
            {
                healthController.onReceiveDamage.AddListener(OnPlayerTookDamage);
            }
        }

        private void OnPlayerDealtDamage(vHitInfo hitInfo)
        {
            if (elementSystem != null)
            {
                elementSystem.GainEnergy(5f);
            }

            if (ultimateSystem != null)
            {
                float styleMultiplier = 1f;
                var styleMeter = GetComponent<StyleMeter>();
                if (styleMeter != null) styleMultiplier = styleMeter.GetMultiplier();

                ultimateSystem.AddAdrenalineForHit(styleMultiplier);
            }

            if (onDamageDealtEvent != null)
            {
                var data = new DamageEventData
                {
                    Sender = transform,
                    Receiver = hitInfo.targetCollider?.transform,
                    DamageValue = hitInfo.attackObject?.damage?.damageValue ?? 0,
                    Element = elementSystem != null ? elementSystem.ActiveElement : ElementType.None,
                    HitPoint = hitInfo.hitPoint
                };
                onDamageDealtEvent.RaiseEvent(data);
            }

            // Camera shake on attack hit
            if (CameraShakeController.Instance != null)
                CameraShakeController.Instance.ShakeOnAttack();
        }

        private void OnPlayerTookDamage(Invector.vDamage damage)
        {
            if (ultimateSystem != null)
            {
                ultimateSystem.AddAdrenalineForDamageTaken();
            }

            // Camera shake on taking damage
            if (CameraShakeController.Instance != null)
                CameraShakeController.Instance.ShakeOnHit();
        }

        private void OnDestroy()
        {
            if (meleeManager != null)
            {
                meleeManager.onDamageHit.RemoveListener(OnPlayerDealtDamage);
            }
        }
    }
}
