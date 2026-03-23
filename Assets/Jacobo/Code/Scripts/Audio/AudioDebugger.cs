using UnityEngine;

public class AudioDebugger : MonoBehaviour
{
    void Update()
    {
        AudioSource[] sources = FindObjectsOfType<AudioSource>();

        foreach (AudioSource source in sources)
        {
            if (source.isPlaying)
            {
                Debug.Log("Sonando: " + source.clip.name + " en " + source.gameObject.name);
            }
        }
    }
}