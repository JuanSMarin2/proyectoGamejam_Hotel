using System;
using System.Collections;
using UnityEngine;

public class CharacterNecesidad : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private GameObject necesidadHolder;
    [SerializeField] private SpriteRenderer necesidadRenderer;

    [Header("Sprites")]
    [SerializeField] private Sprite spriteSed;
    [SerializeField] private Sprite spriteSol;
    [SerializeField] private Sprite spriteDiversion;

    [Header("Timing")]
    [SerializeField] private float minWaitBetweenNeeds = 2f;
    [SerializeField] private float maxWaitBetweenNeeds = 4f;
    [SerializeField] private float solveTime = 4f;

    [SerializeField] private bool autoStart = true;

    private Coroutine loopRoutine;

    public bool HasActiveNeed { get; private set; }
    public Necesidad CurrentNeed { get; private set; }

    public event Action<Necesidad> NeedStarted;
    public event Action NeedResolved;
    public event Action<Necesidad> NeedFailed;

    private void Start()
    {
        HideNeedVisual();

        if (autoStart)
            StartNeeds();
    }

    public void StartNeeds()
    {
        StopNeeds();
        loopRoutine = StartCoroutine(NeedLoop());
    }

    public void StopNeeds()
    {
        if (loopRoutine != null)
            StopCoroutine(loopRoutine);

        loopRoutine = null;
        HasActiveNeed = false;

        HideNeedVisual();
    }

    public bool ResolveIfMatches(Necesidad soldNeed)
    {
        if (!HasActiveNeed) return false;
        if (soldNeed != CurrentNeed) return false;

        HasActiveNeed = false;
        HideNeedVisual();

        NeedResolved?.Invoke();
        return true;
    }

    private IEnumerator NeedLoop()
    {
        while (true)
        {
            float waitTime = UnityEngine.Random.Range(Mathf.Min(minWaitBetweenNeeds, maxWaitBetweenNeeds), Mathf.Max(minWaitBetweenNeeds, maxWaitBetweenNeeds));
            yield return new WaitForSeconds(waitTime);

            CurrentNeed = PickRandomNeed();
            HasActiveNeed = true;

            ShowNeedVisual(GetSpriteByNeed(CurrentNeed));

            NeedStarted?.Invoke(CurrentNeed);

            float timer = solveTime;
            while (HasActiveNeed && timer > 0f)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            if (HasActiveNeed)
            {
                HasActiveNeed = false;
                HideNeedVisual();

                NeedFailed?.Invoke(CurrentNeed);
            }
        }
    }

    private void ShowNeedVisual(Sprite sprite)
    {
        if (necesidadRenderer != null)
            necesidadRenderer.sprite = sprite;

        if (necesidadHolder != null)
            necesidadHolder.SetActive(sprite != null);
        else if (necesidadRenderer != null)
            necesidadRenderer.gameObject.SetActive(sprite != null);
    }

    private void HideNeedVisual()
    {
        if (necesidadHolder != null)
            necesidadHolder.SetActive(false);
        else if (necesidadRenderer != null)
            necesidadRenderer.gameObject.SetActive(false);
    }

    private Necesidad PickRandomNeed()
    {
        int randomNoMasajes = UnityEngine.Random.Range(0, 3);
        return (Necesidad)randomNoMasajes;
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
