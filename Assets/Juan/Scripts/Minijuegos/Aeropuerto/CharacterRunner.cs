using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CharacterRunner : MonoBehaviour
{
        [Header("Animations")]
        
        [SerializeField] private Animator animator;

    [Header("Movement")]
    [SerializeField] private float fallbackSpeed = 2f;

    [Header("Jump")]
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private int groundRecheckDelayFrames = 4;
    [SerializeField] private float jumpInputBufferTime = 0.12f;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;

    [Header("Camera Follow (X only)")]
    [SerializeField] private bool followCameraX = true;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraOffsetX = 0f;
  

    private bool _losed = false;

    private Rigidbody2D rb;

    private bool isGrounded;
    private int jumpsUsed;
    private int groundRecheckFramesRemaining;
    private float jumpBufferTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (animator != null)
        {
            animator.SetTrigger("Run");
        }
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
               Input.GetMouseButtonDown(0))
        {
            jumpBufferTimer = Mathf.Max(0f, jumpInputBufferTime);
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        if (groundRecheckFramesRemaining > 0)
        {
            groundRecheckFramesRemaining--;
        }

        float speed = fallbackSpeed;
        if (MinigameManager.instance != null)
        {
            speed = 5 * MinigameManager.instance.Speed;
        }

        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);

        if (jumpBufferTimer > 0f && TryJump())
        {
            jumpBufferTimer = 0f;
        }

        if (animator != null)
        {
            animator.SetBool("isJumping", !isGrounded);
        }

        if (isGrounded)
        {
            animator.SetTrigger("Run");
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

    private bool TryJump()
    {
        if (_losed) return false;

        if (isGrounded)
        {
            jumpsUsed = 0;
        }

        if (jumpsUsed >= Mathf.Max(1, maxJumps)) return false;
        Vector2 vel = rb.linearVelocity;
        vel.y = 0f;
        rb.linearVelocity = vel;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        jumpsUsed++;
        isGrounded = false;
        groundRecheckFramesRemaining = Mathf.Max(0, groundRecheckDelayFrames);
        return true;
    }

    private bool IsGroundLayer(int layer)
    {
        return (groundLayer.value & (1 << layer)) != 0;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (groundRecheckFramesRemaining > 0) return;

        if (IsGroundLayer(collision.gameObject.layer))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (groundRecheckFramesRemaining > 0) return;

        if (IsGroundLayer(collision.gameObject.layer))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (IsGroundLayer(collision.gameObject.layer))
        {
            isGrounded = false;
        }
    }

        private void OnTriggerEnter2D(Collider2D collision)
    {
        _losed = true;
        
        ResultManager.instance.LoseMinigame(0);
    }
}
