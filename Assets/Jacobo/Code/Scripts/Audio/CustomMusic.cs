using System;
using UnityEngine;

public class CustomMusic : MonoBehaviour
{
    [Header("Sound Data")]
    [SerializeField] private SoundsSO soundsSO;

    [Header("Custom Music")]
    [SerializeField] private bool playMusic = true;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private string musicSoundId = "MenuMusic";
    [SerializeField] private float musicAssignedVolume = 1f;
    [SerializeField] private bool musicLoop = true;

    [Header("Optional Ambience")]
    [SerializeField] private bool playAmbience = false;
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private string ambienceSoundId = "AmbientMusic";
    [SerializeField] private float ambienceAssignedVolume = 1f;
    [SerializeField] private bool ambienceLoop = true;

    [Header("Volume Channel")]
    [SerializeField] private bool useMusicChannelForMusic = true;
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
            ConfigureAndPlay(musicSource, musicSoundId, musicLoop, musicAssignedVolume, useMusicChannelForMusic);

        if (playAmbience)
            ConfigureAndPlay(ambienceSource, ambienceSoundId, ambienceLoop, ambienceAssignedVolume, useMusicChannelForAmbience);

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
            RefreshSourceVolume(musicSource, musicSoundId, musicAssignedVolume, useMusicChannelForMusic);

        if (playAmbience)
            RefreshSourceVolume(ambienceSource, ambienceSoundId, ambienceAssignedVolume, useMusicChannelForAmbience);
    }

    private void RefreshSourceVolume(AudioSource source, string soundId, float assignedVolume, bool useMusicChannel)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundId, out SoundList entry))
            return;

        source.volume = SoundManager.ComposeFinalVolume(entry.volume, assignedVolume, useMusicChannel);

        if (debugLogs)
            Debug.Log($"[CustomMusic] Refreshed volume -> source='{source.name}', id='{soundId}', finalVolume={source.volume:F3}", this);
    }

    private void ConfigureAndPlay(AudioSource source, string soundId, bool loop, float assignedVolume, bool useMusicChannel)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundId, out SoundList entry))
        {
            Debug.LogWarning($"[CustomMusic] Sound ID not found in SoundsSO: {soundId}", this);
            return;
        }

        AudioClip clip = GetFirstValidClip(entry.sounds);
        if (clip == null)
        {
            Debug.LogWarning($"[CustomMusic] No valid clip for sound ID: {soundId}", this);
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
            Debug.Log($"[CustomMusic] Playing -> source='{source.name}', id='{soundId}', clip='{clipName}', loop={loop}, finalVolume={source.volume:F3}", this);
        }
    }

    private bool TryGetSoundEntry(string soundId, out SoundList entry)
    {
        entry = default;

        if (soundsSO == null || soundsSO.sounds == null || string.IsNullOrWhiteSpace(soundId))
            return false;

        for (int i = 0; i < soundsSO.sounds.Count; i++)
        {
            SoundList candidate = soundsSO.sounds[i];
            if (string.IsNullOrWhiteSpace(candidate.id))
                continue;

            if (string.Equals(candidate.id, soundId, StringComparison.OrdinalIgnoreCase))
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
