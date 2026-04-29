using UnityEngine;
using System.Collections;

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
    [SerializeField] private Animator animator;
    [SerializeField] private string horizontalSpeedParameter = "speedX";

    [Header("Front/Back Visuals")]
    [SerializeField] private Vector3 backScale = Vector3.one;
    [SerializeField] private Vector3 frontScale = Vector3.one;
    [SerializeField] private bool mirrorFrontOnYAxis = true;
    [SerializeField] private string backSortingLayerName = "Background";
    [SerializeField] private string frontSortingLayerName = "Foreground";

    [Header("Bought Visual")]
    [SerializeField] private string boughtTriggerName = "bought";
    [SerializeField] private float boughtDelayBeforeFade = 0.15f;
    [SerializeField] private float boughtFadeDuration = 0.35f;

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
    private Collider[] cachedColliders3D;
    private Collider2D[] cachedColliders2D;
    private SpawnPointPreference currentSpawnSide = SpawnPointPreference.Any;

    public Necesidad NecesidadVenta => necesidadVenta;
    public SpawnPointPreference SpawnPreference => spawnPreference;

    public void Initialize(Vector3 direction, float moveSpeed, float despawnLimitX)
    {
        Initialize(direction, moveSpeed, despawnLimitX, SpawnPointPreference.Any);
    }

    public void Initialize(Vector3 direction, float moveSpeed, float despawnLimitX, SpawnPointPreference spawnSide)
    {
        movementDirection = direction.normalized;
        speed = Mathf.Max(0f, moveSpeed);
        despawnX = despawnLimitX;
        moving = true;
        temporarilyStopped = false;
        stopDurationTimer = 0f;
        stopAttemptTimer = GetRandomStopAttemptInterval();
        currentSpawnSide = spawnSide;

        ApplyFrontBackVisuals(currentSpawnSide);
        UpdateAnimatorHorizontalSpeed(0f);

        ApplySignSprite();
    }

    public void SetNecesidad(Necesidad newNecesidad)
    {
        if (!System.Enum.IsDefined(typeof(Necesidad), newNecesidad))
        {
            Debug.LogWarning($"[Vendedor] Invalid necesidad assigned: {newNecesidad}. Fallback to {Necesidad.Sed}.", this);
            newNecesidad = Necesidad.Sed;
        }

        necesidadVenta = newNecesidad;
        ApplySignSprite();
    }

    public void StopMovement()
    {
        moving = false;
        UpdateAnimatorHorizontalSpeed(0f);
    }

    public void ApplyDifficultyStopTuning(float stopChanceMultiplier, float stopAttemptIntervalMultiplier)
    {
        float chanceMult = Mathf.Clamp01(stopChanceMultiplier);
        float attemptMult = Mathf.Max(1f, stopAttemptIntervalMultiplier);

        stopChancePerAttempt = Mathf.Clamp01(stopChancePerAttempt * chanceMult);
        minTimeBetweenStopAttempts = Mathf.Max(0.05f, minTimeBetweenStopAttempts * attemptMult);
        maxTimeBetweenStopAttempts = Mathf.Max(minTimeBetweenStopAttempts, maxTimeBetweenStopAttempts * attemptMult);
    }

    public void HandleCompraAttemptVisualState()
    {
        StopMovement();
        temporarilyStopped = false;
        stopDurationTimer = 0f;
        UpdateAnimatorHorizontalSpeed(0f);

        SetSortingOrderRecursive(0);

        if (cachedColliders3D == null)
            cachedColliders3D = GetComponentsInChildren<Collider>(true);

        if (cachedColliders2D == null)
            cachedColliders2D = GetComponentsInChildren<Collider2D>(true);

        for (int i = 0; i < cachedColliders3D.Length; i++)
        {
            if (cachedColliders3D[i] != null)
                cachedColliders3D[i].enabled = false;
        }

        for (int i = 0; i < cachedColliders2D.Length; i++)
        {
            if (cachedColliders2D[i] != null)
                cachedColliders2D[i].enabled = false;
        }
    }

    public void PlayBoughtAndFadeOut()
    {
        StartCoroutine(BoughtAndFadeRoutine());
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

    public void AddSortingOrderOffsetRecursive(int offset)
    {
        if (offset == 0)
            return;

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null) continue;
            cachedRenderers[i].sortingOrder += offset;
        }
    }

    private void Awake()
    {
        if (signRenderer == null)
            signRenderer = GetComponentInChildren<SpriteRenderer>();

        if (signSpriteLibrary == null)
            signSpriteLibrary = GetComponent<VendedorSignSpriteLibrary>();

        if (signSpriteLibrary == null)
            signSpriteLibrary = FindFirstObjectByType<VendedorSignSpriteLibrary>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        cachedColliders3D = GetComponentsInChildren<Collider>(true);
        cachedColliders2D = GetComponentsInChildren<Collider2D>(true);

        ApplySignSprite();
    }

    private void Update()
    {
        float horizontalSpeedThisFrame = 0f;

        if (moving)
        {
            HandleRandomStops();
            if (!temporarilyStopped)
                horizontalSpeedThisFrame = Move();
        }

        UpdateAnimatorHorizontalSpeed(horizontalSpeedThisFrame);
    }

    private float Move()
    {
        if (!moving) return 0f;

        Vector3 previousPosition = transform.position;

        transform.position += movementDirection * speed * Time.deltaTime;

        float horizontalSpeed = Mathf.Abs(transform.position.x - previousPosition.x) / Mathf.Max(Time.deltaTime, 0.0001f);

        bool movingRight = movementDirection.x >= 0f;
        if (movingRight && transform.position.x >= despawnX)
        {
            Destroy(gameObject);
        }
        else if (!movingRight && transform.position.x <= despawnX)
        {
            Destroy(gameObject);
        }

        return horizontalSpeed;
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

    private void ApplyFrontBackVisuals(SpawnPointPreference spawnSide)
    {
        bool isFront = spawnSide == SpawnPointPreference.Front;

        Vector3 targetScale = isFront ? frontScale : backScale;

        // Reflect on Y-axis (horizontal flip) for front spawns when enabled.
        if (isFront && mirrorFrontOnYAxis)
            targetScale.x = -Mathf.Abs(targetScale.x);

        transform.localScale = targetScale;

        // Force refresh so newly added/activated children are always included.
        cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        string layerName = isFront ? frontSortingLayerName : backSortingLayerName;

        if (cachedRenderers == null) return;
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null) continue;
            cachedRenderers[i].sortingLayerName = layerName;

            // Also enforce the same layer for any nested SpriteRenderer under this renderer's subtree.
            SpriteRenderer[] nestedRenderers = cachedRenderers[i].GetComponentsInChildren<SpriteRenderer>(true);
            for (int j = 0; j < nestedRenderers.Length; j++)
            {
                if (nestedRenderers[j] == null) continue;
                nestedRenderers[j].sortingLayerName = layerName;
            }
        }
    }

    private void UpdateAnimatorHorizontalSpeed(float horizontalSpeed)
    {
        if (animator == null || string.IsNullOrWhiteSpace(horizontalSpeedParameter))
            return;

        animator.SetFloat(horizontalSpeedParameter, Mathf.Max(0f, horizontalSpeed));
    }

    private IEnumerator BoughtAndFadeRoutine()
    {
        if (animator != null && !string.IsNullOrWhiteSpace(boughtTriggerName))
            animator.SetTrigger(boughtTriggerName);

        yield return new WaitForSeconds(Mathf.Max(0f, boughtDelayBeforeFade));

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            cachedRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        float duration = Mathf.Max(0.01f, boughtFadeDuration);
        float elapsed = 0f;

        Color[] startColors = new Color[cachedRenderers.Length];
        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] != null)
                startColors[i] = cachedRenderers[i].color;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] == null) continue;

                Color c = startColors[i];
                c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                cachedRenderers[i].color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}
