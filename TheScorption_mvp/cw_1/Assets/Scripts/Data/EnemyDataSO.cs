using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Data
{
    [CreateAssetMenu(menuName = "Scorpion/Data/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public EnemyType enemyType;

        [Header("Stats")]
        public int maxHealth = 30;
        public float moveSpeed = 3f;
        public int attackDamage = 8;
        public float attackRange = 1.5f;
        public float attackWindup = 1f;
        public float attackRecovery = 0.8f;

        [Header("Poise / Stagger")]
        public float poiseMax = 20f;
        public float poiseRecoveryRate = 5f;
        public float staggerDuration = 1.5f;

        [Header("Element Resistances")]
        [Range(0f, 1f)] public float fireResistance = 0f;
        [Range(0f, 1f)] public float lightningResistance = 0f;
        public float burnSlowMultiplier = 1f;
        public float stunDurationMultiplier = 1f;

        [Header("Light Attack Resistance")]
        [Range(0f, 1f)] public float lightAttackReduction = 0f;

        [Header("Combat AI")]
        public int maxAttackCount = 1;
        public float minTimeBetweenAttacks = 1f;
        public float maxTimeBetweenAttacks = 2f;
        [Range(0f, 1f)] public float chanceToBlock = 0f;

        [Header("Rewards")]
        public int adrenalineOnKill = 5;
    }
}
