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

    [Header("Difficulty Ramp")]
    [Tooltip("Cuanto aumenta la dificultad al FINAL del nivel. 0.5 = +50% de fuerzas, 1 = +100%, 1.5 = +150%...")]
    [SerializeField] private float difficultyRampStrength = 1f;
    [Tooltip("Curvatura de la rampa. 1 = lineal, 2 = suave al inicio y agresiva al final, 3 = aun mas tardia")]
    [SerializeField] private float difficultyRampExponent = 2f;
    [SerializeField] private float fallbackRoundDuration = 10f;
    [Tooltip("Velocidad minima absoluta: el juego SIEMPRE empieza aqui, antes del primer input")]
    [SerializeField] private float lowerSpeedLimit = 1f;
    [Tooltip("Velocidad MAXIMA real que puede entregar MinigameManager.Speed. Con este valor la dificultad base llega al 100%")]
    [SerializeField] private float upperSpeedLimit = 2.5f;

    [Header("UI")]
    [SerializeField] private Slider balanceSlider;

    [Header("Lose")]
    [SerializeField] private int loseDirectorIndexLowerLimit = 0;
    [SerializeField] private int loseDirectorIndexUpperLimit = 1;

    private bool hasLost = false;

    // La rampa de dificultad arranca con el primer input (el primer PlayCampana)
    private bool difficultyStarted;
    private float difficultyStartTime;
    private float roundDuration;

    private float BaseDifficultySpeed => MinigameManager.instance != null ? Mathf.Max(0.1f, MinigameManager.instance.Speed) : 1f;

    /// <summary>
    /// Progreso de la rampa (0 a 1) con curva exponencial: sube suave al
    /// inicio del nivel y cada vez mas rapido hacia el final.
    /// </summary>
    private float GetRampProgress()
    {
        if (!difficultyStarted)
        {
            return 0f;
        }

        float linear = Mathf.Clamp01((Time.time - difficultyStartTime) / Mathf.Max(0.01f, roundDuration));
        return Mathf.Pow(linear, Mathf.Max(0.01f, difficultyRampExponent));
    }

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

    private void Start()
    {
        TimeManager timeManager = FindObjectOfType<TimeManager>();
        roundDuration = timeManager != null ? Mathf.Max(1f, timeManager.StartTime) : fallbackRoundDuration;
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
        // El primer input arranca la rampa de dificultad
        if (!difficultyStarted)
        {
            difficultyStarted = true;
            difficultyStartTime = Time.time;
        }

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
        return Mathf.LerpUnclamped(inputForceAtMinSpeed, inputForceAtMaxSpeed, t);
    }

    private float GetEffectiveGravityForce()
    {
        float t = GetDifficultyT();
        return -Mathf.LerpUnclamped(gravityAtMinSpeed, gravityAtMaxSpeed, t);
    }

    private float GetEffectiveVelocityReturnRate()
    {
        float t = GetDifficultyT();
        return Mathf.LerpUnclamped(velocityReturnRate, velocityReturnRate * 1.35f, t);
    }

    private float GetEffectiveMaxRiseSpeed()
    {
        float t = GetDifficultyT();
        return Mathf.LerpUnclamped(maxRiseSpeed, maxRiseSpeed * 1.35f, t);
    }

    private float GetEffectiveMaxFallSpeed()
    {
        float t = GetDifficultyT();
        return Mathf.LerpUnclamped(maxFallSpeed, maxFallSpeed * 1.45f, t);
    }

    /// <summary>
    /// t de dificultad para los lerps de fuerzas:
    /// - Antes del primer input: 0 (limite inferior SIEMPRE).
    /// - Con el primer input: el t del parametro externo (como antes).
    /// - Hasta el final del nivel: suma hasta difficultyRampStrength EXTRA.
    /// t PUEDE SUPERAR 1: los GetEffective* usan LerpUnclamped, asi las
    /// fuerzas se extrapolan mas alla de sus valores "maximos".
    /// </summary>
    private float GetDifficultyT()
    {
        if (!difficultyStarted)
        {
            return 0f;
        }

        float baseT = Mathf.InverseLerp(lowerSpeedLimit, Mathf.Max(lowerSpeedLimit + 0.01f, upperSpeedLimit), BaseDifficultySpeed);
        float rampT = GetRampProgress() * Mathf.Max(0f, difficultyRampStrength);
        return baseT + rampT;
    }
}