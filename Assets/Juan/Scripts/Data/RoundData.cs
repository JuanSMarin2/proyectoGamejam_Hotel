using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class RoundData : MonoBehaviour
{
    public static RoundData instance;

    [Header("Mode")]
    [SerializeField] private bool isStoryMode = false;

    public bool IsStoryMode => isStoryMode;

    [Header("Money")]
    [SerializeField] private int startMoney = 100;
    [SerializeField] private int loseMoney = 10;


    [SerializeField] private string endSceneName = "EndAnimationScene";

    public int StartMoney => startMoney;

    private int money;

    [Header("Minigames")]
    private List<string> sceneOrder = new List<string>();
    private int currentIndex = 0;

    [SerializeField] private float storyStartSpeed = 2f;
    [SerializeField] private float storySpeedIncreasePerMinigame = 0.3f;

    [SerializeField] private float nonStoryStartSpeed = 1f;
    [SerializeField] private float nonStorySpeedIncreasePerMinigame = 0.3f;
    [SerializeField] private int nonStoryIncreaseMultiplierEvery = 5;
    [SerializeField] private float nonStoryIncreaseMultiplier = 1.5f;

    private int currentMinigameIndex = -1;

    public int CurrentMinigameIndex => currentMinigameIndex;

    public int CurrentMinigameNumber => currentMinigameIndex < 0 ? 0 : (currentMinigameIndex + 1);


    private int previousMoney;
    private int lastMoneyChange;



    public int PreviousMoney => previousMoney;
    public int LastMoneyChange => lastMoneyChange;

    public int Money => money;



    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    private void Start()
    {
        money = startMoney;
    }

    public void SetGameOrder(List<string> order)
    {
        sceneOrder = order;
        currentIndex = 0;
        currentMinigameIndex = -1;
    }

    public void SetStoryMode(bool value)
    {
        isStoryMode = value;
    }

    public float GetCurrentMinigameSpeed()
    {
        int minigameIndex = Mathf.Max(0, currentMinigameIndex);

        if (isStoryMode)
        {
            return Mathf.Max(0f, storyStartSpeed + (minigameIndex * storySpeedIncreasePerMinigame));
        }

        if (minigameIndex == 0) return Mathf.Max(0f, nonStoryStartSpeed);

        float speedValue = nonStoryStartSpeed;
        int every = Mathf.Max(1, nonStoryIncreaseMultiplierEvery);

        for (int t = 0; t < minigameIndex; t++)
        {
            int transitionNumber = t + 1;
            int exponent = transitionNumber / every;
            float factor = Mathf.Pow(nonStoryIncreaseMultiplier, exponent);
            speedValue += nonStorySpeedIncreasePerMinigame * factor;
        }

        return Mathf.Max(0f, speedValue);
    }

    public void NextMinigame()
    {
      
        if (money <= 0)
        {
            LoadEndScene();
            return;
        }

      
        if (currentIndex >= sceneOrder.Count)
        {
            LoadEndScene();
            return;
        }

        string nextScene = sceneOrder[currentIndex];
        currentMinigameIndex = currentIndex;
        currentIndex++;

        SceneManager.LoadScene(nextScene);
    }

    private void LoadEndScene()
    {
        SceneManager.LoadScene(endSceneName);
    }

    public void MinigameResult(bool won)
    {
        previousMoney = money;

        if (!won)
        {
            money -= loseMoney;
            money = Mathf.Max(0, money);
            lastMoneyChange = -loseMoney;
        }
        else
        {
            lastMoneyChange = 0;
        }

        Debug.Log("Resultado: " + (won ? "Win" : "Lose") +
                  " | Money: " + money);
    }
}