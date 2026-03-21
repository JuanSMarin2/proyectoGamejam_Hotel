using System.Collections;
using UnityEngine;

public class VisualNeed : MonoBehaviour
{
    private enum ProgressMode
    {
        Scale,
        MaskClipVertical
    }

    [Header("References")]
    [SerializeField] private CharacterNecesidad characterNecesidad;
    [SerializeField] private GameObject holder;
    [SerializeField] private SpriteRenderer needRenderer;

    [Header("Need Sprites")]
    [SerializeField] private Sprite spriteSed;
    [SerializeField] private Sprite spriteSol;
    [SerializeField] private Sprite spriteDiversion;

    [Header("Progress Visual")]
    [SerializeField] private ProgressMode progressMode = ProgressMode.Scale;
    [SerializeField] private bool fallbackToScaleIfMaskMissing = true;
    [SerializeField] private Color startColor = Color.white;
    [SerializeField] private Color dangerColor = Color.red;
    [SerializeField] private Vector3 startScale = new Vector3(0.4f, 0.4f, 1f);
    [SerializeField] private Vector3 endScale = new Vector3(0.2f, 0.2f, 1f);
    [SerializeField] private bool includeChildRenderers = true;
    [SerializeField] private float dangerFadeSpeed = 1f; // multiplier for how fast color approaches dangerColor (1 = normal)
    [SerializeField] private Transform[] scaleTargets;

    [Header("Mask Progress (Vertical Clip)")]
    [SerializeField] private Transform verticalMaskDriver;
    [SerializeField] private Vector3 maskFullLocalPosition;
    [SerializeField] private Vector3 maskEmptyLocalPosition = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private bool captureMaskFullPositionOnAwake = true;

    [Header("Resolve Visual")]
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private float resolveFadeOutDuration = 0.3f;

    [Header("Sound")]
    [SerializeField] private bool playCheckSound = true;
    [SerializeField] private string checkSoundId = "Comprar";

    private Coroutine activeRoutine;
    private float currentSolveTime = 4f;
    private SpriteRenderer[] _childRenderers;

    private void Awake()
    {
        if (characterNecesidad == null)
            characterNecesidad = FindAnyObjectByType<CharacterNecesidad>();

        if (holder == null && needRenderer != null)
            holder = needRenderer.gameObject;

        if (holder != null && includeChildRenderers)
            _childRenderers = holder.GetComponentsInChildren<SpriteRenderer>(true);
        else if (needRenderer != null)
            _childRenderers = new[] { needRenderer };

        if ((scaleTargets == null || scaleTargets.Length == 0) && holder != null)
            scaleTargets = new Transform[] { holder.transform };

        if (verticalMaskDriver != null && captureMaskFullPositionOnAwake)
            maskFullLocalPosition = verticalMaskDriver.localPosition;

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

        // Apply start color to all child renderers (including main one)
        if (_childRenderers != null)
        {
            foreach (var r in _childRenderers)
            {
                if (r == null) continue;
                r.color = startColor;
            }
        }
        else if (needRenderer != null)
        {
            needRenderer.color = startColor;
        }

        // Apply start scale to configured targets
        ApplyProgressShape(0f);

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

            // color progression can be sped-up/slowed by dangerFadeSpeed
            float tColor = Mathf.Clamp01((elapsed * dangerFadeSpeed) / currentSolveTime);

            Color col = Color.Lerp(startColor, dangerColor, tColor);
            ApplyColorToTargets(col);
            ApplyProgressShape(t);

            yield return null;
        }
    }

    private IEnumerator ResolveAndFadeOut()
    {
        if (_childRenderers != null)
        {
            foreach (var r in _childRenderers)
                if (r != null) r.color = successColor;
        }
        else if (needRenderer != null)
        {
            needRenderer.color = successColor;
        }

        if (holder == null || needRenderer == null)
        {
            HideVisualImmediate();
            yield break;
        }

        float duration = Mathf.Max(0.01f, resolveFadeOutDuration);
        float elapsed = 0f;

        // Fade alpha on all child renderers
        float startAlpha = 1f;
        if (_childRenderers != null && _childRenderers.Length > 0)
        {
            startAlpha = _childRenderers[0].color.a;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (_childRenderers != null)
            {
                foreach (var r in _childRenderers)
                {
                    if (r == null) continue;
                    Color c = r.color;
                    c.a = Mathf.Lerp(startAlpha, 0f, t);
                    r.color = c;
                }
            }
            else if (needRenderer != null)
            {
                Color c = needRenderer.color;
                c.a = Mathf.Lerp(startAlpha, 0f, t);
                needRenderer.color = c;
            }

            yield return null;
        }

        HideVisualImmediate();
    }

    private void HideVisualImmediate()
    {
        ApplyColorToTargets(startColor);
        SetAlphaOnTargets(1f);
        ApplyProgressShape(0f);

        if (holder != null)
        {
            holder.SetActive(false);
        }
    }

    private bool UseMaskMode()
    {
        if (progressMode != ProgressMode.MaskClipVertical)
            return false;

        if (verticalMaskDriver != null)
            return true;

        return !fallbackToScaleIfMaskMissing;
    }

    private void ApplyProgressShape(float progress01)
    {
        float t = Mathf.Clamp01(progress01);

        if (progressMode == ProgressMode.MaskClipVertical && verticalMaskDriver != null)
        {
            verticalMaskDriver.localPosition = Vector3.Lerp(maskFullLocalPosition, maskEmptyLocalPosition, t);
            return;
        }

        // Fallback/default: scale mode
        if (scaleTargets == null) return;
        foreach (var tScale in scaleTargets)
            if (tScale != null) tScale.localScale = Vector3.Lerp(startScale, endScale, t);
    }

    private void ApplyColorToTargets(Color color)
    {
        if (_childRenderers != null)
        {
            foreach (var r in _childRenderers)
            {
                if (r == null) continue;
                r.color = color;
            }
            return;
        }

        if (needRenderer != null)
            needRenderer.color = color;
    }

    private void SetAlphaOnTargets(float alpha)
    {
        if (_childRenderers != null)
        {
            foreach (var r in _childRenderers)
            {
                if (r == null) continue;
                Color c = r.color;
                c.a = alpha;
                r.color = c;
            }
            return;
        }

        if (needRenderer != null)
        {
            Color c = needRenderer.color;
            c.a = alpha;
            needRenderer.color = c;
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
