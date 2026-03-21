using UnityEngine;

public class VendedorSignSpriteLibrary : MonoBehaviour
{
    [Header("Sprites by Necesidad")]
    [SerializeField] private Sprite[] sedSprites;
    [SerializeField] private Sprite[] solSprites;
    [SerializeField] private Sprite[] diversionSprites;
    [SerializeField] private Sprite[] masajesSprites;

    public bool TryGetRandomSprite(Necesidad necesidad, out Sprite sprite)
    {
        sprite = null;

        Sprite[] source = GetSprites(necesidad);
        if (source == null || source.Length == 0)
            return false;

        int validCount = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return false;

        int pick = Random.Range(0, validCount);
        int current = 0;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null) continue;

            if (current == pick)
            {
                sprite = source[i];
                return true;
            }

            current++;
        }

        return false;
    }

    private Sprite[] GetSprites(Necesidad necesidad)
    {
        switch (necesidad)
        {
            case Necesidad.Sed:
                return sedSprites;
            case Necesidad.Sol:
                return solSprites;
            case Necesidad.Diversion:
                return diversionSprites;
            case Necesidad.Masajes:
                return masajesSprites;
            default:
                return null;
        }
    }
}
