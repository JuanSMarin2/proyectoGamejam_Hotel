using UnityEngine;

public class MaletaGrabHitbox : MonoBehaviour
{
    [SerializeField] private MaletaGrabInput grabInput;

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

        Maleta maleta = other.GetComponentInParent<Maleta>();
        if (maleta == null) return;

        grabInput.TryGrabMaleta(maleta);
    }
}
