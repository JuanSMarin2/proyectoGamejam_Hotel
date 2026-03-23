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

        if (TryGetBestVendedorFromRay(ray, out Vendedor bestVendedor))
        {
            if (debugOnCompra)
                Debug.Log($"[VentaInputRaycast] Best vendedor selected: {bestVendedor.name}. Calling TryHandleVenta.", bestVendedor);

            ventaManager.TryHandleVenta(bestVendedor);
            return;
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

    private bool TryGetBestVendedorFromRay(Ray ray, out Vendedor bestVendedor)
    {
        bestVendedor = null;
        int bestSortingOrder = int.MinValue;
        float bestDistance = float.MaxValue;

        RaycastHit[] hits3D = Physics.RaycastAll(ray, maxRayDistance, vendedorLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits3D.Length; i++)
        {
            Collider collider = hits3D[i].collider;
            if (collider == null) continue;

            Vendedor vendedor = collider.GetComponentInParent<Vendedor>();
            if (vendedor == null) continue;

            int sortingOrder = GetHighestSortingOrder(vendedor);
            float distance = hits3D[i].distance;

            if (IsBetterHitCandidate(sortingOrder, distance, bestSortingOrder, bestDistance))
            {
                bestVendedor = vendedor;
                bestSortingOrder = sortingOrder;
                bestDistance = distance;
            }
        }

        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, maxRayDistance, vendedorLayers);
        for (int i = 0; i < hits2D.Length; i++)
        {
            Collider2D collider = hits2D[i].collider;
            if (collider == null) continue;

            Vendedor vendedor = collider.GetComponentInParent<Vendedor>();
            if (vendedor == null) continue;

            int sortingOrder = GetHighestSortingOrder(vendedor);
            float distance = hits2D[i].distance;

            if (IsBetterHitCandidate(sortingOrder, distance, bestSortingOrder, bestDistance))
            {
                bestVendedor = vendedor;
                bestSortingOrder = sortingOrder;
                bestDistance = distance;
            }
        }

        return bestVendedor != null;
    }

    private static bool IsBetterHitCandidate(int candidateSortingOrder, float candidateDistance, int currentBestSortingOrder, float currentBestDistance)
    {
        if (candidateSortingOrder > currentBestSortingOrder)
            return true;

        if (candidateSortingOrder < currentBestSortingOrder)
            return false;

        return candidateDistance < currentBestDistance;
    }

    private static int GetHighestSortingOrder(Vendedor vendedor)
    {
        if (vendedor == null)
            return int.MinValue;

        SpriteRenderer[] renderers = vendedor.GetComponentsInChildren<SpriteRenderer>(true);
        int maxSortingOrder = int.MinValue;

        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer renderer = renderers[i];
            if (renderer == null)
                continue;

            if (renderer.sortingOrder > maxSortingOrder)
                maxSortingOrder = renderer.sortingOrder;
        }

        return maxSortingOrder == int.MinValue ? 0 : maxSortingOrder;
    }
}
