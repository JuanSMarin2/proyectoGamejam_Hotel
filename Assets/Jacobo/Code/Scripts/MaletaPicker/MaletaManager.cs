using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaletaManager : MonoBehaviour
{
    [Serializable]
    public struct WinnerTarget
    {
        public int PoolId;
        public Sprite Sprite;

        public WinnerTarget(int poolId, Sprite sprite)
        {
            PoolId = poolId;
            Sprite = sprite;
        }
    }

    [Header("References")]
    [SerializeField] private CintaMovement cintaMovement;
    [SerializeField] private SpriteSelector spriteSelector;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform despawnPoint;

    [Header("Maletas Pool")]
    [SerializeField] private List<Maleta> maletaPool = new List<Maleta>();

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private int maxAliveMaletas = 8;
    [SerializeField] private int prewarmPerPrefab = 3;
    [SerializeField] private float despawnDistance = 0.25f;

    [Header("Gameplay")]
    [SerializeField, Range(1, 3)] private int winners = 1;
    [SerializeField] private bool loseOnWrongPick = true;
    [SerializeField] private float winDelay = 0.5f;

    private readonly List<Maleta> aliveMaletas = new List<Maleta>();
    private readonly Dictionary<int, Queue<Maleta>> pooledByPoolId = new Dictionary<int, Queue<Maleta>>();
    private readonly List<WinnerTarget> winnerTargets = new List<WinnerTarget>();
    private readonly HashSet<int> collectedWinners = new HashSet<int>();
    private readonly Dictionary<int, Sprite> winnerSpriteByPoolId = new Dictionary<int, Sprite>();

    private float spawnTimer;
    private int poolCursor;
    private bool gameEnded;

    public event Action<IReadOnlyList<WinnerTarget>> WinnersAssigned;
    public event Action<Maleta, bool, int, int> MaletaPicked;

    public IReadOnlyList<WinnerTarget> WinnerTargets => winnerTargets;
    public int CollectedWinnersCount => collectedWinners.Count;

    private void Start()
    {
        BuildWinners();
        PrewarmPool();
        spawnTimer = spawnInterval;

        WinnersAssigned?.Invoke(winnerTargets);
    }

    private void Update()
    {
        if (!gameEnded)
            HandleSpawn();

        HandleDespawn();
    }

    private void BuildWinners()
    {
        winnerTargets.Clear();
        collectedWinners.Clear();
        winnerSpriteByPoolId.Clear();

        if (spriteSelector != null)
            spriteSelector.ResetWinnerSpritePool();

        if (maletaPool.Count == 0) return;

        int maxWinners = Mathf.Min(winners, Mathf.Min(3, maletaPool.Count));
        List<int> availableIndices = new List<int>(maletaPool.Count);

        for (int i = 0; i < maletaPool.Count; i++)
            availableIndices.Add(i);

        for (int i = 0; i < maxWinners; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
            int poolIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);

            Sprite winnerSprite = null;
            Maleta prefab = maletaPool[poolIndex];

            if (spriteSelector != null && prefab != null)
            {
                winnerSprite = spriteSelector.TakeWinnerUniqueSprite(prefab.Type);
            }

            winnerSpriteByPoolId[poolIndex] = winnerSprite;
            winnerTargets.Add(new WinnerTarget(poolIndex, winnerSprite));
        }
    }

    private void HandleSpawn()
    {
        if (cintaMovement == null || spawnPoint == null || maletaPool.Count == 0) return;
        if (aliveMaletas.Count >= Mathf.Max(1, maxAliveMaletas)) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnMaleta();
        spawnTimer = Mathf.Max(0.05f, spawnInterval);
    }

    private void SpawnMaleta()
    {
        int spawnPoolIndex = poolCursor;
        poolCursor = (poolCursor + 1) % maletaPool.Count;

        Maleta instance = GetFromPool(spawnPoolIndex);
        if (instance == null) return;

        instance.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        instance.gameObject.SetActive(true);

        bool isWinner = IsWinnerPoolId(spawnPoolIndex);
        AssignSpawnSprite(instance, spawnPoolIndex, isWinner);

        instance.Initialize(
            cintaMovement.Waypoints,
            cintaMovement.MovementSpeed,
            isWinner,
            spawnPoolIndex,
            HandleMaletaPicked
        );

        aliveMaletas.Add(instance);
    }

    private void HandleDespawn()
    {
        if (despawnPoint == null || aliveMaletas.Count == 0) return;

        for (int i = aliveMaletas.Count - 1; i >= 0; i--)
        {
            Maleta maleta = aliveMaletas[i];
            if (maleta == null)
            {
                aliveMaletas.RemoveAt(i);
                continue;
            }

            if (Vector3.Distance(maleta.transform.position, despawnPoint.position) <= despawnDistance)
            {
                aliveMaletas.RemoveAt(i);
                ReturnToPool(maleta);
            }
        }
    }

    private void HandleMaletaPicked(Maleta maleta)
    {
        if (gameEnded || maleta == null) return;

        bool countedAsWinner = false;

        if (maleta.Winner)
        {
            countedAsWinner = collectedWinners.Add(maleta.PoolId);

            if (countedAsWinner && collectedWinners.Count >= winnerTargets.Count)
            {
                gameEnded = true;
                if (winDelay <= 0f)
                {
                    ResultManager.instance.WinMinigame();
                }
                else
                {
                    StartCoroutine(WinAfterDelay());
                }
            }
        }
        else if (loseOnWrongPick)
        {
            gameEnded = true;
            ResultManager.instance.LoseMinigame();
        }

        MaletaPicked?.Invoke(maleta, countedAsWinner, collectedWinners.Count, winnerTargets.Count);
    }

    private bool IsWinnerPoolId(int poolId)
    {
        for (int i = 0; i < winnerTargets.Count; i++)
        {
            if (winnerTargets[i].PoolId == poolId)
                return true;
        }

        return false;
    }

    private IEnumerator WinAfterDelay()
    {
        yield return new WaitForSeconds(winDelay);
        ResultManager.instance.WinMinigame();
    }

    private void PrewarmPool()
    {
        pooledByPoolId.Clear();

        if (maletaPool.Count == 0) return;

        int instancesPerPrefab = Mathf.Max(1, prewarmPerPrefab);
        Vector3 prewarmPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion prewarmRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        for (int i = 0; i < maletaPool.Count; i++)
        {
            pooledByPoolId[i] = new Queue<Maleta>();

            Maleta prefab = maletaPool[i];
            if (prefab == null) continue;

            for (int j = 0; j < instancesPerPrefab; j++)
            {
                Maleta created = Instantiate(prefab, prewarmPosition, prewarmRotation);
                created.gameObject.SetActive(false);
                pooledByPoolId[i].Enqueue(created);
            }
        }
    }

    private Maleta GetFromPool(int poolId)
    {
        if (!pooledByPoolId.TryGetValue(poolId, out Queue<Maleta> queue) || queue == null)
            return CreatePoolItem(poolId);

        if (queue.Count > 0)
            return queue.Dequeue();

        return CreatePoolItem(poolId);
    }

    private Maleta CreatePoolItem(int poolId)
    {
        if (poolId < 0 || poolId >= maletaPool.Count) return null;

        Maleta prefab = maletaPool[poolId];
        if (prefab == null) return null;

        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : Quaternion.identity;

        Maleta created = Instantiate(prefab, spawnPosition, spawnRotation);
        created.gameObject.SetActive(false);
        return created;
    }

    private void ReturnToPool(Maleta maleta)
    {
        if (maleta == null) return;

        maleta.StopMovement();
        maleta.gameObject.SetActive(false);

        int poolId = maleta.PoolId;
        if (!pooledByPoolId.TryGetValue(poolId, out Queue<Maleta> queue) || queue == null)
        {
            queue = new Queue<Maleta>();
            pooledByPoolId[poolId] = queue;
        }

        queue.Enqueue(maleta);
    }

    private void AssignSpawnSprite(Maleta instance, int poolId, bool isWinner)
    {
        if (instance == null || spriteSelector == null) return;

        SpriteRenderer renderer = instance.GetComponentInChildren<SpriteRenderer>();
        if (renderer == null) return;

        Sprite selected = null;

        if (isWinner)
        {
            winnerSpriteByPoolId.TryGetValue(poolId, out selected);
        }
        else
        {
            selected = spriteSelector.GetRandomReusableSprite(instance.Type);
        }

        if (selected == null)
            selected = spriteSelector.GetAnyPreviewSprite();

        if (selected != null)
            renderer.sprite = selected;
    }
}
