using UnityEngine;
using System;

namespace TheScorpion.Core
{
    [CreateAssetMenu(menuName = "Scorpion/Events/Float Event Channel")]
    public class FloatEventChannelSO : ScriptableObject
    {
        public event Action<float> OnEventRaised;

        public void RaiseEvent(float value)
        {
            OnEventRaised?.Invoke(value);
        }
    }
}
