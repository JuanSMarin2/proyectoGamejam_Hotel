using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentGeneralMusicController : MonoBehaviour
{
    private static PersistentGeneralMusicController _instance;

    [Header("References")]
    [SerializeField] private AudioSource musicSource;

    [Header("Game Speed Pitch (Optional)")]
    [SerializeField] private bool useGameSpeedForPitch = false;
    [SerializeField] private float baseGameSpeed = 1f;
    [SerializeField] private float pitchAtBaseSpeed = 1f;
    [SerializeField] private float pitchMultiplier = 0.25f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.4f;
    [SerializeField] private float pitchRefreshInterval = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float pitchRefreshTimer;

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

        pitchRefreshTimer = 0f;

        Scene current = SceneManager.GetActiveScene();
        ApplyGeneralMusicStateForScene(current);
        UpdatePitchFromGameSpeed(true);
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
        ApplyGeneralMusicStateForScene(scene);
        UpdatePitchFromGameSpeed(true);
    }

    private void Update()
    {
        if (musicSource == null)
            return;

        if (pitchRefreshInterval <= 0f)
        {
            UpdatePitchFromGameSpeed(false);
            return;
        }

        pitchRefreshTimer -= Time.unscaledDeltaTime;
        if (pitchRefreshTimer > 0f)
            return;

        pitchRefreshTimer = pitchRefreshInterval;
        UpdatePitchFromGameSpeed(false);
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

    private void ApplyGeneralMusicStateForScene(Scene scene)
    {
        if (musicSource == null)
        {
            if (debugLogs)
                Debug.LogWarning("[PersistentGeneralMusicController] Missing AudioSource reference.", this);
            return;
        }

        bool hasActiveCustomMusic = SceneHasActiveCustomMusic(scene, out string customMusicName);

        if (hasActiveCustomMusic)
        {
            if (musicSource.isPlaying)
                musicSource.Pause();

            if (debugLogs)
                Debug.Log($"[PersistentGeneralMusicController] General music paused. Active CustomMusic='{customMusicName}' detected in scene '{scene.name}'.", this);
            return;
        }

        if (musicSource.clip != null)
        {
            UpdatePitchFromGameSpeed(true);
            if (!musicSource.isPlaying)
            {
                if (musicSource.time > 0f)
                    musicSource.UnPause();
                else
                    musicSource.Play();
            }

            if (debugLogs)
                Debug.Log($"[PersistentGeneralMusicController] General music active (continuous) clip='{musicSource.clip.name}' time={musicSource.time:F2}s volume={musicSource.volume:F3} in scene '{scene.name}'.", this);
        }
        else if (debugLogs)
        {
            Debug.LogWarning("[PersistentGeneralMusicController] No clip assigned on musicSource.", this);
        }

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] General music kept/resumed in scene '{scene.name}' (no active CustomMusic).", this);
    }

    private bool SceneHasActiveCustomMusic(Scene scene, out string customMusicName)
    {
        customMusicName = string.Empty;

        if (!scene.IsValid() || !scene.isLoaded)
            return false;

        GameObject[] roots = scene.GetRootGameObjects();
        if (roots == null || roots.Length == 0)
            return false;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] == null)
                continue;

            CustomMusic custom = roots[i].GetComponentInChildren<CustomMusic>(true);
            if (custom == null)
                continue;

            // Disabled CustomMusic does NOT count, and playMusic must be enabled to replace general music.
            if (custom.enabled && custom.gameObject.activeInHierarchy && custom.PlayMusicEnabled)
            {
                customMusicName = custom.name;
                return true;
            }

            if (debugLogs && custom.enabled && custom.gameObject.activeInHierarchy && !custom.PlayMusicEnabled)
                Debug.Log($"[PersistentGeneralMusicController] Ignoring CustomMusic='{custom.name}' in scene '{scene.name}' because Play Music is disabled.", custom);
        }

        return false;
    }

    private void UpdatePitchFromGameSpeed(bool force)
    {
        if (musicSource == null)
            return;

        if (!useGameSpeedForPitch)
        {
            if (force || !Mathf.Approximately(musicSource.pitch, 1f))
                musicSource.pitch = 1f;
            return;
        }

        float gameSpeed = 1f;
        if (RoundData.instance != null)
            gameSpeed = Mathf.Max(0f, RoundData.instance.GetCurrentMinigameSpeed());

        float unclampedPitch = pitchAtBaseSpeed + ((gameSpeed - baseGameSpeed) * pitchMultiplier);
        float clampedMin = Mathf.Min(minPitch, maxPitch);
        float clampedMax = Mathf.Max(minPitch, maxPitch);
        float targetPitch = Mathf.Clamp(unclampedPitch, clampedMin, clampedMax);

        if (!force && Mathf.Abs(musicSource.pitch - targetPitch) < 0.001f)
            return;

        musicSource.pitch = targetPitch;

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] Pitch update -> gameSpeed={gameSpeed:F3}, pitch={targetPitch:F3}, useGameSpeedForPitch={useGameSpeedForPitch}", this);
    }
}
