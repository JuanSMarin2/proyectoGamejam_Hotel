using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ConveyorBeltUV : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CintaMovement cintaMovement;

    [Header("UV Scroll")]
    [SerializeField] private float speedMultiplier = 1f;

    private Material runtimeMaterial;
    private Vector2 offset;

    private void Awake()
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        runtimeMaterial = spriteRenderer != null ? spriteRenderer.material : null;
    }

    private void Update()
    {
        if (runtimeMaterial == null || cintaMovement == null) return;

        float uvSpeed = cintaMovement.MovementSpeed * speedMultiplier;
        offset.x += uvSpeed * Time.deltaTime;

        runtimeMaterial.mainTextureOffset = offset;
    }
}
