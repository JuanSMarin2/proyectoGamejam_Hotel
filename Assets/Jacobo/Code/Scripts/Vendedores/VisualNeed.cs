using System.Collections;
using UnityEngine;

public class VisualNeed : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterNecesidad characterNecesidad;
    [SerializeField] private GameObject holder;
    [SerializeField] private GameObject holderBackground;
    [SerializeField] private SpriteRenderer needRenderer;

    [Header("Need Sprites")]
    [SerializeField] private Sprite spriteSed;
    [SerializeField] private Sprite spriteSol;
    [SerializeField] private Sprite spriteDiversion;

    [Header("Mask Progress (Vertical Clip)")]
    [SerializeField] private Transform verticalMaskDriver;
    [SerializeField] private Vector3 maskFullLocalPosition;
    [SerializeField] private Vector3 maskEmptyLocalPosition = new Vector3(0f, -0.5f, 0f);
    [SerializeField] private bool captureMaskFullPositionOnAwake = true;

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

        ApplyMaskProgress(0f);
        SetHoldersActive(true);

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

        HideVisualImmediate();
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

            ApplyMaskProgress(t);

            yield return null;
        }
    }

    private void HideVisualImmediate()
    {
        ApplyMaskProgress(0f);
        SetHoldersActive(false);
    }

    private void ApplyMaskProgress(float progress01)
    {
        if (verticalMaskDriver == null) return;
        float t = Mathf.Clamp01(progress01);
        verticalMaskDriver.localPosition = Vector3.Lerp(maskFullLocalPosition, maskEmptyLocalPosition, t);
    }

    private void SetHoldersActive(bool active)
    {
        if (holder != null)
            holder.SetActive(active);

        if (holderBackground != null)
            holderBackground.SetActive(active);
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
