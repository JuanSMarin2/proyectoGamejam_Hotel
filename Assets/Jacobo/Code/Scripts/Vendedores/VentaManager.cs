using System;
using UnityEngine;

public class VentaManager : MonoBehaviour
{
    public static VentaManager instance;

    [Header("References")]
    [SerializeField] private CharacterNecesidad characterNecesidad;

    [Header("Rules")]
    [SerializeField] private bool loseOnWrongVendedor = true;
    [SerializeField] private bool loseOnNeedTimeout = true;

    public event Action<Vendedor, Necesidad, bool> OnCompra;
    public event Action<Vendedor, Necesidad, bool> OnVenta;

    private bool gameEnded;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (characterNecesidad != null)
            characterNecesidad.NeedFailed += HandleNeedFailed;
    }

    private void OnDisable()
    {
        if (characterNecesidad != null)
            characterNecesidad.NeedFailed -= HandleNeedFailed;
    }

    public void TryHandleVenta(Vendedor vendedor)
    {
        if (gameEnded || vendedor == null || characterNecesidad == null) return;
        if (!characterNecesidad.HasActiveNeed) return;

        bool success = characterNecesidad.ResolveIfMatches(vendedor.NecesidadVenta);

        OnCompra?.Invoke(vendedor, vendedor.NecesidadVenta, success);
        OnVenta?.Invoke(vendedor, vendedor.NecesidadVenta, success);

        if (!success && loseOnWrongVendedor)
        {
            gameEnded = true;
            ResultManager.instance.LoseMinigame();
        }
    }

    private void HandleNeedFailed(Necesidad failedNeed)
    {
        if (gameEnded || !loseOnNeedTimeout) return;

        gameEnded = true;
        ResultManager.instance.LoseMinigame();
    }
}
