using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EnemyGenerator : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Vector2 spawnRangeSize = new Vector2(10f, 10f);
    [SerializeField] private float minDistanceBetweenEnemies = 1.5f;
    [SerializeField] private int maxAttemptsPerEnemy = 30;
    [SerializeField] private bool generateOnStart = true;

    [Header("Enemy Count By Speed")]
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 2.5f;
    [SerializeField] private int enemiesAtMinSpeed = 30;
    [SerializeField] private int enemiesAtMaxSpeed = 15;

    [Header("Sprite Variants")]
    [SerializeField] private bool underWater = true;
    [SerializeField] private List<Sprite> waterList = new List<Sprite>();
    [SerializeField] private List<Sprite> groundList = new List<Sprite>();

    private readonly List<Vector3> generatedPositions = new List<Vector3>();

    private IEnumerator Start()
    {
        if (!generateOnStart)
        {
            yield break;
        }

        int waitFrames = 5;
        while (MinigameManager.instance == null && waitFrames > 0)
        {
            waitFrames--;
            yield return null;
        }

        if (MinigameManager.instance != null)
        {
            yield return null;
        }

        GenerateEnemies();
    }

    [ContextMenu("Generate Enemies")]
    public void GenerateEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning("EnemyGenerator: No enemy prefab assigned.", this);
            return;
        }

        generatedPositions.Clear();

        int enemyCount = GetEnemyCountForCurrentSpeed();

        for (int enemyIndex = 0; enemyIndex < enemyCount; enemyIndex++)
        {
            bool positionFound = TryGetValidPosition(out Vector3 spawnPosition);

            if (!positionFound)
            {
                Debug.LogWarning($"EnemyGenerator: Could not find valid position for enemy {enemyIndex + 1}/{enemyCount}. Increase range or lower minimum distance.", this);
                continue;
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity, transform);
            ApplyRandomSprite(enemy);
            generatedPositions.Add(spawnPosition);
        }
    }

    private void ApplyRandomSprite(GameObject enemy)
    {
        List<Sprite> selectedList = underWater ? waterList : groundList;
        if (selectedList == null || selectedList.Count == 0) return;

        SpriteRenderer spriteRenderer = enemy.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) return;

        int randomIndex = Random.Range(0, selectedList.Count);
        spriteRenderer.sprite = selectedList[randomIndex];
    }

    private int GetEnemyCountForCurrentSpeed()
    {
        float currentSpeed = minSpeed;
        if (MinigameManager.instance != null)
        {
            currentSpeed = MinigameManager.instance.Speed;
        }

        float clampedSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
        float t = Mathf.InverseLerp(minSpeed, maxSpeed, clampedSpeed);
        float countValue = Mathf.Lerp(enemiesAtMinSpeed, enemiesAtMaxSpeed, t);

        return Mathf.Max(1, Mathf.RoundToInt(countValue));
    }

    private bool TryGetValidPosition(out Vector3 validPosition)
    {
        for (int attempt = 0; attempt < maxAttemptsPerEnemy; attempt++)
        {
            float halfWidth = spawnRangeSize.x * 0.5f;
            float halfHeight = spawnRangeSize.y * 0.5f;
            Vector2 randomOffset = new Vector2(
                Random.Range(-halfWidth, halfWidth),
                Random.Range(-halfHeight, halfHeight)
            );
            Vector3 candidatePosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            if (IsFarEnoughFromOthers(candidatePosition))
            {
                validPosition = candidatePosition;
                return true;
            }
        }

        validPosition = transform.position;
        return false;
    }

    private bool IsFarEnoughFromOthers(Vector3 candidatePosition)
    {
        for (int i = 0; i < generatedPositions.Count; i++)
        {
            if (Vector3.Distance(candidatePosition, generatedPositions[i]) < minDistanceBetweenEnemies)
            {
                return false;
            }
        }

        return true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(spawnRangeSize.x, spawnRangeSize.y, 0.01f));

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, minDistanceBetweenEnemies);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Handles.color = Color.cyan;
        Vector3 currentSize = new Vector3(spawnRangeSize.x, spawnRangeSize.y, 1f);
        Vector3 updatedSize = Handles.ScaleHandle(
            currentSize,
            transform.position,
            Quaternion.identity,
            HandleUtility.GetHandleSize(transform.position)
        );
        if (updatedSize != currentSize)
        {
            spawnRangeSize = new Vector2(Mathf.Abs(updatedSize.x), Mathf.Abs(updatedSize.y));
        }

        Handles.color = Color.yellow;
        float updatedMinDistance = Handles.RadiusHandle(Quaternion.identity, transform.position, minDistanceBetweenEnemies);
        if (updatedMinDistance != minDistanceBetweenEnemies)
        {
            minDistanceBetweenEnemies = Mathf.Max(0f, updatedMinDistance);
        }
    }
#endif
}