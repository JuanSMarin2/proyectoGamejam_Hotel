using System.Collections.Generic;
using UnityEngine;

public class ConveyorBeltLinesMovement : MonoBehaviour
{
    private struct LineMover
    {
        public Transform line;
        public int currentWaypoint;
        public int targetWaypoint;
    }

    [Header("Speed")]
    [SerializeField] private float movementSpeed = 1.5f;
    [SerializeField] private CintaMovement cintaMovement;
    [SerializeField] private float speedMultiplierFromCinta = 1f;
    [SerializeField] private bool useCintaSpeed = true;

    [Header("Follow")]
    [SerializeField] private float waypointReachDistance = 0.02f;
    [SerializeField] private float rotationLerpSpeed = 10f;

    private readonly List<LineMover> movers = new List<LineMover>();
    private readonly List<Vector3> waypointPositions = new List<Vector3>();
    private readonly List<Quaternion> waypointRotations = new List<Quaternion>();

    private void Start()
    {
        BuildFromChildren();
    }

    private void Update()
    {
        if (movers.Count == 0 || waypointPositions.Count == 0) return;

        float speed = useCintaSpeed && cintaMovement != null
            ? Mathf.Max(0f, cintaMovement.MovementSpeed * speedMultiplierFromCinta)
            : Mathf.Max(0f, movementSpeed);

        for (int i = 0; i < movers.Count; i++)
        {
            LineMover mover = movers[i];
            MoveLine(ref mover, speed);
            movers[i] = mover;
        }
    }

    [ContextMenu("Rebuild From Children")]
    public void BuildFromChildren()
    {
        movers.Clear();
        waypointPositions.Clear();
        waypointRotations.Clear();

        int childCount = transform.childCount;
        if (childCount == 0) return;

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            waypointPositions.Add(child.position);
            waypointRotations.Add(child.rotation);
        }

        for (int i = 0; i < childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            LineMover mover = new LineMover
            {
                line = child,
                currentWaypoint = i,
                targetWaypoint = (i + 1) % childCount
            };

            movers.Add(mover);
        }
    }

    private void MoveLine(ref LineMover mover, float speed)
    {
        if (mover.line == null || waypointPositions.Count == 0) return;

        float remainingDistance = speed * Time.deltaTime;
        int safety = 0;

        while (remainingDistance > 0f && safety < waypointPositions.Count + 2)
        {
            Vector3 targetPos = waypointPositions[mover.targetWaypoint];
            float distance = Vector3.Distance(mover.line.position, targetPos);

            if (distance <= waypointReachDistance)
            {
                mover.line.position = targetPos;
                mover.currentWaypoint = mover.targetWaypoint;
                mover.targetWaypoint = (mover.targetWaypoint + 1) % waypointPositions.Count;
                safety++;
                continue;
            }

            if (remainingDistance >= distance)
            {
                mover.line.position = targetPos;
                remainingDistance -= distance;
                mover.currentWaypoint = mover.targetWaypoint;
                mover.targetWaypoint = (mover.targetWaypoint + 1) % waypointPositions.Count;
                safety++;
            }
            else
            {
                Vector3 dir = (targetPos - mover.line.position).normalized;
                mover.line.position += dir * remainingDistance;
                remainingDistance = 0f;
            }
        }

        Quaternion targetRot = waypointRotations[mover.targetWaypoint];
        float rotationT = Mathf.Clamp01(rotationLerpSpeed * Time.deltaTime);
        mover.line.rotation = Quaternion.Slerp(mover.line.rotation, targetRot, rotationT);
    }
}
