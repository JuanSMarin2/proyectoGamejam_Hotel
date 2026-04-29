using UnityEngine;

/// <summary>
/// Controlador del jugador para juego 2D (top-down) con movimiento libre en 4 direcciones.
/// Control: W/A/S/D
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    #region Serialized Fields

    [Header("Velocidad de Movimiento")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Rotación del Sprite")]
    [SerializeField] private float rotationSmoothing = 10f; // Velocidad de suavizado de rotación
    [SerializeField] private float maxRotationAngle = 22f; // Ángulo máximo de rotación en grados
    [SerializeField] private Transform spriteTransform; // Asigna el SpriteRenderer (o su GameObject) si es hijo
    [SerializeField] private float forwardThreshold = 0.1f; // Umbral para considerar que va "hacia adelante" (W)
    [SerializeField] private float sidewaysThreshold = 0.1f; // Umbral para considerar giro izquierda/derecha

    [Header("Audio")]
    [SerializeField] private AudioCarro audioCarro;
    [SerializeField] private float choqueVolume = 1f;
    [SerializeField] private float frenadoVolume = 50f;

    [Header("Móvil")]
    [SerializeField] private float swipeSensitivity = 1f;
    [SerializeField] private float swipeDeadZone = 5f;

    #endregion

    #region Private Fields

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool brakeInputHeld;
    private float targetTiltZ;
    private float baseSpriteZ;
    private float currentTiltVelocity;
    private bool wasMovingForward;
    private float forwardHoldTime;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteTransform == null)
            spriteTransform = transform;

        if (audioCarro == null)
            audioCarro = GetComponent<AudioCarro>();

        if (audioCarro == null)
            audioCarro = GetComponentInChildren<AudioCarro>();

        baseSpriteZ = spriteTransform.localEulerAngles.z;
    }

    public void Start()
    {
        if (audioCarro != null)
            audioCarro.StartEngine();
        else
            SoundManager.PlaySound(SoundType.Motores);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("CarroMalo"))
        {
            SoundManager.PlaySound(SoundType.ChoqueCarro, null, choqueVolume);
            ResultManager.instance.LoseMinigame();
        }
        else if (other.CompareTag("Obstacle"))
        {
            SoundManager.PlaySound(SoundType.ChoqueCarro, null, choqueVolume);
            ResultManager.instance.LoseMinigame();
        }
        
    }

    private void Update()
    {
        ReadInput();
        if (audioCarro != null)
            audioCarro.SetThrottleInput(moveInput);
        CheckBrakeSound();
        UpdateTargetTilt();
    }

    private void LateUpdate()
    {
        ApplySpriteTilt();
    }

    private void FixedUpdate()
    {
        ApplyMovement();
    }

    #endregion

    #region Input Handling

    /// <summary>Lee el input del jugador (W/A/S/D)</summary>
    private void ReadInput()
    {
        if (Application.isMobilePlatform)
        {
            ReadMobileInput();
            return;
        }

        ReadDesktopInput();
    }

    /// <summary>Lee el input clásico de PC (W/A/S/D)</summary>
    private void ReadDesktopInput()
    {
        moveInput = Vector2.zero;
        brakeInputHeld = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveInput.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveInput.y -= 1;
            brakeInputHeld = true;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveInput.x += 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveInput.x -= 1;

        // Normalizar para evitar movimiento diagonal más rápido
        moveInput = moveInput.normalized;
    }

    /// <summary>Lee el input táctil para móviles usando un swipe invisible.</summary>
    private void ReadMobileInput()
    {
        brakeInputHeld = false;

        if (rb == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (Input.touchCount <= 0)
        {
            moveInput = Vector2.zero;
            return;
        }

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                moveInput = Vector2.zero;
                break;

            case TouchPhase.Moved:
            case TouchPhase.Stationary:
                moveInput = CalculateMobileInput(touch);
                brakeInputHeld = moveInput.y < -forwardThreshold;
                break;

            case TouchPhase.Ended:
            case TouchPhase.Canceled:
                moveInput = Vector2.zero;
                break;
        }
    }

    /// <summary>Convierte el delta táctil en un vector normalizado para movimiento 2D.</summary>
    private Vector2 CalculateMobileInput(Touch touch)
    {
        Vector2 rawInput = touch.deltaPosition * swipeSensitivity;

        if (rawInput.sqrMagnitude < swipeDeadZone * swipeDeadZone)
        {
            return Vector2.zero;
        }

        return rawInput.normalized;
    }

    private void CheckBrakeSound()
    {
        bool isMovingForward = moveInput.y > forwardThreshold;
        bool isMovingBackward = brakeInputHeld;

        if (isMovingForward)
        {
            forwardHoldTime += Time.deltaTime;
        }

        if (wasMovingForward && isMovingBackward && forwardHoldTime >= 0.5f)
        {
            if (audioCarro != null)
                audioCarro.PlayBrakeOneShot();
            else
                SoundManager.PlaySound(SoundType.Frenado, null, frenadoVolume);
        }

        if (!isMovingForward)
        {
            forwardHoldTime = 0f;
        }

        wasMovingForward = isMovingForward;
    }

    #endregion

    #region Movement Application

    /// <summary>Aplica el movimiento al Rigidbody2D</summary>
    private void ApplyMovement()
    {
        rb.linearVelocity = moveInput * moveSpeed;
    }

    /// <summary>
    /// Calcula el "tilt" del sprite como un carro: solo inclina al girar a izquierda/derecha mientras avanza.
    /// Mantiene la rotación base del sprite.
    /// </summary>
    private void UpdateTargetTilt()
    {
        bool isMovingForward = moveInput.y > forwardThreshold;
        bool isTurning = Mathf.Abs(moveInput.x) > sidewaysThreshold;

        if (isMovingForward && isTurning)
        {
            // Inclinación fija al máximo (se nota más en diagonales con teclado)
            targetTiltZ = Mathf.Sign(moveInput.x) * maxRotationAngle;
        }
        else
        {
            targetTiltZ = 0f;
        }
    }

    private void ApplySpriteTilt()
    {
        if (spriteTransform == null)
        {
            return;
        }

        float currentZ = spriteTransform.localEulerAngles.z;
        float desiredZ = baseSpriteZ + targetTiltZ;

        if (float.IsNaN(currentZ) || float.IsNaN(desiredZ) || float.IsNaN(currentTiltVelocity) ||
            float.IsInfinity(currentTiltVelocity))
        {
            currentTiltVelocity = 0f;
            Vector3 resetEuler = spriteTransform.localEulerAngles;
            resetEuler.z = baseSpriteZ;
            spriteTransform.localEulerAngles = resetEuler;
            return;
        }

        float newZ = Mathf.SmoothDampAngle(
            currentZ,
            desiredZ,
            ref currentTiltVelocity,
            1f / Mathf.Max(0.0001f, rotationSmoothing),
            Mathf.Infinity,
            Time.deltaTime
        );

        Vector3 euler = spriteTransform.localEulerAngles;
        euler.z = newZ;
        spriteTransform.localEulerAngles = euler;
    }

    #endregion

    #region Public Methods

    /// <summary>Obtiene la velocidad actual del jugador</summary>
    public Vector2 GetVelocity() => rb.linearVelocity;

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        // Dibujar la dirección actual de movimiento
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)moveInput * 0.5f);

        // Dibujar el radio de colisión
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }

    #endregion
}
