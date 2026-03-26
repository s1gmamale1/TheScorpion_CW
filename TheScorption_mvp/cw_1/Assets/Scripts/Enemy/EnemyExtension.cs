using UnityEngine;
using TheScorpion.Core;
using TheScorpion.Data;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Sits on each Invector AI enemy. Loads stats from EnemyDataSO onto Invector components,
    /// hooks onDead for wave tracking and adrenaline rewards.
    /// </summary>
    public class EnemyExtension : MonoBehaviour
    {
        [SerializeField] private EnemyDataSO enemyData;

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;
        [SerializeField] private IntEventChannelSO onAdrenalineGainEvent;

        private Invector.vHealthController healthController;
        private UnityEngine.AI.NavMeshAgent agent;

        public EnemyDataSO EnemyData => enemyData;
        public bool IsDead => healthController != null && healthController.isDead;

        private void Awake()
        {
            healthController = GetComponent<Invector.vHealthController>();
            agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        }

        private void Start()
        {
            if (enemyData != null)
                ApplyDataToInvector();

            if (healthController != null)
                healthController.onDead.AddListener(OnDeath);
        }

        public void Initialize(EnemyDataSO data, VoidEventChannelSO killEvent, IntEventChannelSO adrenalineEvent)
        {
            enemyData = data;
            onEnemyKilledEvent = killEvent;
            onAdrenalineGainEvent = adrenalineEvent;
            ApplyDataToInvector();
        }

        private void ApplyDataToInvector()
        {
            if (enemyData == null) return;

            // Health
            if (healthController != null)
            {
                healthController.maxHealth = enemyData.maxHealth;
                healthController.ChangeMaxHealth(enemyData.maxHealth);
                healthController.ResetHealth();
            }

            // Movement speed
            if (agent != null)
                agent.speed = enemyData.moveSpeed;
        }

        private void OnDeath(GameObject deadObj)
        {
            // Raise kill event for wave tracking
            if (onEnemyKilledEvent != null)
                onEnemyKilledEvent.RaiseEvent();

            // Grant adrenaline
            if (onAdrenalineGainEvent != null && enemyData != null)
                onAdrenalineGainEvent.RaiseEvent(enemyData.adrenalineOnKill);

            Debug.Log($"[Scorpion] {enemyData?.enemyName ?? "Enemy"} killed");
        }

        private void OnDestroy()
        {
            if (healthController != null)
                healthController.onDead.RemoveListener(OnDeath);
        }
    }
}
