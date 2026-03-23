using System;
using System.Collections;
using UnityEngine;

public class CharacterNecesidad : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private int playerChildrenSortingOffset = 35;

    [Header("Timing")]
    [SerializeField] private float minWaitBetweenNeeds = 2f;
    [SerializeField] private float maxWaitBetweenNeeds = 4f;
    [SerializeField] private float solveTime = 6f;

    [SerializeField] private bool autoStart = true;

    private Coroutine loopRoutine;

    public bool HasActiveNeed { get; private set; }
    public Necesidad CurrentNeed { get; private set; }
    public float SolveTime => solveTime;

    public event Action<Necesidad> NeedStarted;
    public event Action NeedResolved;
    public event Action<Necesidad> NeedFailed;

    private void Start()
    {
        ApplyPlayerChildrenSortingOffset();

        if (autoStart)
            StartNeeds();
    }

    private void ApplyPlayerChildrenSortingOffset()
    {
        if (player == null)
            return;

        int offset = playerChildrenSortingOffset;
        if (offset == 0)
            return;

        SpriteRenderer[] renderers = player.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.transform == player)
                continue;

            renderer.sortingOrder += offset;
        }
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
    }

    public bool ResolveIfMatches(Necesidad soldNeed)
    {
        if (!HasActiveNeed) return false;
        if (soldNeed != CurrentNeed) return false;

        HasActiveNeed = false;

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

                NeedFailed?.Invoke(CurrentNeed);
            }
        }
    }

    private Necesidad PickRandomNeed()
    {
        Array values = Enum.GetValues(typeof(Necesidad));
        if (values == null || values.Length == 0)
            return default;

        int random = UnityEngine.Random.Range(0, values.Length);
        return (Necesidad)values.GetValue(random);
    }
}
