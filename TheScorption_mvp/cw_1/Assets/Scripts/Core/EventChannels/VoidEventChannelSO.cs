using UnityEngine;
using System;

namespace TheScorpion.Core
{
    [CreateAssetMenu(menuName = "Scorpion/Events/Void Event Channel")]
    public class VoidEventChannelSO : ScriptableObject
    {
        public event Action OnEventRaised;

        public void RaiseEvent()
        {
            OnEventRaised?.Invoke();
        }
    }
}
