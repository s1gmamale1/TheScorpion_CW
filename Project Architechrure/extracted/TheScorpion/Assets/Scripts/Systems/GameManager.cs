using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton GameManager — controls game state, pause, restart.
/// Attach to an empty GameObject named "GameManager" in the scene.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Playing, Paused, GameOver, Victory }
    public GameState CurrentState { get; private set; } = GameState.Playing;

    public delegate void OnGameStateChanged(GameState newState);
    public event OnGameStateChanged GameStateChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
                PauseGame();
            else if (CurrentState == GameState.Paused)
                ResumeGame();
        }
    }

    public void SetState(GameState state)
    {
        CurrentState = state;
        GameStateChanged?.Invoke(state);

        switch (state)
        {
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
            case GameState.Playing:
                Time.timeScale = 1f;
                break;
            case GameState.GameOver:
                Time.timeScale = 0f;
                break;
            case GameState.Victory:
                Time.timeScale = 0f;
                break;
        }
    }

    public void PauseGame() => SetState(GameState.Paused);
    public void ResumeGame() => SetState(GameState.Playing);

    public void GameOver()
    {
        SetState(GameState.GameOver);
        Debug.Log("GAME OVER");
    }

    public void Victory()
    {
        SetState(GameState.Victory);
        Debug.Log("VICTORY — All waves cleared!");
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
