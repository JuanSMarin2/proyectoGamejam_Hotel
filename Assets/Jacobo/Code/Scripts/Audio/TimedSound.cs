using UnityEngine;

public class TimedSound : MonoBehaviour
{
    [Header("Sound")]
    [SerializeField] private SoundType soundType = SoundType.TimerTicking;
    [SerializeField] private AudioSource targetSource;
    [SerializeField] private float volume = 1f;

    [Header("Timing")]
    [SerializeField] private float intervalSeconds = 1f;
    [SerializeField] private bool startSilent = true;

    private float timer;

    private void Awake()
    {
      
    }

private void Start(){
      timer = startSilent ? Mathf.Max(0.01f, intervalSeconds) : 0f;
      if(!startSilent)
        PlayNow();
}
    

    private void Update()
    {
        float interval = Mathf.Max(0.01f, intervalSeconds);

        timer -= Time.deltaTime;
        if (timer > 0f)
            return;

        PlayNow();
        timer = interval;
    }

    public void PlayNow()
    {
        SoundManager.PlaySound(soundType, targetSource, volume);
    }
}
