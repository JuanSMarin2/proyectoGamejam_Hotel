using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Vendor Separation")]
    [SerializeField] private bool enableVendorSeparation = true;
    [SerializeField] private float separationColliderScale = 1.1f;
    [SerializeField] private Collider2D separationTrigger2D;
    [SerializeField] private bool autoCreateSeparationTrigger = true;
    [SerializeField] private bool addKinematicRigidbody2DForTriggers = true;
    [SerializeField] private float separationSpeedBoost = 0.05f;
    [SerializeField] private float separationSpeedReturnDuration = 0.35f;
    [SerializeField] private bool debugVendorSeparation = false;

    private Vector3 movementDirection;
    private float speed;
    private float baseSpeed;
    private float despawnX;
    private bool moving;
    private bool temporarilyStopped;
    private float stopAttemptTimer;
    private float stopDurationTimer;
    private SpriteRenderer[] cachedRenderers;
    private Collider[] cachedColliders3D;
    private Collider2D[] cachedColliders2D;
    private SpawnPointPreference currentSpawnSide = SpawnPointPreference.Any;
    private Rigidbody2D cachedRigidbody2D;
    private readonly HashSet<int> separationContactIds = new HashSet<int>();
    private bool returningToBaseSpeed;
    private float returnSpeedElapsed;
    private float returnSpeedStart;

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
        baseSpeed = speed;
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

        SetupSeparationTrigger();

        ApplySignSprite();
    }

    private void Update()
    {
        UpdateSpeedReturn();
        float horizontalSpeedThisFrame = 0f;

        if (moving)
        {
            HandleRandomStops();
            if (!temporarilyStopped)
                horizontalSpeedThisFrame = Move();
        }

        UpdateAnimatorHorizontalSpeed(horizontalSpeedThisFrame);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleVendorSeparationTrigger(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        HandleVendorSeparationTrigger(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        HandleVendorSeparationExit(other);
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

    private void SetupSeparationTrigger()
    {
        if (!enableVendorSeparation)
            return;

        if (separationTrigger2D == null && autoCreateSeparationTrigger)
        {
            Collider2D referenceCollider = null;
            if (cachedColliders2D != null)
            {
                for (int i = 0; i < cachedColliders2D.Length; i++)
                {
                    if (cachedColliders2D[i] == null) continue;
                    if (cachedColliders2D[i].isTrigger) continue;
                    referenceCollider = cachedColliders2D[i];
                    break;
                }
            }

            if (referenceCollider != null)
            {
                GameObject triggerObject = new GameObject("VendorSeparationTrigger");
                triggerObject.transform.SetParent(transform, false);
                triggerObject.layer = gameObject.layer;

                BoxCollider2D box = triggerObject.AddComponent<BoxCollider2D>();
                Vector3 boundsSize = referenceCollider.bounds.size;
                Vector3 localSize = transform.lossyScale.x == 0f || transform.lossyScale.y == 0f
                    ? boundsSize
                    : new Vector3(boundsSize.x / transform.lossyScale.x, boundsSize.y / transform.lossyScale.y, 1f);

                float scale = Mathf.Max(1f, separationColliderScale);
                box.size = new Vector2(localSize.x * scale, localSize.y * scale);
                box.isTrigger = true;
                separationTrigger2D = box;
            }
        }

        if (separationTrigger2D != null)
            separationTrigger2D.isTrigger = true;

        if (addKinematicRigidbody2DForTriggers)
        {
            cachedRigidbody2D = GetComponent<Rigidbody2D>();
            if (cachedRigidbody2D == null)
            {
                cachedRigidbody2D = gameObject.AddComponent<Rigidbody2D>();
                cachedRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                cachedRigidbody2D.gravityScale = 0f;
            }
        }
    }

    private void HandleVendorSeparationTrigger(Collider2D other)
    {
        if (!enableVendorSeparation)
            return;

        if (other == null)
            return;

        if (other.transform == transform || other.transform.IsChildOf(transform))
            return;

        Vendedor otherVendor = other.GetComponentInParent<Vendedor>();
        if (otherVendor == null || otherVendor == this)
            return;

        if (!TryGetFrontAndBackVendor(otherVendor, out Vendedor frontVendor, out Vendedor backVendor))
            return;

        if (frontVendor != this)
            return;

        RegisterSeparationContact(backVendor);

        float previousSpeed = speed;
        float targetSpeed = backVendor.speed + Mathf.Max(0f, separationSpeedBoost);
        MatchOrExceedSpeed(targetSpeed);

        if (debugVendorSeparation)
        {
            Debug.Log(
            $"[Vendedor] Separation: front={name} back={backVendor.name} backSpeed={backVendor.speed:F2} targetSpeed={targetSpeed:F2} prevSpeed={previousSpeed:F2} newSpeed={speed:F2}",
                this);
        }
    }

    private void HandleVendorSeparationExit(Collider2D other)
    {
        if (!enableVendorSeparation)
            return;

        if (other == null)
            return;

        if (other.transform == transform || other.transform.IsChildOf(transform))
            return;

        Vendedor otherVendor = other.GetComponentInParent<Vendedor>();
        if (otherVendor == null || otherVendor == this)
            return;

        UnregisterSeparationContact(otherVendor);
    }

    private bool AreMovingSameDirection(Vendedor otherVendor)
    {
        if (otherVendor == null)
            return false;

        if (Mathf.Approximately(movementDirection.x, 0f) || Mathf.Approximately(otherVendor.movementDirection.x, 0f))
            return false;

        return Mathf.Sign(movementDirection.x) == Mathf.Sign(otherVendor.movementDirection.x);
    }

    private bool TryGetFrontAndBackVendor(Vendedor otherVendor, out Vendedor frontVendor, out Vendedor backVendor)
    {
        frontVendor = null;
        backVendor = null;

        if (otherVendor == null)
            return false;

        if (!AreMovingSameDirection(otherVendor))
            return false;

        bool movingRight = movementDirection.x > 0f;

        if (movingRight)
        {
            if (transform.position.x >= otherVendor.transform.position.x)
            {
                frontVendor = this;
                backVendor = otherVendor;
            }
            else
            {
                frontVendor = otherVendor;
                backVendor = this;
            }
        }
        else
        {
            if (transform.position.x <= otherVendor.transform.position.x)
            {
                frontVendor = this;
                backVendor = otherVendor;
            }
            else
            {
                frontVendor = otherVendor;
                backVendor = this;
            }
        }

        return frontVendor != null && backVendor != null;
    }

    public void MatchOrExceedSpeed(float targetSpeed)
    {
        float desiredSpeed = Mathf.Max(speed, Mathf.Max(0f, targetSpeed));
        if (desiredSpeed > speed)
            speed = desiredSpeed;

        returningToBaseSpeed = false;

        if (!moving || temporarilyStopped)
        {
            moving = true;
            temporarilyStopped = false;
            stopDurationTimer = 0f;
            stopAttemptTimer = GetRandomStopAttemptInterval();
            UpdateAnimatorHorizontalSpeed(0f);
        }
    }

    private void RegisterSeparationContact(Vendedor otherVendor)
    {
        if (otherVendor == null)
            return;

        int id = otherVendor.GetInstanceID();
        if (separationContactIds.Add(id))
        {
            if (debugVendorSeparation)
                Debug.Log($"[Vendedor] Separation contact added => {name} with {otherVendor.name}", this);
        }

        returningToBaseSpeed = false;
    }

    private void UnregisterSeparationContact(Vendedor otherVendor)
    {
        if (otherVendor == null)
            return;

        int id = otherVendor.GetInstanceID();
        if (!separationContactIds.Remove(id))
            return;

        if (debugVendorSeparation)
            Debug.Log($"[Vendedor] Separation contact removed => {name} with {otherVendor.name}", this);

        if (separationContactIds.Count == 0)
            BeginReturnToBaseSpeed();
    }

    private void BeginReturnToBaseSpeed()
    {
        if (!enableVendorSeparation)
            return;

        returningToBaseSpeed = true;
        returnSpeedElapsed = 0f;
        returnSpeedStart = speed;
    }

    private void UpdateSpeedReturn()
    {
        if (!returningToBaseSpeed)
            return;

        if (separationContactIds.Count > 0)
        {
            returningToBaseSpeed = false;
            return;
        }

        if (Mathf.Approximately(speed, baseSpeed))
        {
            speed = baseSpeed;
            returningToBaseSpeed = false;
            return;
        }

        float duration = Mathf.Max(0f, separationSpeedReturnDuration);
        if (duration <= 0f)
        {
            speed = baseSpeed;
            returningToBaseSpeed = false;
            return;
        }

        returnSpeedElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(returnSpeedElapsed / duration);
        speed = Mathf.Lerp(returnSpeedStart, baseSpeed, t);

        if (t >= 1f)
        {
            speed = baseSpeed;
            returningToBaseSpeed = false;
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
