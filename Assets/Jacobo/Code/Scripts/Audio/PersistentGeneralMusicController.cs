using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentGeneralMusicController : MonoBehaviour
{
    private static PersistentGeneralMusicController _instance;
    private static int _externalPauseRequests;

    [Header("References")]
    [SerializeField] private AudioSource musicSource;

    [Header("Pause By Scene")]
    [SerializeField] private string[] pauseInScenes = { "Tienda", "Buceo", "Recepcionista" };
    [SerializeField] private bool sceneNameCaseInsensitive = true;
    [SerializeField] private bool playIfStoppedWhenUnpaused = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        TryAttachAsChildOfSoundManager();

        // Keep this object alive. Prefer keeping SoundManager root alive so this can stay its child.
        SoundManager sm = FindFirstObjectByType<SoundManager>();
        if (sm != null)
            DontDestroyOnLoad(sm.gameObject);
        else
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        Scene current = SceneManager.GetActiveScene();
        ApplyPauseStateForScene(current.name);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttachAsChildOfSoundManager();
        ApplyPauseStateForScene(scene.name);
    }

    private void TryAttachAsChildOfSoundManager()
    {
        SoundManager sm = FindFirstObjectByType<SoundManager>();
        if (sm == null)
        {
            if (debugLogs)
                Debug.LogWarning("[PersistentGeneralMusicController] No SoundManager found to parent music object.", this);
            return;
        }

        if (transform.parent != sm.transform)
            transform.SetParent(sm.transform, true);

        // Ensure parent root is persistent across scenes.
        DontDestroyOnLoad(sm.gameObject);
    }

    private void ApplyPauseStateForScene(string sceneName)
    {
        if (musicSource == null)
        {
            if (debugLogs)
                Debug.LogWarning("[PersistentGeneralMusicController] Missing AudioSource reference.", this);
            return;
        }

        bool shouldPause = ShouldPauseInScene(sceneName) || _externalPauseRequests > 0;

        if (shouldPause)
        {
            if (musicSource.isPlaying)
                musicSource.Pause();

            if (debugLogs)
                Debug.Log($"[PersistentGeneralMusicController] Music paused in scene '{sceneName}'.", this);
            return;
        }

        if (musicSource.clip != null)
        {
            if (playIfStoppedWhenUnpaused && !musicSource.isPlaying)
                musicSource.Play();
            else
                musicSource.UnPause();
        }

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] Music unpaused in scene '{sceneName}'.", this);
    }

    public static void RequestExternalPause()
    {
        _externalPauseRequests++;
        if (_instance != null)
            _instance.ApplyPauseStateForScene(SceneManager.GetActiveScene().name);
    }

    public static void ReleaseExternalPause()
    {
        _externalPauseRequests = Mathf.Max(0, _externalPauseRequests - 1);
        if (_instance != null)
            _instance.ApplyPauseStateForScene(SceneManager.GetActiveScene().name);
    }

    private bool ShouldPauseInScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName) || pauseInScenes == null || pauseInScenes.Length == 0)
            return false;

        StringComparison comparison = sceneNameCaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        for (int i = 0; i < pauseInScenes.Length; i++)
        {
            string blockedScene = pauseInScenes[i];
            if (string.IsNullOrWhiteSpace(blockedScene))
                continue;

            if (string.Equals(sceneName, blockedScene.Trim(), comparison))
                return true;
        }

        return false;
    }
}
