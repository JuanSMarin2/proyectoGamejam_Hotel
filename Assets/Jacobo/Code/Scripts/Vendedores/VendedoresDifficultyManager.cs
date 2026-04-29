using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public struct VendedoresDifficultySettings
{
    public int occurrence;
    public float vendorSpeedMultiplier;
    public float stopChanceMultiplier;
    public float stopAttemptIntervalMultiplier;
    public int extraMaxAliveVendedores;
    public float solveTime;
    public float minWaitBetweenNeeds;
    public float maxWaitBetweenNeeds;
}

public class VendedoresDifficultyManager : MonoBehaviour
{
    [Header("Testing")]
    [SerializeField] private bool useManualOccurrence = false;
    [SerializeField, Min(1)] private int currentOccurrence = 1;

    [Header("Per-Occurrence Scaling")]
    [SerializeField] private float vendorSpeedIncreasePerOccurrence = 0.15f;
    [SerializeField] private float stopChanceReductionPerOccurrence = 0.12f;
    [SerializeField] private float stopAttemptIntervalIncreasePerOccurrence = 0.12f;
    [SerializeField] private int maxAliveIncreasePerOccurrence = 2;
    [SerializeField] private float solveTimeReductionPerOccurrence = 1.5f;
    [SerializeField] private float waitBetweenNeedsReductionPerOccurrence = 0.5f;

    [Header("Limits")]
    [SerializeField] private float minSolveTime = 3f;
    [SerializeField] private float minWaitBetweenNeeds = 0.25f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool isResolved;
    private VendedoresDifficultySettings cachedSettings;

    public VendedoresDifficultySettings ResolveDifficulty(float baseSolveTime, float baseMinWaitBetweenNeeds, float baseMaxWaitBetweenNeeds)
    {
        if (isResolved)
            return cachedSettings;

        int occurrence = useManualOccurrence ? currentOccurrence : RegisterAndGetCurrentSceneOccurrence();
        occurrence = Mathf.Max(1, occurrence);

        int step = occurrence - 1;

        float speedMultiplier = 1f + Mathf.Max(0f, vendorSpeedIncreasePerOccurrence) * step;
        float stopChanceMultiplier = Mathf.Clamp01(1f - Mathf.Max(0f, stopChanceReductionPerOccurrence) * step);
        float stopAttemptMultiplier = Mathf.Max(1f, 1f + Mathf.Max(0f, stopAttemptIntervalIncreasePerOccurrence) * step);
        int extraMaxAlive = Mathf.Max(0, maxAliveIncreasePerOccurrence * step);

        float solveTime = Mathf.Max(Mathf.Max(0.01f, minSolveTime), baseSolveTime - Mathf.Max(0f, solveTimeReductionPerOccurrence) * step);

        float minWait = Mathf.Max(Mathf.Max(0.01f, minWaitBetweenNeeds), baseMinWaitBetweenNeeds - Mathf.Max(0f, waitBetweenNeedsReductionPerOccurrence) * step);
        float maxWait = Mathf.Max(minWait, baseMaxWaitBetweenNeeds - Mathf.Max(0f, waitBetweenNeedsReductionPerOccurrence) * step);

        cachedSettings = new VendedoresDifficultySettings
        {
            occurrence = occurrence,
            vendorSpeedMultiplier = speedMultiplier,
            stopChanceMultiplier = stopChanceMultiplier,
            stopAttemptIntervalMultiplier = stopAttemptMultiplier,
            extraMaxAliveVendedores = extraMaxAlive,
            solveTime = solveTime,
            minWaitBetweenNeeds = minWait,
            maxWaitBetweenNeeds = maxWait
        };

        isResolved = true;

        if (debugLogs)
        {
            Debug.Log($"[VendedoresDifficultyManager] occurrence={cachedSettings.occurrence}, speedMult={cachedSettings.vendorSpeedMultiplier:0.00}, stopChanceMult={cachedSettings.stopChanceMultiplier:0.00}, stopAttemptMult={cachedSettings.stopAttemptIntervalMultiplier:0.00}, extraMaxAlive={cachedSettings.extraMaxAliveVendedores}, solveTime={cachedSettings.solveTime:0.00}, minWait={cachedSettings.minWaitBetweenNeeds:0.00}, maxWait={cachedSettings.maxWaitBetweenNeeds:0.00}", this);
        }

        return cachedSettings;
    }

    private int RegisterAndGetCurrentSceneOccurrence()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (RoundData.instance != null)
            return RoundData.instance.RegisterMinigameAppearance(sceneName);

        return 1;
    }
}
