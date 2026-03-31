using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Data
{
    [CreateAssetMenu(menuName = "Scorpion/Data/Element Data")]
    public class ElementDataSO : ScriptableObject
    {
        [Header("Identity")]
        public ElementType elementType;
        public string elementName;
        public Color elementColor = Color.white;

        [Header("Ability 1")]
        public string ability1Name;
        public float ability1Damage = 15f;
        public float ability1Radius = 4f;
        public float ability1Cooldown = 8f;
        public float ability1Cost = 40f;
        public float ability1Duration = 3f;

        [Header("Ability 2")]
        public string ability2Name;
        public float ability2Duration = 6f;
        public float ability2Cooldown = 12f;
        public float ability2Cost = 30f;

        [Header("Ability 2 - Buff Values")]
        public float ability2BurnDamagePerTick = 5f;
        public float ability2AttackSpeedBonus = 0.4f;
        public float ability2MoveSpeedBonus = 0.25f;

        [Header("Ultimate Burst")]
        public float burstDamage = 60f;
        public float burstRadius = 8f;
        public float burstStunDuration = 0f;

        [Header("VFX")]
        public GameObject ability1VFXPrefab;
        public GameObject ability2VFXPrefab;
        public GameObject burstVFXPrefab;
        public Color weaponTrailColor = Color.white;
    }
}
