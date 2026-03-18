using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class RoundData : MonoBehaviour
{
    public static RoundData instance;

    [Header("Money")]
    [SerializeField] private int startMoney = 100;
    [SerializeField] private int loseMoney = 10;


    [SerializeField] private string endSceneName = "EndAnimationScene";

    public int StartMoney => startMoney;

    private int money;

    [Header("Minigames")]
    private List<string> sceneOrder = new List<string>();
    private int currentIndex = 0;


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