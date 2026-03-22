using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class Fader : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private float fadeInDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.25f;
    [SerializeField] private bool playFadeInOnEnable = true;
    [SerializeField] private bool useUnscaledTime = true;

    private CanvasGroup canvasGroup;
    private Coroutine currentFade;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        if (!playFadeInOnEnable)
        {
            return;
        }

        StartFade(0f, 1f, fadeInDuration);
    }

    public void Activate()
    {
        gameObject.SetActive(true);
        StartFade(0f, 1f, fadeInDuration);
    }

    public void Deactivate()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }

        currentFade = StartCoroutine(FadeOutAndDisable());
    }

    public void SetVisible(bool visible)
    {
        if (visible)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    private IEnumerator FadeOutAndDisable()
    {
        yield return FadeRoutine(canvasGroup.alpha, 0f, fadeOutDuration);
        gameObject.SetActive(false);
    }

    private void StartFade(float from, float to, float duration)
    {
        if (currentFade != null)
        {
            StopCoroutine(currentFade);
        }

        currentFade = StartCoroutine(FadeRoutine(from, to, duration));
    }

    private IEnumerator FadeRoutine(float from, float to, float duration)
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (duration <= 0f)
        {
            SetAlpha(to);
            currentFade = null;
            yield break;
        }

        SetAlpha(from);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetAlpha(to);
        currentFade = null;
    }

    private void SetAlpha(float value)
    {
        canvasGroup.alpha = Mathf.Clamp01(value);
        bool visible = canvasGroup.alpha > 0.001f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }
}
