using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class SwimController : MonoBehaviour
{
     [Header("Animation")]
     [SerializeField] private Animator animator;
    [Header("Swim Settings")]
    
    [SerializeField] private float swimForce = 8f;
    [SerializeField] private float maxVerticalSpeed = 6f;
    [SerializeField] private float waterGravityScale = 0.5f;
    private bool _losed = false;

    [Header("Camera")]
    [SerializeField] private Transform cam;
    [SerializeField] private float cameraXOffset = 4f;

    private Rigidbody2D rb;

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
        rb.gravityScale = waterGravityScale;
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

        velocity.y = swimForce;

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