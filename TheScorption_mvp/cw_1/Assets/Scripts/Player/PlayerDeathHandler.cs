using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Player
{
    public class PlayerDeathHandler : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO onPlayerDiedEvent;

        private Invector.vHealthController healthController;

        private void Start()
        {
            healthController = GetComponent<Invector.vHealthController>();
            if (healthController != null)
            {
                healthController.onDead.AddListener(OnPlayerDead);
                Debug.Log("[Scorpion] PlayerDeathHandler: Hooked into vHealthController.onDead");
            }
            else
            {
                Debug.LogError("[Scorpion] PlayerDeathHandler: No vHealthController found!");
            }
        }

        private void OnPlayerDead(GameObject go)
        {
            Debug.Log("[Scorpion] PLAYER DIED! Game Over.");

            if (onPlayerDiedEvent != null)
                onPlayerDiedEvent.RaiseEvent();
        }
    }
}
