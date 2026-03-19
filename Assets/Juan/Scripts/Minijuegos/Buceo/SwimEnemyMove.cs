using UnityEngine;

public class SwimEnemyMove : MonoBehaviour
{
    [SerializeField] private bool canMove = false;
    [SerializeField] private float speed = 2f;
    [SerializeField] private float amplitude = 1f;

    private float startY;

    private void Start()
    {
        startY = transform.position.y;
    }

    private void Update()
    {
        if (!canMove) return;

        float newY = startY + Mathf.Sin(Time.time * speed) * amplitude;

        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
    }
}