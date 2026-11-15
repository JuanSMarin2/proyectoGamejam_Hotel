using System.Collections.Generic;
using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class SpawnerVendedor : MonoBehaviour
{
    /// <summary>
    /// Punto de spawn individual. Cada punto tiene su PROPIO order in layer:
    /// la capa de renderizado depende del punto especifico, no de la lista.
    /// </summary>
    [Serializable]
    public class SpawnPointData
    {
        public Transform point;
        [Tooltip("Order in layer de los vendedores que salen de este punto")]
        public int sortingOrder;
    }

    [Header("Prefabs")]
    [SerializeField] private List<Vendedor> vendedorPrefabs = new List<Vendedor>();

    [Header("Spawn Points")]
    [SerializeField] private List<SpawnPointData> leftSpawnPoints = new List<SpawnPointData>();
    [SerializeField] private List<SpawnPointData> rightSpawnPoints = new List<SpawnPointData>();
    [SerializeField] private bool randomDirection = true;
    [Tooltip("Solo aplica si randomDirection = false")]
    [SerializeField] private bool spawnFromLeft = true;

    [Header("Need Guarantee")]
    [SerializeField] private CharacterNecesidad characterNecesidad;
    [SerializeField] private VendedoresDifficultyManager difficultyManager;
    [SerializeField] private bool guaranteeMatchingVendorOnNeedStart = true;
    [SerializeField] private bool ignoreMaxAliveForGuaranteedSpawn = true;
    [SerializeField] private float guaranteedNeedSpeedMultiplier = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Spawn")]
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float maxSpawnInterval = 1.6f;
    [SerializeField] private int maxAliveVendedores = 6;
    [SerializeField] private float minSpeed = 0.8f;
    [SerializeField] private float maxSpeed = 2.2f;

    [Header("Sorting")]
    [Tooltip("Offset de order in layer del vendedor garantizado respecto a SU punto de spawn (delante de sus compañeros, no de todos)")]
    [SerializeField] private int guaranteedNeedSortingOffset = 5;

    private readonly List<Vendedor> aliveVendedores = new List<Vendedor>();
    private readonly List<Vendedor> candidatePrefabs = new List<Vendedor>();

    // Bolsas barajadas: indices de cada lista que aun no han salido.
    // Se agotan por completo antes de permitir repeticiones.
    private readonly List<int> leftBag = new List<int>();
    private readonly List<int> rightBag = new List<int>();

    private float spawnTimer;
    private int runtimeMaxAliveVendedores;
    private float runtimeSpeedMultiplier = 1f;
    private float runtimeStopChanceMultiplier = 1f;
    private float runtimeStopAttemptIntervalMultiplier = 1f;

    public float BaseMinSpeed => minSpeed;
    public float BaseMaxSpeed => maxSpeed;
    public int BaseMaxAliveVendedores => maxAliveVendedores;

    private void OnEnable()
    {
        if (characterNecesidad != null)
            characterNecesidad.NeedStarted += HandleNeedStarted;
    }

    private void OnDisable()
    {
        if (characterNecesidad != null)
            characterNecesidad.NeedStarted -= HandleNeedStarted;
    }

    private void Start()
    {
        ResolveDifficulty();
        spawnTimer = GetRandomSpawnInterval();
    }

    private void Update()
    {
        CleanupNulls();

        if (aliveVendedores.Count >= Mathf.Max(1, runtimeMaxAliveVendedores)) return;
        if (!HasAnyValidPoint(leftSpawnPoints) || !HasAnyValidPoint(rightSpawnPoints)) return;
        if (vendedorPrefabs.Count == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnOne();
        spawnTimer = GetRandomSpawnInterval();
    }

    private void SpawnOne()
    {
        bool fromLeft = randomDirection ? Random.value > 0.5f : spawnFromLeft;

        SpawnPointData spawnPoint = DrawSpawnPoint(fromLeft);
        if (spawnPoint == null) return;

        Vector3 spawnPos = spawnPoint.point.position;
        Vector3 direction = fromLeft ? Vector3.right : Vector3.left;
        float despawnX = GetOppositeDespawnX(fromLeft);

        Vendedor prefab = PickRandomPrefabForStart(fromLeft);
        if (prefab == null) return;

        Vendedor instance = Instantiate(prefab, spawnPos, prefab.transform.rotation);

        Necesidad spawnedNeed = GetRandomNecesidadVenta();
        instance.SetNecesidad(spawnedNeed);

        if (debugLogs)
            Debug.Log($"[SpawnerVendedor] SpawnOne => prefab={prefab.name}, punto={spawnPoint.point.name}, necesidad={spawnedNeed}", this);

        float speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));
        speed *= Mathf.Max(0.01f, runtimeSpeedMultiplier);
        instance.Initialize(direction, speed, despawnX,
            fromLeft ? Vendedor.SpawnPointPreference.Back : Vendedor.SpawnPointPreference.Front);
        instance.ApplyDifficultyStopTuning(runtimeStopChanceMultiplier, runtimeStopAttemptIntervalMultiplier);

        // El order in layer viene del PUNTO ESPECIFICO, no de la lista.
        // El factor de profundidad (0-1) se aplica DESPUES de Initialize;
        // cada Vendedor interpola su propia escala min/max segun ese factor.
        instance.SetSortingOrderRecursive(spawnPoint.sortingOrder);
        instance.SetSpawnScale(ComputeDepthFactor(spawnPoint));

        aliveVendedores.Add(instance);
    }

    /// <summary>
    /// Saca un punto de la bolsa barajada del lado indicado.
    /// La bolsa se agota por completo (sin repeticiones) antes de rellenarse.
    /// </summary>
    private SpawnPointData DrawSpawnPoint(bool fromLeft)
    {
        List<SpawnPointData> points = fromLeft ? leftSpawnPoints : rightSpawnPoints;
        List<int> bag = fromLeft ? leftBag : rightBag;

        // Limpiar indices invalidos (lista editada o puntos destruidos en runtime)
        for (int i = bag.Count - 1; i >= 0; i--)
        {
            int index = bag[i];
            if (index < 0 || index >= points.Count || !IsValidPoint(points[index]))
                bag.RemoveAt(i);
        }

        // Bolsa agotada: rellenar con todos los puntos validos y barajar
        if (bag.Count == 0)
        {
            for (int i = 0; i < points.Count; i++)
            {
                if (IsValidPoint(points[i]))
                    bag.Add(i);
            }

            Shuffle(bag);
        }

        if (bag.Count == 0) return null;

        int lastIndex = bag.Count - 1;
        int pointIndex = bag[lastIndex];
        bag.RemoveAt(lastIndex);

        return points[pointIndex];
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private bool IsValidPoint(SpawnPointData data)
    {
        return data != null && data.point != null;
    }

    private bool HasAnyValidPoint(List<SpawnPointData> points)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (IsValidPoint(points[i]))
                return true;
        }
        return false;
    }

    /// <summary>
    /// X de despawn: el punto mas lejano del lado opuesto, para garantizar
    /// que el vendedor cruza toda la pantalla sin importar de que punto salio.
    /// </summary>
    private float GetOppositeDespawnX(bool fromLeft)
    {
        List<SpawnPointData> opposite = fromLeft ? rightSpawnPoints : leftSpawnPoints;
        float best = fromLeft ? float.NegativeInfinity : float.PositiveInfinity;
        bool found = false;

        for (int i = 0; i < opposite.Count; i++)
        {
            if (!IsValidPoint(opposite[i])) continue;

            float x = opposite[i].point.position.x;
            best = fromLeft ? Mathf.Max(best, x) : Mathf.Min(best, x);
            found = true;
        }

        return found ? best : transform.position.x;
    }

    private float GetRandomSpawnInterval()
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minSpawnInterval, maxSpawnInterval));
        float max = Mathf.Max(0.05f, Mathf.Max(minSpawnInterval, maxSpawnInterval));
        return Random.Range(min, max);
    }

    private Vendedor PickRandomPrefabForStart(bool fromLeft)
    {
        candidatePrefabs.Clear();

        for (int i = 0; i < vendedorPrefabs.Count; i++)
        {
            Vendedor prefab = vendedorPrefabs[i];
            if (prefab == null) continue;

            // Back/Front son nombres heredados del Vendedor: Back = sale de la izquierda, Front = de la derecha
            bool valid = prefab.SpawnPreference == Vendedor.SpawnPointPreference.Any ||
                         (fromLeft && prefab.SpawnPreference == Vendedor.SpawnPointPreference.Back) ||
                         (!fromLeft && prefab.SpawnPreference == Vendedor.SpawnPointPreference.Front);

            if (valid)
                candidatePrefabs.Add(prefab);
        }

        if (candidatePrefabs.Count == 0)
        {
            Debug.LogWarning($"SpawnerVendedor: no prefab matches side {(fromLeft ? "Left" : "Right")}. Falling back to full list.");

            for (int i = 0; i < vendedorPrefabs.Count; i++)
            {
                if (vendedorPrefabs[i] != null)
                    candidatePrefabs.Add(vendedorPrefabs[i]);
            }
        }

        if (candidatePrefabs.Count == 0) return null;

        int randomPrefab = Random.Range(0, candidatePrefabs.Count);
        return candidatePrefabs[randomPrefab];
    }

    private void HandleNeedStarted(Necesidad need)
    {
        if (!guaranteeMatchingVendorOnNeedStart) return;
        if (!HasAnyValidPoint(leftSpawnPoints) || !HasAnyValidPoint(rightSpawnPoints)) return;
        if (vendedorPrefabs.Count == 0) return;

        CleanupNulls();

        if (!ignoreMaxAliveForGuaranteedSpawn && aliveVendedores.Count >= Mathf.Max(1, runtimeMaxAliveVendedores))
            return;

        SpawnOneWithNeed(need);
    }

    private void SpawnOneWithNeed(Necesidad need)
    {
        bool fromLeft = randomDirection ? Random.value > 0.5f : spawnFromLeft;

        SpawnPointData spawnPoint = DrawSpawnPoint(fromLeft);
        if (spawnPoint == null) return;

        Vector3 spawnPos = spawnPoint.point.position;
        Vector3 direction = fromLeft ? Vector3.right : Vector3.left;
        float despawnX = GetOppositeDespawnX(fromLeft);

        Vendedor prefab = PickRandomPrefabForStart(fromLeft);
        if (prefab == null) return;

        Vendedor instance = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        instance.SetNecesidad(need);

        if (debugLogs)
            Debug.Log($"[SpawnerVendedor] SpawnOneWithNeed => prefab={prefab.name}, punto={spawnPoint.point.name}, necesidad={need}", this);

        float speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));
        speed *= Mathf.Max(0.01f, runtimeSpeedMultiplier);
        speed *= Mathf.Max(1f, guaranteedNeedSpeedMultiplier);
        instance.Initialize(direction, speed, despawnX,
            fromLeft ? Vendedor.SpawnPointPreference.Back : Vendedor.SpawnPointPreference.Front);
        instance.ApplyDifficultyStopTuning(runtimeStopChanceMultiplier, runtimeStopAttemptIntervalMultiplier);

        // Mismo tamaño que sus compañeros del punto (mismo factor de profundidad),
        // pero +offset de order in layer: delante de sus compañeros, no de todos
        instance.SetSortingOrderRecursive(spawnPoint.sortingOrder + guaranteedNeedSortingOffset);
        instance.SetSpawnScale(ComputeDepthFactor(spawnPoint));

        aliveVendedores.Add(instance);
    }

    /// <summary>
    /// Factor de profundidad del punto segun su order in layer:
    /// 0 = order MAS BAJO de todos los puntos, 1 = el MAS ALTO,
    /// interpolacion lineal para los intermedios. El Vendedor usa este
    /// factor para interpolar entre su propia minScale y maxScale.
    /// Si todos los puntos tienen el mismo order, el factor es 0.5.
    /// </summary>
    private float ComputeDepthFactor(SpawnPointData spawnPoint)
    {
        int minOrder;
        int maxOrder;
        GetGlobalOrderRange(out minOrder, out maxOrder);

        int range = maxOrder - minOrder;
        if (range <= 0)
            return 0.5f;

        return Mathf.InverseLerp(minOrder, maxOrder, spawnPoint.sortingOrder);
    }

    /// <summary>Order in layer minimo y maximo entre TODOS los puntos validos (ambos lados).</summary>
    private void GetGlobalOrderRange(out int minOrder, out int maxOrder)
    {
        minOrder = int.MaxValue;
        maxOrder = int.MinValue;

        AccumulateOrderRange(leftSpawnPoints, ref minOrder, ref maxOrder);
        AccumulateOrderRange(rightSpawnPoints, ref minOrder, ref maxOrder);

        if (minOrder == int.MaxValue)
        {
            minOrder = 0;
            maxOrder = 0;
        }
    }

    private void AccumulateOrderRange(List<SpawnPointData> points, ref int minOrder, ref int maxOrder)
    {
        for (int i = 0; i < points.Count; i++)
        {
            if (!IsValidPoint(points[i])) continue;

            minOrder = Mathf.Min(minOrder, points[i].sortingOrder);
            maxOrder = Mathf.Max(maxOrder, points[i].sortingOrder);
        }
    }

    private Necesidad GetRandomNecesidadVenta()
    {
        Array values = Enum.GetValues(typeof(Necesidad));
        if (values == null || values.Length == 0)
            return default;

        int random = Random.Range(0, values.Length);
        return (Necesidad)values.GetValue(random);
    }

    private void CleanupNulls()
    {
        for (int i = aliveVendedores.Count - 1; i >= 0; i--)
        {
            if (aliveVendedores[i] == null)
                aliveVendedores.RemoveAt(i);
        }
    }

    private void ResolveDifficulty()
    {
        runtimeMaxAliveVendedores = Mathf.Max(1, maxAliveVendedores);
        runtimeSpeedMultiplier = 1f;
        runtimeStopChanceMultiplier = 1f;
        runtimeStopAttemptIntervalMultiplier = 1f;

        if (difficultyManager == null)
            return;

        float baseSolve = characterNecesidad != null ? characterNecesidad.BaseSolveTime : 6f;
        float baseMinWait = characterNecesidad != null ? characterNecesidad.BaseMinWaitBetweenNeeds : 2f;
        float baseMaxWait = characterNecesidad != null ? characterNecesidad.BaseMaxWaitBetweenNeeds : 4f;

        VendedoresDifficultySettings settings = difficultyManager.ResolveDifficulty(baseSolve, baseMinWait, baseMaxWait);

        runtimeMaxAliveVendedores = Mathf.Max(1, maxAliveVendedores + Mathf.Max(0, settings.extraMaxAliveVendedores));
        runtimeSpeedMultiplier = Mathf.Max(0.01f, settings.vendorSpeedMultiplier);
        runtimeStopChanceMultiplier = Mathf.Clamp01(settings.stopChanceMultiplier);
        runtimeStopAttemptIntervalMultiplier = Mathf.Max(1f, settings.stopAttemptIntervalMultiplier);
    }
}