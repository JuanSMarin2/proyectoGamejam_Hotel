using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using System;

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
    [SerializeField] private string infiniteFinalSceneName = "FinalScene";

    public int StartMoney => startMoney;
    public int LoseMoney => loseMoney;

    private int money;

    [Header("Minigames")]
    private List<string> sceneOrder = new List<string>();
    private int currentIndex = 0;
    private List<string> infiniteShuffledOrder = new List<string>();
    private int infiniteOrderIndex = 0;
    private readonly Dictionary<string, int> minigameAppearanceCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    [SerializeField] private float startSpeed = 1f;
    [SerializeField] private float speedIncreasePerMinigame = 0.2f;
    [SerializeField] private float maxMinigameSpeed = 2.5f;

    private int currentMinigameIndex = -1;

    public int CurrentMinigameIndex => currentMinigameIndex;

    public int CurrentMinigameNumber => currentMinigameIndex < 0 ? 0 : (currentMinigameIndex + 1);


    private int previousMoney;
    private int lastMoneyChange;
    private int completedMinigames;



    public int PreviousMoney => previousMoney;
    public int LastMoneyChange => lastMoneyChange;
    public int CompletedMinigames => completedMinigames;

    public int Money => money;

    public static void ResetForMainMenu()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
            instance = null;
        }
    }



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
        completedMinigames = 0;
        infiniteShuffledOrder.Clear();
        infiniteOrderIndex = 0;
        minigameAppearanceCounts.Clear();
    }

    public int RegisterMinigameAppearance(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return 0;

        if (!minigameAppearanceCounts.TryGetValue(sceneName, out int count))
            count = 0;

        count++;
        minigameAppearanceCounts[sceneName] = count;
        return count;
    }

    public int GetMinigameAppearanceCount(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return 0;

        return minigameAppearanceCounts.TryGetValue(sceneName, out int count) ? count : 0;
    }

    public void SetStoryMode(bool value)
    {
        isStoryMode = value;
    }

    public float GetCurrentMinigameSpeed()
    {
        int minigameIndex = Mathf.Max(0, currentMinigameIndex);
        float speedValue = startSpeed + (minigameIndex * speedIncreasePerMinigame);
        return Mathf.Clamp(speedValue, 0f, maxMinigameSpeed);
    }

    public void NextMinigame()
    {
      
        if (money <= 0)
        {
            if (!isStoryMode)
            {
                SceneManager.LoadScene(infiniteFinalSceneName);
                return;
            }

            LoadEndScene();
            return;
        }

        if (!isStoryMode)
        {
            string nextInfiniteScene = GetNextInfiniteScene();
            if (string.IsNullOrEmpty(nextInfiniteScene))
            {
                SceneManager.LoadScene(infiniteFinalSceneName);
                return;
            }

            currentMinigameIndex++;
            SceneManager.LoadScene(nextInfiniteScene);
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
            completedMinigames++;
        }

        Debug.Log("Resultado: " + (won ? "Win" : "Lose") +
                  " | Money: " + money);
    }

    private string GetNextInfiniteScene()
    {
        if (sceneOrder == null || sceneOrder.Count == 0)
            return null;

        if (infiniteShuffledOrder.Count == 0 || infiniteOrderIndex >= infiniteShuffledOrder.Count)
        {
            infiniteShuffledOrder = new List<string>(sceneOrder);
            ShuffleList(infiniteShuffledOrder);
            infiniteOrderIndex = 0;
        }

        string scene = infiniteShuffledOrder[infiniteOrderIndex];
        infiniteOrderIndex++;
        return scene;
    }

    private void ShuffleList(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }
}