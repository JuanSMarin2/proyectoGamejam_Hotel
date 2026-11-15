using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System;

public class FinalSceneManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI currentMoneyText;
    [SerializeField] private TextMeshProUGUI globalMoneyText;
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("Buttons")]
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject shopButton;
    [Tooltip("Boton para volver a jugar el modo infinito con el mismo usuario (opcional)")]
    [SerializeField] private GameObject replayButton;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float transferDuration = 2f;

    [Header("Audio")]
    [SerializeField] private int coinSoundStep = 10;
    [SerializeField] private SoundType transferTickSoundType = SoundType.TickMoney;
    [SerializeField] private float transferTickVolume = 1f;

    private bool leaderboardSubmitted;
    private bool transferFinished;

    private void Start()
    {
        StartCoroutine(TransferMoney());

        if (!RoundData.instance.IsStoryMode)
        {
            SubmitInfiniteLeaderboardScore();
        }
    }

    private async void SubmitInfiniteLeaderboardScore()
    {
        if (leaderboardSubmitted)
            return;

        leaderboardSubmitted = true;

        try
        {
            // CurrentUser se encarga de guardar solo el mejor puntaje (new best)
            await CurrentUser.Instance.SubmitInfiniteModeScoreAsync(RoundData.instance.CompletedMinigames);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("FinalSceneManager: no se pudo enviar la puntuacion al leaderboard. " + exception.Message);
        }
    }

   public void LoadLeaderboardScene(){
        SceneManager.LoadScene("Leaderboard");
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
        if (replayButton != null)
            replayButton.SetActive(false);
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


        transferFinished = true;

        homeButton.SetActive(true);
        shopButton.SetActive(true);

        // El replay solo aplica al modo infinito
        if (replayButton != null)
            replayButton.SetActive(!RoundData.instance.IsStoryMode);
    }

    // Boton de replay: vuelve a jugar el modo infinito con el mismo usuario,
    // sin pasar por el menu principal.
    public void PlayAgain()
    {
        // El dinero de la ronda se suma al terminar la transferencia;
        // no se permite reintentar antes para no perderlo.
        if (!transferFinished)
            return;

        // No se destruye RoundData: conserva la lista de minijuegos.
        // CurrentUser persiste entre escenas, asi que el usuario sigue siendo el mismo.
        RoundData.instance.RestartInfiniteRun();

        SceneManager.LoadScene("IntroScene");
    }
    public void LoadScene(string sceneName)
    {

            RoundData.ResetForMainMenu();


        SceneManager.LoadScene(sceneName);
    }
}