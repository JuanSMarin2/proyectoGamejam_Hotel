using System.Collections;
using UnityEngine;

public class VisualNeed : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterNecesidad characterNecesidad;
    [SerializeField] private GameObject holder;
    [SerializeField] private SpriteRenderer needRenderer;

    [Header("Need Sprites")]
    [SerializeField] private Sprite spriteSed;
    [SerializeField] private Sprite spriteSol;
    [SerializeField] private Sprite spriteDiversion;

    [Header("Progress Visual")]
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Vector3 startScale = Vector3.one;
    [SerializeField] private Vector3 endScale = new Vector3(0.7f, 0.7f, 1f);

    [Header("Resolve Visual")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private float resolveFadeOutDuration = 0.3f;

    [Header("Sound")]
    [SerializeField] private bool playCheckSound = true;
    [SerializeField] private string checkSoundId = "Comprar";

    private Coroutine activeRoutine;
    private float currentSolveTime = 4f;

    private void Awake()
    {
        if (characterNecesidad == null)
            characterNecesidad = FindAnyObjectByType<CharacterNecesidad>();

        if (holder == null && needRenderer != null)
            holder = needRenderer.gameObject;

        HideVisualImmediate();
    }

    private void OnEnable()
    {
        if (characterNecesidad == null) return;

        characterNecesidad.NeedStarted += HandleNeedStarted;
        characterNecesidad.NeedResolved += HandleNeedResolved;
        characterNecesidad.NeedFailed += HandleNeedFailed;
    }

    private void OnDisable()
    {
        if (characterNecesidad != null)
        {
            characterNecesidad.NeedStarted -= HandleNeedStarted;
            characterNecesidad.NeedResolved -= HandleNeedResolved;
            characterNecesidad.NeedFailed -= HandleNeedFailed;
        }

        StopCurrentRoutine();
    }

    private void HandleNeedStarted(Necesidad need)
    {
        Sprite sprite = GetSpriteByNeed(need);
        if (sprite == null || needRenderer == null || holder == null)
            return;

        StopCurrentRoutine();

        needRenderer.sprite = sprite;
        needRenderer.color = startColor;
        holder.transform.localScale = startScale;
        holder.SetActive(true);

        currentSolveTime = Mathf.Max(0.05f, characterNecesidad != null ? characterNecesidad.SolveTime : currentSolveTime);
        activeRoutine = StartCoroutine(AnimateNeedOverTime());
    }

    private void HandleNeedResolved()
    {
        if (needRenderer == null || holder == null)
            return;

        StopCurrentRoutine();

        if (playCheckSound && !string.IsNullOrWhiteSpace(checkSoundId))
            SoundManager.PlaySound(checkSoundId);

        activeRoutine = StartCoroutine(ResolveAndFadeOut());
    }

    private void HandleNeedFailed(Necesidad failedNeed)
    {
        StopCurrentRoutine();
        HideVisualImmediate();
    }

    private IEnumerator AnimateNeedOverTime()
    {
        float elapsed = 0f;

        while (elapsed < currentSolveTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / currentSolveTime);

            if (needRenderer != null)
                needRenderer.color = Color.Lerp(startColor, dangerColor, t);

            if (holder != null)
                holder.transform.localScale = Vector3.Lerp(startScale, endScale, t);

            yield return null;
        }
    }

    private IEnumerator ResolveAndFadeOut()
    {
        if (needRenderer != null)
            needRenderer.color = successColor;

        if (holder == null || needRenderer == null)
        {
            HideVisualImmediate();
            yield break;
        }

        float duration = Mathf.Max(0.01f, resolveFadeOutDuration);
        float elapsed = 0f;

        Color start = needRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Color c = start;
            c.a = Mathf.Lerp(start.a, 0f, t);
            needRenderer.color = c;

            yield return null;
        }

        HideVisualImmediate();
    }

    private void HideVisualImmediate()
    {
        if (needRenderer != null)
        {
            Color c = needRenderer.color;
            c.a = 1f;
            needRenderer.color = c;
        }

        if (holder != null)
        {
            holder.transform.localScale = startScale;
            holder.SetActive(false);
        }
    }

    private void StopCurrentRoutine()
    {
        if (activeRoutine == null) return;
        StopCoroutine(activeRoutine);
        activeRoutine = null;
    }

    private Sprite GetSpriteByNeed(Necesidad need)
    {
        switch (need)
        {
            case Necesidad.Sed:
                return spriteSed;
            case Necesidad.Sol:
                return spriteSol;
            case Necesidad.Diversion:
                return spriteDiversion;
            default:
                return null;
        }
    }
}
