using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Maleta : MonoBehaviour
{
    public enum MaletaType
    {
        Large,
        Medium,
        Small
    }

    [Header("Config")]
    [SerializeField] private MaletaType type = MaletaType.Medium;
    [SerializeField] private bool winner = false;

    [Header("Movement")]
    [SerializeField] private float waypointReachDistance = 0.08f;
    [SerializeField] private float rotationLerpSpeed = 10f;

    [Header("Pick Success Fade")]
    [SerializeField] private float defaultFadeDuration = 0.2f;

    private IReadOnlyList<Transform> waypoints;
    private float movementSpeed;
    private int targetWaypointIndex;
    private bool canMove;
    private bool isPicked;
    private bool routeCompleted;

    private Action<Maleta> onPicked;
    private Action<Maleta> onRouteCompleted;
    private Coroutine fadeRoutine;
    private SpriteRenderer[] cachedRenderers;

    public MaletaType Type => type;
    public bool Winner => winner;
    public int PoolId { get; private set; }
    public bool IsPicked => isPicked;

    public void SetType(MaletaType newType)
    {
        type = newType;
    }

    private void Update()
    {
        if (!canMove || routeCompleted || waypoints == null || waypoints.Count == 0) return;

        float remainingDistance = movementSpeed * Time.deltaTime;
        int safety = 0;

        while (remainingDistance > 0f && safety < waypoints.Count + 2)
        {
            Transform target = waypoints[targetWaypointIndex];
            if (target == null)
            {
                NextWaypoint();
                safety++;
                continue;
            }

            float distanceToTarget = Vector3.Distance(transform.position, target.position);

            if (distanceToTarget <= waypointReachDistance)
            {
                transform.position = target.position;
                if (IsLastWaypoint())
                {
                    CompleteRoute();
                    return;
                }

                NextWaypoint();
                safety++;
                continue;
            }

            if (remainingDistance >= distanceToTarget)
            {
                transform.position = target.position;
                remainingDistance -= distanceToTarget;

                if (IsLastWaypoint())
                {
                    CompleteRoute();
                    return;
                }

                NextWaypoint();
                safety++;
            }
            else
            {
                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * remainingDistance;
                remainingDistance = 0f;
            }
        }

        Transform currentTarget = targetWaypointIndex >= 0 && targetWaypointIndex < waypoints.Count
            ? waypoints[targetWaypointIndex]
            : null;
        if (currentTarget != null)
        {
            float rotationT = Mathf.Clamp01(rotationLerpSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentTarget.rotation, rotationT);
        }
    }

    public void Initialize(
        IReadOnlyList<Transform> newWaypoints,
        float newMovementSpeed,
        bool isWinner,
        int poolId,
        Action<Maleta> pickedCallback,
        Action<Maleta> routeCompletedCallback = null)
    {
        waypoints = newWaypoints;
        movementSpeed = Mathf.Max(0f, newMovementSpeed);
        winner = isWinner;
        PoolId = poolId;
        onPicked = pickedCallback;
        onRouteCompleted = routeCompletedCallback;

        targetWaypointIndex = 0;
        isPicked = false;
        canMove = true;
        routeCompleted = false;
    }

    public void StopMovement()
    {
        canMove = false;
    }

    public void PlaySuccessFadeOut(Action onCompleted, float durationOverride = -1f)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(SuccessFadeRoutine(onCompleted, durationOverride));
    }

    public void TryPick()
    {
        if (isPicked) return;

        isPicked = true;
        canMove = false;

        onPicked?.Invoke(this);
    }

    private void NextWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Count;
    }

    private bool IsLastWaypoint()
    {
        return waypoints != null && waypoints.Count > 0 && targetWaypointIndex >= waypoints.Count - 1;
    }

    private void CompleteRoute()
    {
        routeCompleted = true;
        canMove = false;
        onRouteCompleted?.Invoke(this);
    }

    private IEnumerator SuccessFadeRoutine(Action onCompleted, float durationOverride)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            onCompleted?.Invoke();
            yield break;
        }

        float duration = durationOverride > 0f ? durationOverride : defaultFadeDuration;
        duration = Mathf.Max(0.01f, duration);

        Color[] startColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
                startColors[i] = cachedRenderers[i].color;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == null)
                    continue;

                Color c = startColors[i];
                c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                cachedRenderers[i].color = c;
            }

            yield return null;
        }

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            cachedRenderers[i].color = startColors[i];
        }

        fadeRoutine = null;
        onCompleted?.Invoke();
    }
}
