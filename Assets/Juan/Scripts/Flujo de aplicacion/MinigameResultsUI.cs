using UnityEngine;
using TMPro;
using System.Collections;

public class MinigameResultsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI changeText;
    [SerializeField] private TextMeshProUGUI completedText;

    [Header("Character")]
    [SerializeField] private HeadFixer headFixer;

    [Header("Animation")]
    [SerializeField] private float lerpDuration = 1.5f;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float endDelay = 1.0f;

    private void Start()
    {
        StartCoroutine(AnimateMoney());
    }

    private IEnumerator AnimateMoney()
    {
        int startMoney = RoundData.instance.PreviousMoney;
        int finalMoney = RoundData.instance.Money;
        int change = RoundData.instance.LastMoneyChange;

        ApplyResultFace(change);

        if (!RoundData.instance.IsStoryMode && completedText != null)
        {
            completedText.gameObject.SetActive(true);
            completedText.text = "Minijuegos completados: " + RoundData.instance.CompletedMinigames;
        }
        else if (completedText != null)
        {
            completedText.gameObject.SetActive(false);
        }

    
        moneyText.text = startMoney.ToString();
        changeText.text = change == 0 ? "-0" : change.ToString();

        changeText.color = change < 0 ? Color.red : Color.green;

       
        yield return new WaitForSeconds(startDelay);

  
        yield return StartCoroutine(Pulse(changeText.transform));

     
        float elapsed = 0f;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lerpDuration;

            int currentMoney = Mathf.RoundToInt(Mathf.Lerp(startMoney, finalMoney, t));
            moneyText.text = currentMoney.ToString();

            yield return null;
        }

        moneyText.text = finalMoney.ToString();

    
        yield return new WaitForSeconds(endDelay);

    
        RoundData.instance.NextMinigame();
    }

    private void ApplyResultFace(int change)
    {
        if (headFixer == null)
        {
            headFixer = FindObjectOfType<HeadFixer>();
        }

        if (headFixer == null)
        {
            return;
        }

        HeadFixer.Face resultFace = change < 0 ? HeadFixer.Face.Sad : HeadFixer.Face.Happy;
        headFixer.SwapFace(resultFace);
    }

    private IEnumerator Pulse(Transform target)
    {
        Vector3 originalScale = target.localScale;
        Vector3 targetScale = originalScale * pulseScale;

        float t = 0f;

    
        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float lerp = t / pulseDuration;
            target.localScale = Vector3.Lerp(originalScale, targetScale, lerp);
            yield return null;
        }

        t = 0f;

      
        while (t < pulseDuration)
        {
            t += Time.deltaTime;
            float lerp = t / pulseDuration;
            target.localScale = Vector3.Lerp(targetScale, originalScale, lerp);
            yield return null;
        }

        target.localScale = originalScale;
    }
}