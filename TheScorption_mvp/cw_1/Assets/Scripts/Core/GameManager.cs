using UnityEngine;
using TheScorpion.Core;

namespace TheScorpion.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Event Channels")]
        [SerializeField] private VoidEventChannelSO onPlayerDiedEvent;
        [SerializeField] private VoidEventChannelSO onVictoryEvent;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.Playing;

        public GameState CurrentState => currentState;

        private float savedTimeScale = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (onPlayerDiedEvent != null) onPlayerDiedEvent.OnEventRaised += OnPlayerDied;
            if (onVictoryEvent != null) onVictoryEvent.OnEventRaised += OnVictory;
        }

        private void OnDisable()
        {
            if (onPlayerDiedEvent != null) onPlayerDiedEvent.OnEventRaised -= OnPlayerDied;
            if (onVictoryEvent != null) onVictoryEvent.OnEventRaised -= OnVictory;
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                savedTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                currentState = GameState.Paused;
            }
            else if (currentState == GameState.Paused)
            {
                Time.timeScale = savedTimeScale;
                currentState = GameState.Playing;
            }
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                Time.timeScale = savedTimeScale;
                currentState = GameState.Playing;
            }
        }

        private void OnPlayerDied()
        {
            currentState = GameState.GameOver;
            Time.timeScale = 0f;
        }

        private void OnVictory()
        {
            currentState = GameState.Victory;
            Time.timeScale = 0f;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        public void SetGameState(GameState state)
        {
            currentState = state;
            if (state == GameState.Victory || state == GameState.GameOver)
                Time.timeScale = 0f;
        }

        public bool IsPlaying => currentState == GameState.Playing;
    }
}
