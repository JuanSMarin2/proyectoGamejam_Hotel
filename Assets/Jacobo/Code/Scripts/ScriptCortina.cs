using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScriptCortina : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform curtainRect;

    [Header("Positions")]
    [SerializeField] private Vector2 pointA;
    [SerializeField] private Vector2 pointB;

    [Header("Timing")]
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private float timeQuedarseEnPuntoB = 0.5f;
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Audio")]
    [SerializeField] private SoundType curtainSound = SoundType.Cortina;
    [SerializeField] private float curtainSoundVolume = 1f;

    private Coroutine activeRoutine;

    private void Awake()
    {
        if (curtainRect == null)
            curtainRect = GetComponent<RectTransform>();
    }

    public bool IsPlaying => activeRoutine != null;

    public void PlayCurtain(Action onCompleted = null)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(PlayCurtainRoutine(null, onCompleted));
    }

    // Play full curtain A -> B -> (wait) -> A. Optionally invoke `onReachedB` when the curtain reaches point B,
    // and `onCompleted` when the full round-trip finishes.
    public IEnumerator PlayCurtainRoutine(Action onReachedB = null, Action onCompleted = null)
    {
        if (curtainRect == null)
        {
            onCompleted?.Invoke();
            activeRoutine = null;
            yield break;
        }

        PlayCurtainSound();

        float travelDuration = Mathf.Max(0.01f, animationDuration);

        curtainRect.anchoredPosition = pointA;

        // Move A -> B
        yield return MoveBetweenPoints(pointA, pointB, travelDuration);

        // Invoke mid-callback so callers can continue flow when curtain reaches B.
        onReachedB?.Invoke();

        yield return new WaitForSecondsRealtime(0f);

        float waitTime = Mathf.Max(0f, timeQuedarseEnPuntoB);
        if (waitTime > 0f)
        {
            if (useUnscaledTime)
                yield return new WaitForSecondsRealtime(waitTime);
            else
                yield return new WaitForSeconds(waitTime);
        }

        // Move B -> A
        yield return MoveBetweenPoints(pointB, pointA, travelDuration);

        curtainRect.anchoredPosition = pointA;
        activeRoutine = null;
        onCompleted?.Invoke();
    }

    private IEnumerator MoveBetweenPoints(Vector2 from, Vector2 to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += GetDeltaTime();
            float t = Mathf.Clamp01(elapsed / duration);
            curtainRect.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
            yield return null;
        }

        curtainRect.anchoredPosition = to;
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private void PlayCurtainSound()
    {
        if (FindAnyObjectByType<SoundManager>() == null)
            return;

        SoundManager.PlaySound(curtainSound, null, curtainSoundVolume);
    }
}
