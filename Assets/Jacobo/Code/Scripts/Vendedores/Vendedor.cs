using UnityEngine;

public class Vendedor : MonoBehaviour
{
    public enum SpawnPointPreference
    {
        Any,
        Back,
        Front
    }

    [Header("Config")]
    [SerializeField] private Necesidad necesidadVenta = Necesidad.Sed;
    [SerializeField] private SpawnPointPreference spawnPreference = SpawnPointPreference.Any;

    [Header("Sign")]
    [SerializeField] private SpriteRenderer signRenderer;
    [SerializeField] private VendedorSignSpriteLibrary signSpriteLibrary;

    [Header("Random Stops")]
    [SerializeField] private bool enableRandomStops = true;
    [SerializeField, Range(0f, 1f)] private float stopChancePerAttempt = 0.3f;
    [SerializeField] private float minTimeBetweenStopAttempts = 1.2f;
    [SerializeField] private float maxTimeBetweenStopAttempts = 2.8f;
    [SerializeField] private float minStopDuration = 0.2f;
    [SerializeField] private float maxStopDuration = 0.7f;

    private Vector3 movementDirection;
    private float speed;
    private float despawnX;
    private bool moving;
    private bool temporarilyStopped;
    private float stopAttemptTimer;
    private float stopDurationTimer;
    private SpriteRenderer[] cachedRenderers;

    public Necesidad NecesidadVenta => necesidadVenta;
    public SpawnPointPreference SpawnPreference => spawnPreference;

    public void Initialize(Vector3 direction, float moveSpeed, float despawnLimitX)
    {
        movementDirection = direction.normalized;
        speed = Mathf.Max(0f, moveSpeed);
        despawnX = despawnLimitX;
        moving = true;
        temporarilyStopped = false;
        stopDurationTimer = 0f;
        stopAttemptTimer = GetRandomStopAttemptInterval();

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

    public void SetSortingOrderRecursive(int baseSortingOrder)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (cachedRenderers == null || cachedRenderers.Length == 0) return;

        int referenceOrder = cachedRenderers[0] != null ? cachedRenderers[0].sortingOrder : 0;
        int delta = baseSortingOrder - referenceOrder;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null) continue;
            cachedRenderers[i].sortingOrder += delta;
        }
    }

    private void Awake()
    {
        if (signRenderer == null)
            signRenderer = GetComponentInChildren<SpriteRenderer>();

        if (signSpriteLibrary == null)
            signSpriteLibrary = GetComponent<VendedorSignSpriteLibrary>();

        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        ApplySignSprite();
    }

    private void Update()
    {
        if (!moving) return;

        HandleRandomStops();
        if (temporarilyStopped) return;

        Move();
    }

    private void Move()
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

    private void HandleRandomStops()
    {
        if (!enableRandomStops)
            return;

        if (temporarilyStopped)
        {
            stopDurationTimer -= Time.deltaTime;
            if (stopDurationTimer <= 0f)
            {
                temporarilyStopped = false;
                stopAttemptTimer = GetRandomStopAttemptInterval();
            }

            return;
        }

        stopAttemptTimer -= Time.deltaTime;
        if (stopAttemptTimer > 0f)
            return;

        stopAttemptTimer = GetRandomStopAttemptInterval();

        if (Random.value <= Mathf.Clamp01(stopChancePerAttempt))
        {
            temporarilyStopped = true;
            stopDurationTimer = GetRandomStopDuration();
        }
    }

    private float GetRandomStopAttemptInterval()
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minTimeBetweenStopAttempts, maxTimeBetweenStopAttempts));
        float max = Mathf.Max(0.05f, Mathf.Max(minTimeBetweenStopAttempts, maxTimeBetweenStopAttempts));
        return Random.Range(min, max);
    }

    private float GetRandomStopDuration()
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minStopDuration, maxStopDuration));
        float max = Mathf.Max(0.05f, Mathf.Max(minStopDuration, maxStopDuration));
        return Random.Range(min, max);
    }

    private void ApplySignSprite()
    {
        if (signRenderer == null || signSpriteLibrary == null) return;

        if (signSpriteLibrary.TryGetRandomSprite(necesidadVenta, out Sprite sprite))
            signRenderer.sprite = sprite;
    }
}
