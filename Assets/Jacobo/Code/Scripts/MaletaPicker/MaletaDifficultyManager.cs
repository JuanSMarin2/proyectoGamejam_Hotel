using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct MaletaDifficultySettings
{
    public int occurrence;
    public int winnerSlots;
    public bool requireDistinctWinnerTypes;
    public bool winnerCanSpawnFirst;
    public int minSimilarPerWinner;
    public float movementSpeedMultiplier;
    public int extraMaxAlive;
    public int extraPrewarmPerPrefab;
    public int extraSpawnPoolEntries;
}

public class MaletaDifficultyManager : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField] private bool useManualOccurrence = true;
    [SerializeField, Min(1)] private int currentOccurrence = 1;

    [Header("Rules")]
    [SerializeField] private bool winnerCanSpawnFirst = false;

    [Header("Speed")]
    [SerializeField] private float secondOccurrenceSpeedMultiplier = 1f;
    [SerializeField] private float thirdOccurrenceSpeedMultiplier = 1.25f;

    [Header("Pool Increase (3rd+)")]
    [SerializeField] private int thirdOccurrenceExtraMaxAlive = 2;
    [SerializeField] private int thirdOccurrenceExtraPrewarmPerPrefab = 1;
    [SerializeField] private int thirdOccurrenceExtraSpawnPoolEntries = 3;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public MaletaDifficultySettings ResolveDifficulty()
    {
        int occurrence = useManualOccurrence ? currentOccurrence : RegisterAndGetCurrentSceneOccurrence();
        occurrence = Mathf.Max(1, occurrence);

        MaletaDifficultySettings settings = new MaletaDifficultySettings
        {
            occurrence = occurrence,
            winnerSlots = 1,
            requireDistinctWinnerTypes = false,
            winnerCanSpawnFirst = winnerCanSpawnFirst,
            minSimilarPerWinner = 1,
            movementSpeedMultiplier = 1f,
            extraMaxAlive = 0,
            extraPrewarmPerPrefab = 0,
            extraSpawnPoolEntries = 0
        };

        if (occurrence == 2)
        {
            settings.winnerSlots = 2;
            settings.requireDistinctWinnerTypes = true;
            settings.movementSpeedMultiplier = Mathf.Max(0.01f, secondOccurrenceSpeedMultiplier);
            settings.minSimilarPerWinner = 1;
        }
        else if (occurrence >= 3)
        {
            settings.winnerSlots = 3;
            settings.requireDistinctWinnerTypes = true;
            settings.movementSpeedMultiplier = Mathf.Max(0.01f, thirdOccurrenceSpeedMultiplier);
            settings.minSimilarPerWinner = 2;
            settings.extraMaxAlive = Mathf.Max(0, thirdOccurrenceExtraMaxAlive);
            settings.extraPrewarmPerPrefab = Mathf.Max(0, thirdOccurrenceExtraPrewarmPerPrefab);
            settings.extraSpawnPoolEntries = Mathf.Max(0, thirdOccurrenceExtraSpawnPoolEntries);
        }

        if (debugLogs)
        {
            Debug.Log($"[MaletaDifficultyManager] occurrence={settings.occurrence}, winners={settings.winnerSlots}, distinctTypes={settings.requireDistinctWinnerTypes}, minSimilarPerWinner={settings.minSimilarPerWinner}, speedMult={settings.movementSpeedMultiplier:0.00}, extraMaxAlive={settings.extraMaxAlive}, extraPrewarm={settings.extraPrewarmPerPrefab}, extraPoolEntries={settings.extraSpawnPoolEntries}", this);
        }

        return settings;
    }

    private int RegisterAndGetCurrentSceneOccurrence()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (RoundData.instance != null)
            return RoundData.instance.RegisterMinigameAppearance(sceneName);

        return 1;
    }
}
