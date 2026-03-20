using UnityEngine;

public class Vendedor : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private Necesidad necesidadVenta = Necesidad.Sed;

    [Header("Sign")]
    [SerializeField] private SpriteRenderer signRenderer;
    [SerializeField] private Sprite spriteSed;
    [SerializeField] private Sprite spriteSol;
    [SerializeField] private Sprite spriteDiversion;
    [SerializeField] private Sprite spriteMasajes;

    private Vector3 movementDirection;
    private float speed;
    private float despawnX;
    private bool moving;

    public Necesidad NecesidadVenta => necesidadVenta;

    public void Initialize(Vector3 direction, float moveSpeed, float despawnLimitX)
    {
        movementDirection = direction.normalized;
        speed = Mathf.Max(0f, moveSpeed);
        despawnX = despawnLimitX;
        moving = true;

        ApplySignSprite();
    }

    public void SetNecesidad(Necesidad newNecesidad)
    {
        necesidadVenta = newNecesidad;
        ApplySignSprite();
    }

    public void StopMovement()
    {
        moving = false;
    }

    private void Awake()
    {
        if (signRenderer == null)
            signRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplySignSprite();
    }

    private void Update()
    {
        if (!moving) return;

        transform.position += movementDirection * speed * Time.deltaTime;

        bool movingRight = movementDirection.x >= 0f;
        if (movingRight && transform.position.x >= despawnX)
        {
            Destroy(gameObject);
        }
        else if (!movingRight && transform.position.x <= despawnX)
        {
            Destroy(gameObject);
        }
    }

    private void ApplySignSprite()
    {
        if (signRenderer == null) return;

        switch (necesidadVenta)
        {
            case Necesidad.Sed:
                signRenderer.sprite = spriteSed;
                break;
            case Necesidad.Sol:
                signRenderer.sprite = spriteSol;
                break;
            case Necesidad.Diversion:
                signRenderer.sprite = spriteDiversion;
                break;
            case Necesidad.Masajes:
                signRenderer.sprite = spriteMasajes;
                break;
        }
    }
}
