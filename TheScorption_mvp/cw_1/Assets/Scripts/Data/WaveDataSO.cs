using UnityEngine;
using System.Collections.Generic;

namespace TheScorpion.Data
{
    [System.Serializable]
    public class WaveDefinition
    {
        public int waveNumber;
        public int basicEnemyCount;
        public int fastEnemyCount;
        public int heavyEnemyCount;
        public bool isBossWave;
        public float delayBeforeWave = 3f;
        public float spawnInterval = 0.5f;

        public int TotalEnemies => basicEnemyCount + fastEnemyCount + heavyEnemyCount + (isBossWave ? 1 : 0);
    }

    [CreateAssetMenu(menuName = "Scorpion/Data/Wave Data")]
    public class WaveDataSO : ScriptableObject
    {
        public List<WaveDefinition> waves = new List<WaveDefinition>();

        public int TotalWaves => waves.Count;

        public WaveDefinition GetWave(int index)
        {
            if (index < 0 || index >= waves.Count) return null;
            return waves[index];
        }
    }
}
