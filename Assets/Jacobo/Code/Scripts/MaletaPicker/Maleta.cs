using System;
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

    private IReadOnlyList<Transform> waypoints;
    private float movementSpeed;
    private int targetWaypointIndex;
    private bool canMove;
    private bool isPicked;

    private Action<Maleta> onPicked;

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
        if (!canMove || waypoints == null || waypoints.Count == 0) return;

        Transform target = waypoints[targetWaypointIndex];
        if (target == null)
        {
            NextWaypoint();
            return;
        }

        float lerpFactor = Time.deltaTime * movementSpeed;

        transform.position = Vector3.Lerp(transform.position, target.position, lerpFactor);
        transform.rotation = Quaternion.Slerp(transform.rotation, target.rotation, lerpFactor);

        float distance = Vector3.Distance(transform.position, target.position);
        if (distance <= waypointReachDistance)
        {
            transform.position = target.position;
            transform.rotation = target.rotation;
            NextWaypoint();
        }
    }

    public void Initialize(IReadOnlyList<Transform> newWaypoints, float newMovementSpeed, bool isWinner, int poolId, Action<Maleta> pickedCallback)
    {
        waypoints = newWaypoints;
        movementSpeed = Mathf.Max(0f, newMovementSpeed);
        winner = isWinner;
        PoolId = poolId;
        onPicked = pickedCallback;

        targetWaypointIndex = 0;
        isPicked = false;
        canMove = true;
    }

    public void StopMovement()
    {
        canMove = false;
    }

    public void TryPick()
    {
        if (isPicked) return;

        isPicked = true;
        canMove = false;

        onPicked?.Invoke(this);
    }

    private void OnMouseDown()
    {
        TryPick();
    }

    private void NextWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        targetWaypointIndex = (targetWaypointIndex + 1) % waypoints.Count;
    }
}
