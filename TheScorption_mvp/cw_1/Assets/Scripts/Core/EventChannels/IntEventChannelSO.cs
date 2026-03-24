using UnityEngine;
using System;

namespace TheScorpion.Core
{
    [CreateAssetMenu(menuName = "Scorpion/Events/Int Event Channel")]
    public class IntEventChannelSO : ScriptableObject
    {
        public event Action<int> OnEventRaised;

        public void RaiseEvent(int value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
