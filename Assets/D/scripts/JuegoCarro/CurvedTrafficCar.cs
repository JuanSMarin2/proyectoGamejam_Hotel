using UnityEngine;

/// <summary>
/// Variante de TrafficCar que aplica una ligera curvatura suave a la dirección
/// usando Perlin Noise o una onda senoidal, manteniendo la detección de tráfico.
/// </summary>
public class CurvedTrafficCar : TrafficCar
{
    public enum CurveSource
    {
        PerlinNoise,
        Sine
    }

    [Header("Curva")]
    [SerializeField] private CurveSource curveSource = CurveSource.PerlinNoise;

    [Tooltip("Intensidad de la curva (0 = recto). Valores típicos: 0.05 - 0.35")]
    [Range(0f, 2f)]
    [SerializeField] private float curveIntensity = 0.25f;

    [Tooltip("Velocidad/frecuencia del cambio de curva. Valores típicos: 0.2 - 1.0")]
    [Min(0f)]
    [SerializeField] private float curveFrequency = 0.45f;

    [Tooltip("Suavizado del cambio de curva (segundos). Más alto = más lento/suave")]
    [Min(0.001f)]
    [SerializeField] private float curveDirectionSmoothTime = 0.65f;

    [Header("Perlin Noise")]
    [Tooltip("Escala del Perlin (en el eje X del ruido). Si es 0, se randomiza.")]
    [SerializeField] private float perlinSeed = 0f;

    [Tooltip("Offset temporal para desfasar carros entre sí. Si es 0, se randomiza.")]
    [SerializeField] private float timeOffset = 0f;

    private Vector2 straightDirection;
    private Vector2 curveCurrentDirection;
    private Vector2 curveDirectionVelocity;

    protected override void OnEnable()
    {
        base.OnEnable();

        // Captura la dirección "recta" actual (por si el spawner la configura).
        straightDirection = GetDirection();
        curveCurrentDirection = straightDirection;
        curveDirectionVelocity = Vector2.zero;

        if (Mathf.Approximately(perlinSeed, 0f))
            perlinSeed = Random.Range(0.01f, 999.99f);

        if (Mathf.Approximately(timeOffset, 0f))
            timeOffset = Random.Range(0.01f, 999.99f);
    }

    protected override void FixedUpdate()
    {
        ApplyCurvedDirection(Time.fixedTime, Time.fixedDeltaTime);
        base.FixedUpdate();
    }

    protected override void OnValidate()
    {
        base.OnValidate();

        curveIntensity = Mathf.Clamp(curveIntensity, 0f, 2f);
        curveFrequency = Mathf.Max(0f, curveFrequency);
        curveDirectionSmoothTime = Mathf.Max(0.001f, curveDirectionSmoothTime);
    }

    private void ApplyCurvedDirection(float t, float dt)
    {
        if (curveIntensity <= 0f || curveFrequency <= 0f)
        {
            // Mantener dirección recta.
            SetDirection(straightDirection);
            curveCurrentDirection = straightDirection;
            curveDirectionVelocity = Vector2.zero;
            return;
        }

        // Vector perpendicular a la dirección recta ("derecha" en 2D).
        Vector2 right = new Vector2(straightDirection.y, -straightDirection.x);
        if (right.sqrMagnitude < 0.0001f)
            right = Vector2.right;
        else
            right.Normalize();

        float curveValue = GetCurveValue(t + timeOffset);

        // Intensidad como "lateral/forward ratio".
        Vector2 targetDirection = straightDirection + (right * (curveValue * curveIntensity));
        if (targetDirection.sqrMagnitude < 0.0001f)
            targetDirection = straightDirection;
        else
            targetDirection.Normalize();

        curveCurrentDirection = Vector2.SmoothDamp(
            curveCurrentDirection,
            targetDirection,
            ref curveDirectionVelocity,
            curveDirectionSmoothTime,
            Mathf.Infinity,
            dt
        );

        if (curveCurrentDirection.sqrMagnitude < 0.0001f)
            curveCurrentDirection = straightDirection;
        else
            curveCurrentDirection.Normalize();

        SetDirection(curveCurrentDirection);
    }

    private float GetCurveValue(float t)
    {
        switch (curveSource)
        {
            case CurveSource.Sine:
                // 2π para que curveFrequency se sienta como "ciclos por segundo".
                return Mathf.Sin(t * curveFrequency * (2f * Mathf.PI));

            case CurveSource.PerlinNoise:
            default:
                // Perlin => [0..1], lo mapeamos a [-1..1]
                float n = Mathf.PerlinNoise(perlinSeed, t * curveFrequency);
                return (n * 2f) - 1f;
        }
    }
}
