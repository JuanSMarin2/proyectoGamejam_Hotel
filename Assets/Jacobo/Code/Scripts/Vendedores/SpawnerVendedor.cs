using System.Collections.Generic;
using UnityEngine;

public class SpawnerVendedor : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private List<Vendedor> vendedorPrefabs = new List<Vendedor>();

    [Header("Route")]
    [SerializeField] private Transform leftSpawnPoint;
    [SerializeField] private Transform rightSpawnPoint;
    [SerializeField] private bool randomDirection = true;
    [SerializeField] private bool spawnLeftToRight = true;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private int maxAliveVendedores = 6;
    [SerializeField] private float minSpeed = 0.8f;
    [SerializeField] private float maxSpeed = 2.2f;

    private readonly List<Vendedor> aliveVendedores = new List<Vendedor>();
    private float spawnTimer;

    private void Start()
    {
        spawnTimer = spawnInterval;
    }

    private void Update()
    {
        CleanupNulls();

        if (aliveVendedores.Count >= Mathf.Max(1, maxAliveVendedores)) return;
        if (leftSpawnPoint == null || rightSpawnPoint == null) return;
        if (vendedorPrefabs.Count == 0) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnOne();
        spawnTimer = Mathf.Max(0.05f, spawnInterval);
    }

    private void SpawnOne()
    {
        bool leftToRight = randomDirection ? Random.value > 0.5f : spawnLeftToRight;

        Vector3 spawnPos = leftToRight ? leftSpawnPoint.position : rightSpawnPoint.position;
        Vector3 direction = leftToRight ? Vector3.right : Vector3.left;
        float despawnX = leftToRight ? rightSpawnPoint.position.x : leftSpawnPoint.position.x;

        int randomPrefab = Random.Range(0, vendedorPrefabs.Count);
        Vendedor prefab = vendedorPrefabs[randomPrefab];
        if (prefab == null) return;

        Vendedor instance = Instantiate(prefab, spawnPos, prefab.transform.rotation);

        float speed = Random.Range(Mathf.Min(minSpeed, maxSpeed), Mathf.Max(minSpeed, maxSpeed));
        instance.Initialize(direction, speed, despawnX);

        aliveVendedores.Add(instance);
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
