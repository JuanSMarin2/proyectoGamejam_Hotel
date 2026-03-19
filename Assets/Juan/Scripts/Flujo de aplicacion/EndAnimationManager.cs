using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndAnimationManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Animator endingAnimator;

    [Header("Triggers")]
    [SerializeField] private string goodTrigger = "Good";
    [SerializeField] private string neutralTrigger = "Neutral";
    [SerializeField] private string badTrigger = "Bad";

    [Header("Scene")]
    [SerializeField] private string finalSceneName = "FinalScene";

    private void Start()
    {
        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        int money = RoundData.instance.Money;
        int startMoney = RoundData.instance.StartMoney;


        if (money >= startMoney)
        {
            endingAnimator.SetTrigger(goodTrigger);
        }
        else if (money > 0)
        {
            endingAnimator.SetTrigger(neutralTrigger);
        }
        else
        {
            endingAnimator.SetTrigger(badTrigger);
        }

   
        yield return null;

      
        float animLength = endingAnimator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSecondsRealtime(animLength);

        SceneManager.LoadScene(finalSceneName);
    }
}