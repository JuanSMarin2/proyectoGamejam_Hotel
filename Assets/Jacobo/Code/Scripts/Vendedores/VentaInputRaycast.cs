using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class VentaInputRaycast : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private VentaManager ventaManager;

    [Header("Raycast")]
    [SerializeField] private LayerMask vendedorLayers = ~0;
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private bool ignoreInputOverUI = true;

    private void Awake()
    {
        if (inputCamera == null)
            inputCamera = Camera.main;

        if (ventaManager == null)
            ventaManager = VentaManager.instance != null ? VentaManager.instance : FindAnyObjectByType<VentaManager>();
    }

    public void OnCompra(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (ignoreInputOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (inputCamera == null || ventaManager == null) return;

        Vector2 pointerPos = Vector2.zero;

        if (Pointer.current != null)
            pointerPos = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            pointerPos = Mouse.current.position.ReadValue();

        Ray ray = inputCamera.ScreenPointToRay(pointerPos);

        if (Physics.Raycast(ray, out RaycastHit hit3D, maxRayDistance, vendedorLayers, QueryTriggerInteraction.Collide))
        {
            Vendedor vendedor3D = hit3D.collider.GetComponentInParent<Vendedor>();
            if (vendedor3D != null)
            {
                ventaManager.TryHandleVenta(vendedor3D);
                return;
            }
        }

        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, maxRayDistance, vendedorLayers);
        if (hit2D.collider != null)
        {
            Vendedor vendedor2D = hit2D.collider.GetComponentInParent<Vendedor>();
            if (vendedor2D != null)
                ventaManager.TryHandleVenta(vendedor2D);
        }
    }
}
