using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class FinalSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentMoneyText;
    [SerializeField] private TextMeshProUGUI globalMoneyText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Buttons")]
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject shopButton;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float transferDuration = 2f;

    [Header("Audio")]
    [SerializeField] private int coinSoundStep = 10;
    [SerializeField] private SoundType transferTickSoundType = SoundType.TickMoney;
    [SerializeField] private float transferTickVolume = 1f;

    private void Start()
    {
        StartCoroutine(TransferMoney());
    }

    private IEnumerator TransferMoney()
    {
        int roundMoney = RoundData.instance.IsStoryMode
            ? RoundData.instance.Money
            : RoundData.instance.CompletedMinigames * 100;
        int globalMoney = GameData.instance.Money;

        int startRoundMoney = roundMoney;
        int startGlobalMoney = globalMoney;
        int lastCoinStepPlayed = 0;


        currentMoneyText.text = startRoundMoney.ToString();
        globalMoneyText.text = startGlobalMoney.ToString();

        homeButton.SetActive(false);
        shopButton.SetActive(false);
        resultText.gameObject.SetActive(false);

   
        yield return new WaitForSeconds(startDelay);

        float elapsed = 0f;

        while (elapsed < transferDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transferDuration;

            int current = Mathf.RoundToInt(Mathf.Lerp(startRoundMoney, 0, t));
            int global = Mathf.RoundToInt(Mathf.Lerp(startGlobalMoney, startGlobalMoney + startRoundMoney, t));

            currentMoneyText.text = current.ToString();
            globalMoneyText.text = global.ToString();

            int transferred = startRoundMoney - current;
            int coinStep = Mathf.Max(1, coinSoundStep);
            int currentCoinStep = transferred / coinStep;
            while (lastCoinStepPlayed < currentCoinStep)
            {
                lastCoinStepPlayed++;
                SoundManager.PlaySound(transferTickSoundType, null, transferTickVolume);
            }

            yield return null;
        }

 
        currentMoneyText.text = "0";
        globalMoneyText.text = (startGlobalMoney + startRoundMoney).ToString();

 
        GameData.instance.Money += startRoundMoney;

    
        resultText.gameObject.SetActive(true);
        resultText.text = RoundData.instance.IsStoryMode
            ? "Conservaste " + startRoundMoney + "$"
            : "Conseguiste " + startRoundMoney + "$ por completar " + RoundData.instance.CompletedMinigames + " minijuegos!";

   
        homeButton.SetActive(true);
        shopButton.SetActive(true);
    }

 
    public void LoadScene(string sceneName)
    {
        if (sceneName == "MainMenu")
        {
            RoundData.ResetForMainMenu();
        }

        SceneManager.LoadScene(sceneName);
    }
}