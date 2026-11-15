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

    [Header("Fast Fall")]
    [Tooltip("Porcentaje extra de velocidad de caida durante la caida rapida (25 = cae 25% mas rapido)")]
    [SerializeField] private float fastFallExtraPercent = 25f;
    [Tooltip("Si esta activo, la caida rapida se activa SOLA al llegar a la altura maxima del salto")]
    [SerializeField] private bool autoFastFallAtApex = false;

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

    // Caida rapida: se activa al presionar salto en el aire sin saltos restantes,
    // o con Abajo/S/Click derecho aunque queden saltos. Se desactiva al tocar suelo.
    private bool fastFallActive;
    private float baseGravityScale;

    public static bool IsRunnerSceneActive()
    {
        return FindObjectOfType<CharacterRunner>() != null;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseGravityScale = rb.gravityScale;

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
        if (_losed) return;

        bool jumpPressed = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ||
               Input.GetMouseButtonDown(0);

        if (jumpPressed)
        {
            jumpBufferTimer = Mathf.Max(0f, jumpInputBufferTime);

            // Salto presionado en el aire SIN saltos restantes -> caida rapida
            if (!isGrounded && jumpsUsed >= Mathf.Max(1, maxJumps))
            {
                ActivateFastFallByPlayer();
            }
        }

        // Abajo / S / Click derecho -> caida rapida aunque queden saltos
        if (!isGrounded && (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) || Input.GetMouseButtonDown(1)))
        {
            ActivateFastFallByPlayer();
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

        CheckAutoFastFallAtApex();
        ApplyFastFallGravity();

        if (animator != null)
        {
            animator.SetBool("isJumping", !isGrounded);
        }

        if (isGrounded)
        {
            fastFallActive = false;
            animator.SetTrigger("Run");
            jumpsUsed = 0;
        }
    }

    /// <summary>
    /// Caida rapida ordenada por el jugador: suena el sonido de tropezarse.
    /// No se stackea: si ya esta activa, no suena ni se acumula.
    /// </summary>
    private void ActivateFastFallByPlayer()
    {
        if (fastFallActive)
        {
            return;
        }

        fastFallActive = true;
        SoundManager.PlaySound(SoundType.Tropezandose);
    }

    /// <summary>
    /// Si autoFastFallAtApex esta activo, la caida rapida se dispara SOLA al
    /// llegar a la altura maxima del salto (cuando la velocidad vertical
    /// deja de ser positiva despues de haber saltado).
    /// </summary>
    private void CheckAutoFastFallAtApex()
    {
        if (!autoFastFallAtApex || fastFallActive || isGrounded)
        {
            return;
        }

        if (jumpsUsed <= 0)
        {
            return;
        }

        if (rb.linearVelocity.y <= 0f)
        {
            fastFallActive = true;
        }
    }

    /// <summary>
    /// Mientras la caida rapida esta activa y el personaje esta en el aire,
    /// la gravedad se multiplica por (1 + fastFallExtraPercent/100), haciendo
    /// que caiga ese porcentaje mas rapido. Al tocar suelo vuelve a la normal.
    /// </summary>
    private void ApplyFastFallGravity()
    {
        if (fastFallActive && !isGrounded)
        {
            rb.gravityScale = baseGravityScale * (1f + Mathf.Max(0f, fastFallExtraPercent) / 100f);
        }
        else
        {
            rb.gravityScale = baseGravityScale;
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

        // Saltar durante una caida rapida la DESACTIVA (si aun quedaban saltos).
        // Se puede reactivar en el aire sin tocar suelo, pero no se stackea.
        fastFallActive = false;

        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        SoundManager.PlaySound(SoundType.Saltando);
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
        if (_losed) return;

        _losed = true;
        SoundManager.PlaySound(SoundType.Tropezandose);
        SoundManager.PlaySound(SoundType.Avion);
        
        ResultManager.instance.LoseMinigame(0);
    }
}