using UnityEngine;
using System;

namespace TheScorpion.Core
{
    public struct DamageEventData
    {
        public Transform Sender;
        public Transform Receiver;
        public float DamageValue;
        public ElementType Element;
        public Vector3 HitPoint;
        public bool IsFinisher;
    }

    [CreateAssetMenu(menuName = "Scorpion/Events/Damage Event Channel")]
    public class DamageEventChannelSO : ScriptableObject
    {
        public event Action<DamageEventData> OnEventRaised;

        public void RaiseEvent(DamageEventData data)
        {
            OnEventRaised?.Invoke(data);
        }
    }
}
