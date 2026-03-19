using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class ResultManager : MonoBehaviour
{
    public static ResultManager instance;

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private Animator resultAnimator;

    [Header("Animation")]
    [SerializeField] private string winTrigger = "Win";
    [SerializeField] private string loseTrigger = "Lose";

    [Header("Timing")]
    [SerializeField] private float loseDelay = 1.5f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "MinigameResults";

    private bool hasFinished = false;



    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

 
    public void WinMinigame()
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(true));
    }

   
    public void LoseMinigame()
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("Minijuego perdido, delay de " +loseDelay +" segundos");
        StartCoroutine(LoseWithDelay());
    }

 
    public void DirectLose()
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(false));
    }

    private IEnumerator LoseWithDelay()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(loseDelay);

        yield return HandleResult(false);
    }

    private IEnumerator HandleResult(bool won)
    {
    
        Time.timeScale = 0f;

    
        resultPanel.SetActive(true);

   
        if (won)
            resultAnimator.SetTrigger(winTrigger);
        else
            resultAnimator.SetTrigger(loseTrigger);

     
        yield return null;

 
        float animLength = resultAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSecondsRealtime(animLength);

   
        RoundData.instance.MinigameResult(won);

      
        Time.timeScale = 1f;

   
        SceneManager.LoadScene(nextSceneName);
    }
}