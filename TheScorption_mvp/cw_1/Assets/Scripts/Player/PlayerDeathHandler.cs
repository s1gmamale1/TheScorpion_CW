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

            // Freeze the game after a short delay
            Invoke(nameof(FreezeGame), 2f);
        }

        private void FreezeGame()
        {
            Time.timeScale = 0f;
            Debug.Log("[Scorpion] Game frozen. Press Escape to restart.");
        }

        private void Update()
        {
            // Allow restart when dead
            if (healthController != null && healthController.isDead)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Time.timeScale = 1f;
                    Time.fixedDeltaTime = 0.02f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene(
                        UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
                }
            }
        }
    }
}
