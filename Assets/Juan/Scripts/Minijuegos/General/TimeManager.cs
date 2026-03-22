using UnityEngine;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private float startTime = 10f;

    [Header("References")]
    [SerializeField] private Image imageTimeBar;
 

    private float timer;
    private bool hasFinished = false;
    private bool hasPlayedTimerTicking = false;


    public float StartTime
    {
        get => startTime;
        set
        {
            startTime = Mathf.Max(0f, value);
            timer = startTime;
            UpdateBar();
        }
    }

    private void Start()
    {
        timer = startTime;
        hasPlayedTimerTicking = false;
        UpdateBar();
    }

    private void Update()
    {
   
        if (hasFinished) return;

        timer -= Time.deltaTime;

        if (!hasPlayedTimerTicking && timer <= 3f && timer > 0f)
        {
            hasPlayedTimerTicking = true;
            SoundManager.PlaySound(SoundType.TimerTicking);
        }

        if (timer <= 0f)
        {
            timer = 0f;
            hasFinished = true;
            UpdateBar();

            if(!MinigameManager.instance.LosesWithTime)
            {
                if (CharacterRunner.IsRunnerSceneActive())
                {
                    SoundManager.PlaySound(SoundType.Avion);
                }

                if (SwimController.IsSwimSceneActive())
                {
                    SoundManager.PlaySound(SoundType.PjRiendo);
                }

                ResultManager.instance.WinMinigame();
            }
            else
                ResultManager.instance.LoseMinigame();

            return;
        }

        UpdateBar();
    }

    private void UpdateBar()
    {
        if (imageTimeBar != null && startTime > 0f)
        {
            imageTimeBar.fillAmount = timer / startTime;
        }
    }

    public void ResetTimer()
    {
        timer = startTime;
        hasFinished = false;
        hasPlayedTimerTicking = false;
        UpdateBar();
    }
}