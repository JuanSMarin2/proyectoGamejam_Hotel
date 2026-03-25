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
    [SerializeField] private MaletaDifficultyManager difficultyManager;
    [SerializeField] private Transform spawnPoint;

    [Header("Maletas Pool")]
    [SerializeField] private List<Maleta> maletaPool = new List<Maleta>();

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 1.2f;
    [SerializeField] private int maxAliveMaletas = 8;
    [SerializeField] private int prewarmPerPrefab = 3;

    [Header("Gameplay")]
    [SerializeField, Range(1, 3)] private int winners = 1;
    [SerializeField] private bool loseOnWrongPick = true;
    [SerializeField] private float winDelay = 0.5f;
    [SerializeField] private float successfulPickFadeDuration = 0.2f;

    private readonly List<Maleta> aliveMaletas = new List<Maleta>();
    private readonly Dictionary<int, Queue<Maleta>> pooledByPoolId = new Dictionary<int, Queue<Maleta>>();
    private readonly List<WinnerTarget> winnerTargets = new List<WinnerTarget>();
    private readonly HashSet<int> collectedWinners = new HashSet<int>();
    private readonly Dictionary<int, Sprite> winnerSpriteByPoolId = new Dictionary<int, Sprite>();
    private readonly Dictionary<Maleta.MaletaType, Queue<Sprite>> forcedSimilarByType = new Dictionary<Maleta.MaletaType, Queue<Sprite>>();
    private readonly List<int> activeSpawnPoolIds = new List<int>();
    private readonly HashSet<int> pendingWinnerPoolIds = new HashSet<int>();

    private float spawnTimer;
    private int poolCursor;
    private bool gameEnded;
    private bool firstSpawnEmitted;
    private bool firstSpawnWinnerRuleApplied;
    private MaletaDifficultySettings difficultySettings;
    private int runtimeMaxAliveMaletas;
    private int runtimePrewarmPerPrefab;
    private float runtimeMovementSpeed;

    public event Action<IReadOnlyList<WinnerTarget>> WinnersAssigned;
    public event Action<Maleta, bool, int, int> MaletaPicked;

    public IReadOnlyList<WinnerTarget> WinnerTargets => winnerTargets;
    public int CollectedWinnersCount => collectedWinners.Count;

    private void Start()
    {
        ApplyDifficultySettings();
        BuildWinners();
        ResetPendingWinnerSpawns();
        PrepareForcedSimilarSprites();
        BuildSpawnPoolIds();
        PrewarmPool();
        spawnTimer = spawnInterval;

        WinnersAssigned?.Invoke(winnerTargets);
    }

    private void Update()
    {
        if (!gameEnded)
            HandleSpawn();
    }

    private void BuildWinners()
    {
        winnerTargets.Clear();
        collectedWinners.Clear();
        winnerSpriteByPoolId.Clear();

        if (spriteSelector != null)
            spriteSelector.ResetWinnerSpritePool();

        if (maletaPool.Count == 0) return;

        int targetWinners = Mathf.Max(1, difficultySettings.winnerSlots > 0 ? difficultySettings.winnerSlots : winners);
        int maxWinners = Mathf.Min(targetWinners, Mathf.Min(3, maletaPool.Count));
        List<int> availableIndices = new List<int>(maletaPool.Count);

        for (int i = 0; i < maletaPool.Count; i++)
            availableIndices.Add(i);

        if (difficultySettings.requireDistinctWinnerTypes)
        {
            AssignDistinctTypeWinners(maxWinners, availableIndices);
            maxWinners -= winnerTargets.Count;
        }

        for (int i = 0; i < maxWinners; i++)
        {
            if (availableIndices.Count == 0)
                break;

            int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
            int poolIndex = availableIndices[randomIndex];
            availableIndices.RemoveAt(randomIndex);

            AssignWinnerForPoolIndex(poolIndex);
        }
    }

    private void AssignDistinctTypeWinners(int maxWinners, List<int> availableIndices)
    {
        if (maxWinners <= 0 || availableIndices == null || availableIndices.Count == 0)
            return;

        Dictionary<Maleta.MaletaType, List<int>> byType = new Dictionary<Maleta.MaletaType, List<int>>();

        for (int i = 0; i < availableIndices.Count; i++)
        {
            int poolIndex = availableIndices[i];
            Maleta prefab = poolIndex >= 0 && poolIndex < maletaPool.Count ? maletaPool[poolIndex] : null;
            if (prefab == null)
                continue;

            if (!byType.TryGetValue(prefab.Type, out List<int> list))
            {
                list = new List<int>();
                byType[prefab.Type] = list;
            }

            list.Add(poolIndex);
        }

        List<Maleta.MaletaType> types = new List<Maleta.MaletaType>(byType.Keys);
        Shuffle(types);

        int winnersToTake = Mathf.Min(maxWinners, types.Count);
        for (int i = 0; i < winnersToTake; i++)
        {
            Maleta.MaletaType type = types[i];
            if (!byType.TryGetValue(type, out List<int> candidates) || candidates == null || candidates.Count == 0)
                continue;

            int selectedIndex = UnityEngine.Random.Range(0, candidates.Count);
            int poolIndex = candidates[selectedIndex];

            availableIndices.Remove(poolIndex);
            AssignWinnerForPoolIndex(poolIndex);
        }
    }

    private void AssignWinnerForPoolIndex(int poolIndex)
    {
        Sprite winnerSprite = null;
        Maleta prefab = poolIndex >= 0 && poolIndex < maletaPool.Count ? maletaPool[poolIndex] : null;

        if (spriteSelector != null && prefab != null)
            winnerSprite = spriteSelector.TakeWinnerUniqueSprite(prefab.Type);

        winnerSpriteByPoolId[poolIndex] = winnerSprite;
        winnerTargets.Add(new WinnerTarget(poolIndex, winnerSprite));
    }

    private void HandleSpawn()
    {
        if (cintaMovement == null || spawnPoint == null || maletaPool.Count == 0) return;
        if (aliveMaletas.Count >= Mathf.Max(1, runtimeMaxAliveMaletas)) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        SpawnMaleta();
        spawnTimer = Mathf.Max(0.05f, spawnInterval);
    }

    private void SpawnMaleta()
    {
        int spawnPoolIndex = GetNextSpawnPoolId();
        if (spawnPoolIndex < 0)
            return;

        Maleta instance = GetFromPool(spawnPoolIndex);
        if (instance == null) return;

        instance.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        instance.gameObject.SetActive(true);

        bool isWinner = DecideSpawnWinner(spawnPoolIndex);
        AssignSpawnSprite(instance, spawnPoolIndex, isWinner);

        instance.Initialize(
            cintaMovement.Waypoints,
            runtimeMovementSpeed,
            isWinner,
            spawnPoolIndex,
            HandleMaletaPicked,
            HandleMaletaReachedEnd
        );

        aliveMaletas.Add(instance);
    }

    private void HandleMaletaReachedEnd(Maleta maleta)
    {
        if (maleta == null) return;

        aliveMaletas.Remove(maleta);
        ReturnToPool(maleta);
    }

    private void HandleMaletaPicked(Maleta maleta)
    {
        if (gameEnded || maleta == null) return;

        bool countedAsWinner = false;

        Debug.Log($"[MaletaManager] Pick received: {maleta.name} | winner={maleta.Winner} | poolId={maleta.PoolId}");

        if (maleta.Winner)
        {
            TryPlaySoundIfManagerExists(SoundType.SelloPasaporte);
            maleta.PlaySuccessFadeOut(() => HandleMaletaReachedEnd(maleta), successfulPickFadeDuration);

            countedAsWinner = collectedWinners.Add(maleta.PoolId);
            Debug.Log($"[MaletaManager] Winner counted={countedAsWinner}. Progress: {collectedWinners.Count}/{winnerTargets.Count}");

            if (countedAsWinner && collectedWinners.Count >= winnerTargets.Count)
            {
                gameEnded = true;
                Debug.Log("[MaletaManager] All winner maletas collected. Triggering win.");
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
            TryPlaySoundIfManagerExists(SoundType.VidaPerdida);
            gameEnded = true;
            ResultManager.instance.LoseMinigame();
        }

        MaletaPicked?.Invoke(maleta, countedAsWinner, collectedWinners.Count, winnerTargets.Count);
    }

    public void TryPickMaleta(Maleta maleta)
    {
        if (gameEnded || maleta == null) return;
        maleta.TryPick();
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

    private bool DecideSpawnWinner(int poolId)
    {
        if (!firstSpawnWinnerRuleApplied)
        {
            firstSpawnWinnerRuleApplied = true;
            return false;
        }

        if (!pendingWinnerPoolIds.Contains(poolId))
            return false;

        pendingWinnerPoolIds.Remove(poolId);
        return true;
    }

    private void ResetPendingWinnerSpawns()
    {
        pendingWinnerPoolIds.Clear();

        for (int i = 0; i < winnerTargets.Count; i++)
            pendingWinnerPoolIds.Add(winnerTargets[i].PoolId);
    }

    private IEnumerator WinAfterDelay()
    {
        yield return new WaitForSeconds(winDelay);
        ResultManager.instance.WinMinigame();
    }

    private void ApplyDifficultySettings()
    {
        if (difficultyManager != null)
            difficultySettings = difficultyManager.ResolveDifficulty();
        else
        {
            difficultySettings = new MaletaDifficultySettings
            {
                occurrence = 1,
                winnerSlots = winners,
                requireDistinctWinnerTypes = false,
                winnerCanSpawnFirst = false,
                minSimilarPerWinner = 1,
                movementSpeedMultiplier = 1f,
                extraMaxAlive = 0,
                extraPrewarmPerPrefab = 0,
                extraSpawnPoolEntries = 0
            };
        }

        float runSpeed = 1f;
        if (MinigameManager.instance != null)
            runSpeed = Mathf.Max(0.01f, MinigameManager.instance.Speed);
        else if (RoundData.instance != null)
            runSpeed = Mathf.Max(0.01f, RoundData.instance.GetCurrentMinigameSpeed());

        float levelMultiplier = Mathf.Max(0.01f, difficultySettings.movementSpeedMultiplier);
        runtimeMovementSpeed = Mathf.Max(0.01f, cintaMovement != null ? cintaMovement.MovementSpeed * runSpeed * levelMultiplier : runSpeed * levelMultiplier);

        runtimeMaxAliveMaletas = Mathf.Max(1, maxAliveMaletas + Mathf.Max(0, difficultySettings.extraMaxAlive));
        runtimePrewarmPerPrefab = Mathf.Max(1, prewarmPerPrefab + Mathf.Max(0, difficultySettings.extraPrewarmPerPrefab));
    }

    private void PrepareForcedSimilarSprites()
    {
        forcedSimilarByType.Clear();

        if (spriteSelector == null)
            return;

        int amountPerWinner = Mathf.Max(0, difficultySettings.minSimilarPerWinner);
        if (amountPerWinner <= 0)
            return;

        for (int i = 0; i < winnerTargets.Count; i++)
        {
            WinnerTarget target = winnerTargets[i];
            if (target.PoolId < 0 || target.PoolId >= maletaPool.Count)
                continue;

            Maleta prefab = maletaPool[target.PoolId];
            if (prefab == null || target.Sprite == null)
                continue;

            if (!spriteSelector.TryGetPairedVariant(prefab.Type, target.Sprite, out Sprite paired))
                continue;

            if (paired == null)
                continue;

            if (!forcedSimilarByType.TryGetValue(prefab.Type, out Queue<Sprite> queue) || queue == null)
            {
                queue = new Queue<Sprite>();
                forcedSimilarByType[prefab.Type] = queue;
            }

            for (int c = 0; c < amountPerWinner; c++)
                queue.Enqueue(paired);
        }
    }

    private void BuildSpawnPoolIds()
    {
        activeSpawnPoolIds.Clear();

        for (int i = 0; i < maletaPool.Count; i++)
            activeSpawnPoolIds.Add(i);

        int extraEntries = Mathf.Max(0, difficultySettings.extraSpawnPoolEntries);
        for (int i = 0; i < extraEntries; i++)
        {
            if (maletaPool.Count == 0)
                break;

            activeSpawnPoolIds.Add(UnityEngine.Random.Range(0, maletaPool.Count));
        }

        Shuffle(activeSpawnPoolIds);
        TrySwapFirstSpawnToNonWinner();

        poolCursor = 0;
        firstSpawnEmitted = false;
        firstSpawnWinnerRuleApplied = false;
    }

    private int GetNextSpawnPoolId()
    {
        if (activeSpawnPoolIds.Count == 0)
            return -1;

        int count = activeSpawnPoolIds.Count;

        for (int i = 0; i < count; i++)
        {
            int index = poolCursor;
            poolCursor = (poolCursor + 1) % count;

            int poolId = activeSpawnPoolIds[index];
            bool canBeFirstWinner = difficultySettings.winnerCanSpawnFirst;

            if (!firstSpawnEmitted && !canBeFirstWinner && IsWinnerPoolId(poolId))
                continue;

            firstSpawnEmitted = true;
            return poolId;
        }

        int fallback = activeSpawnPoolIds[poolCursor];
        poolCursor = (poolCursor + 1) % count;
        firstSpawnEmitted = true;
        return fallback;
    }

    private void TrySwapFirstSpawnToNonWinner()
    {
        if (difficultySettings.winnerCanSpawnFirst)
            return;

        if (activeSpawnPoolIds.Count <= 1)
            return;

        if (!IsWinnerPoolId(activeSpawnPoolIds[0]))
            return;

        for (int i = 1; i < activeSpawnPoolIds.Count; i++)
        {
            if (IsWinnerPoolId(activeSpawnPoolIds[i]))
                continue;

            int temp = activeSpawnPoolIds[0];
            activeSpawnPoolIds[0] = activeSpawnPoolIds[i];
            activeSpawnPoolIds[i] = temp;
            return;
        }
    }

    private static void Shuffle<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
        }
    }

    private static void TryPlaySoundIfManagerExists(SoundType soundType)
    {
        if (FindAnyObjectByType<SoundManager>() == null)
            return;

        SoundManager.PlaySound(soundType);
        
    }

    private void PrewarmPool()
    {
        pooledByPoolId.Clear();

        if (maletaPool.Count == 0) return;

        int instancesPerPrefab = Mathf.Max(1, runtimePrewarmPerPrefab);
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
            if (forcedSimilarByType.TryGetValue(instance.Type, out Queue<Sprite> queue) && queue != null && queue.Count > 0)
                selected = queue.Dequeue();

            if (selected == null)
                selected = spriteSelector.GetRandomReusableSprite(instance.Type);
        }

        if (selected == null)
            selected = spriteSelector.GetAnyPreviewSprite();

        if (selected != null)
            renderer.sprite = selected;
    }
}
