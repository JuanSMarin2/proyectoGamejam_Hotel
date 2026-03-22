using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public class VentaInputRaycast : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera inputCamera;
    [SerializeField] private VentaManager ventaManager;

    [Header("Raycast")]
    [SerializeField] private LayerMask vendedorLayers = ~0;
    [SerializeField] private float maxRayDistance = 100f;
    [SerializeField] private bool ignoreInputOverUI = true;
    [SerializeField] private bool blockOnlyInteractiveUI = true;

    [Header("Debug")]
    [SerializeField] private bool debugOnCompra = false;

    private void Awake()
    {
        if (inputCamera == null)
            inputCamera = Camera.main;

        if (ventaManager == null)
            ventaManager = VentaManager.instance != null ? VentaManager.instance : FindAnyObjectByType<VentaManager>();
    }

    public void OnCompra(InputAction.CallbackContext context)
    {
        if (debugOnCompra)
            Debug.Log($"[VentaInputRaycast] OnCompra called. phase={context.phase}, performed={context.performed}, started={context.started}, canceled={context.canceled}", this);

        if (!context.performed)
        {
            if (debugOnCompra)
                Debug.Log("[VentaInputRaycast] Ignored input because context is not performed.", this);
            return;
        }

        if (inputCamera == null || ventaManager == null)
        {
            if (debugOnCompra)
                Debug.LogWarning($"[VentaInputRaycast] Missing references. inputCamera={(inputCamera != null ? inputCamera.name : "NULL")}, ventaManager={(ventaManager != null ? ventaManager.name : "NULL")}", this);
            return;
        }

        Vector2 pointerPos = Vector2.zero;

        if (Pointer.current != null)
            pointerPos = Pointer.current.position.ReadValue();
        else if (Mouse.current != null)
            pointerPos = Mouse.current.position.ReadValue();

        bool blockedByUI = IsPointerBlockedByUI(pointerPos);
        if (blockedByUI)
        {
            if (debugOnCompra)
                Debug.Log("[VentaInputRaycast] Input blocked because pointer is over blocking UI.", this);
            return;
        }

        if (debugOnCompra)
            Debug.Log($"[VentaInputRaycast] Pointer position: {pointerPos}", this);

        Ray ray = inputCamera.ScreenPointToRay(pointerPos);

        if (debugOnCompra)
            Debug.Log($"[VentaInputRaycast] Ray origin={ray.origin}, dir={ray.direction}, maxDist={maxRayDistance}, layerMask={vendedorLayers.value}", this);

        if (Physics.Raycast(ray, out RaycastHit hit3D, maxRayDistance, vendedorLayers, QueryTriggerInteraction.Collide))
        {
            if (debugOnCompra)
                Debug.Log($"[VentaInputRaycast] 3D hit collider={hit3D.collider.name}, point={hit3D.point}", hit3D.collider);

            Vendedor vendedor3D = hit3D.collider.GetComponentInParent<Vendedor>();
            if (vendedor3D != null)
            {
                if (debugOnCompra)
                    Debug.Log($"[VentaInputRaycast] 3D vendedor found: {vendedor3D.name}. Calling TryHandleVenta.", vendedor3D);
                ventaManager.TryHandleVenta(vendedor3D);
                return;
            }

            if (debugOnCompra)
                Debug.Log("[VentaInputRaycast] 3D collider hit but no Vendedor in parents.", hit3D.collider);
        }
        else if (debugOnCompra)
        {
            Debug.Log("[VentaInputRaycast] No 3D hit.", this);
        }

        RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray, maxRayDistance, vendedorLayers);
        if (hit2D.collider != null)
        {
            if (debugOnCompra)
                Debug.Log($"[VentaInputRaycast] 2D hit collider={hit2D.collider.name}, point={hit2D.point}", hit2D.collider);

            Vendedor vendedor2D = hit2D.collider.GetComponentInParent<Vendedor>();
            if (vendedor2D != null)
            {
                if (debugOnCompra)
                    Debug.Log($"[VentaInputRaycast] 2D vendedor found: {vendedor2D.name}. Calling TryHandleVenta.", vendedor2D);
                ventaManager.TryHandleVenta(vendedor2D);
                return;
            }

            if (debugOnCompra)
                Debug.Log("[VentaInputRaycast] 2D collider hit but no Vendedor in parents.", hit2D.collider);
        }
        else if (debugOnCompra)
        {
            Debug.Log("[VentaInputRaycast] No 2D hit.", this);
        }

        if (debugOnCompra)
            Debug.Log("[VentaInputRaycast] OnCompra finished without valid vendedor hit.", this);
    }

    private bool IsPointerBlockedByUI(Vector2 pointerPos)
    {
        if (!ignoreInputOverUI)
            return false;

        if (EventSystem.current == null)
            return false;

        if (!blockOnlyInteractiveUI)
            return EventSystem.current.IsPointerOverGameObject();

        var pointerData = new PointerEventData(EventSystem.current)
        {
            position = pointerPos
        };

        List<RaycastResult> results = new List<RaycastResult>(8);
        EventSystem.current.RaycastAll(pointerData, results);

        for (int i = 0; i < results.Count; i++)
        {
            GameObject go = results[i].gameObject;
            if (go == null) continue;

            if (go.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }
}
