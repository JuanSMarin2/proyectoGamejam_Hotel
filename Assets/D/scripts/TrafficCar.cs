using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class TrafficCar : MonoBehaviour
{
    #region Serialized Fields

    [Header("Velocidad Base (Random)")]
    [SerializeField] private int baseSpeedMin = 3;
    [SerializeField] private int baseSpeedMax = 6;

    [Header("Visual - Sprites")]
    [Tooltip("Renderer al que se le asignará el sprite. Si está vacío, se intentará encontrar uno en este objeto o en sus hijos.")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Tooltip("Gama de sprites posibles. Al habilitarse el carro, se elige 1 aleatorio.")]
    [SerializeField] private Sprite[] spriteOptions;

    [Header("Steering - Capas")]
    [Tooltip("Capa(s) de obstáculos: paredes, props, etc.")]
    [SerializeField] private LayerMask obstacleLayerMask;

    [Tooltip("Capa(s) de otros carros (para separación).")]
    [SerializeField] private LayerMask carLayerMask;

    [Header("Steering - Avance")]
    [Tooltip("Dirección base hacia la que el carro intenta avanzar.")]
    [SerializeField] private Vector2 preferredDirection = Vector2.up;

    [Tooltip("Qué tan fuerte se mantiene el avance hacia preferredDirection.")]
    [Min(0f)]
    [SerializeField] private float forwardWeight = 1.0f;

    [Header("Steering - Obstáculos")]
    [Tooltip("Distancia máxima de detección de obstáculos.")]
    [Min(0.01f)]
    [SerializeField] private float obstacleDetectDistance = 1.6f;

    [Tooltip("Ángulos (en grados) para lanzar rayos relativos a la dirección actual. Ej: -45,-20,0,20,45")]
    [SerializeField] private float[] obstacleRayAngles = new float[] { -45f, -20f, 0f, 20f, 45f };

    [Tooltip("Separación entre el frente del collider y el origen del rayo.")]
    [Min(0f)]
    [SerializeField] private float rayOriginOffset = 0.05f;

    [Tooltip("Cada cuántos segundos se recalculan raycasts. 0 = cada FixedUpdate")]
    [Min(0f)]
    [SerializeField] private float obstacleDetectInterval = 0.05f;

    [Tooltip("Peso del steering de evasión de obstáculos.")]
    [Min(0f)]
    [SerializeField] private float avoidanceWeight = 3.0f;

    [Tooltip("Si el vector combinado queda casi sin avance, aplica un sesgo lateral para destrabarse.")]
    [Min(0f)]
    [SerializeField] private float unstuckSideBias = 0.25f;

    [Header("Steering - Separación")]
    [Tooltip("Radio para buscar otros carros y separarse.")]
    [Min(0f)]
    [SerializeField] private float separationRadius = 0.9f;

    [Tooltip("Cada cuántos segundos se recalcula separación. 0 = cada FixedUpdate")]
    [Min(0f)]
    [SerializeField] private float separationInterval = 0.08f;

    [Tooltip("Peso del steering de separación (boids).")]
    [Min(0f)]
    [SerializeField] private float separationWeight = 1.8f;

    [Header("Tráfico - Seguimiento (Velocidad)")]
    [Tooltip("Distancia máxima para detectar un carro al frente.")]
    [Min(0.01f)]
    [SerializeField] private float trafficDetectDistance = 1.42f;

    [Tooltip("Cada cuántos segundos se recalcula el raycast de tráfico. 0 = cada FixedUpdate")]
    [Min(0f)]
    [SerializeField] private float trafficDetectInterval = 0.05f;

    [Tooltip("Gap objetivo entre carros (aprox).")]
    [Min(0.01f)]
    [SerializeField] private float desiredGap = 1.42f;

    [Tooltip("Ganancia del seguimiento: targetSpeed = leadSpeed + followGain * (gapError)")]
    [Min(0f)]
    [SerializeField] private float followGain = 2.0f;

    [Tooltip("Si está activo, no superará la velocidad del carro líder cuando esté dentro del rango.")]
    [SerializeField] private bool matchLeadCarSpeed = true;

    [Tooltip("Tiempo de suavizado al acelerar hacia baseSpeed.")]
    [Min(0.001f)]
    [SerializeField] private float accelerationTime = 0.25f;

    [Tooltip("Tiempo de suavizado al frenar para mantener gap.")]
    [Min(0.001f)]
    [SerializeField] private float decelerationTime = 0.12f;

    [Header("Steering - Suavizado")]
    [Tooltip("Suavizado del cambio de dirección (segundos). Más alto = más lento/suave")]
    [Min(0.001f)]
    [SerializeField] private float directionSmoothTime = 0.35f;

    [Tooltip("Limita la magnitud máxima de los vectores de steering antes de combinar.")]
    [Min(0f)]
    [SerializeField] private float maxSteeringMagnitude = 1.0f;

    #endregion

    #region Private Fields

    private Rigidbody2D rb;
    private Collider2D col;
    private float baseSpeed;

    private float currentSpeed;
    private float speedVelocity;

    private Vector2 steerCurrentDirection;
    private Vector2 steerDirectionVelocity;

    private float nextObstacleDetectTime;
    private float nextSeparationTime;
    private float nextTrafficDetectTime;

    private ContactFilter2D separationFilter;

    // Estado cacheado para evitar cálculos innecesarios
    private Vector2 cachedAvoidance;
    private Vector2 cachedSeparation;

    // Buffers NonAlloc
    private readonly RaycastHit2D[] rayHitBuffer = new RaycastHit2D[4];
    private readonly RaycastHit2D[] trafficHitBuffer = new RaycastHit2D[8];
    private readonly Collider2D[] separationBuffer = new Collider2D[24];

    // Debug cache
    private Vector2 debugFinalDirection;

    // Estado de tráfico
    private bool hasCarAhead;
    private float distanceToCar;
    private float leadSpeedAlongForward;
    private Rigidbody2D leadRb;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        preferredDirection = NormalizeOrDefault(preferredDirection, Vector2.up);
        steerCurrentDirection = preferredDirection;
        steerDirectionVelocity = Vector2.zero;

        currentSpeed = 0f;
        speedVelocity = 0f;

        ConfigureSeparationFilter();
    }

    protected virtual void OnEnable()
    {
        RerollBaseSpeed();
        ApplyRandomSprite();

        nextObstacleDetectTime = 0f;
        nextSeparationTime = 0f;
        cachedAvoidance = Vector2.zero;
        cachedSeparation = Vector2.zero;
        debugFinalDirection = steerCurrentDirection;

        nextTrafficDetectTime = 0f;
        hasCarAhead = false;
        distanceToCar = trafficDetectDistance;
        leadSpeedAlongForward = baseSpeed;
        leadRb = null;

        // Arrancar suave al habilitar.
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, baseSpeed);

        ConfigureSeparationFilter();
    }

    protected virtual void OnValidate()
    {
        obstacleDetectDistance = Mathf.Max(0.01f, obstacleDetectDistance);
        obstacleDetectInterval = Mathf.Max(0f, obstacleDetectInterval);
        separationRadius = Mathf.Max(0f, separationRadius);
        separationInterval = Mathf.Max(0f, separationInterval);
        rayOriginOffset = Mathf.Max(0f, rayOriginOffset);
        forwardWeight = Mathf.Max(0f, forwardWeight);
        avoidanceWeight = Mathf.Max(0f, avoidanceWeight);
        separationWeight = Mathf.Max(0f, separationWeight);
        trafficDetectDistance = Mathf.Max(0.01f, trafficDetectDistance);
        trafficDetectInterval = Mathf.Max(0f, trafficDetectInterval);
        desiredGap = Mathf.Max(0.01f, desiredGap);
        followGain = Mathf.Max(0f, followGain);
        accelerationTime = Mathf.Max(0.001f, accelerationTime);
        decelerationTime = Mathf.Max(0.001f, decelerationTime);
        directionSmoothTime = Mathf.Max(0.001f, directionSmoothTime);
        maxSteeringMagnitude = Mathf.Max(0f, maxSteeringMagnitude);
        unstuckSideBias = Mathf.Max(0f, unstuckSideBias);
        preferredDirection = NormalizeOrDefault(preferredDirection, Vector2.up);

        ConfigureSeparationFilter();

        // Validar rango de velocidad en inspector
        if (baseSpeedMin < 0) baseSpeedMin = 0;
        if (baseSpeedMax < 0) baseSpeedMax = 0;
        if (baseSpeedMax < baseSpeedMin) baseSpeedMax = baseSpeedMin;

        if (obstacleRayAngles == null || obstacleRayAngles.Length == 0)
            obstacleRayAngles = new float[] { 0f };

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void ApplyRandomSprite()
    {
        if (spriteRenderer == null)
            return;

        if (spriteOptions == null || spriteOptions.Length == 0)
            return;

        int tries = 0;
        Sprite chosen = null;
        while (chosen == null && tries < spriteOptions.Length)
        {
            chosen = spriteOptions[Random.Range(0, spriteOptions.Length)];
            tries++;
        }

        if (chosen != null)
            spriteRenderer.sprite = chosen;
    }

    protected virtual void FixedUpdate()
    {
        DetectObstacles();

        // Steering: se calculan solo si corresponde por intervalos
        Vector2 avoidance = CalculateAvoidance();
        Vector2 separation = CalculateSeparation();
        Vector2 finalDirection = CalculateFinalDirection(avoidance, separation);

        DetectTraffic(finalDirection);
        AdjustSpeed(finalDirection);

        ApplyMovement(finalDirection);
    }

    #endregion

    #region Core Logic

    /// <summary>
    /// Detecta obstáculos con Raycast2D en múltiples direcciones. Cachea resultados por intervalos.
    /// </summary>
    protected virtual void DetectObstacles()
    {
        if (avoidanceWeight <= 0f || obstacleDetectDistance <= 0f)
        {
            cachedAvoidance = Vector2.zero;
            return;
        }

        if (obstacleDetectInterval > 0f && Time.fixedTime < nextObstacleDetectTime)
            return;

        nextObstacleDetectTime = Time.fixedTime + obstacleDetectInterval;

        Vector2 forward = NormalizeOrDefault(steerCurrentDirection, preferredDirection);
        Vector2 origin = GetRayOrigin(forward);

        Vector2 steering = Vector2.zero;

        for (int i = 0; i < obstacleRayAngles.Length; i++)
        {
            Vector2 rayDir = Rotate(forward, obstacleRayAngles[i]);
            if (rayDir.sqrMagnitude < 0.0001f)
                continue;

            int hitCount = Physics2D.RaycastNonAlloc(
                origin,
                rayDir,
                rayHitBuffer,
                obstacleDetectDistance,
                obstacleLayerMask
            );

            if (hitCount <= 0)
                continue;

            // Elegir el obstáculo válido más cercano
            float bestDistance = float.PositiveInfinity;
            Vector2 bestNormal = Vector2.zero;

            for (int h = 0; h < hitCount; h++)
            {
                RaycastHit2D hit = rayHitBuffer[h];
                if (!hit.collider)
                    continue;

                // Evitar autohit
                if (hit.collider.attachedRigidbody == rb)
                    continue;

                if (hit.distance < bestDistance)
                {
                    bestDistance = hit.distance;
                    bestNormal = hit.normal;
                }
            }

            if (bestDistance < float.PositiveInfinity)
            {
                float t = 1f - Mathf.Clamp01(bestDistance / obstacleDetectDistance);

                // Preferir empujar usando la normal del choque (más estable en esquinas)
                Vector2 push = (bestNormal.sqrMagnitude > 0.0001f) ? bestNormal : -rayDir;
                steering += push.normalized * t;
            }
        }

        cachedAvoidance = ClampMagnitude(steering, maxSteeringMagnitude);
    }

    /// <summary>Calcula el vector de evasión en base a lo detectado en DetectObstacles.</summary>
    protected virtual Vector2 CalculateAvoidance()
    {
        return cachedAvoidance;
    }

    /// <summary>
    /// Separación (boids) para mantener distancia con otros carros. Cachea resultados por intervalos.
    /// </summary>
    protected virtual Vector2 CalculateSeparation()
    {
        if (separationWeight <= 0f || separationRadius <= 0f)
        {
            cachedSeparation = Vector2.zero;
            return cachedSeparation;
        }

        if (separationInterval > 0f && Time.fixedTime < nextSeparationTime)
            return cachedSeparation;

        nextSeparationTime = Time.fixedTime + separationInterval;

        int count = Physics2D.OverlapCircle(
            rb.position,
            separationRadius,
            separationFilter,
            separationBuffer
        );

        if (count <= 0)
        {
            cachedSeparation = Vector2.zero;
            return cachedSeparation;
        }

        Vector2 away = Vector2.zero;
        int neighbors = 0;

        // Separación lateral para evitar que el steering empuje hacia atrás.
        Vector2 forward = NormalizeOrDefault(steerCurrentDirection, preferredDirection);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = separationBuffer[i];
            if (!other)
                continue;

            if (other.attachedRigidbody == rb)
                continue;

            Vector2 otherPos = other.attachedRigidbody ? other.attachedRigidbody.position : (Vector2)other.transform.position;
            Vector2 delta = rb.position - otherPos;
            float dSqr = delta.sqrMagnitude;

            if (dSqr < 0.0001f)
                continue;

            // Más fuerte cuanto más cerca.
            float inv = 1f / dSqr;
            away += delta * inv;
            neighbors++;
        }

        if (neighbors <= 0)
        {
            cachedSeparation = Vector2.zero;
            return cachedSeparation;
        }

        away /= neighbors;

        // Quitar componente en el eje forward (deja solo lateral).
        float along = Vector2.Dot(away, forward);
        away -= forward * along;

        cachedSeparation = ClampMagnitude(away, maxSteeringMagnitude);
        return cachedSeparation;
    }

    /// <summary>
    /// Raycast frontal para detectar un carro adelante (para control de velocidad).
    /// </summary>
    protected virtual void DetectTraffic(Vector2 forward)
    {
        if (trafficDetectDistance <= 0f)
        {
            hasCarAhead = false;
            return;
        }

        if (trafficDetectInterval > 0f && Time.fixedTime < nextTrafficDetectTime)
            return;

        nextTrafficDetectTime = Time.fixedTime + trafficDetectInterval;

        Vector2 dir = NormalizeOrDefault(forward, preferredDirection);
        Vector2 origin = GetRayOrigin(dir);

        int hitCount = Physics2D.RaycastNonAlloc(
            origin,
            dir,
            trafficHitBuffer,
            trafficDetectDistance,
            carLayerMask
        );

        hasCarAhead = false;
        distanceToCar = trafficDetectDistance;
        leadSpeedAlongForward = baseSpeed;
        leadRb = null;

        if (hitCount <= 0)
            return;

        float bestDistance = float.PositiveInfinity;
        RaycastHit2D bestHit = default;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = trafficHitBuffer[i];
            if (!hit.collider)
                continue;

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

        leadRb = bestHit.collider.attachedRigidbody;
        if (leadRb != null)
            leadSpeedAlongForward = Mathf.Max(0f, Vector2.Dot(leadRb.linearVelocity, dir));
    }

    /// <summary>
    /// Ajusta la velocidad suavemente para mantener distancia con el carro de adelante.
    /// No usa reversa: mínimo 0.
    /// </summary>
    protected virtual void AdjustSpeed(Vector2 forward)
    {
        float targetSpeed = baseSpeed;
        float smoothTime = accelerationTime;

        if (hasCarAhead)
        {
            float gapError = distanceToCar - desiredGap;
            float leadSpeed = Mathf.Max(0f, leadSpeedAlongForward);

            float followTarget = leadSpeed + (followGain * gapError);
            targetSpeed = Mathf.Clamp(followTarget, 0f, baseSpeed);
            smoothTime = (targetSpeed < currentSpeed) ? decelerationTime : accelerationTime;

            if (matchLeadCarSpeed)
                targetSpeed = Mathf.Min(targetSpeed, leadSpeed);
        }

        currentSpeed = Mathf.SmoothDamp(
            currentSpeed,
            targetSpeed,
            ref speedVelocity,
            smoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        currentSpeed = Mathf.Clamp(currentSpeed, 0f, baseSpeed);
    }

    private void ConfigureSeparationFilter()
    {
        separationFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = carLayerMask,
            useTriggers = false
        };
    }

    /// <summary>
    /// Combina avance + evasión + separación y devuelve la dirección objetivo normalizada.
    /// </summary>
    protected virtual Vector2 CalculateFinalDirection(Vector2 avoidance, Vector2 separation)
    {
        Vector2 forward = NormalizeOrDefault(preferredDirection, Vector2.up);

        Vector2 combined = (forward * forwardWeight) + (avoidance * avoidanceWeight) + (separation * separationWeight);
        if (combined.sqrMagnitude < 0.0001f)
            combined = forward;

        // Si el combinado se opone demasiado al avance, añade un sesgo lateral estable
        // para evitar quedarse "pegado" a una pared sin frenar.
        if (unstuckSideBias > 0f)
        {
            float forwardDot = Vector2.Dot(combined.normalized, forward);
            if (forwardDot < 0.15f)
            {
                Vector2 right = new Vector2(forward.y, -forward.x);
                combined += right * unstuckSideBias;
            }
        }

        Vector2 target = combined.normalized;

        steerCurrentDirection = Vector2.SmoothDamp(
            steerCurrentDirection,
            target,
            ref steerDirectionVelocity,
            directionSmoothTime,
            Mathf.Infinity,
            Time.fixedDeltaTime
        );

        if (steerCurrentDirection.sqrMagnitude < 0.0001f)
            steerCurrentDirection = forward;
        else
            steerCurrentDirection.Normalize();

        debugFinalDirection = steerCurrentDirection;
        return steerCurrentDirection;
    }

    /// <summary>
    /// Aplica el movimiento final. Por defecto: velocidad constante hacia finalDirection.
    /// </summary>
    protected virtual void ApplyMovement(Vector2 finalDirection)
    {
        rb.linearVelocity = finalDirection * currentSpeed;
    }

    #endregion

    #region Public API

    public int GetBaseSpeedMin() => baseSpeedMin;

    public int GetBaseSpeedMax() => baseSpeedMax;

    /// <summary>Fuerza una velocidad base (por código). También limita la velocidad actual.</summary>
    public void SetBaseSpeed(float newSpeed)
    {
        baseSpeed = Mathf.Max(0f, newSpeed);
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
    }

    public void SetDirection(Vector2 newDirection)
    {
        preferredDirection = NormalizeOrDefault(newDirection, preferredDirection);
        if (steerCurrentDirection.sqrMagnitude < 0.0001f)
            steerCurrentDirection = preferredDirection;
    }

    public Vector2 GetDirection() => steerCurrentDirection;

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

    private Vector2 GetRayOrigin(Vector2 direction)
    {
        Vector2 dir = NormalizeOrDefault(direction, preferredDirection);
        Vector2 front = rb.position;
        if (col != null)
        {
            Bounds b = col.bounds;
            float extent = Mathf.Abs(Vector2.Dot(dir, new Vector2(b.extents.x, b.extents.y)));
            front += dir * extent;
        }

        return front + dir * rayOriginOffset;
    }

    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float sin = Mathf.Sin(rad);
        float cos = Mathf.Cos(rad);
        return new Vector2((v.x * cos) - (v.y * sin), (v.x * sin) + (v.y * cos));
    }

    private static Vector2 ClampMagnitude(Vector2 v, float max)
    {
        if (max <= 0f)
            return Vector2.zero;

        float mSqr = v.sqrMagnitude;
        if (mSqr <= (max * max))
            return v;

        return v.normalized * max;
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Raycasts de obstáculos
        Vector2 forward = (Application.isPlaying)
            ? NormalizeOrDefault(steerCurrentDirection, NormalizeOrDefault(preferredDirection, Vector2.up))
            : NormalizeOrDefault(preferredDirection, Vector2.up);

        Vector2 origin2D = (Application.isPlaying && rb != null) ? rb.position : (Vector2)transform.position;
        Vector2 rayOrigin = origin2D + forward * rayOriginOffset;

        if (obstacleRayAngles != null)
        {
            for (int i = 0; i < obstacleRayAngles.Length; i++)
            {
                Vector2 dir = Rotate(forward, obstacleRayAngles[i]);
                Gizmos.color = new Color(0f, 1f, 1f, 0.8f);
                Gizmos.DrawLine(rayOrigin, rayOrigin + dir * obstacleDetectDistance);
            }
        }

        // Vectores de steering
        Vector3 o = (Vector3)origin2D;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(o, o + (Vector3)(NormalizeOrDefault(preferredDirection, Vector2.up) * 0.8f));

        Gizmos.color = Color.red;
        Gizmos.DrawLine(o, o + (Vector3)(cachedAvoidance * 0.8f));

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(o, o + (Vector3)(cachedSeparation * 0.8f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(o, o + (Vector3)(NormalizeOrDefault(debugFinalDirection, forward) * 1.0f));
    }
#endif
}
