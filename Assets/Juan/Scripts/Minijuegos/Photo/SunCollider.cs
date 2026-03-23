using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class SunCollider : MonoBehaviour
{

    public bool isBlocked = false;
    [SerializeField] private Image backlighting;
    [SerializeField] private float fadeDuration = 0.25f;

    private const float MaxBacklightingAlpha = 175f / 255f;

    private readonly HashSet<Collider2D> collidersInside = new HashSet<Collider2D>();
    private readonly Dictionary<PhotoParticleActiver, int> activersInside = new Dictionary<PhotoParticleActiver, int>();
    private Coroutine backlightingFadeCoroutine;

    private void Awake()
    {
        SetBacklightingAlpha(0f);
    }

 private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        collidersInside.Add(other);
        isBlocked = collidersInside.Count > 0;

        PhotoParticleActiver activer = other.GetComponentInParent<PhotoParticleActiver>();
        if (activer != null)
        {
            if (!activersInside.ContainsKey(activer))
                activersInside[activer] = 0;

            activersInside[activer]++;
        }

        UpdateBacklightingState();
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
            return;

        collidersInside.Remove(other);
        isBlocked = collidersInside.Count > 0;

        PhotoParticleActiver activer = other.GetComponentInParent<PhotoParticleActiver>();
        if (activer != null && activersInside.TryGetValue(activer, out int count))
        {
            count--;
            if (count <= 0)
                activersInside.Remove(activer);
            else
                activersInside[activer] = count;
        }

        UpdateBacklightingState();
        
    }

    public void ActivateAngryParticlesInside()
    {
        foreach (KeyValuePair<PhotoParticleActiver, int> entry in activersInside)
        {
            PhotoParticleActiver activer = entry.Key;
            if (activer == null)
                continue;

            activer.ActivateAngryParticle();
        }
    }

    private void UpdateBacklightingState()
    {
        float targetAlpha = isBlocked ? MaxBacklightingAlpha : 0f;
        StartBacklightingFade(targetAlpha);
    }

    private void StartBacklightingFade(float targetAlpha)
    {
        if (backlighting == null)
            return;

        if (backlightingFadeCoroutine != null)
            StopCoroutine(backlightingFadeCoroutine);

        backlightingFadeCoroutine = StartCoroutine(FadeBacklightingAlpha(targetAlpha));
    }

    private System.Collections.IEnumerator FadeBacklightingAlpha(float targetAlpha)
    {
        Color color = backlighting.color;
        float startAlpha = color.a;
        float elapsed = 0f;

        if (Mathf.Approximately(fadeDuration, 0f))
        {
            SetBacklightingAlpha(targetAlpha);
            backlightingFadeCoroutine = null;
            yield break;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            SetBacklightingAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetBacklightingAlpha(targetAlpha);
        backlightingFadeCoroutine = null;
    }

    private void SetBacklightingAlpha(float alpha)
    {
        if (backlighting == null)
            return;

        Color color = backlighting.color;
        color.a = alpha;
        backlighting.color = color;
    }
}
