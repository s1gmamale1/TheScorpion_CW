using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Player
{
    public class StyleMeter : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float decayTime = 4f;
        [SerializeField] private float[] rankMultipliers = { 1.0f, 1.2f, 1.5f, 2.0f, 2.5f };

        private StyleRank currentRank = StyleRank.D;
        private float stylePoints;
        private float decayTimer;
        private ElementType lastElementUsed;
        private int lastAttackType;

        public StyleRank CurrentRank => currentRank;

        public float GetMultiplier()
        {
            int index = (int)currentRank;
            if (index >= 0 && index < rankMultipliers.Length)
                return rankMultipliers[index];
            return 1f;
        }

        public void OnHitLanded(int attackType, ElementType element)
        {
            float points = 1f;

            if (attackType != lastAttackType)
                points += 2f;

            if (element != lastElementUsed && element != ElementType.None)
                points += 3f;

            lastAttackType = attackType;
            lastElementUsed = element;

            AddStylePoints(points);
        }

        public void OnAbilityUsed()
        {
            AddStylePoints(4f);
        }

        public void OnHitTaken()
        {
            if ((int)currentRank > 1)
                currentRank = (StyleRank)((int)currentRank - 1);
            else
                currentRank = StyleRank.D;
            decayTimer = decayTime;
        }

        private void AddStylePoints(float points)
        {
            stylePoints += points;
            decayTimer = decayTime;

            if (stylePoints >= 40f && currentRank < StyleRank.S)
            {
                currentRank = StyleRank.S;
            }
            else if (stylePoints >= 25f && currentRank < StyleRank.A)
            {
                currentRank = StyleRank.A;
            }
            else if (stylePoints >= 15f && currentRank < StyleRank.B)
            {
                currentRank = StyleRank.B;
            }
            else if (stylePoints >= 5f && currentRank < StyleRank.C)
            {
                currentRank = StyleRank.C;
            }
        }

        private void Update()
        {
            decayTimer -= Time.deltaTime;
            if (decayTimer <= 0f)
            {
                stylePoints = Mathf.Max(0f, stylePoints - 3f * Time.deltaTime);

                if (stylePoints < 5f) currentRank = StyleRank.D;
                else if (stylePoints < 15f) currentRank = StyleRank.C;
                else if (stylePoints < 25f) currentRank = StyleRank.B;
                else if (stylePoints < 40f) currentRank = StyleRank.A;
            }
        }
    }
}
