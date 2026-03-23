using UnityEngine;

public class TimedSound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private SoundType soundType = SoundType.TimerTicking;
    [SerializeField] private AudioSource targetSource;
    [SerializeField] private float volume = 1f;

    [Header("Timing")]
    [SerializeField] private float intervalSeconds = 1f;
    [SerializeField] private bool useRandomInterval = false;
    [SerializeField] private float minIntervalSeconds = 1f;
    [SerializeField] private float maxIntervalSeconds = 3f;
    [SerializeField] private bool startSilent = true;

    private float timer;

    private void Awake()
    {
      
    }

    private void Start()
    {
        timer = startSilent ? GetNextInterval() : 0f;
        if (!startSilent)
            PlayNow();
    }
    

    private void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        PlayNow();
        timer = GetNextInterval();
    }

    private float GetNextInterval()
    {
        if (!useRandomInterval)
            return Mathf.Max(0.01f, intervalSeconds);

        float min = Mathf.Max(0.01f, Mathf.Min(minIntervalSeconds, maxIntervalSeconds));
        float max = Mathf.Max(0.01f, Mathf.Max(minIntervalSeconds, maxIntervalSeconds));
        return Random.Range(min, max);
    }

    public void PlayNow()
    {
        SoundManager.PlaySound(soundType, targetSource, volume);
    }
}
