using UnityEngine;

namespace TheScorpion.Systems
{
    /// <summary>
    /// Manages N/S/E/W spawn points around the arena.
    /// Provides random or directional spawn positions with slight offset to avoid stacking.
    /// </summary>
    public class SpawnPointManager : MonoBehaviour
    {
        public static SpawnPointManager Instance { get; private set; }

        [SerializeField] private Transform[] spawnPoints; // Assign 4 empty GameObjects at arena edges
        [SerializeField] private float spawnRandomOffset = 2f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public Vector3 GetRandomSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[Scorpion] SpawnPointManager: No spawn points assigned!");
                return Vector3.zero;
            }

            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return point.position + GetRandomOffset();
        }

        public Quaternion GetRandomSpawnRotation()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Quaternion.identity;

            return spawnPoints[Random.Range(0, spawnPoints.Length)].rotation;
        }

        public (Vector3 position, Quaternion rotation) GetSpawnPointData()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return (Vector3.zero, Quaternion.identity);

            var point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            return (point.position + GetRandomOffset(), point.rotation);
        }

        private Vector3 GetRandomOffset()
        {
            return new Vector3(
                Random.Range(-spawnRandomOffset, spawnRandomOffset),
                0f,
                Random.Range(-spawnRandomOffset, spawnRandomOffset)
            );
        }
    }
}
