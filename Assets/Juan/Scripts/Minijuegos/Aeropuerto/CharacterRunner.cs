using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float fallbackSpeed = 2f;

    [Header("Jump")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.12f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Camera Follow (X only)")]
    [SerializeField] private bool followCameraX = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraOffsetX = 0f;

    private Rigidbody2D rb;

    private bool isGrounded;
    private int jumpsUsed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetMouseButtonDown(0))
        {
            TryJump();
        }
    }

    private void FixedUpdate()
    {
        UpdateGrounded();

        float speed = fallbackSpeed;
        if (MinigameManager.instance != null)
        {
            speed = 5 * MinigameManager.instance.Speed;
        }

        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        if (isGrounded)
        {
            jumpsUsed = 0;
        }
    }

    private void LateUpdate()
    {
        if (!followCameraX || cameraTransform == null) return;

        Vector3 camPos = cameraTransform.position;
        camPos.x = transform.position.x + cameraOffsetX;
        cameraTransform.position = camPos;
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            jumpsUsed = 0;
        }

        if (jumpsUsed >= Mathf.Max(1, maxJumps)) return;

        Vector2 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpsUsed++;
    }

    private void UpdateGrounded()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer) != null;
            return;
        }

     
        isGrounded = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
        private void OnTriggerEnter2D(Collider2D collision)
    {
        ResultManager.instance.LoseMinigame();
    }
}
