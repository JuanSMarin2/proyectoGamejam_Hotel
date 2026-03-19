using UnityEngine;

public class SpriteSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetRenderer;

    [Header("Sprites by Type")]
    [SerializeField] private Sprite[] largeSprites;
    [SerializeField] private Sprite[] mediumSprites;
    [SerializeField] private Sprite[] smallSprites;

    private void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void AssignRandomSprite(Maleta.MaletaType type)
    {
        if (targetRenderer == null) return;

        Sprite[] source = GetArrayByType(type);
        if (source == null || source.Length == 0) return;

        int randomIndex = Random.Range(0, source.Length);
        targetRenderer.sprite = source[randomIndex];
    }

    public Sprite GetAnyPreviewSprite()
    {
        Sprite sprite = FirstValid(largeSprites);
        if (sprite != null) return sprite;

        sprite = FirstValid(mediumSprites);
        if (sprite != null) return sprite;

        return FirstValid(smallSprites);
    }

    private Sprite[] GetArrayByType(Maleta.MaletaType type)
    {
        switch (type)
        {
            case Maleta.MaletaType.Large:
                return largeSprites;
            case Maleta.MaletaType.Small:
                return smallSprites;
            default:
                return mediumSprites;
        }
    }

    private Sprite FirstValid(Sprite[] sprites)
    {
        if (sprites == null || sprites.Length == 0) return null;

        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null)
                return sprites[i];
        }

        return null;
    }
}
