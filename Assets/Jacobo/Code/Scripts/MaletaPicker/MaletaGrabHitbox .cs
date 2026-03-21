using UnityEngine;

public class MaletaGrabHitbox : MonoBehaviour
{
    [SerializeField] private MaletaGrabInput grabInput;

    [Header("Layer Filter")]
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private LayerMask maletaLayerMask;

    private void Awake()
    {
        if (grabInput == null)
            grabInput = GetComponentInParent<MaletaGrabInput>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHandle(other.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHandle(other.gameObject);
    }

    private void TryHandle(GameObject other)
    {
        if (grabInput == null || other == null) return;

        if (!IsPlayerVsMaletaCollision(other))
            return;

        Maleta maleta = other.GetComponentInParent<Maleta>();
        if (maleta == null) return;

        grabInput.TryGrabMaleta(maleta);
    }

    private bool IsPlayerVsMaletaCollision(GameObject other)
    {
        bool thisIsPlayer = IsInMask(gameObject.layer, playerLayerMask);
        bool otherIsMaleta = IsInMask(other.layer, maletaLayerMask);

        if (thisIsPlayer && otherIsMaleta)
            return true;

        // Allow inverse setup in case this script is placed on maleta side by mistake.
        bool thisIsMaleta = IsInMask(gameObject.layer, maletaLayerMask);
        bool otherIsPlayer = IsInMask(other.layer, playerLayerMask);

        return thisIsMaleta && otherIsPlayer;
    }

    private static bool IsInMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}