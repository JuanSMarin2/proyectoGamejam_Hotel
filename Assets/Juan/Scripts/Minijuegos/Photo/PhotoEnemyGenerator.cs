using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoEnemyGenerator : MonoBehaviour
{
    [Header("Enemy")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform toRight;
    [SerializeField] private Transform toLeft;

    [Header("Areas")]
    [SerializeField] private Transform photoArea;
    [SerializeField] private Vector2 photoAreaSize = new Vector2(6f, 3f);
    [SerializeField] private Vector2 photoSpaceSize = new Vector2(14f, 6f);

    [Header("Blocker")]
    [SerializeField] private Transform blocker;
    [SerializeField] private float blockerMoveSpeed = 3f;

    [Header("Spawn By Speed")]
    [SerializeField] private float minSpawnInterval = 0.7f;
    [SerializeField] private float maxSpawnInterval = 1.4f;

    [Header("Spawn Density")]
    [SerializeField] private float spawnIntervalMultiplier = 0.7f;
    [SerializeField] private int minEnemiesPerSpawn = 1;
    [SerializeField] private int maxEnemiesPerSpawn = 2;

    [Header("Enemy Velocity")]
    [SerializeField] private float minEnemySpeed = 2.5f;
    [SerializeField] private float maxEnemySpeed = 5f;

    [Header("Timing")]
    [SerializeField] private float fallbackRoundTime = 10f;
    [SerializeField] private float safeWindowAtMinSpeed = 1f;
    [SerializeField] private float safeWindowAtMaxSpeed = 0.5f;
    [SerializeField] private float minigameMinSpeed = 1f;
    [SerializeField] private float minigameMaxSpeed = 2.5f;
    [SerializeField] private float minLifeTime = 3f;
    [SerializeField] private float maxLifeTime = 6f;

    [SerializeField] private SunCollider sunCollider;

    private MinigameManager minigameManager;
    private TimeManager timeManager;

    private float speedMultiplier = 1f;
    private float roundDuration = 10f;
    private float roundStartTime;
    private float safeWindowStart;
    private float safeWindowEnd;
    private bool safeWindowInitialized;
    private float safeWindowDuration;
    private readonly List<PhotoEnemyMover> activeEnemies = new List<PhotoEnemyMover>();
    private bool blockerTriggered;
    private int blockerDirection;
    private bool photoTaken;

    private float AreaMinX => GetPhotoAreaCenter().x - (photoAreaSize.x * 0.5f);
    private float AreaMaxX => GetPhotoAreaCenter().x + (photoAreaSize.x * 0.5f);

    private float SpaceMinY => transform.position.y - (photoSpaceSize.y * 0.5f);
    private float SpaceMaxY => transform.position.y + (photoSpaceSize.y * 0.5f);

    private IEnumerator Start()
    {
        int waitFrames = 10;
        while (waitFrames > 0 && (MinigameManager.instance == null || FindObjectOfType<TimeManager>() == null))
        {
            waitFrames--;
            yield return null;
        }

        minigameManager = MinigameManager.instance;
        timeManager = FindObjectOfType<TimeManager>();

        speedMultiplier = minigameManager != null ? Mathf.Max(0.1f, minigameManager.Speed) : 1f;
        roundDuration = timeManager != null ? Mathf.Max(1f, timeManager.StartTime) : fallbackRoundTime;
        roundStartTime = Time.time;
        safeWindowDuration = GetInterpolatedSafeWindowDuration(speedMultiplier, roundDuration);
        safeWindowInitialized = false;

        yield return StartCoroutine(SpawnRoutine());
    }

    private void Update()
    {
        if (!photoTaken && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            TakePhoto();
            return;
        }

        UpdateBlockerBehavior();
        CleanupDestroyedEnemies();
    }

    public void TakePhoto()
    {
        if (photoTaken)
        {
            return;
        }

        photoTaken = true;
        StartCoroutine(TakePhotoRoutine());
    }

    private IEnumerator TakePhotoRoutine()
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.5f);

        if (ResultManager.instance == null)
        {
            Debug.LogWarning("PhotoEnemyGenerator: ResultManager.instance is null.", this);
            Time.timeScale = 1f;
            yield break;
        }

        bool blocked = sunCollider != null && sunCollider.isBlocked;
        if (blocked)
        {
            ResultManager.instance.LoseMinigame();
        }
        else
        {
            ResultManager.instance.WinMinigame();
        }
    }

    private IEnumerator SpawnRoutine()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("PhotoEnemyGenerator: No enemyPrefab assigned.", this);
            yield break;
        }

        if (toRight == null || toLeft == null)
        {
            Debug.LogWarning("PhotoEnemyGenerator: Assign both ToRight and ToLeft transforms.", this);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < roundDuration)
        {
            float interval = GetNextSpawnInterval();
            yield return new WaitForSeconds(interval);
            elapsed += interval;

            if (elapsed >= roundDuration)
            {
                break;
            }

            int enemiesThisWave = Random.Range(
                Mathf.Max(1, minEnemiesPerSpawn),
                Mathf.Max(Mathf.Max(1, minEnemiesPerSpawn), maxEnemiesPerSpawn) + 1
            );

            float spawnTime = GetRoundElapsedTime();
            for (int i = 0; i < enemiesThisWave; i++)
            {
                TrySpawnEnemy(spawnTime);
            }
        }
    }

    private float GetNextSpawnInterval()
    {
        float randomBase = Random.Range(minSpawnInterval, maxSpawnInterval);
        float intervalScale = Mathf.Max(0.05f, spawnIntervalMultiplier);
        return Mathf.Max(0.02f, (randomBase / speedMultiplier) * intervalScale);
    }

    private float GetInterpolatedSafeWindowDuration(float currentSpeed, float totalDuration)
    {
        float clampedMinSpeed = Mathf.Min(minigameMinSpeed, minigameMaxSpeed);
        float clampedMaxSpeed = Mathf.Max(minigameMinSpeed, minigameMaxSpeed);
        float t = Mathf.InverseLerp(clampedMinSpeed, clampedMaxSpeed, currentSpeed);
        float interpolatedWindow = Mathf.Lerp(safeWindowAtMinSpeed, safeWindowAtMaxSpeed, t);
        return Mathf.Clamp(interpolatedWindow, 0.01f, totalDuration);
    }

    private void TrySpawnEnemy(float spawnTime)
    {
        int attempts = 5;
        while (attempts > 0)
        {
            attempts--;

            float enemySpeed = Random.Range(minEnemySpeed, maxEnemySpeed) * speedMultiplier;
            if (enemySpeed <= 0f)
            {
                continue;
            }

            bool fromRightSpawn = Random.value > 0.5f;

            if (CrossesSafeWindow(spawnTime, enemySpeed, fromRightSpawn))
            {
                fromRightSpawn = !fromRightSpawn;
                if (CrossesSafeWindow(spawnTime, enemySpeed, fromRightSpawn))
                {
                    continue;
                }
            }

            if (fromRightSpawn && toRight == null)
            {
                continue;
            }

            if (!fromRightSpawn && toLeft == null)
            {
                continue;
            }

            SpawnEnemy(enemySpeed, fromRightSpawn);
            return;
        }
    }

    private bool CrossesSafeWindow(float spawnTime, float enemySpeed, bool fromRightSpawn)
    {
        if (!safeWindowInitialized)
        {
            return false;
        }

        if (!TryGetPhotoAreaCrossingInterval(spawnTime, enemySpeed, fromRightSpawn, out float enterTime, out float exitTime))
        {
            return false;
        }

        bool overlaps = enterTime < safeWindowEnd && exitTime > safeWindowStart;
        return overlaps;
    }

    private bool TryGetPhotoAreaCrossingInterval(float spawnTime, float enemySpeed, bool fromRightSpawn, out float enterTime, out float exitTime)
    {
        enterTime = 0f;
        exitTime = 0f;

        float spawnX = fromRightSpawn ? toRight.position.x : toLeft.position.x;
        int direction = fromRightSpawn ? -1 : 1;

        if (direction > 0)
        {
            float enterRelative = (AreaMinX - spawnX) / enemySpeed;
            float exitRelative = (AreaMaxX - spawnX) / enemySpeed;

            if (exitRelative < 0f)
            {
                return false;
            }

            enterRelative = Mathf.Max(0f, enterRelative);
            enterTime = spawnTime + enterRelative;
            exitTime = spawnTime + exitRelative;
            return exitTime > enterTime;
        }

        float enterRelativeFromRight = (spawnX - AreaMaxX) / enemySpeed;
        float exitRelativeFromRight = (spawnX - AreaMinX) / enemySpeed;

        if (exitRelativeFromRight < 0f)
        {
            return false;
        }

        enterRelativeFromRight = Mathf.Max(0f, enterRelativeFromRight);
        enterTime = spawnTime + enterRelativeFromRight;
        exitTime = spawnTime + exitRelativeFromRight;
        return exitTime > enterTime;
    }

    private void SpawnEnemy(float enemySpeed, bool fromRightSpawn)
    {
        float randomY = Random.Range(SpaceMinY, SpaceMaxY);
        Transform spawnPoint = fromRightSpawn ? toRight : toLeft;

        Vector3 spawnPosition = new Vector3(spawnPoint.position.x, randomY, spawnPoint.position.z);
        int direction = fromRightSpawn ? -1 : 1;
        float lifeTime = Random.Range(minLifeTime, maxLifeTime);

        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);

        if (!fromRightSpawn)
        {
            FlipSprite(enemy);
        }

        PhotoEnemyMover mover = enemy.GetComponent<PhotoEnemyMover>();
        if (mover == null)
        {
            mover = enemy.AddComponent<PhotoEnemyMover>();
        }

        mover.Initialize(direction, enemySpeed, lifeTime);
        activeEnemies.Add(mover);
    }

    private void FlipSprite(GameObject enemy)
    {
        SpriteRenderer[] renderers = enemy.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].flipX = true;
        }
    }

    private void OnDrawGizmos()
    {
        Vector3 areaCenter = GetPhotoAreaCenter();
        Vector3 spaceCenter = transform.position;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireCube(areaCenter, new Vector3(photoAreaSize.x, photoAreaSize.y, 0.01f));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(spaceCenter, new Vector3(photoSpaceSize.x, photoSpaceSize.y, 0.01f));

        Gizmos.color = Color.yellow;
        if (toRight != null)
        {
            Gizmos.DrawWireSphere(toRight.position, 0.2f);
        }

        if (toLeft != null)
        {
            Gizmos.DrawWireSphere(toLeft.position, 0.2f);
        }

        if (blocker != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(blocker.position, 0.25f);
        }
    }

    private Vector3 GetPhotoAreaCenter()
    {
        return photoArea != null ? photoArea.position : transform.position;
    }

    private void UpdateBlockerBehavior()
    {
        if (blocker == null)
        {
            return;
        }

        if (!blockerTriggered)
        {
            for (int i = 0; i < activeEnemies.Count; i++)
            {
                PhotoEnemyMover enemyMover = activeEnemies[i];
                if (enemyMover == null)
                {
                    continue;
                }

                if (IsEnemyTouchingPhotoArea(enemyMover))
                {
                    blockerTriggered = true;
                    blockerDirection = -enemyMover.MoveDirection;
                    InitializeSafeWindowAfterBlocker();
                    break;
                }
            }

            return;
        }

        blocker.position += Vector3.right * (blockerDirection * blockerMoveSpeed * Time.deltaTime);
    }

    private void InitializeSafeWindowAfterBlocker()
    {
        if (safeWindowInitialized)
        {
            return;
        }

        float currentElapsed = GetRoundElapsedTime();
        float earliestStart = Mathf.Min(roundDuration, currentElapsed + 0.01f);
        float latestStart = Mathf.Max(earliestStart, roundDuration - safeWindowDuration);

        safeWindowStart = Random.Range(earliestStart, latestStart);
        safeWindowEnd = Mathf.Min(roundDuration, safeWindowStart + safeWindowDuration);
        safeWindowInitialized = true;
    }

    private float GetRoundElapsedTime()
    {
        return Mathf.Clamp(Time.time - roundStartTime, 0f, roundDuration);
    }

    private bool IsEnemyTouchingPhotoArea(PhotoEnemyMover enemyMover)
    {
        if (enemyMover == null)
        {
            return false;
        }

        Bounds areaBounds = GetPhotoAreaBounds();

        Collider2D enemyCollider = enemyMover.GetComponentInChildren<Collider2D>();
        if (enemyCollider != null)
        {
            return areaBounds.Intersects(enemyCollider.bounds);
        }

        Renderer enemyRenderer = enemyMover.GetComponentInChildren<Renderer>();
        if (enemyRenderer != null)
        {
            return areaBounds.Intersects(enemyRenderer.bounds);
        }

        Vector3 enemyPosition = enemyMover.transform.position;
        Vector3 center = areaBounds.center;
        Vector3 extents = areaBounds.extents;

        bool insideX = enemyPosition.x >= (center.x - extents.x) && enemyPosition.x <= (center.x + extents.x);
        bool insideY = enemyPosition.y >= (center.y - extents.y) && enemyPosition.y <= (center.y + extents.y);

        return insideX && insideY;
    }

    private Bounds GetPhotoAreaBounds()
    {
        Vector3 center = GetPhotoAreaCenter();
        Vector3 size = new Vector3(photoAreaSize.x, photoAreaSize.y, 0.5f);
        return new Bounds(center, size);
    }

    private void CleanupDestroyedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);
            }
        }
    }
}

public class PhotoEnemyMover : MonoBehaviour
{
    private int moveDirection;
    private float moveSpeed;
    private float lifeTime;
    private float lifeTimer;

    public int MoveDirection => moveDirection;

    public void Initialize(int direction, float speed, float targetLifeTime)
    {
        moveDirection = direction;
        moveSpeed = speed;
        lifeTime = Mathf.Max(0.01f, targetLifeTime);
        lifeTimer = 0f;
    }

    private void Update()
    {
        transform.position += Vector3.right * (moveDirection * moveSpeed * Time.deltaTime);

        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}
