using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager instance;

    [Header("UI")]
    [SerializeField] private GameObject pausePanel;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private bool pausedByManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("Escape presionado. Pausa actual: " + pausedByManager);
            if (pausedByManager)
            {
                Continue();
                return;
            }

            if (!HasMinigameManagerInScene())
            {
                return;
            }

            Pause();
        }
    }

    public void Pause()
    {
        if (pausedByManager)
        {
            return;
        }

        if (!HasMinigameManagerInScene())
        {
            return;
        }

        if (Time.timeScale <= 0f)
        {
            return;
        }

        pausedByManager = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    public void Continue()
    {
        if (!pausedByManager)
        {
            return;
        }

        pausedByManager = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void GoToMainMenu()
    {
        pausedByManager = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        RoundData.ResetForMainMenu();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private bool HasMinigameManagerInScene()
    {
        return FindObjectOfType<MinigameManager>() != null;
    }
}
