using UnityEngine;
using System.Collections.Generic;

namespace TheScorpion.Enemy
{
    /// <summary>
    /// Limits simultaneous attackers (Genshin-style mob AI). Max 2-3 enemies attack at once,
    /// others circle/strafe. Enemies request permission to attack and release when done.
    /// </summary>
    public class AttackQueueManager : MonoBehaviour
    {
        public static AttackQueueManager Instance { get; private set; }

        [SerializeField] private int maxSimultaneousAttackers = 2;

        private HashSet<GameObject> activeAttackers = new HashSet<GameObject>();
        private Queue<GameObject> waitingQueue = new Queue<GameObject>();

        public int ActiveAttackerCount => activeAttackers.Count;
        public int MaxAttackers => maxSimultaneousAttackers;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Enemy requests permission to attack. Returns true if slot available.
        /// </summary>
        public bool RequestAttackSlot(GameObject enemy)
        {
            if (enemy == null) return false;

            // Already attacking
            if (activeAttackers.Contains(enemy)) return true;

            if (activeAttackers.Count < maxSimultaneousAttackers)
            {
                activeAttackers.Add(enemy);
                return true;
            }

            // Queue up if full
            if (!waitingQueue.Contains(enemy))
                waitingQueue.Enqueue(enemy);

            return false;
        }

        /// <summary>
        /// Enemy finished attacking, release slot.
        /// </summary>
        public void ReleaseAttackSlot(GameObject enemy)
        {
            activeAttackers.Remove(enemy);
            ProcessQueue();
        }

        /// <summary>
        /// Remove enemy entirely (died, despawned).
        /// </summary>
        public void UnregisterEnemy(GameObject enemy)
        {
            activeAttackers.Remove(enemy);
            // Can't remove from middle of Queue, but dead enemies will be skipped in ProcessQueue
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            while (waitingQueue.Count > 0 && activeAttackers.Count < maxSimultaneousAttackers)
            {
                var next = waitingQueue.Dequeue();
                // Skip destroyed/dead enemies
                if (next == null) continue;

                var health = next.GetComponent<Invector.vHealthController>();
                if (health != null && health.isDead) continue;

                activeAttackers.Add(next);
            }
        }

        /// <summary>
        /// Check if enemy is allowed to attack right now (without requesting a slot).
        /// </summary>
        public bool CanAttack(GameObject enemy)
        {
            return activeAttackers.Contains(enemy) || activeAttackers.Count < maxSimultaneousAttackers;
        }
    }
}
