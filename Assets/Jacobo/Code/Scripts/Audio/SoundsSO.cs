using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "SonidosJacobo/Sounds SO", fileName = "Sounds SO")]
public class SoundsSO : ScriptableObject
{
    public List<SoundList> sounds = new List<SoundList>();
}

[Serializable]
public struct SoundList
{
    [Header("Identity")]
    public string id;
    public string category;

    [Header("Mix")]
    [Range(0, 1)] public float volume;
    public AudioMixerGroup mixer;

    [Header("Randomization")]
    [Range(0f, 2f)] public float randomVolumeMin;
    [Range(0f, 2f)] public float randomVolumeMax;
    [Range(-3f, 3f)] public float minPitch;
    [Range(-3f, 3f)] public float maxPitch;

    [Header("Clips")]
    public AudioClip[] sounds;
}