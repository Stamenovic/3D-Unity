using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    [SerializeField] private PlayerInput playerInput;

    public bool IsPaused => GameManager.Instance != null && GameManager.Instance.State == GameState.Paused;

    // Standalone action so it fires even while PlayerInput is disabled during pause
    private InputAction pauseAction;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        pauseAction = new InputAction("Pause", binding: "<Keyboard>/escape");
        pauseAction.performed += _ => TogglePause();
    }

    private void OnEnable() => pauseAction.Enable();

    private void OnDisable() => pauseAction.Disable();

    private void OnDestroy() => pauseAction.Dispose();

    private void Start()
    {
        if (playerInput == null)
            playerInput = FindFirstObjectByType<PlayerInput>();
    }

    public void PauseGame()
    {
        if (IsPaused) return;
        Time.timeScale = 0f;
        if (playerInput != null) playerInput.enabled = false;
        GameManager.Instance.SetState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        Time.timeScale = 1f;
        if (playerInput != null) playerInput.enabled = true;
        GameManager.Instance.SetState(GameState.Playing);
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    public void ResetLevel()
    {
        Time.timeScale = 1f;
        if (GameManager.Instance != null)
            GameManager.Instance.LoadScene(SceneManager.GetActiveScene().buildIndex);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
