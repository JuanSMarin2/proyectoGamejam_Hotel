using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SwimController : MonoBehaviour
{
     [Header("Animation")]
     [SerializeField] private Animator animator;
    [Header("Swim Settings")]

    [SerializeField] private float maxVerticalSpeed = 6f;
    [Tooltip("Gravedad de caida en dificultad MINIMA (el valor actual)")]
    [SerializeField] private float waterGravityScale = 0.5f;

    [Header("Caida por Dificultad")]
    [Tooltip("Gravedad de caida al llegar a la dificultad MAXIMA. Interpola desde waterGravityScale.")]
    [SerializeField] private float waterGravityScaleAtMaxDifficulty = 1f;
    [Tooltip("Velocidad MINIMA que entrega MinigameManager.Speed")]
    [SerializeField] private float minDifficultySpeed = 1f;
    [Tooltip("Velocidad MAXIMA real que entrega MinigameManager.Speed")]
    [SerializeField] private float maxDifficultySpeed = 2.5f;

    [Header("Brazada por Dificultad")]
    [Tooltip("Fuerza de la brazada en dificultad MINIMA (el valor actual)")]
    [SerializeField] private float swimForceAtMinDifficulty = 8f;
    [Tooltip("Fuerza de la brazada al llegar a la dificultad MAXIMA (menor = se eleva menos por brazada)")]
    [SerializeField] private float swimForceAtMaxDifficulty = 6f;
    private bool _losed = false;

    [Header("Camera")]
    [SerializeField] private Transform cam;
    [SerializeField] private float cameraXOffset = 4f;

    private Rigidbody2D rb;

    /// <summary>
    /// Fuerza de la brazada efectiva: en dificultad minima usa
    /// swimForceAtMinDifficulty (el valor actual) y DISMINUYE hasta
    /// swimForceAtMaxDifficulty. A mayor dificultad, el jugador se eleva
    /// menos por brazada.
    /// </summary>
    private float GetEffectiveSwimForce()
    {
        float speed = MinigameManager.instance != null ? MinigameManager.instance.Speed : minDifficultySpeed;
        float t = Mathf.InverseLerp(minDifficultySpeed, maxDifficultySpeed, speed);
        return Mathf.Lerp(swimForceAtMinDifficulty, swimForceAtMaxDifficulty, t);
    }

    /// <summary>
    /// Gravedad de caida efectiva: en dificultad minima usa waterGravityScale
    /// (el valor actual) y aumenta hasta waterGravityScaleAtMaxDifficulty al
    /// llegar a la dificultad maxima.
    /// </summary>
    private float GetEffectiveWaterGravityScale()
    {
        float speed = MinigameManager.instance != null ? MinigameManager.instance.Speed : minDifficultySpeed;
        float t = Mathf.InverseLerp(minDifficultySpeed, maxDifficultySpeed, speed);
        return Mathf.Lerp(waterGravityScale, waterGravityScaleAtMaxDifficulty, t);
    }

    public static bool IsSwimSceneActive()
    {
        return FindObjectOfType<SwimController>() != null;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
     
    }

    private void Start()
    {
        rb.gravityScale = GetEffectiveWaterGravityScale();
        animator.SetTrigger("SwimIdle");
    }

    private void Update()
    {
        if (SwimInput() && !_losed)
        {
            Swim();
            SoundManager.PlaySound(SoundType.Nadando);
            animator.SetTrigger("SwimAction");
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
             Input.GetKeyDown(KeyCode.W) ||
               Input.GetKeyDown(KeyCode.UpArrow) ||
               Input.GetMouseButtonDown(0) ||
               (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
    }

    private void Swim()
    {
        Vector2 velocity = rb.linearVelocity;

        if (velocity.y > maxVerticalSpeed)
            velocity.y = maxVerticalSpeed;

        velocity.y = GetEffectiveSwimForce();

        rb.linearVelocity = velocity;
    }

    private void MoveRight()
    {
        float speed = 5* MinigameManager.instance.Speed;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = speed;

        rb.linearVelocity = velocity;
    }

    private void MoveCamera()
    {
        if (cam == null) return;

        Vector3 pos = cam.position;
        pos.x = transform.position.x + cameraXOffset;
        cam.position = pos;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_losed) return;

        _losed = true;
        SoundManager.PlaySound(SoundType.Electrocutandose);
        animator.SetTrigger("SwimLose");
        ResultManager.instance.LoseMinigame();
    }
}