using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

   
    public class SoundManager : MonoBehaviour
    {
        [SerializeField] private SoundsSO SO;
        private static SoundManager instance = null;
        private AudioSource audioSource;

        private const string PrefSfxVolume = "audio.sfx.volume";
        private const string PrefMusicVolume = "audio.music.volume";
        private const string PrefSfxLastNonZero = "audio.sfx.lastNonZero";
        private const string PrefMusicLastNonZero = "audio.music.lastNonZero";

        private static bool prefsLoaded;
        private static float sfxUserVolume = 1f;
        private static float musicUserVolume = 1f;
        private static float sfxLastNonZeroVolume = 1f;
        private static float musicLastNonZeroVolume = 1f;
        private static float pauseDucking = 1f;
        private static float normalPauseDucking = 1f;

        [Header("SFX Pool")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private bool expandPoolIfNeeded = true;
        [SerializeField] private int maxPoolSize = 32;
        [SerializeField] private bool allowVoiceStealing = true;

        [Header("Debug")]
        [SerializeField] private bool debugSceneAudioSync = false;

        private readonly List<AudioSource> sfxPool = new List<AudioSource>();
        private readonly Dictionary<string, SoundList> soundLookup = new Dictionary<string, SoundList>(StringComparer.OrdinalIgnoreCase);
        private int stealIndex;
        private SceneAudioSourceVolumeSync sceneAudioSourceVolumeSync;

        private void Awake()
        {
            instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

            EnsurePrefsLoaded();

            BuildSoundLookup();
            WarmPool();
            sceneAudioSourceVolumeSync = new SceneAudioSourceVolumeSync(
                SO,
                transform,
                audioSource,
                GetEffectiveSfxVolume,
                debugSceneAudioSync
            );
              sceneAudioSourceVolumeSync?.ApplyConfiguredVolumesToSceneAudioSources(SceneManager.GetActiveScene());
              if(sceneAudioSourceVolumeSync == null)
              {
                  Debug.LogWarning("SceneAudioSourceVolumeSync is null. Scene audio source volumes will not be synced.");
              }
              else if (debugSceneAudioSync)
              {
                  Debug.Log("SceneAudioSourceVolumeSync initialized successfully.");
              }
             
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }


        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (instance == this)
                instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sceneAudioSourceVolumeSync?.ApplyConfiguredVolumesToSceneAudioSources(scene);
        }

        private void WarmPool()
        {
            sfxPool.Clear();

            int size = Mathf.Max(1, initialPoolSize);
            for (int i = 0; i < size; i++)
            {
                sfxPool.Add(CreatePooledSource(i));
            }
        }

        private AudioSource CreatePooledSource(int index)
        {
            GameObject go = new GameObject($"SFX_{index:00}");
            go.transform.SetParent(transform, false);

            AudioSource src = go.AddComponent<AudioSource>();
            CopyAudioSourceSettings(audioSource, src);
            return src;
        }

        private static void CopyAudioSourceSettings(AudioSource template, AudioSource target)
        {
            if (target == null) return;

            target.playOnAwake = false;
            target.loop = false;

            if (template == null) return;

            target.mute = template.mute;
            target.bypassEffects = template.bypassEffects;
            target.bypassListenerEffects = template.bypassListenerEffects;
            target.bypassReverbZones = template.bypassReverbZones;
            target.priority = template.priority;
            target.pitch = 1f;
            target.panStereo = template.panStereo;
            target.spatialBlend = template.spatialBlend;
            target.reverbZoneMix = template.reverbZoneMix;
            target.dopplerLevel = template.dopplerLevel;
            target.spread = template.spread;
            target.rolloffMode = template.rolloffMode;
            target.minDistance = template.minDistance;
            target.maxDistance = template.maxDistance;
        }

        private AudioSource GetSfxSource()
        {
            for (int i = 0; i < sfxPool.Count; i++)
            {
                if (!sfxPool[i].isPlaying)
                    return sfxPool[i];
            }

            if (expandPoolIfNeeded && sfxPool.Count < Mathf.Max(1, maxPoolSize))
            {
                AudioSource src = CreatePooledSource(sfxPool.Count);
                sfxPool.Add(src);
                return src;
            }

            if (!allowVoiceStealing || sfxPool.Count == 0)
                return null;

            stealIndex = (stealIndex + 1) % sfxPool.Count;
            AudioSource stolen = sfxPool[stealIndex];
            stolen.Stop();
            return stolen;
        }

        private static void EnsurePrefsLoaded()
        {
            if (prefsLoaded)
                return;

            sfxUserVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxVolume, 1f));
            musicUserVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicVolume, 1f));

            sfxLastNonZeroVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefSfxLastNonZero, Mathf.Max(0.01f, sfxUserVolume)));
            musicLastNonZeroVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMusicLastNonZero, Mathf.Max(0.01f, musicUserVolume)));

            if (sfxLastNonZeroVolume <= 0f) sfxLastNonZeroVolume = 1f;
            if (musicLastNonZeroVolume <= 0f) musicLastNonZeroVolume = 1f;

            prefsLoaded = true;
        }

        private static void SavePrefs()
        {
            PlayerPrefs.SetFloat(PrefSfxVolume, sfxUserVolume);
            PlayerPrefs.SetFloat(PrefMusicVolume, musicUserVolume);
            PlayerPrefs.SetFloat(PrefSfxLastNonZero, sfxLastNonZeroVolume);
            PlayerPrefs.SetFloat(PrefMusicLastNonZero, musicLastNonZeroVolume);
            PlayerPrefs.Save();
        }

        public static float GetSfxVolume()
        {
            EnsurePrefsLoaded();
            return sfxUserVolume;
        }

        public static float GetMusicVolume()
        {
            EnsurePrefsLoaded();
            return musicUserVolume;
        }

        public static float GetEffectiveSfxVolume()
        {
            EnsurePrefsLoaded();
            return sfxUserVolume * pauseDucking;
        }

        public static float GetEffectiveMusicVolume()
        {
            EnsurePrefsLoaded();
            return musicUserVolume * pauseDucking;
        }

        public static void SetSfxVolume(float value01)
        {
            EnsurePrefsLoaded();

            sfxUserVolume = Mathf.Clamp01(value01);
            if (sfxUserVolume > 0.0001f)
                sfxLastNonZeroVolume = sfxUserVolume;

            SavePrefs();
            RefreshSyncedSceneSfxAudioSources();
        }

        public static void SetMusicVolume(float value01)
        {
            EnsurePrefsLoaded();

            musicUserVolume = Mathf.Clamp01(value01);
            if (musicUserVolume > 0.0001f)
                musicLastNonZeroVolume = musicUserVolume;

            SavePrefs();
        }

        public static void MuteSfx()
        {
            EnsurePrefsLoaded();

            if (sfxUserVolume > 0.0001f)
                sfxLastNonZeroVolume = sfxUserVolume;

            sfxUserVolume = 0f;
            SavePrefs();
            RefreshSyncedSceneSfxAudioSources();
        }

        public static void UnmuteSfxRestore()
        {
            EnsurePrefsLoaded();

            if (sfxUserVolume > 0.0001f)
                return;

            sfxUserVolume = Mathf.Clamp01(Mathf.Max(0.01f, sfxLastNonZeroVolume));
            SavePrefs();
            RefreshSyncedSceneSfxAudioSources();
        }

        public static void MuteMusic()
        {
            EnsurePrefsLoaded();

            if (musicUserVolume > 0.0001f)
                musicLastNonZeroVolume = musicUserVolume;

            musicUserVolume = 0f;
            SavePrefs();
        }

        public static void UnmuteMusicRestore()
        {
            EnsurePrefsLoaded();

            if (musicUserVolume > 0.0001f)
                return;

            musicUserVolume = Mathf.Clamp01(Mathf.Max(0.01f, musicLastNonZeroVolume));
            SavePrefs();
        }

        public static void SetGlobalVolume(float value01)
        {
            pauseDucking = Mathf.Clamp01(value01);
            RefreshSyncedSceneSfxAudioSources();
        }

        public static void LowerGlobalVolume(float value01 = 0.25f)
        {
            normalPauseDucking = pauseDucking;
            SetGlobalVolume(value01);
        }

        public static void RestoreGlobalVolume()
        {
            SetGlobalVolume(normalPauseDucking);
        }

        public static void PlaySound(SoundType sound, AudioSource source = null, float volume = 1)
        {
            PlaySound(sound.ToString(), source, volume);
        }

        public static bool PlayLoopedSound(SoundType sound, AudioSource source, float volume = 1f)
        {
            return PlayLoopedSound(sound.ToString(), source, volume);
        }

        public static bool PlayLoopedSound(string soundId, AudioSource source, float volume = 1f)
        {
            if (source == null)
            {
                Debug.LogWarning("[SoundManager] Source is null. Cannot play looped sound: " + soundId);
                return false;
            }

            if (instance == null)
            {
                Debug.LogWarning("[SoundManager] No instance in scene. Cannot play looped sound: " + soundId);
                return false;
            }

            if (!instance.TryGetSound(soundId, out SoundList soundList))
            {
                Debug.LogWarning("[SoundManager] Sound ID not found: " + soundId);
                return false;
            }

            AudioClip randomClip = GetRandomValidClip(soundList.sounds);
            if (randomClip == null)
            {
                Debug.LogWarning("[SoundManager] No valid clips assigned for sound: " + soundId);
                return false;
            }

            float randomizedVolumeMultiplier = UnityEngine.Random.Range(
                Mathf.Min(soundList.randomVolumeMin, soundList.randomVolumeMax),
                Mathf.Max(soundList.randomVolumeMin, soundList.randomVolumeMax));

            float randomizedPitch = UnityEngine.Random.Range(
                Mathf.Min(soundList.minPitch, soundList.maxPitch),
                Mathf.Max(soundList.minPitch, soundList.maxPitch));

            source.outputAudioMixerGroup = soundList.mixer;
            source.volume = volume * soundList.volume * randomizedVolumeMultiplier * GetEffectiveMusicVolume();
            source.pitch = randomizedPitch;
            source.loop = true;
            source.clip = randomClip;
            source.Play();
            return true;
        }

        public static void StopSound(AudioSource source)
        {
            if (source == null)
                return;

            source.Stop();
            source.clip = null;
            source.loop = false;
        }

        public static void PlaySound(string soundId, AudioSource source = null, float volume = 1f)
        {
            if (instance == null)
            {
                Debug.LogWarning("[SoundManager] No instance in scene. Cannot play sound: " + soundId);
                return;
            }

            if (!instance.TryGetSound(soundId, out SoundList soundList))
            {
                Debug.LogWarning("[SoundManager] Sound ID not found: " + soundId);
                return;
            }

            AudioClip randomClip = GetRandomValidClip(soundList.sounds);
            if (randomClip == null)
            {
                Debug.LogWarning("[SoundManager] No valid clips assigned for sound: " + soundId);
                return;
            }

            float randomizedVolumeMultiplier = UnityEngine.Random.Range(
                Mathf.Min(soundList.randomVolumeMin, soundList.randomVolumeMax),
                Mathf.Max(soundList.randomVolumeMin, soundList.randomVolumeMax));

            float randomizedPitch = UnityEngine.Random.Range(
                Mathf.Min(soundList.minPitch, soundList.maxPitch),
                Mathf.Max(soundList.minPitch, soundList.maxPitch));

            if(source)
            {
                source.outputAudioMixerGroup = soundList.mixer;
                source.volume = volume * soundList.volume * randomizedVolumeMultiplier * GetEffectiveSfxVolume();
                source.pitch = randomizedPitch;

                // Permite solapar sonidos en el mismo AudioSource.
                source.PlayOneShot(randomClip);
            }
            else
            {
                AudioSource sfx = instance.GetSfxSource();
                if (sfx == null) return;

                sfx.outputAudioMixerGroup = soundList.mixer;
                sfx.volume = volume * soundList.volume * randomizedVolumeMultiplier * GetEffectiveSfxVolume();
                sfx.pitch = randomizedPitch;
                sfx.clip = randomClip;
                sfx.Play();
            }
        }

        private void BuildSoundLookup()
        {
            soundLookup.Clear();

            if (SO == null || SO.sounds == null)
                return;

            for (int i = 0; i < SO.sounds.Count; i++)
            {
                SoundList entry = SO.sounds[i];
                if (string.IsNullOrWhiteSpace(entry.id))
                    continue;

                if (entry.volume <= 0f)
                    entry.volume = 1f;

                if (Mathf.Approximately(entry.randomVolumeMin, 0f) && Mathf.Approximately(entry.randomVolumeMax, 0f))
                {
                    entry.randomVolumeMin = 1f;
                    entry.randomVolumeMax = 1f;
                }

                if (Mathf.Approximately(entry.minPitch, 0f) && Mathf.Approximately(entry.maxPitch, 0f))
                {
                    entry.minPitch = 1f;
                    entry.maxPitch = 1f;
                }

                if (soundLookup.ContainsKey(entry.id))
                {
                    Debug.LogWarning("[SoundManager] Duplicate sound id found. Last one wins: " + entry.id);
                    soundLookup[entry.id] = entry;
                }
                else
                {
                    soundLookup.Add(entry.id, entry);
                }
            }
        }

        private bool TryGetSound(string soundId, out SoundList sound)
        {
            if (string.IsNullOrWhiteSpace(soundId))
            {
                sound = default;
                return false;
            }

            return soundLookup.TryGetValue(soundId, out sound);
        }

        private static AudioClip GetRandomValidClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
                return null;

            List<AudioClip> valid = null;
            for (int i = 0; i < clips.Length; i++)
            {
                if (clips[i] == null) continue;

                if (valid == null)
                    valid = new List<AudioClip>();

                valid.Add(clips[i]);
            }

            if (valid == null || valid.Count == 0)
                return null;

            int random = UnityEngine.Random.Range(0, valid.Count);
            return valid[random];
        }

        private static void RefreshSyncedSceneSfxAudioSources()
        {
            if (instance == null || instance.sceneAudioSourceVolumeSync == null)
                return;

            instance.sceneAudioSourceVolumeSync.ApplyConfiguredVolumesToSceneAudioSources(SceneManager.GetActiveScene());
        }
    }