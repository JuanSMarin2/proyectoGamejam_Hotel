using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SpawnerVendedor : MonoBehaviour
{
    private enum SpawnStartPoint
    {
        Back,
        Front
    }

    [Header("Prefabs")]
    [SerializeField] private List<Vendedor> vendedorPrefabs = new List<Vendedor>();

    [Header("Route")]
    [FormerlySerializedAs("leftSpawnPoint")]
    [SerializeField] private Transform backSpawnPoint;
    [FormerlySerializedAs("rightSpawnPoint")]
    [SerializeField] private Transform frontSpawnPoint;
    [SerializeField] private bool randomDirection = true;
    [FormerlySerializedAs("spawnLeftToRight")]
    [SerializeField] private bool spawnBackToFront = true;

    [Header("Need Guarantee")]
    [SerializeField] private CharacterNecesidad characterNecesidad;
    [SerializeField] private bool guaranteeMatchingVendorOnNeedStart = true;
    [SerializeField] private bool ignoreMaxAliveForGuaranteedSpawn = true;
    [SerializeField] private bool includeMasajesInRandomVendorNeed = true;

    [Header("Spawn")]
    [SerializeField] private float minSpawnInterval = 0.8f;
    [SerializeField] private float maxSpawnInterval = 1.6f;
    [SerializeField] private int maxAliveVendedores = 6;
    [SerializeField] private float minSpeed = 0.8f;
    [SerializeField] private float maxSpeed = 2.2f;

    [Header("Sorting")]
    [SerializeField] private int initialSortingOrder = 0;
    [SerializeField] private int sortingOrderStep = 1;

    private readonly List<Vendedor> aliveVendedores = new List<Vendedor>();
    private readonly List<Vendedor> candidatePrefabs = new List<Vendedor>();
    private float spawnTimer;
    private int nextSortingOrder;

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
        spawnTimer = GetRandomSpawnInterval();
        nextSortingOrder = initialSortingOrder;
    }

    private void Update()
    {
        CleanupNulls();

        if (aliveVendedores.Count >= Mathf.Max(1, maxAliveVendedores)) return;
        if (backSpawnPoint == null || frontSpawnPoint == null) return;
        if (vendedorPrefabs.Count == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnOne();
        spawnTimer = GetRandomSpawnInterval();
    }

    private void SpawnOne()
    {
        bool backToFront = randomDirection ? Random.value > 0.5f : spawnBackToFront;
        SpawnStartPoint startPoint = backToFront ? SpawnStartPoint.Back : SpawnStartPoint.Front;

        Vector3 spawnPos = backToFront ? backSpawnPoint.position : frontSpawnPoint.position;
        Vector3 direction = backToFront ? Vector3.right : Vector3.left;
        float despawnX = backToFront ? frontSpawnPoint.position.x : backSpawnPoint.position.x;

        Vendedor prefab = PickRandomPrefabForStart(startPoint);
        if (prefab == null) return;

        Vendedor instance = Instantiate(prefab, spawnPos, prefab.transform.rotation);

        instance.SetNecesidad(GetRandomNecesidadVenta());

        float speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));
        instance.Initialize(direction, speed, despawnX);
        instance.SetSortingOrderRecursive(nextSortingOrder);
        nextSortingOrder += Mathf.Max(1, sortingOrderStep);

        aliveVendedores.Add(instance);
    }

    private float GetRandomSpawnInterval()
    {
        float min = Mathf.Max(0.05f, Mathf.Min(minSpawnInterval, maxSpawnInterval));
        float max = Mathf.Max(0.05f, Mathf.Max(minSpawnInterval, maxSpawnInterval));
        return Random.Range(min, max);
    }

    private Vendedor PickRandomPrefabForStart(SpawnStartPoint startPoint)
    {
        candidatePrefabs.Clear();

        for (int i = 0; i < vendedorPrefabs.Count; i++)
        {
            Vendedor prefab = vendedorPrefabs[i];
            if (prefab == null) continue;

            bool valid = prefab.SpawnPreference == Vendedor.SpawnPointPreference.Any ||
                         (startPoint == SpawnStartPoint.Back && prefab.SpawnPreference == Vendedor.SpawnPointPreference.Back) ||
                         (startPoint == SpawnStartPoint.Front && prefab.SpawnPreference == Vendedor.SpawnPointPreference.Front);

            if (valid)
                candidatePrefabs.Add(prefab);
        }

        if (candidatePrefabs.Count == 0)
        {
            Debug.LogWarning($"SpawnerVendedor: no prefab matches start point {startPoint}. Falling back to full list.");

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
        if (backSpawnPoint == null || frontSpawnPoint == null) return;
        if (vendedorPrefabs.Count == 0) return;

        CleanupNulls();

        if (!ignoreMaxAliveForGuaranteedSpawn && aliveVendedores.Count >= Mathf.Max(1, maxAliveVendedores))
            return;

        SpawnOneWithNeed(need);
    }

    private void SpawnOneWithNeed(Necesidad need)
    {
        bool backToFront = randomDirection ? Random.value > 0.5f : spawnBackToFront;
        SpawnStartPoint startPoint = backToFront ? SpawnStartPoint.Back : SpawnStartPoint.Front;

        Vector3 spawnPos = backToFront ? backSpawnPoint.position : frontSpawnPoint.position;
        Vector3 direction = backToFront ? Vector3.right : Vector3.left;
        float despawnX = backToFront ? frontSpawnPoint.position.x : backSpawnPoint.position.x;

        Vendedor prefab = PickRandomPrefabForStart(startPoint);
        if (prefab == null) return;

        Vendedor instance = Instantiate(prefab, spawnPos, prefab.transform.rotation);
        instance.SetNecesidad(need);

        float speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));
        instance.Initialize(direction, speed, despawnX);
        instance.SetSortingOrderRecursive(nextSortingOrder);
        nextSortingOrder += Mathf.Max(1, sortingOrderStep);

        aliveVendedores.Add(instance);
    }

    private Necesidad GetRandomNecesidadVenta()
    {
        if (includeMasajesInRandomVendorNeed)
        {
            int random = Random.Range(0, 4);
            return (Necesidad)random;
        }

        int randomNoMasajes = Random.Range(0, 3);
        return (Necesidad)randomNoMasajes;
    }

    private void CleanupNulls()
    {
        for (int i = aliveVendedores.Count - 1; i >= 0; i--)
        {
            if (aliveVendedores[i] == null)
                aliveVendedores.RemoveAt(i);
        }
    }
}
