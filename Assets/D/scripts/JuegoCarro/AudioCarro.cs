using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioCarro : MonoBehaviour
{
    [Header("Sound Types")]
    [SerializeField] private SoundType engineLoopSoundType = SoundType.Motores;
    [SerializeField] private SoundType accelerateOneShotSoundType = SoundType.AcelerandoCarro;
    [SerializeField] private SoundType brakeOneShotSoundType = SoundType.Frenado;

    [Header("Engine Tuning")]
    [SerializeField] private float engineBaseVolume = 1f;
    [SerializeField] private float accelerationVolumeBoost = 0.20f;
    [SerializeField] private float normalPitch = 1f;
    [SerializeField] private float acceleratedPitch = 2.5f;
    [SerializeField] private float pitchLerpSpeed = 6f;
    [SerializeField] private float volumeLerpSpeed = 6f;
    [SerializeField] private float throttleThreshold = 0.1f;

    [Header("One Shot Volumes")]
    [SerializeField] private float accelerateOneShotVolume = 1f;
    [SerializeField] private float brakeOneShotVolume = 1f;

    private AudioSource engineSource;
    private bool engineStarted;
    private bool isAccelerating;
    private float targetPitch;
    private float targetVolumeMultiplier;

    private void Awake()
    {
        engineSource = GetComponent<AudioSource>();
        engineSource.playOnAwake = false;
        engineSource.loop = true;
        targetPitch = normalPitch;
        targetVolumeMultiplier = 1f;
    }

    private void OnDisable()
    {
        StopEngine();
    }

    public void StartEngine()
    {
        if (engineStarted)
            return;

        if (!SoundManager.TryGetSoundData(engineLoopSoundType, out _))
        {
            Debug.LogWarning($"[AudioCarro] Engine sound type not found: {engineLoopSoundType}", this);
            return;
        }

        SoundManager.PlayLoopedSound(engineLoopSoundType, engineSource, engineBaseVolume);
        engineSource.pitch = normalPitch;
        engineStarted = true;
        RefreshEngineVolume();
    }

    public void StopEngine()
    {
        if (engineSource != null)
            SoundManager.StopSound(engineSource);

        engineStarted = false;
    }

    public void SetThrottleInput(Vector2 input)
    {
        bool acceleratingNow = input.y > throttleThreshold;

        if (acceleratingNow && !isAccelerating)
            PlayAccelerateOneShot();

        isAccelerating = acceleratingNow;
        targetPitch = isAccelerating ? acceleratedPitch : normalPitch;
        targetVolumeMultiplier = isAccelerating ? 1f + accelerationVolumeBoost : 1f;
    }

    public void PlayBrakeOneShot()
    {
        SoundManager.PlaySound(brakeOneShotSoundType, null, brakeOneShotVolume);
        targetPitch = normalPitch;
        targetVolumeMultiplier = 1f;
        isAccelerating = false;
    }

    private void PlayAccelerateOneShot()
    {
        SoundManager.PlaySound(accelerateOneShotSoundType, null, accelerateOneShotVolume);
    }

    private void Update()
    {
        if (!engineStarted || engineSource == null)
            return;

        engineSource.pitch = Mathf.MoveTowards(
            engineSource.pitch,
            targetPitch,
            Mathf.Max(0.01f, pitchLerpSpeed) * Time.deltaTime);

        RefreshEngineVolume();
    }

    private void RefreshEngineVolume()
    {
        if (engineSource == null)
            return;

        float soundVolume = 1f;
        if (SoundManager.TryGetSoundData(engineLoopSoundType, out SoundList soundData))
            soundVolume = Mathf.Max(0f, soundData.volume);

        float effectiveVolume = engineBaseVolume * targetVolumeMultiplier;
        engineSource.volume = SoundManager.GetEffectiveSfxVolume() * soundVolume * Mathf.Max(0f, effectiveVolume);
    }
}
