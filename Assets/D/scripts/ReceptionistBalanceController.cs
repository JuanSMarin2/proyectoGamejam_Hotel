using UnityEngine;
using UnityEngine.UI;

public class ReceptionistBalanceController : MonoBehaviour
{
    [Header("Balance State")]
    [SerializeField, Range(-1f, 1f)] private float balanceValue = 0f;
    [SerializeField] private float velocity = 0f;

    [Header("Forces")]
    [SerializeField] private float gravityForce = -2.4f;
    [SerializeField] private float inputForce = 0.95f;
    [SerializeField, Range(0.8f, 0.999f)] private float damping = 0.965f;

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

        ApplyGravity();
        velocity *= GetEffectiveDamping();
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

    private void ApplyGravity()
    {
        velocity += GetEffectiveGravityForce() * Time.deltaTime;
    }

    private void ApplyInput()
    {
        velocity += GetEffectiveInputForce();
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
        float t = Mathf.Clamp01(DifficultySpeed - 1f);
        return inputForce / Mathf.Lerp(1f, 1.35f, t);
    }

    private float GetEffectiveGravityForce()
    {
        return (gravityForce * DifficultySpeed)+0.5f;
    }

    private float GetEffectiveDamping()
    {
        float extraLoss = Mathf.Clamp01((DifficultySpeed - 1f) * 0.15f);
        return Mathf.Clamp(damping - extraLoss, 0.8f, 0.999f);
    }
}