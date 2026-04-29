using UnityEngine;
using UnityEngine.UI;

public class ReceptionistBalanceController : MonoBehaviour
{
    [Header("Balance State")]
    [SerializeField, Range(-1f, 1f)] private float balanceValue = 0f;
    [SerializeField] private float velocity = 0f;

    [Header("Forces")]
    [SerializeField] private float inputForceAtMinSpeed = 1f;
    [SerializeField] private float inputForceAtMaxSpeed = 5f;
    [SerializeField] private float gravityAtMinSpeed = 1f;
    [SerializeField] private float gravityAtMaxSpeed = 3f;
    [SerializeField] private float velocityReturnRate = 6.5f;
    [SerializeField] private float maxRiseSpeed = 0.8f;
    [SerializeField] private float maxFallSpeed = 1.1f;

    [Header("UI")]
    [SerializeField] private Slider balanceSlider;

    [Header("Lose")]
    [SerializeField] private int loseDirectorIndexLowerLimit = 0;
    [SerializeField] private int loseDirectorIndexUpperLimit = 1;

    private bool hasLost = false;

    private float DifficultySpeed => MinigameManager.instance != null ? Mathf.Max(0.1f, MinigameManager.instance.Speed) : 1f;

    private void Awake()
    {
        if (balanceSlider != null)
        {
            balanceSlider.minValue = -1f;
            balanceSlider.maxValue = 1f;
            balanceSlider.wholeNumbers = false;
        }

        UpdateUI();
    }

    private void Update()
    {
        if (hasLost)
        {
            return;
        }

        HandleInput();
        UpdateBalance();
        CheckLoseCondition();
        UpdateUI();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.anyKeyDown)
        {
            ApplyInput();
        }
    }

    private void UpdateBalance()
    {
        float deltaTime = Time.deltaTime;

        ApplyGravity(deltaTime);
        velocity = Mathf.Clamp(velocity, -GetEffectiveMaxFallSpeed(), GetEffectiveMaxRiseSpeed());
        balanceValue += velocity * deltaTime;

        if (balanceValue > 1f)
        {
            balanceValue = 1f;
        }
        else if (balanceValue < -1f)
        {
            balanceValue = -1f;
        }
    }

    private void ApplyGravity(float deltaTime)
    {
        float targetFallVelocity = GetEffectiveGravityForce();
        float returnStep = GetEffectiveVelocityReturnRate() * deltaTime;
        velocity = Mathf.MoveTowards(velocity, targetFallVelocity, returnStep);
    }

    private void ApplyInput()
    {
        velocity += GetEffectiveInputForce();
        velocity = Mathf.Clamp(velocity, -GetEffectiveMaxFallSpeed(), GetEffectiveMaxRiseSpeed());
    }

    private void CheckLoseCondition()
    {
        bool reachedUpperLimit = balanceValue >= 1f;
        bool reachedLowerLimit = balanceValue <= -1f;

        if (reachedUpperLimit || reachedLowerLimit)
        {
            hasLost = true;
            balanceValue = Mathf.Clamp(balanceValue, -1f, 1f);

            if (ResultManager.instance != null)
            {
                int directorIndex = reachedUpperLimit ? loseDirectorIndexUpperLimit : loseDirectorIndexLowerLimit;
                ResultManager.instance.LoseMinigame(directorIndex);
            }
        }
    }

    private void UpdateUI()
    {
        if (balanceSlider != null)
        {
            balanceSlider.SetValueWithoutNotify(balanceValue);
        }
    }

    private float GetEffectiveInputForce()
    {
        float t = GetDifficultyT();
        return Mathf.Lerp(inputForceAtMinSpeed, inputForceAtMaxSpeed, t);
    }

    private float GetEffectiveGravityForce()
    {
        float t = GetDifficultyT();
        return -Mathf.Lerp(gravityAtMinSpeed, gravityAtMaxSpeed, t);
    }

    private float GetEffectiveVelocityReturnRate()
    {
        float t = GetDifficultyT();
        return Mathf.Lerp(velocityReturnRate, velocityReturnRate * 1.35f, t);
    }

    private float GetEffectiveMaxRiseSpeed()
    {
        float t = GetDifficultyT();
        return Mathf.Lerp(maxRiseSpeed, maxRiseSpeed * 1.35f, t);
    }

    private float GetEffectiveMaxFallSpeed()
    {
        float t = GetDifficultyT();
        return Mathf.Lerp(maxFallSpeed, maxFallSpeed * 1.45f, t);
    }

    private float GetDifficultyT()
    {
        return Mathf.InverseLerp(1f, 3f, DifficultySpeed);
    }
}