using System.Collections.Generic;
using UnityEngine;

public class CintaMovement : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] private List<Transform> waypoints = new List<Transform>();
    [SerializeField] private float movementSpeed = 2f;

    public IReadOnlyList<Transform> Waypoints => waypoints;
    public float MovementSpeed => movementSpeed;

    private void OnValidate()
    {
        movementSpeed = Mathf.Max(0f, movementSpeed);
    }

    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < waypoints.Count; i++)
        {
            Transform current = waypoints[i];
            if (current == null) continue;

            Gizmos.DrawSphere(current.position, 0.15f);

            Transform next = waypoints[(i + 1) % waypoints.Count];
            if (next == null) continue;

            Gizmos.DrawLine(current.position, next.position);
        }
    }
}
