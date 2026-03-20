using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SceneAudioSourceVolumeSync
{
    private readonly SoundsSO soundSO;
    private readonly Transform ownerTransform;
    private readonly AudioSource ownerAudioSource;
    private readonly Func<float> getEffectiveSfxVolume;
    private readonly bool debugLogs;

    private readonly Dictionary<AudioClip, float> clipVolumeLookup = new Dictionary<AudioClip, float>();
    private readonly Dictionary<AudioMixerGroup, float> mixerVolumeLookup = new Dictionary<AudioMixerGroup, float>();
    private bool lookupsBuilt;

    public SceneAudioSourceVolumeSync(
        SoundsSO soundSO,
        Transform ownerTransform,
        AudioSource ownerAudioSource,
        Func<float> getEffectiveSfxVolume,
        bool debugLogs = false)
    {
        this.soundSO = soundSO;
        this.ownerTransform = ownerTransform;
        this.ownerAudioSource = ownerAudioSource;
        this.getEffectiveSfxVolume = getEffectiveSfxVolume;
        this.debugLogs = debugLogs;

        BuildVolumeLookups();
    }

    public void ApplyConfiguredVolumesToSceneAudioSources(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            if (debugLogs)
                Debug.LogWarning($"[SceneAudioSync] Escena inválida o no cargada. valid={scene.IsValid()} loaded={scene.isLoaded}");
            return;
        }

        AudioSource[] sources = UnityEngine.Object.FindObjectsOfType<AudioSource>(true);
        if (sources == null || sources.Length == 0)
        {
            if (debugLogs)
                Debug.LogWarning($"[SceneAudioSync] No se encontraron AudioSource en escena '{scene.name}'.");
            return;
        }

        float effectiveSfx = getEffectiveSfxVolume != null ? getEffectiveSfxVolume() : 1f;
        int total = 0, otherScene = 0, ownerIgnored = 0, twoDIgnored = 0, noMatch = 0, applied = 0;

        if (debugLogs)
            Debug.Log($"[SceneAudioSync] Iniciando sync en escena '{scene.name}'. sources={sources.Length}, effectiveSfx={effectiveSfx:F3}");

        for (int i = 0; i < sources.Length; i++)
        {
            AudioSource src = sources[i];
            if (src == null) continue;
            total++;

            // Solo objetos de la escena cargada, no de otras escenas o DDOL.
            if (src.gameObject.scene != scene)
            {
                otherScene++;
                continue;
            }

            // Evita tocar el AudioSource plantilla del manager y su pool.
            if (ownerTransform != null && src.transform.IsChildOf(ownerTransform))
            {
                ownerIgnored++;
                continue;
            }

            if (src == ownerAudioSource)
            {
                ownerIgnored++;
                continue;
            }

            // Solo SFX 3D en escena, no música/UI 2D.
            if (src.spatialBlend <= 0.01f)
            {
                twoDIgnored++;
                continue;
            }

            if (!TryGetSoundSOBaseVolume(src, out float soundSOBaseVolume))
            {
                noMatch++;
                if (debugLogs)
                {
                    string clipName = src.clip != null ? src.clip.name : "(sin clip)";
                    string mixerName = src.outputAudioMixerGroup != null ? src.outputAudioMixerGroup.name : "(sin mixer)";
                    Debug.LogWarning($"[SceneAudioSync] Sin match SO -> '{src.gameObject.name}' clip={clipName} mixer={mixerName} spatial={src.spatialBlend:F2}");
                }
                continue;
            }

            src.volume = soundSOBaseVolume * effectiveSfx;
            applied++;

            if (debugLogs)
                Debug.Log($"[SceneAudioSync] Applied -> '{src.gameObject.name}' vol={src.volume:F3} (so={soundSOBaseVolume:F3} * sfx={effectiveSfx:F3})");
        }

        if (debugLogs)
            Debug.Log($"[SceneAudioSync] Fin sync '{scene.name}'. total={total}, applied={applied}, otherScene={otherScene}, ownerIgnored={ownerIgnored}, twoDIgnored={twoDIgnored}, noMatch={noMatch}");
    }

    private void BuildVolumeLookups()
    {
        clipVolumeLookup.Clear();
        mixerVolumeLookup.Clear();
        lookupsBuilt = true;

        if (soundSO == null || soundSO.sounds == null)
        {
            if (debugLogs)
                Debug.LogWarning("[SceneAudioSync] SoundsSO es null o sounds está vacío. No se pudo crear lookup.");
            return;
        }

        for (int i = 0; i < soundSO.sounds.Count; i++)
        {
            SoundList list = soundSO.sounds[i];
            float listVolume = Mathf.Clamp01(list.volume);

            if (list.mixer != null && !mixerVolumeLookup.ContainsKey(list.mixer))
                mixerVolumeLookup.Add(list.mixer, listVolume);

            if (list.sounds == null) continue;

            for (int c = 0; c < list.sounds.Length; c++)
            {
                AudioClip clip = list.sounds[c];
                if (clip == null) continue;

                if (!clipVolumeLookup.ContainsKey(clip))
                    clipVolumeLookup.Add(clip, listVolume);
            }
        }

        if (debugLogs)
            Debug.Log($"[SceneAudioSync] Lookup construido. clips={clipVolumeLookup.Count}, mixers={mixerVolumeLookup.Count}, entriesSO={soundSO.sounds.Count}");
    }

    private bool TryGetSoundSOBaseVolume(AudioSource source, out float baseVolume)
    {
        baseVolume = 1f;
        if (source == null)
            return false;

        if (!lookupsBuilt)
            BuildVolumeLookups();

        if (source.clip != null && clipVolumeLookup.TryGetValue(source.clip, out float clipVolume))
        {
            baseVolume = clipVolume;
            return true;
        }

        if (source.outputAudioMixerGroup != null && mixerVolumeLookup.TryGetValue(source.outputAudioMixerGroup, out float mixerVolume))
        {
            baseVolume = mixerVolume;
            return true;
        }

        return false;
    }
}
