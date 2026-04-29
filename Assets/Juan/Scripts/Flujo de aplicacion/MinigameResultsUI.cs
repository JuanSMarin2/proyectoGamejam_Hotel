using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class MinigameResultsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI changeText;
    [SerializeField] private TextMeshProUGUI completedText;

    [Header("Character")]
    [SerializeField] private HeadFixer headFixer;

    [Header("Money Icons (ordered: first, second, third)")]
    [SerializeField] private GameObject[] moneyLeftImages = new GameObject[3];

    [Header("Sound")]
    [SerializeField] private bool playTickOnMoneyTextChange = false;
    [SerializeField] private SoundType tickMoneySoundType = SoundType.TickMoney;
    [SerializeField] private float tickMoneyVolume = 1f;

    [Header("Colors")]
    [SerializeField] private Color positiveChangeColor = Color.green;
    [SerializeField] private Color negativeChangeColor = Color.red;

    [Header("Animation")]
    [SerializeField] private float lerpDuration = 1.5f;
    [SerializeField] private float pulseScale = 1.2f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private float imageDisappearDuration = 0.25f;
    [SerializeField] private float imageDisappearScale = 0.75f;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float endDelay = 1.0f;
    [SerializeField] private float resultSoundDelay = 0.5f;

    private void Start()
    {
        StartCoroutine(AnimateMoney());
    }

    private IEnumerator AnimateMoney()
    {
        if (RoundData.instance == null)
            yield break;

        int startMoney = RoundData.instance.PreviousMoney;
        int finalMoney = RoundData.instance.Money;
        int change = RoundData.instance.LastMoneyChange;
        int startMoneyCap = Mathf.Max(1, RoundData.instance.StartMoney);
        int loseMoneyStep = Mathf.Max(1, RoundData.instance.LoseMoney);

        int lossesBeforeCurrentResult = Mathf.Clamp((startMoneyCap - startMoney) / loseMoneyStep, 0, moneyLeftImages.Length);
        int lossesAfterCurrentResult = Mathf.Clamp((startMoneyCap - finalMoney) / loseMoneyStep, 0, moneyLeftImages.Length);

        ApplyResultFace(change);
        StartCoroutine(PlayResultSoundDelayed(change));
        ApplyImagesState(lossesBeforeCurrentResult);

        if (!RoundData.instance.IsStoryMode && completedText != null)
        {
            completedText.gameObject.SetActive(true);
            completedText.text = "Minijuegos completados: " + RoundData.instance.CompletedMinigames;
        }
        else if (completedText != null)
        {
            completedText.gameObject.SetActive(false);
        }

    
        moneyText.text = FormatMoney(startMoney);
        changeText.text = FormatMoney(change == 0 ? 0 : change);

        changeText.color = change < 0 ? negativeChangeColor : positiveChangeColor;

       
        yield return new WaitForSeconds(startDelay);

  
        yield return StartCoroutine(Pulse(changeText.transform));

     
        float elapsed = 0f;
        int lastDisplayedMoney = startMoney;

        while (elapsed < lerpDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lerpDuration;

            int currentMoney = Mathf.RoundToInt(Mathf.Lerp(startMoney, finalMoney, t));

            if (playTickOnMoneyTextChange && currentMoney != lastDisplayedMoney)
                SoundManager.PlaySound(tickMoneySoundType, null, tickMoneyVolume);

            moneyText.text = FormatMoney(currentMoney);
            lastDisplayedMoney = currentMoney;

            yield return null;
        }

        moneyText.text = FormatMoney(finalMoney);

        if (change < 0)
            yield return StartCoroutine(AnimateLostMoneyImages(lossesBeforeCurrentResult, lossesAfterCurrentResult));

    
        yield return new WaitForSeconds(endDelay);

    
        RoundData.instance.NextMinigame();
    }

    private IEnumerator PlayResultSoundDelayed(int change)
    {
        yield return new WaitForSeconds(resultSoundDelay);
        PlayResultSound(change);
    }

    private void PlayResultSound(int change)
    {
        if (change < 0)
        {
            SoundManager.PlaySound(SoundType.NegativeResultMinijuego);
            return;
        }

        SoundManager.PlaySound(SoundType.PositiveResultMinijuego);
    }

    private void ApplyResultFace(int change)
    {
        

        if (headFixer == null)
        {
            headFixer = FindFirstObjectByType<HeadFixer>();
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

    private void ApplyImagesState(int hiddenCount)
    {
        if (moneyLeftImages == null || moneyLeftImages.Length == 0)
            return;

        int clampedHidden = Mathf.Clamp(hiddenCount, 0, moneyLeftImages.Length);

        for (int i = 0; i < moneyLeftImages.Length; i++)
        {
            GameObject go = moneyLeftImages[i];
            if (go == null)
                continue;

            bool shouldBeVisible = i >= clampedHidden;
            if (go.activeSelf != shouldBeVisible)
                go.SetActive(shouldBeVisible);

            if (shouldBeVisible)
                go.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator AnimateLostMoneyImages(int hiddenBefore, int hiddenAfter)
    {
        if (moneyLeftImages == null || moneyLeftImages.Length == 0)
            yield break;

        int from = Mathf.Clamp(hiddenBefore, 0, moneyLeftImages.Length);
        int to = Mathf.Clamp(hiddenAfter, 0, moneyLeftImages.Length);

        for (int hideIndex = from; hideIndex < to; hideIndex++)
        {
            if (hideIndex < 0 || hideIndex >= moneyLeftImages.Length)
                continue;

            GameObject go = moneyLeftImages[hideIndex];
            if (go == null || !go.activeSelf)
                continue;

            SoundManager.PlaySound(SoundType.VidaPerdida);
            yield return StartCoroutine(AnimateAndHideImage(go));
        }
    }

    private IEnumerator AnimateAndHideImage(GameObject target)
    {
        if (target == null)
            yield break;

        Transform imageTransform = target.transform;
        Vector3 startScale = imageTransform.localScale;
        Vector3 endScale = startScale * Mathf.Max(0f, imageDisappearScale);

        Graphic graphic = target.GetComponent<Graphic>();
        Color startColor = Color.white;
        bool hasGraphic = graphic != null;

        if (hasGraphic)
            startColor = graphic.color;

        float duration = Mathf.Max(0.01f, imageDisappearDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            imageTransform.localScale = Vector3.Lerp(startScale, endScale, t);

            if (hasGraphic)
            {
                Color c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, t);
                graphic.color = c;
            }

            yield return null;
        }

        if (hasGraphic)
        {
            Color reset = startColor;
            graphic.color = reset;
        }

        imageTransform.localScale = startScale;
        target.SetActive(false);
    }

    private static string FormatMoney(int value)
    {
        return "$" + value;
    }
}