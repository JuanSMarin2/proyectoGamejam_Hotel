using System;
using UnityEngine;

public class GeneralMusicAmbienceOnStart : MonoBehaviour
{
    [Header("Sound Data")]
    [SerializeField] private SoundsSO soundsSO;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private string musicSoundId = "MenuMusic";
    [SerializeField] private bool playMusicOnStart = true;
    [SerializeField] private bool musicLoop = true;
    [SerializeField] private float musicAssignedVolume = 1f;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceSource;
    [SerializeField] private string ambienceSoundId = "AmbientMusic";
    [SerializeField] private bool playAmbienceOnStart = true;
    [SerializeField] private bool ambienceLoop = true;
    [SerializeField] private float ambienceAssignedVolume = 1f;

    [Header("Volume")]
    [SerializeField] private bool useMusicVolumeForMusic = true;
    [SerializeField] private bool useMusicVolumeForAmbience = true;

    private void Awake()
    {
        if (soundsSO == null)
            Debug.LogWarning("[GeneralMusicAmbienceOnStart] SoundsSO is missing.", this);

        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SoundManager.VolumeSettingsChanged += HandleVolumeSettingsChanged;

        if (playMusicOnStart)
            ConfigureAndPlay(musicSource, musicSoundId, musicLoop, useMusicVolumeForMusic, musicAssignedVolume);

        if (playAmbienceOnStart)
            ConfigureAndPlay(ambienceSource, ambienceSoundId, ambienceLoop, useMusicVolumeForAmbience, ambienceAssignedVolume);

        RefreshActiveVolumes();
    }

    private void OnDisable()
    {
        SoundManager.VolumeSettingsChanged -= HandleVolumeSettingsChanged;
    }

    private void ConfigureAndPlay(AudioSource source, string soundId, bool loop, bool useMusicVolume, float assignedVolume)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundId, out SoundList entry))
        {
            Debug.LogWarning($"[GeneralMusicAmbienceOnStart] Sound ID not found in SoundsSO: {soundId}", this);
            return;
        }

        AudioClip clip = GetFirstValidClip(entry.sounds);
        if (clip == null)
        {
            Debug.LogWarning($"[GeneralMusicAmbienceOnStart] No valid clip for sound ID: {soundId}", this);
            return;
        }

        source.outputAudioMixerGroup = entry.mixer;
        source.clip = clip;
        source.loop = loop;
        source.pitch = 1f;

        source.volume = SoundManager.ComposeFinalVolume(entry.volume, assignedVolume, useMusicVolume);

        source.Play();
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

    private void HandleVolumeSettingsChanged()
    {
        RefreshActiveVolumes();
    }

    private void RefreshActiveVolumes()
    {
        if (playMusicOnStart)
            RefreshSourceVolume(musicSource, musicSoundId, musicAssignedVolume, useMusicVolumeForMusic);

        if (playAmbienceOnStart)
            RefreshSourceVolume(ambienceSource, ambienceSoundId, ambienceAssignedVolume, useMusicVolumeForAmbience);
    }

    private void RefreshSourceVolume(AudioSource source, string soundId, float assignedVolume, bool useMusicChannel)
    {
        if (source == null)
            return;

        if (!TryGetSoundEntry(soundId, out SoundList entry))
            return;

        source.volume = SoundManager.ComposeFinalVolume(entry.volume, assignedVolume, useMusicChannel);
    }
}
