using UnityEngine;

public class MaletaGrabHitbox : MonoBehaviour
{
    [SerializeField] private MaletaGrabInput grabInput;
    [SerializeField] private bool debugSetup = true;

    private void Awake()
    {
        if (grabInput == null)
            grabInput = GetComponentInParent<MaletaGrabInput>();

        if (debugSetup)
            Validate2DTriggerSetup();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[MaletaGrabHitbox] ENTER 2D -> other='{other.name}', otherTag='{other.tag}', otherLayer='{LayerMask.LayerToName(other.gameObject.layer)}'");
        TryHandle(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Useful when overlap starts while animation/collider is already active.
        TryHandle(other.gameObject);
    }


    private void TryHandle(GameObject other)
    {
        if (grabInput == null)
        {
            Debug.LogWarning("[MaletaGrabHitbox] grabInput is NULL. Assign MaletaGrabInput in inspector or parent.");
            return;
        }

        if (other == null) return;

        Maleta maleta = other.GetComponentInParent<Maleta>();
        if (maleta == null) return;

        if (maleta.IsPicked) return;

        Debug.Log($"[MaletaGrabHitbox] MALETA DETECTED -> name='{maleta.name}', winner={maleta.Winner}, poolId={maleta.PoolId}, isPicked={maleta.IsPicked}");

        grabInput.TryGrabMaleta(maleta);
    }

    private void Validate2DTriggerSetup()
    {
        Collider2D ownCol = GetComponent<Collider2D>();
        Rigidbody2D ownRb = GetComponent<Rigidbody2D>();
        Rigidbody2D parentRb = GetComponentInParent<Rigidbody2D>();

        if (ownCol == null)
            Debug.LogWarning("[MaletaGrabHitbox][SETUP] Missing Collider2D on hitbox object.");
        else if (!ownCol.isTrigger)
            Debug.LogWarning("[MaletaGrabHitbox][SETUP] Hitbox Collider2D must have 'Is Trigger' enabled.");

        if (ownRb == null && parentRb == null)
        {
            Debug.LogWarning("[MaletaGrabHitbox][SETUP] No Rigidbody2D on hitbox/player hierarchy. At least one collider pair side needs Rigidbody2D for trigger callbacks.");
        }

        Maleta[] maletas = FindObjectsByType<Maleta>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int maletasWithCollider2D = 0;
        int maletasWithRb2D = 0;

        for (int i = 0; i < maletas.Length; i++)
        {
            if (maletas[i] == null) continue;

            Collider2D maletaCol = maletas[i].GetComponentInChildren<Collider2D>(true);
            Rigidbody2D maletaRb = maletas[i].GetComponentInParent<Rigidbody2D>();

            if (maletaCol != null) maletasWithCollider2D++;
            if (maletaRb != null) maletasWithRb2D++;
        }

        Debug.Log($"[MaletaGrabHitbox][SETUP] hitboxCollider2D={(ownCol != null)}, hitboxIsTrigger={(ownCol != null && ownCol.isTrigger)}, hitboxRb2D={(ownRb != null || parentRb != null)}, maletas={maletas.Length}, maletasWithCollider2D={maletasWithCollider2D}, maletasWithRb2D={maletasWithRb2D}");
    }
}