using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TrafficCar : MonoBehaviour
{
    #region Serialized Fields

    [Header("Velocidad Base (Random)")]
    [SerializeField] private int baseSpeedMin = 3;
    [SerializeField] private int baseSpeedMax = 6;

    [Header("Detección de Tráfico")]
    [SerializeField] private LayerMask carLayerMask;
    [Tooltip("Largo del raycast y distancia objetivo entre carros (gap). Ej: 1.42")]
    [SerializeField] private float detectionDistance = 1.42f;

    #endregion

    #region Private Fields

    private Rigidbody2D rb;
    private Collider2D col;
    private Vector2 directionNormalized;

    // Parámetros de comportamiento (solo por código)
    private Vector2 direction = Vector2.up;
    private float baseSpeed;

    private float rayOriginOffset = 0.05f;
    private float detectInterval = 0.05f;

    private float accelerationTime = 0.25f;
    private float decelerationTime = 0.12f;
    private float reverseSpeed = 1.5f;
    private float reverseTime = 0.10f;
    private bool allowReverseWhenTooClose = true;
    private bool matchLeadCarSpeed = true;
    private float followGain = 2.0f;
    private float gapTolerance = 0.02f;

    private float currentSpeed;
    private float speedVelocity;
    private float nextDetectTime;

    private bool hasCarAhead;
    private float distanceToCar;
    private float leadSpeedAlongDirection;
    private Rigidbody2D leadRb;
    private Collider2D leadCollider;

    private bool isCollidingWithCar;
    private float lastCollisionTime;

    // Buffer para evitar allocations
    private readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[8];

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        directionNormalized = NormalizeOrDefault(direction, Vector2.up);
    }

    private void OnEnable()
    {
        RerollBaseSpeed();

        // Reset de estado para spawns/pooling
        speedVelocity = 0f;
        nextDetectTime = 0f;
        hasCarAhead = false;
        isCollidingWithCar = false;
        lastCollisionTime = 0f;

        // Alinear velocidad actual con la nueva base
        currentSpeed = Mathf.Clamp(currentSpeed, allowReverseWhenTooClose ? -reverseSpeed : 0f, baseSpeed);
    }

    private void OnValidate()
    {
        detectionDistance = Mathf.Max(0.01f, detectionDistance);

        // Validar rango de velocidad en inspector
        if (baseSpeedMin < 0) baseSpeedMin = 0;
        if (baseSpeedMax < 0) baseSpeedMax = 0;
        if (baseSpeedMax < baseSpeedMin) baseSpeedMax = baseSpeedMin;

        // Nota: el resto de parámetros se modifican solo por código.
    }

    private void FixedUpdate()
    {
        DetectTraffic();
        AdjustSpeed();
        Move();
    }

    #endregion

    #region Core Logic

    private void Move()
    {
        rb.linearVelocity = directionNormalized * currentSpeed;
    }

    private void DetectTraffic()
    {
        if (detectInterval > 0f && Time.fixedTime < nextDetectTime)
            return;

        nextDetectTime = Time.fixedTime + detectInterval;

        Vector2 origin = GetRayOrigin();

        int hitCount = Physics2D.RaycastNonAlloc(
            origin,
            directionNormalized,
            hitBuffer,
            detectionDistance,
            carLayerMask
        );

        hasCarAhead = false;
        distanceToCar = detectionDistance;
        leadSpeedAlongDirection = baseSpeed;
        leadRb = null;
        leadCollider = null;

        if (hitCount <= 0)
            return;

        // Elegir el hit válido más cercano
        float bestDistance = float.PositiveInfinity;
        RaycastHit2D bestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hitBuffer[i];
            if (!hit.collider)
                continue;

            // Evitar autohit
            if (hit.collider.attachedRigidbody == rb)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                bestHit = hit;
            }
        }

        if (!bestHit.collider)
            return;

        hasCarAhead = true;
        distanceToCar = bestHit.distance;

        leadCollider = bestHit.collider;
        leadRb = bestHit.collider.attachedRigidbody;
        if (leadRb != null)
            leadSpeedAlongDirection = Vector2.Dot(leadRb.linearVelocity, directionNormalized);
    }

    private void AdjustSpeed()
    {
        float desiredGap = detectionDistance;

        // Base: sin tráfico = ir a velocidad normal
        float targetSpeed = baseSpeed;
        float smoothTime = accelerationTime;

        // Si estamos chocando/solapados con un carro, forzar reversa hasta separar
        if (allowReverseWhenTooClose && IsRecentlyCollidingWithCar())
        {
            targetSpeed = -reverseSpeed;
            smoothTime = reverseTime;
        }
        else if (hasCarAhead)
        {
            // Error de gap: positivo = hay más espacio que el deseado, negativo = estamos muy cerca
            float gapError = distanceToCar - desiredGap;

            // Velocidad del líder a lo largo de la dirección
            float leadForwardSpeed = Mathf.Max(0f, leadSpeedAlongDirection);

            // Control simple: intenta mantener el gap ajustando velocidad respecto al líder
            // target = leaderSpeed + gain * error
            float followTarget = leadForwardSpeed + followGain * gapError;

            // Si estamos demasiado cerca (o casi), frenar fuerte o reversa
            if (gapError <= -gapTolerance)
            {
                smoothTime = decelerationTime;
                if (allowReverseWhenTooClose)
                    targetSpeed = Mathf.Clamp(followTarget, -reverseSpeed, baseSpeed);
                else
                    targetSpeed = Mathf.Clamp(followTarget, 0f, baseSpeed);
            }
            else
            {
                // Si hay espacio suficiente, nunca superar baseSpeed.
                smoothTime = (followTarget < currentSpeed) ? decelerationTime : accelerationTime;
                targetSpeed = Mathf.Clamp(followTarget, 0f, baseSpeed);
            }

            // Para evitar alcanzar al líder cuando todavía estamos dentro del rango, opcionalmente limita
            if (matchLeadCarSpeed)
                targetSpeed = Mathf.Min(targetSpeed, leadForwardSpeed);
        }

        currentSpeed = Mathf.SmoothDamp(
            currentSpeed,
            targetSpeed,
            ref speedVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        float minSpeed = allowReverseWhenTooClose ? -reverseSpeed : 0f;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, baseSpeed);
    }

    #endregion

    #region Public API

    public int GetBaseSpeedMin() => baseSpeedMin;

    public int GetBaseSpeedMax() => baseSpeedMax;

    /// <summary>Fuerza una velocidad base (por código). También limita la velocidad actual.</summary>
    public void SetBaseSpeed(float newSpeed)
    {
        baseSpeed = Mathf.Max(0f, newSpeed);
        currentSpeed = Mathf.Clamp(currentSpeed, allowReverseWhenTooClose ? -reverseSpeed : 0f, baseSpeed);
    }

    /// <summary>Configura el rango de velocidad base que se randomiza en Awake.</summary>
    public void SetBaseSpeedRange(int min, int max)
    {
        baseSpeedMin = Mathf.Max(0, min);
        baseSpeedMax = Mathf.Max(baseSpeedMin, max);
    }

    public float GetBaseSpeed() => baseSpeed;

    /// <summary>Vuelve a elegir una velocidad base aleatoria según el rango del Inspector.</summary>
    public void RerollBaseSpeed()
    {
        baseSpeed = PickRandomBaseSpeed();

        // Si el carro estaba más rápido que la nueva base, recortarlo.
        float minSpeed = allowReverseWhenTooClose ? -reverseSpeed : 0f;
        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, baseSpeed);
    }

    public void SetDirection(Vector2 newDirection)
    {
        direction = newDirection;
        directionNormalized = NormalizeOrDefault(direction, directionNormalized);
    }

    /// <summary>Permite ajustar la detección sin exponerlo en Inspector.</summary>
    public void SetDetectionSettings(float newDetectionDistance, float newRayOriginOffset, float newDetectInterval)
    {
        detectionDistance = Mathf.Max(0.01f, newDetectionDistance);
        rayOriginOffset = Mathf.Max(0f, newRayOriginOffset);
        detectInterval = Mathf.Max(0f, newDetectInterval);
    }

    /// <summary>Permite ajustar el seguimiento/frenado por código (no Inspector).</summary>
    public void SetFollowTuning(float newFollowGain, float newGapTolerance, float newAccelerationTime, float newDecelerationTime)
    {
        followGain = Mathf.Max(0f, newFollowGain);
        gapTolerance = Mathf.Max(0f, newGapTolerance);
        accelerationTime = Mathf.Max(0.0001f, newAccelerationTime);
        decelerationTime = Mathf.Max(0.0001f, newDecelerationTime);
    }

    /// <summary>Permite ajustar reversa por código (no Inspector).</summary>
    public void SetReverseSettings(bool allowReverse, float newReverseSpeed, float newReverseTime)
    {
        allowReverseWhenTooClose = allowReverse;
        reverseSpeed = Mathf.Max(0f, newReverseSpeed);
        reverseTime = Mathf.Max(0.0001f, newReverseTime);
    }

    public void SetMatchLeadCarSpeed(bool enabled) => matchLeadCarSpeed = enabled;

    public Vector2 GetDirection() => directionNormalized;

    public float GetCurrentSpeed() => currentSpeed;

    #endregion

    #region Helpers

    private float PickRandomBaseSpeed()
    {
        int min = Mathf.Max(0, baseSpeedMin);
        int max = Mathf.Max(min, baseSpeedMax);
        // Random.Range(int, int) => max exclusivo
        return Random.Range(min, max + 1);
    }

    private static Vector2 NormalizeOrDefault(Vector2 value, Vector2 fallback)
    {
        if (value.sqrMagnitude < 0.0001f)
            return fallback;

        return value.normalized;
    }

    private Vector2 GetRayOrigin()
    {
        // Lanza el raycast desde el frente del collider, para que la distancia sea el gap real entre carros.
        Vector2 front = rb.position;
        if (col != null)
        {
            Bounds b = col.bounds;
            float extent = Mathf.Abs(Vector2.Dot(directionNormalized, new Vector2(b.extents.x, b.extents.y)));
            // La línea de arriba es una aproximación barata; mejor que disparar desde el centro.
            front += directionNormalized * extent;
        }

        return front + directionNormalized * rayOriginOffset;
    }

    private bool IsRecentlyCollidingWithCar()
    {
        // Evita que un frame sin evento de colisión corte la reversa inmediatamente.
        return isCollidingWithCar && (Time.fixedTime - lastCollisionTime) < 0.15f;
    }

    private bool IsInCarLayerMask(int layer)
    {
        return (carLayerMask.value & (1 << layer)) != 0;
    }

    #endregion

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!IsInCarLayerMask(collision.gameObject.layer))
            return;

        isCollidingWithCar = true;
        lastCollisionTime = Time.fixedTime;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!IsInCarLayerMask(collision.gameObject.layer))
            return;

        isCollidingWithCar = false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 dir = (direction.sqrMagnitude < 0.0001f) ? Vector2.up : direction.normalized;
        Vector3 origin = transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + (Vector3)(dir * detectionDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin + (Vector3)(dir * detectionDistance), 0.05f);
    }
#endif
}
