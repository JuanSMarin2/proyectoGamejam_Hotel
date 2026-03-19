using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawner avanzado de tráfico 2D con distribución natural + pooling.
/// - Mantiene densidad constante (targetActiveVehicles)
/// - Evita clusters (minSpawnDistance)
/// - Evita huecos grandes (maxGapDistance, opcional)
/// - Variación de spawn (jitter) y ráfagas pequeñas (bursts)
/// - Reutiliza vehículos desactivados (object pooling)
/// </summary>
[DisallowMultipleComponent]
public class TrafficSpawner2D : MonoBehaviour
{
    #region Serialized Fields

    [Header("Prefabs")]
    [Tooltip("Prefabs con TrafficCar (o derivados, ej: CurvedTrafficCar)")]
    [SerializeField] private TrafficCar[] vehiclePrefabs;

    [Header("Zonas")]
    [Tooltip("Zona donde se spawnea el tráfico.")]
    [SerializeField] private BoxCollider2D spawnArea;

    [Tooltip("Zona donde el vehículo sigue activo. Al salir, se desactiva y vuelve al pool. Si está vacío, se usa spawnArea con padding.")]
    [SerializeField] private BoxCollider2D despawnArea;

    [Tooltip("Padding extra para despawn cuando no hay despawnArea (en unidades de mundo).")]
    [Min(0f)]
    [SerializeField] private float despawnPadding = 6f;

    [Header("Densidad")]
    [Min(0)]
    [SerializeField] private int targetActiveVehicles = 12;

    [Header("Distribución")]
    [Tooltip("Distancia mínima entre posiciones de spawn recientes/vehículos activos.")]
    [Min(0.01f)]
    [SerializeField] private float minSpawnDistance = 1.2f;

    [Tooltip("Si > 0, evita spawns demasiado lejos de todo (reduce huecos grandes).")]
    [Min(0f)]
    [SerializeField] private float maxGapDistance = 0f;

    [Tooltip("Cuántas posiciones recientes se recuerdan para evitar clusters.")]
    [Min(0)]
    [SerializeField] private int recentPositionsCapacity = 20;

    [Tooltip("Segundos que una posición permanece en la lista de recientes. 0 = no expira por tiempo (solo por capacidad).")]
    [Min(0f)]
    [SerializeField] private float recentPositionLifetime = 8f;

    [Tooltip("Cantidad de candidatos por intento (más = mejor distribución, más costo).")]
    [Range(4, 64)]
    [SerializeField] private int candidatesPerSpawn = 18;

    [Tooltip("Intentos máximos para encontrar una posición válida.")]
    [Range(1, 60)]
    [SerializeField] private int maxAttemptsPerVehicle = 16;

    [Header("Reciclaje (Atascados)")]
    [Tooltip("Si un vehículo no se mueve más que este delta, cuenta como quieto.")]
    [Min(0f)]
    [SerializeField] private float stuckMinMoveDelta = 0.03f;

    [Tooltip("Segundos quieto para reciclar al pool (evita que vehículos trabados bloqueen la densidad).")]
    [Min(0.1f)]
    [SerializeField] private float stuckTimeToRecycle = 2.5f;

    [Header("Spawn Dinámico")]
    [Tooltip("Intervalo base entre spawns (segundos).")]
    [Min(0.01f)]
    [SerializeField] private float baseSpawnInterval = 0.6f;

    [Tooltip("Variación aleatoria del intervalo: +- jitter.")]
    [Min(0f)]
    [SerializeField] private float spawnIntervalJitter = 0.25f;

    [Tooltip("Probabilidad de iniciar una pequeña ráfaga cuando falta tráfico.")]
    [Range(0f, 1f)]
    [SerializeField] private float burstChance = 0.18f;

    [Tooltip("Cantidad mínima de vehículos en una ráfaga.")]
    [Min(1)]
    [SerializeField] private int burstMinCount = 2;

    [Tooltip("Cantidad máxima de vehículos en una ráfaga.")]
    [Min(1)]
    [SerializeField] private int burstMaxCount = 4;

    [Tooltip("Separación entre spawns dentro de una ráfaga (segundos).")]
    [Min(0.01f)]
    [SerializeField] private float burstSpacing = 0.12f;

    [Header("Pooling")]
    [Min(0)]
    [SerializeField] private int prewarmPerPrefab = 6;

    [Header("Compatibilidad Prefabs")]
    [Tooltip("Si está activo, corrige Rigidbody2D al spawnear (evita prefabs con bodyType Static o FreezePosition).")]
    [SerializeField] private bool fixRigidbodiesOnSpawn = true;

    [Header("Debug")]
    [Tooltip("Si está activo, loguea Awake/OnEnable/OnDisable/OnDestroy (útil para encontrar quién destruye el spawner al iniciar Play).")]
    [SerializeField] private bool logLifecycle;

    [Tooltip("Si está activo, loguea fallos de spawn (cuando no se encuentra posición válida).")]
    [SerializeField] private bool logSpawnFailures;

    #endregion

    #region Private Fields

    private readonly List<TrafficCar> activeVehicles = new List<TrafficCar>(64);
    private readonly Dictionary<int, Stack<TrafficCar>> poolsByPrefabIndex = new Dictionary<int, Stack<TrafficCar>>();
    private readonly Dictionary<TrafficCar, int> instanceToPrefabIndex = new Dictionary<TrafficCar, int>();

    private readonly Dictionary<TrafficCar, Vector2> lastPositionByVehicle = new Dictionary<TrafficCar, Vector2>();
    private readonly Dictionary<TrafficCar, float> stillTimeByVehicle = new Dictionary<TrafficCar, float>();

    private struct RecentSpawn
    {
        public Vector2 pos;
        public float time;
    }

    private readonly Queue<RecentSpawn> recentSpawnPositions = new Queue<RecentSpawn>();

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
        if (!logLifecycle)
            return;

        if (!Application.isPlaying)
            return;

        Debug.LogWarning($"[TrafficSpawner2D] OnDestroy: {name} (id={GetInstanceID()}, scene={gameObject.scene.name}, frame={Time.frameCount})\n{System.Environment.StackTrace}", this);
    }

    private void Update()
    {
        ReclaimDespawnedVehicles();
        ReclaimStuckVehicles(Time.deltaTime);

        // Auto-recuperación: si algo detuvo la coroutine, la re-inicia.
        if (enabled && gameObject.activeInHierarchy && spawnRoutine == null)
            spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnValidate()
    {
        if (burstMaxCount < burstMinCount)
            burstMaxCount = burstMinCount;

        candidatesPerSpawn = Mathf.Clamp(candidatesPerSpawn, 4, 64);
        maxAttemptsPerVehicle = Mathf.Clamp(maxAttemptsPerVehicle, 1, 60);

        if (spawnArea == null)
            spawnArea = GetComponent<BoxCollider2D>();

        recentPositionLifetime = Mathf.Max(0f, recentPositionLifetime);

        stuckMinMoveDelta = Mathf.Max(0f, stuckMinMoveDelta);
        stuckTimeToRecycle = Mathf.Max(0.1f, stuckTimeToRecycle);
    }

    #endregion

    #region Core

    private IEnumerator SpawnLoop()
    {
        // Arranque suave: deja que la escena inicialice.
        yield return null;

        while (enabled)
        {
            CleanupActiveList();

            int deficit = Mathf.Max(0, targetActiveVehicles - activeVehicles.Count);
            if (deficit > 0)
            {
                bool doBurst = (deficit >= 2) && (Random.value < burstChance);
                int spawnCount = doBurst
                    ? Mathf.Min(deficit, Random.Range(burstMinCount, burstMaxCount + 1))
                    : 1;

                for (int i = 0; i < spawnCount; i++)
                {
                    if (TrySpawnOne())
                    {
                        // Pequeña separación dentro de ráfaga para que no se vea artificial.
                        if (doBurst && i < spawnCount - 1)
                            yield return new WaitForSeconds(burstSpacing);
                    }
                    else
                    {
                        // Si no pudo spawnear, no insistir en ráfaga.
                        break;
                    }
                }
            }

            float wait = GetNextSpawnInterval();
            yield return new WaitForSeconds(wait);
        }
    }

    private float GetNextSpawnInterval()
    {
        float jitter = spawnIntervalJitter;
        float min = Mathf.Max(0.01f, baseSpawnInterval - jitter);
        float max = Mathf.Max(min, baseSpawnInterval + jitter);
        return Random.Range(min, max);
    }

    private bool TrySpawnOne()
    {
        if (vehiclePrefabs == null || vehiclePrefabs.Length == 0)
            return false;

        if (spawnArea == null)
            return false;

        for (int attempt = 0; attempt < maxAttemptsPerVehicle; attempt++)
        {
            if (!TryPickSpawnPosition(out Vector2 pos))
                continue;

            int prefabIndex = Random.Range(0, vehiclePrefabs.Length);
            TrafficCar vehicle = GetFromPool(prefabIndex);
            if (vehicle == null)
                return false;

            vehicle.transform.SetPositionAndRotation(pos, Quaternion.identity);
            vehicle.SetDirection(Vector2.up);

            // Asegurar que el script esté habilitado (si el prefab lo tiene apagado, se quedará quieto).
            vehicle.enabled = true;

            if (!vehicle.gameObject.activeSelf)
                vehicle.gameObject.SetActive(true);

            // Asegurar simulación física.
            Rigidbody2D rb = vehicle.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;

                if (fixRigidbodiesOnSpawn)
                {
                    if (rb.bodyType == RigidbodyType2D.Static)
                        rb.bodyType = RigidbodyType2D.Dynamic;

                    // Quitar freezes de posición (mantener freeze rotation si existía)
                    if ((rb.constraints & (RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezePositionY)) != 0)
                    {
                        bool keepFreezeRotation = (rb.constraints & RigidbodyConstraints2D.FreezeRotation) != 0;
                        rb.constraints = keepFreezeRotation ? RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.None;
                    }
                }

                rb.WakeUp();
            }

            // Reset tracking (atascados)
            lastPositionByVehicle[vehicle] = vehicle.transform.position;
            stillTimeByVehicle[vehicle] = 0f;

            activeVehicles.Add(vehicle);
            RememberSpawnPosition(pos);
            return true;
        }

        if (logSpawnFailures)
            Debug.LogWarning($"[TrafficSpawner2D] No se encontró posición válida (minSpawnDistance={minSpawnDistance}, activos={activeVehicles.Count}).", this);

        return false;
    }

    private void RememberSpawnPosition(Vector2 pos)
    {
        if (recentPositionsCapacity <= 0)
            return;

        recentSpawnPositions.Enqueue(new RecentSpawn { pos = pos, time = Time.time });
        TrimRecentPositions();
    }

    private void TrimRecentPositions()
    {
        // Expirar por tiempo (si aplica)
        if (recentPositionLifetime > 0f)
        {
            float cutoff = Time.time - recentPositionLifetime;
            while (recentSpawnPositions.Count > 0 && recentSpawnPositions.Peek().time < cutoff)
                recentSpawnPositions.Dequeue();
        }

        // Limitar por capacidad
        while (recentSpawnPositions.Count > recentPositionsCapacity)
            recentSpawnPositions.Dequeue();
    }

    #endregion

    #region Distribution

    private bool TryPickSpawnPosition(out Vector2 bestPos)
    {
        bestPos = default;

        TrimRecentPositions();

        Rect spawnRect = GetWorldRect(spawnArea);

        float bestScore = float.NegativeInfinity;
        bool found = false;

        for (int i = 0; i < candidatesPerSpawn; i++)
        {
            Vector2 candidate = new Vector2(
                Random.Range(spawnRect.xMin, spawnRect.xMax),
                Random.Range(spawnRect.yMin, spawnRect.yMax)
            );

            if (!IsCandidateValid(candidate, out float nearestDistance))
                continue;

            // Score: queremos cerca de un “spacing” natural, sin clusters ni huecos enormes.
            float score = nearestDistance;

            if (maxGapDistance > 0f)
            {
                // Penaliza si está demasiado lejos de todo.
                if (nearestDistance > maxGapDistance)
                    score -= (nearestDistance - maxGapDistance) * 2f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestPos = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool IsCandidateValid(Vector2 candidate, out float nearestDistance)
    {
        nearestDistance = float.PositiveInfinity;

        float minDist = minSpawnDistance;
        float minDistSqr = minDist * minDist;

        // Comparar con posiciones recientes
        foreach (RecentSpawn r in recentSpawnPositions)
        {
            float dSqr = (candidate - r.pos).sqrMagnitude;
            if (dSqr < minDistSqr)
                return false;

            nearestDistance = Mathf.Min(nearestDistance, Mathf.Sqrt(dSqr));
        }

        // Comparar con vehículos activos
        for (int i = 0; i < activeVehicles.Count; i++)
        {
            TrafficCar v = activeVehicles[i];
            if (v == null || !v.gameObject.activeInHierarchy)
                continue;

            float dSqr = (candidate - (Vector2)v.transform.position).sqrMagnitude;
            if (dSqr < minDistSqr)
                return false;

            nearestDistance = Mathf.Min(nearestDistance, Mathf.Sqrt(dSqr));
        }

        if (nearestDistance == float.PositiveInfinity)
            nearestDistance = maxGapDistance > 0f ? maxGapDistance : minSpawnDistance;

        return true;
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
        if (activeVehicles.Count == 0)
            return;

        if (stuckTimeToRecycle <= 0f)
            return;

        float eps = stuckMinMoveDelta;
        float epsSqr = eps * eps;

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
