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
        [SerializeField] private VoidEventChannelSO onEnemyKilledEvent;

        [Header("State")]
        [SerializeField] private GameState currentState = GameState.PreGame;

        public GameState CurrentState => currentState;

        // Stats
        private float savedTimeScale = 1f;
        private float gameTimer;
        private int totalKills;

        public float GameTime => gameTimer;
        public int TotalKills => totalKills;

        // Events for UI to listen to
        public System.Action<GameState> OnGameStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetGameState(GameState.PreGame);
        }

        private void OnEnable()
        {
            if (onPlayerDiedEvent != null) onPlayerDiedEvent.OnEventRaised += OnPlayerDied;
            if (onVictoryEvent != null) onVictoryEvent.OnEventRaised += OnVictory;
            if (onEnemyKilledEvent != null) onEnemyKilledEvent.OnEventRaised += OnEnemyKilled;
        }

        private void OnDisable()
        {
            if (onPlayerDiedEvent != null) onPlayerDiedEvent.OnEventRaised -= OnPlayerDied;
            if (onVictoryEvent != null) onVictoryEvent.OnEventRaised -= OnVictory;
            if (onEnemyKilledEvent != null) onEnemyKilledEvent.OnEventRaised -= OnEnemyKilled;
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
                gameTimer += Time.deltaTime;
        }

        public void SetGameState(GameState newState)
        {
            var oldState = currentState;
            currentState = newState;

            switch (newState)
            {
                case GameState.PreGame:
                    Time.timeScale = 0f;
                    ShowCursor();
                    break;

                case GameState.Playing:
                    Time.timeScale = 1f;
                    Time.fixedDeltaTime = 0.02f;
                    HideCursor();
                    break;

                case GameState.Paused:
                    savedTimeScale = Time.timeScale;
                    Time.timeScale = 0f;
                    ShowCursor();
                    break;

                case GameState.GameOver:
                    Time.timeScale = 0f;
                    ShowCursor();
                    break;

                case GameState.Victory:
                    Time.timeScale = 0f;
                    ShowCursor();
                    break;
            }

            OnGameStateChanged?.Invoke(newState);
            Debug.Log($"[Scorpion] GameState: {oldState} → {newState}");
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing)
                SetGameState(GameState.Paused);
            else if (currentState == GameState.Paused)
                SetGameState(GameState.Playing);
        }

        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                Time.timeScale = savedTimeScale;
                currentState = GameState.Playing;
                HideCursor();
                OnGameStateChanged?.Invoke(currentState);
            }
        }

        public void StartGame()
        {
            gameTimer = 0f;
            totalKills = 0;
            SetGameState(GameState.Playing);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OnPlayerDied()
        {
            SetGameState(GameState.GameOver);
        }

        private void OnVictory()
        {
            SetGameState(GameState.Victory);
        }

        private void OnEnemyKilled()
        {
            totalKills++;
        }

        private void ShowCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HideCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public bool IsPlaying => currentState == GameState.Playing;

        public string GetFormattedTime()
        {
            int mins = (int)(gameTimer / 60f);
            int secs = (int)(gameTimer % 60f);
            return $"{mins:D2}:{secs:D2}";
        }
    }
}
