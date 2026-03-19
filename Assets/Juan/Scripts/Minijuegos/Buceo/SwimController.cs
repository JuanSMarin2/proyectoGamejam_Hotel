using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SwimController : MonoBehaviour
{
    [Header("Swim Settings")]
    [SerializeField] private float swimForce = 8f;
    [SerializeField] private float maxVerticalSpeed = 6f;
    [SerializeField] private float waterGravityScale = 0.5f;

    [Header("Camera")]
    [SerializeField] private Transform cam;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        rb.gravityScale = waterGravityScale;
    }

    private void Update()
    {
        if (SwimInput())
        {
            Swim();
        }
    }

    private void FixedUpdate()
    {
        MoveRight();
        MoveCamera();
    }

    private bool SwimInput()
    {
        return Input.GetKeyDown(KeyCode.Space) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private void Swim()
    {
        Vector2 velocity = rb.linearVelocity;

        if (velocity.y > maxVerticalSpeed)
            velocity.y = maxVerticalSpeed;

        velocity.y = swimForce;

        rb.linearVelocity = velocity;
    }

    private void MoveRight()
    {
        float speed = MinigameManager.instance.Speed;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = speed;

        rb.linearVelocity = velocity;
    }

    private void MoveCamera()
    {
        if (cam == null) return;

        Vector3 pos = cam.position;
        pos.x = transform.position.x;
        cam.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        ResultManager.instance.LoseMinigame();
    }
}