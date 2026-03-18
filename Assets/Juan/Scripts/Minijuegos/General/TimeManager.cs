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
        UpdateBar();
    }

    private void Update()
    {
   
        if (hasFinished) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            timer = 0f;
            hasFinished = true;
            UpdateBar();
            ResultManager.instance.WinMinigame();
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
        UpdateBar();
    }
}