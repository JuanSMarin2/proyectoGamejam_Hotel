using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentGeneralMusicController : MonoBehaviour
{
    private static PersistentGeneralMusicController _instance;

    [Header("References")]
    [SerializeField] private AudioSource musicSource;

    [Header("Music Source")]
    [SerializeField] private bool configureFromSoundType = true;
    [SerializeField] private SoundType generalLoopedMusicType = SoundType.MusicaGeneral;

    [Header("Game Speed Pitch (Optional)")]
    [SerializeField] private bool useGameSpeedForPitch = false;
    [SerializeField] private float baseGameSpeed = 1f;
    [SerializeField] private float pitchAtBaseSpeed = 1f;
    [SerializeField] private float pitchMultiplier = 0.25f;
    [SerializeField] private float minPitch = 0.8f;
    [SerializeField] private float maxPitch = 1.4f;
    [SerializeField] private float pitchRefreshInterval = 0.15f;

    [Header("Volume Sync")]
    [SerializeField] private bool syncWithMusicSlider = true;
    [SerializeField] private bool autoCaptureBaseVolumeFromSource = true;
    [SerializeField] private float persistentBaseVolume = 1f;

    [Header("Start Scene")]
    [SerializeField] private bool requireStartSceneForFirstPlay = true;
    [SerializeField] private string startSceneName = "MainMenu";
    [SerializeField] private bool startSceneCaseInsensitive = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private float pitchRefreshTimer;
    private bool hasCapturedBaseVolume;
    private bool hasStartedPersistentMusic;

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
        SoundManager.VolumeSettingsChanged += HandleVolumeSettingsChanged;

        pitchRefreshTimer = 0f;

        EnsureMusicSourceConfiguredFromEnum();

        Scene current = SceneManager.GetActiveScene();
        hasStartedPersistentMusic = musicSource != null && (musicSource.isPlaying || musicSource.time > 0f);
        ApplyGeneralMusicStateForScene(current);
        UpdatePitchFromGameSpeed(true);
        TryCaptureBaseVolumeFromCurrentSource();
        RefreshPersistentMusicVolume();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SoundManager.VolumeSettingsChanged -= HandleVolumeSettingsChanged;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttachAsChildOfSoundManager();
        EnsureMusicSourceConfiguredFromEnum();
        ApplyGeneralMusicStateForScene(scene);
        UpdatePitchFromGameSpeed(true);
        TryCaptureBaseVolumeFromCurrentSource();
        RefreshPersistentMusicVolume();
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

            RefreshPersistentMusicVolume();

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
                {
                    if (!CanStartForScene(scene.name))
                    {
                        if (debugLogs)
                            Debug.Log($"[PersistentGeneralMusicController] Waiting to start general music. Current scene '{scene.name}' does not match startSceneName '{startSceneName}'.", this);

                        return;
                    }

                    musicSource.Play();
                    hasStartedPersistentMusic = true;
                }
            }

            TryCaptureBaseVolumeFromCurrentSource();
            RefreshPersistentMusicVolume();

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

    private void HandleVolumeSettingsChanged()
    {
        EnsureMusicSourceConfiguredFromEnum();
        RefreshPersistentMusicVolume();
    }

    private void EnsureMusicSourceConfiguredFromEnum()
    {
        if (!configureFromSoundType || musicSource == null)
            return;

        if (!SoundManager.TryGetSoundData(generalLoopedMusicType, out SoundList entry))
            return;

        AudioClip clip = GetFirstValidClip(entry.sounds);
        if (clip == null)
            return;

        musicSource.outputAudioMixerGroup = entry.mixer;
        musicSource.loop = true;

        if (musicSource.clip != clip)
            musicSource.clip = clip;

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] Configured musicSource from SoundType='{generalLoopedMusicType}', clip='{clip.name}'.", this);
    }

    private static AudioClip GetFirstValidClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
            return null;

        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] != null)
                return clips[i];
        }

        return null;
    }

    private void TryCaptureBaseVolumeFromCurrentSource()
    {
        if (!autoCaptureBaseVolumeFromSource || hasCapturedBaseVolume)
            return;

        if (musicSource == null)
            return;

        float effectiveMusic = SoundManager.GetEffectiveMusicVolume();
        if (effectiveMusic <= 0.0001f)
            return;

        if (musicSource.volume <= 0f)
            return;

        persistentBaseVolume = Mathf.Max(0f, musicSource.volume / effectiveMusic);
        hasCapturedBaseVolume = true;

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] Captured persistentBaseVolume={persistentBaseVolume:F3} from current source volume.", this);
    }

    private void RefreshPersistentMusicVolume()
    {
        if (!syncWithMusicSlider || musicSource == null)
            return;

        musicSource.volume = SoundManager.ComposeFinalVolume(1f, persistentBaseVolume, true);

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] Volume refresh from slider -> finalVolume={musicSource.volume:F3}", this);
    }

    public static void RestartPersistentMusicFromBeginning()
    {
        if (_instance == null)
            return;

        _instance.RestartFromBeginningInternal();
    }

    private void RestartFromBeginningInternal()
    {
        if (musicSource == null || musicSource.clip == null)
            return;

        UpdatePitchFromGameSpeed(true);
        musicSource.Stop();
        musicSource.time = 0f;
        musicSource.Play();
        hasStartedPersistentMusic = true;
        RefreshPersistentMusicVolume();

        if (debugLogs)
            Debug.Log($"[PersistentGeneralMusicController] RestartPersistentMusicFromBeginning -> clip='{musicSource.clip.name}'", this);
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

            CustomMusic[] customs = roots[i].GetComponentsInChildren<CustomMusic>(true);
            if (customs == null || customs.Length == 0)
                continue;

            for (int c = 0; c < customs.Length; c++)
            {
                CustomMusic custom = customs[c];
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

    private bool CanStartForScene(string sceneName)
    {
        if (hasStartedPersistentMusic)
            return true;

        if (!requireStartSceneForFirstPlay)
            return true;

        if (string.IsNullOrWhiteSpace(startSceneName))
            return true;

        StringComparison comparison = startSceneCaseInsensitive
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(sceneName, startSceneName.Trim(), comparison);
    }
}
