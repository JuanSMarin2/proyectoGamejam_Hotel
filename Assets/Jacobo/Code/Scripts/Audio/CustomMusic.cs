using UnityEngine;

public class CustomMusic : MonoBehaviour
{
    [Header("Sound Data")]
    [SerializeField] private SoundsSO soundsSO;

    [Header("Custom Music")]
    [SerializeField] private bool playMusic = true;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private SoundType musicSoundType = SoundType.MusicaGeneral;
    [SerializeField] private float musicAssignedVolume = 1f;
    [SerializeField] private bool musicLoop = true;

    [Header("Optional Ambience")]
    [SerializeField] private bool playAmbience = false;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private SoundType ambienceSoundType = SoundType.AmbientMusic;
    [SerializeField] private float ambienceAssignedVolume = 1f;
    [SerializeField] private bool ambienceLoop = true;

    [Header("Volume Channel")]
    [SerializeField] private bool useMusicChannelForAmbience = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public bool PlayMusicEnabled => playMusic;

    private void Awake()
    {
        if (soundsSO == null)
            Debug.LogWarning("[CustomMusic] SoundsSO is missing.", this);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        SoundManager.VolumeSettingsChanged += HandleVolumeSettingsChanged;

        if (debugLogs)
            Debug.Log($"[CustomMusic] Enabled on '{gameObject.scene.name}'. playMusic={playMusic}, playAmbience={playAmbience}", this);

        PlayConfigured();
    }

    private void OnDisable()
    {
        SoundManager.VolumeSettingsChanged -= HandleVolumeSettingsChanged;
    }

    private void PlayConfigured()
    {
        if (playMusic)
            ConfigureAndPlay(musicSource, musicSoundType, musicLoop, musicAssignedVolume, true);

        if (playAmbience)
            ConfigureAndPlay(ambienceSource, ambienceSoundType, ambienceLoop, ambienceAssignedVolume, useMusicChannelForAmbience);

        RefreshActiveVolumes();
    }

    private void HandleVolumeSettingsChanged()
    {
        if (debugLogs)
            Debug.Log("[CustomMusic] Volume settings changed. Refreshing active source volumes.", this);

        RefreshActiveVolumes();
    }

    private void RefreshActiveVolumes()
    {
        if (playMusic)
            RefreshSourceVolume(musicSource, musicSoundType, musicAssignedVolume, true);

        if (playAmbience)
            RefreshSourceVolume(ambienceSource, ambienceSoundType, ambienceAssignedVolume, useMusicChannelForAmbience);
    }

    private void RefreshSourceVolume(AudioSource source, SoundType soundType, float assignedVolume, bool useMusicChannel)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundType, out SoundList entry))
            return;

        source.volume = SoundManager.ComposeFinalVolume(entry.volume, assignedVolume, useMusicChannel);

        if (debugLogs)
            Debug.Log($"[CustomMusic] Refreshed volume -> source='{source.name}', soundType='{soundType}', finalVolume={source.volume:F3}", this);
    }

    private void ConfigureAndPlay(AudioSource source, SoundType soundType, bool loop, float assignedVolume, bool useMusicChannel)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundType, out SoundList entry))
        {
            Debug.LogWarning($"[CustomMusic] SoundType not found in SoundsSO: {soundType}", this);
            return;
        }

        AudioClip clip = GetFirstValidClip(entry.sounds);
        if (clip == null)
        {
            Debug.LogWarning($"[CustomMusic] No valid clip for SoundType: {soundType}", this);
            return;
        }

        source.outputAudioMixerGroup = entry.mixer;
        source.clip = clip;
        source.loop = loop;
        source.pitch = 1f;
        source.volume = SoundManager.ComposeFinalVolume(entry.volume, assignedVolume, useMusicChannel);

        source.Play();

        if (debugLogs)
        {
            string clipName = source.clip != null ? source.clip.name : "(null clip)";
            Debug.Log($"[CustomMusic] Playing -> source='{source.name}', soundType='{soundType}', clip='{clipName}', loop={loop}, finalVolume={source.volume:F3}", this);
        }
    }

    private bool TryGetSoundEntry(SoundType soundType, out SoundList entry)
    {
        entry = default;

        if (soundsSO == null || soundsSO.sounds == null)
            return false;

        for (int i = 0; i < soundsSO.sounds.Count; i++)
        {
            SoundList candidate = soundsSO.sounds[i];
            if (candidate.soundType == soundType)
            {
                entry = candidate;
                return true;
            }
        }

        return false;
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
}
