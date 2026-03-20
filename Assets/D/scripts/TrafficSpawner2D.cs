using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner de trafico 2D con intervalo fijo y object pooling.
/// - Spawn por tiempo deterministico (sin probabilidad, burst ni jitter)
/// - Batch de tamano variable por ciclo (minCarsPerSpawn..maxCarsPerSpawn)
/// - Validacion de distancia minima para evitar solapamientos
/// </summary>
[DisallowMultipleComponent]
public class TrafficSpawner2D : MonoBehaviour
{
    #region Serialized Fields

    [Header("Prefabs")]
    [Tooltip("Prefabs con TrafficCar (o derivados).")]
    [SerializeField] private TrafficCar[] vehiclePrefabs;

    [Header("Zonas")]
    [Tooltip("Zona de referencia para spawn. Si useSpawnAreaBounds esta activo, se usa para minX/maxX y spawnY.")]
    [SerializeField] private BoxCollider2D spawnArea;

    [Tooltip("Zona donde el vehiculo sigue activo. Al salir, se desactiva y vuelve al pool.")]
    [SerializeField] private BoxCollider2D despawnArea;

    [Tooltip("Padding extra para despawn cuando no hay despawnArea (en unidades de mundo).")]
    [Min(0f)]
    [SerializeField] private float despawnPadding = 6f;

    [Header("Limites")]
    [Min(0)]
    [SerializeField] private int targetActiveVehicles = 12;

    [Header("Spawn Fijo")]
    [Tooltip("Intervalo fijo entre ciclos de spawn.")]
    [Min(0.01f)]
    [SerializeField] private float spawnInterval = 0.6f;

    [Tooltip("Minimo de carros por ciclo.")]
    [Min(1)]
    [SerializeField] private int minCarsPerSpawn = 1;

    [Tooltip("Maximo de carros por ciclo.")]
    [Min(1)]
    [SerializeField] private int maxCarsPerSpawn = 3;

    [Header("Posicion")]
    [Tooltip("Si esta activo, minX/maxX y spawnY se toman del BoxCollider2D spawnArea.")]
    [SerializeField] private bool useSpawnAreaBounds = true;

    [Tooltip("Limite minimo de X para spawn cuando useSpawnAreaBounds esta inactivo.")]
    [SerializeField] private float minX = -5f;

    [Tooltip("Limite maximo de X para spawn cuando useSpawnAreaBounds esta inactivo.")]
    [SerializeField] private float maxX = 5f;

    [Tooltip("Posicion Y fija de spawn (parte inferior) cuando useSpawnAreaBounds esta inactivo.")]
    [SerializeField] private float spawnY = -4f;

    [Header("Espaciado")]
    [Tooltip("Distancia minima entre vehiculos al spawnear.")]
    [Min(0.01f)]
    [SerializeField] private float minSpawnDistance = 1.2f;

    [Tooltip("Separacion horizontal base entre vehiculos del mismo batch.")]
    [Min(0.01f)]
    [SerializeField] private float batchSpacingX = 1.4f;

    [Tooltip("Validacion extra con OverlapCircle para evitar spawn sobre obstaculos/vehiculos externos.")]
    [SerializeField] private bool usePhysicsSpawnValidation = true;

    [Tooltip("Capas que bloquean el spawn al usar OverlapCircle.")]
    [SerializeField] private LayerMask spawnValidationMask;

    [Header("Batch Delay (Opcional)")]
    [Tooltip("Si esta activo, agrega un delay pequeno entre autos del mismo batch.")]
    [SerializeField] private bool useBatchSpawnDelay;

    [Tooltip("Delay minimo entre autos del mismo batch.")]
    [Min(0f)]
    [SerializeField] private float batchSpawnDelayMin = 0.05f;

    [Tooltip("Delay maximo entre autos del mismo batch.")]
    [Min(0f)]
    [SerializeField] private float batchSpawnDelayMax = 0.2f;

    [Header("Pooling")]
    [Min(0)]
    [SerializeField] private int prewarmPerPrefab = 6;

    [Header("Compatibilidad Prefabs")]
    [Tooltip("Si esta activo, corrige Rigidbody2D al spawnear (evita prefabs con bodyType Static o FreezePosition).")]
    [SerializeField] private bool fixRigidbodiesOnSpawn = true;

    [Header("Reciclaje (Atascados)")]
    [Tooltip("Si un vehiculo no se mueve mas que este delta, cuenta como quieto.")]
    [Min(0f)]
    [SerializeField] private float stuckMinMoveDelta = 0.03f;

    [Tooltip("Segundos quieto para reciclar al pool.")]
    [Min(0.1f)]
    [SerializeField] private float stuckTimeToRecycle = 2.5f;

    [Header("Debug")]
    [SerializeField] private bool logLifecycle;
    [SerializeField] private bool logSpawnFailures;

    #endregion

    #region Private Fields

    private readonly List<TrafficCar> activeVehicles = new List<TrafficCar>(64);
    private readonly Dictionary<int, Stack<TrafficCar>> poolsByPrefabIndex = new Dictionary<int, Stack<TrafficCar>>();
    private readonly Dictionary<TrafficCar, int> instanceToPrefabIndex = new Dictionary<TrafficCar, int>();

    private readonly Dictionary<TrafficCar, Vector2> lastPositionByVehicle = new Dictionary<TrafficCar, Vector2>();
    private readonly Dictionary<TrafficCar, float> stillTimeByVehicle = new Dictionary<TrafficCar, float>();

    private readonly Collider2D[] spawnValidationBuffer = new Collider2D[16];

    private Coroutine spawnRoutine;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (logLifecycle)
            Debug.Log($"[TrafficSpawner2D] Awake: {name} (id={GetInstanceID()}, scene={gameObject.scene.name})", this);

        if (spawnArea == null)
            spawnArea = GetComponent<BoxCollider2D>();

        BuildPools();
        Prewarm();
    }

    private void OnEnable()
    {
        if (logLifecycle)
            Debug.Log($"[TrafficSpawner2D] OnEnable: {name} (id={GetInstanceID()}, scene={gameObject.scene.name})", this);

        if (spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (logLifecycle)
            Debug.LogWarning($"[TrafficSpawner2D] OnDisable: {name} (id={GetInstanceID()}, scene={gameObject.scene.name}, frame={Time.frameCount})\n{System.Environment.StackTrace}", this);

        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (!logLifecycle || !Application.isPlaying)
            return;

        Debug.LogWarning($"[TrafficSpawner2D] OnDestroy: {name} (id={GetInstanceID()}, scene={gameObject.scene.name}, frame={Time.frameCount})\n{System.Environment.StackTrace}", this);
    }

    private void Update()
    {
        ReclaimDespawnedVehicles();
        ReclaimStuckVehicles(Time.deltaTime);

        if (enabled && gameObject.activeInHierarchy && spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnValidate()
    {
        minCarsPerSpawn = Mathf.Max(1, minCarsPerSpawn);
        maxCarsPerSpawn = Mathf.Max(minCarsPerSpawn, maxCarsPerSpawn);

        if (maxX < minX)
            maxX = minX;

        spawnInterval = Mathf.Max(0.01f, spawnInterval);
        minSpawnDistance = Mathf.Max(0.01f, minSpawnDistance);
        batchSpacingX = Mathf.Max(0.01f, batchSpacingX);

        batchSpawnDelayMin = Mathf.Max(0f, batchSpawnDelayMin);
        batchSpawnDelayMax = Mathf.Max(batchSpawnDelayMin, batchSpawnDelayMax);

        stuckMinMoveDelta = Mathf.Max(0f, stuckMinMoveDelta);
        stuckTimeToRecycle = Mathf.Max(0.1f, stuckTimeToRecycle);

        if (spawnArea == null)
            spawnArea = GetComponent<BoxCollider2D>();
    }

    #endregion

    #region Core

    private IEnumerator SpawnLoop()
    {
        yield return null;

        while (enabled)
        {
            CleanupActiveList();

            if (activeVehicles.Count < targetActiveVehicles)
                yield return SpawnBatch();

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator SpawnBatch()
    {
        int availableSlots = Mathf.Max(0, targetActiveVehicles - activeVehicles.Count);
        if (availableSlots <= 0)
            yield break;

        int spawnCount = Mathf.Min(GetSpawnCount(), availableSlots);
        if (spawnCount <= 0)
            yield break;

        List<Vector2> spawnedInBatch = new List<Vector2>(spawnCount);

        float cycleMinX;
        float cycleMaxX;
        float cycleSpawnY;
        ResolveSpawnBounds(out cycleMinX, out cycleMaxX, out cycleSpawnY);

        float centerX = Random.Range(cycleMinX, cycleMaxX);
        float spacing = Mathf.Max(minSpawnDistance, batchSpacingX);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 pos = GetRandomSpawnPosition(i, spawnCount, centerX, spacing, cycleMinX, cycleMaxX, cycleSpawnY);
            if (!IsSpawnValid(pos, spawnedInBatch))
                continue;

            if (SpawnVehicle(pos))
                spawnedInBatch.Add(pos);

            if (useBatchSpawnDelay && i < spawnCount - 1)
            {
                float delay = Random.Range(batchSpawnDelayMin, batchSpawnDelayMax);
                if (delay > 0f)
                    yield return new WaitForSeconds(delay);
            }
        }
    }

    private int GetSpawnCount()
    {
        int dynamicMaxCarsPerSpawn = GetDynamicMaxCarsPerSpawn();
        int dynamicMinCarsPerSpawn = Mathf.Min(minCarsPerSpawn, dynamicMaxCarsPerSpawn);
        return Random.Range(dynamicMinCarsPerSpawn, dynamicMaxCarsPerSpawn + 1);
    }

    private int GetDynamicMaxCarsPerSpawn()
    {
        float speed = GetLevelTrafficSpeed();
        float carsToSubtract = Mathf.Max(0f, speed - 1f);
        int adjustedMaxCars = maxCarsPerSpawn - Mathf.FloorToInt(carsToSubtract);
        return Mathf.Max(1, adjustedMaxCars);
    }

    private float GetLevelTrafficSpeed()
    {
        float maxTrafficSpeed = GetTrafficCarMaxSpeedForLevel();

        if (MinigameManager.instance == null)
            return maxTrafficSpeed;

        return Mathf.Min(MinigameManager.instance.Speed, maxTrafficSpeed);
    }

    private float GetTrafficCarMaxSpeedForLevel()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            return 1f;

        float maxSpeed = 1f;
        for (int i = 0; i < vehiclePrefabs.Length; i++)
        {
            TrafficCar prefab = vehiclePrefabs[i];
            if (prefab == null)
                continue;

            maxSpeed = Mathf.Max(maxSpeed, prefab.GetMaxMinigameSpeedMultiplier());
        }

        return maxSpeed;
    }

    private Vector2 GetRandomSpawnPosition(
        int batchIndex,
        int batchCount,
        float centerX,
        float spacing,
        float cycleMinX,
        float cycleMaxX,
        float cycleSpawnY)
    {
        float startX = centerX - (((batchCount - 1) * 0.5f) * spacing);
        float x = startX + (batchIndex * spacing);
        x = Mathf.Clamp(x, cycleMinX, cycleMaxX);
        return new Vector2(x, cycleSpawnY);
    }

    private bool IsSpawnValid(Vector2 position, List<Vector2> spawnedInBatch)
    {
        float minDistSqr = minSpawnDistance * minSpawnDistance;

        for (int i = 0; i < spawnedInBatch.Count; i++)
        {
            if ((position - spawnedInBatch[i]).sqrMagnitude < minDistSqr)
                return false;
        }

        for (int i = 0; i < activeVehicles.Count; i++)
        {
            TrafficCar v = activeVehicles[i];
            if (v == null || !v.gameObject.activeInHierarchy)
                continue;

            if ((position - (Vector2)v.transform.position).sqrMagnitude < minDistSqr)
                return false;
        }

        if (!usePhysicsSpawnValidation)
            return true;

        ContactFilter2D filter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = spawnValidationMask,
            useTriggers = true
        };

        int overlapCount = Physics2D.OverlapCircle(
            position,
            minSpawnDistance,
            filter,
            spawnValidationBuffer
        );

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D hit = spawnValidationBuffer[i];
            if (hit == null)
                continue;

            Rigidbody2D hitRb = hit.attachedRigidbody;
            if (hitRb != null && hitRb.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    private bool SpawnVehicle(Vector2 position)
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            return false;

        int prefabIndex = Random.Range(0, vehiclePrefabs.Length);
        TrafficCar vehicle = GetFromPool(prefabIndex);
        if (vehicle == null)
            return false;

        vehicle.transform.SetPositionAndRotation(position, Quaternion.identity);
        vehicle.SetDirection(Vector2.up);
        vehicle.enabled = true;

        if (!vehicle.gameObject.activeSelf)
            vehicle.gameObject.SetActive(true);

        Rigidbody2D rb = vehicle.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.simulated = true;

            if (fixRigidbodiesOnSpawn)
            {
                if (rb.bodyType == RigidbodyType2D.Static)
                    rb.bodyType = RigidbodyType2D.Dynamic;

                if ((rb.constraints & (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY)) != 0)
                {
                    bool keepFreezeRotation = (rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0;
                    rb.constraints = keepFreezeRotation ? RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.None;
                }
            }

            rb.WakeUp();
        }

        lastPositionByVehicle[vehicle] = vehicle.transform.position;
        stillTimeByVehicle[vehicle] = 0f;
        activeVehicles.Add(vehicle);

        return true;
    }

    private void ResolveSpawnBounds(out float resolvedMinX, out float resolvedMaxX, out float resolvedSpawnY)
    {
        if (useSpawnAreaBounds && spawnArea != null)
        {
            Rect r = GetWorldRect(spawnArea);
            resolvedMinX = r.xMin;
            resolvedMaxX = r.xMax;
            resolvedSpawnY = r.yMin;
            return;
        }

        resolvedMinX = minX;
        resolvedMaxX = maxX;
        resolvedSpawnY = spawnY;
    }

    public void SetSpawnInterval(float newInterval)
    {
        spawnInterval = Mathf.Max(0.01f, newInterval);
    }

    #endregion

    #region Pooling

    private void BuildPools()
    {
        poolsByPrefabIndex.Clear();
        if (vehiclePrefabs == null)
            return;

        for (int i = 0; i < vehiclePrefabs.Length; i++)
            poolsByPrefabIndex[i] = new Stack<TrafficCar>(prewarmPerPrefab);
    }

    private void Prewarm()
    {
        if (vehiclePrefabs == null)
            return;

        for (int i = 0; i < vehiclePrefabs.Length; i++)
        {
            for (int j = 0; j < prewarmPerPrefab; j++)
            {
                TrafficCar v = CreateInstance(i);
                if (v == null)
                    break;

                ReturnToPool(i, v);
            }
        }
    }

    private TrafficCar GetFromPool(int prefabIndex)
    {
        if (!poolsByPrefabIndex.TryGetValue(prefabIndex, out Stack<TrafficCar> stack))
            stack = poolsByPrefabIndex[prefabIndex] = new Stack<TrafficCar>(8);

        while (stack.Count > 0)
        {
            TrafficCar v = stack.Pop();
            if (v != null)
                return v;
        }

        return CreateInstance(prefabIndex);
    }

    private TrafficCar CreateInstance(int prefabIndex)
    {
        if (vehiclePrefabs == null || prefabIndex < 0 || prefabIndex >= vehiclePrefabs.Length)
            return null;

        TrafficCar prefab = vehiclePrefabs[prefabIndex];
        if (prefab == null)
            return null;

        TrafficCar instance = Instantiate(prefab, transform);
        instance.gameObject.SetActive(false);

        if (!instanceToPrefabIndex.ContainsKey(instance))
            instanceToPrefabIndex.Add(instance, prefabIndex);

        return instance;
    }

    private void ReturnToPool(int prefabIndex, TrafficCar vehicle)
    {
        if (vehicle == null)
            return;

        lastPositionByVehicle.Remove(vehicle);
        stillTimeByVehicle.Remove(vehicle);

        vehicle.gameObject.SetActive(false);
        vehicle.transform.SetParent(transform, true);

        if (!poolsByPrefabIndex.TryGetValue(prefabIndex, out Stack<TrafficCar> stack))
            stack = poolsByPrefabIndex[prefabIndex] = new Stack<TrafficCar>(8);

        stack.Push(vehicle);
    }

    #endregion

    #region Despawn / Maintenance

    private void CleanupActiveList()
    {
        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            TrafficCar v = activeVehicles[i];
            if (v == null || !v.gameObject.activeInHierarchy)
            {
                if (v != null)
                {
                    lastPositionByVehicle.Remove(v);
                    stillTimeByVehicle.Remove(v);
                }
                activeVehicles.RemoveAt(i);
            }
        }
    }

    private void ReclaimStuckVehicles(float dt)
    {
        if (activeVehicles.Count == 0 || stuckTimeToRecycle <= 0f)
            return;

        float epsSqr = stuckMinMoveDelta * stuckMinMoveDelta;

        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            TrafficCar v = activeVehicles[i];
            if (v == null || !v.gameObject.activeInHierarchy)
                continue;

            Vector2 pos = v.transform.position;
            if (!lastPositionByVehicle.TryGetValue(v, out Vector2 lastPos))
            {
                lastPositionByVehicle[v] = pos;
                stillTimeByVehicle[v] = 0f;
                continue;
            }

            float dSqr = (pos - lastPos).sqrMagnitude;
            if (dSqr <= epsSqr)
            {
                stillTimeByVehicle.TryGetValue(v, out float still);
                still += dt;
                stillTimeByVehicle[v] = still;

                if (still >= stuckTimeToRecycle)
                {
                    int prefabIndex = GetPrefabIndex(v);
                    activeVehicles.RemoveAt(i);
                    ReturnToPool(prefabIndex, v);
                }
            }
            else
            {
                lastPositionByVehicle[v] = pos;
                stillTimeByVehicle[v] = 0f;
            }
        }
    }

    private void ReclaimDespawnedVehicles()
    {
        if (activeVehicles.Count == 0)
            return;

        Rect despawnRect = GetDespawnRect();

        for (int i = activeVehicles.Count - 1; i >= 0; i--)
        {
            TrafficCar v = activeVehicles[i];
            if (v == null)
            {
                activeVehicles.RemoveAt(i);
                continue;
            }

            Vector2 pos = v.transform.position;
            if (despawnRect.Contains(pos))
                continue;

            int prefabIndex = GetPrefabIndex(v);
            activeVehicles.RemoveAt(i);
            ReturnToPool(prefabIndex, v);
        }
    }

    private int GetPrefabIndex(TrafficCar vehicle)
    {
        if (vehicle == null)
            return 0;

        if (instanceToPrefabIndex.TryGetValue(vehicle, out int idx))
            return idx;

        return 0;
    }

    private Rect GetDespawnRect()
    {
        if (despawnArea != null)
            return GetWorldRect(despawnArea);

        if (spawnArea == null)
            return new Rect(-9999f, -9999f, 19998f, 19998f);

        Rect r = GetWorldRect(spawnArea);
        float p = despawnPadding;
        return new Rect(r.xMin - p, r.yMin - p, r.width + (2f * p), r.height + (2f * p));
    }

    #endregion

    #region Geometry

    private static Rect GetWorldRect(BoxCollider2D box)
    {
        if (box == null)
            return new Rect();

        Vector2 size = Vector2.Scale(box.size, box.transform.lossyScale);
        Vector2 center = (Vector2)box.transform.TransformPoint(box.offset);
        Vector2 half = size * 0.5f;
        return new Rect(center - half, size);
    }

    private void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = new Color(0f, 1f, 0.2f, 0.25f);
            DrawRectGizmo(GetWorldRect(spawnArea));
        }

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.20f);
        DrawRectGizmo(GetDespawnRect());
    }

    private static void DrawRectGizmo(Rect r)
    {
        Vector3 a = new Vector3(r.xMin, r.yMin, 0f);
        Vector3 b = new Vector3(r.xMax, r.yMin, 0f);
        Vector3 c = new Vector3(r.xMax, r.yMax, 0f);
        Vector3 d = new Vector3(r.xMin, r.yMax, 0f);

        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }

    #endregion
}
