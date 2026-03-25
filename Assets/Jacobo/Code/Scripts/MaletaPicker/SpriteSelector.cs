using UnityEngine;
using System.Collections.Generic;

public class SpriteSelector : MonoBehaviour
{
    [Header("Sprites by Type")]
    [SerializeField] private Sprite[] largeSprites;
    [SerializeField] private Sprite[] mediumSprites;
    [SerializeField] private Sprite[] smallSprites;

    [Header("Similar Sprites by Type (index paired)")]
    [SerializeField] private Sprite[] largeSimilarSprites;
    [SerializeField] private Sprite[] mediumSimilarSprites;
    [SerializeField] private Sprite[] smallSimilarSprites;

    [Header("Similar Variant")]
    [SerializeField] private bool useSimilarVariants = false;
    [SerializeField, Range(0f, 1f)] private float similarVariantChance = 0.5f;

    private readonly Dictionary<Maleta.MaletaType, List<Sprite>> winnerAvailableByType = new Dictionary<Maleta.MaletaType, List<Sprite>>();
    private readonly HashSet<Sprite> winnerReservedSprites = new HashSet<Sprite>();

    private void Awake()
    {
        ResetWinnerSpritePool();
    }

    public void ResetWinnerSpritePool()
    {
        winnerAvailableByType.Clear();
        winnerReservedSprites.Clear();

        winnerAvailableByType[Maleta.MaletaType.Large] = BuildValidList(largeSprites);
        winnerAvailableByType[Maleta.MaletaType.Medium] = BuildValidList(mediumSprites);
        winnerAvailableByType[Maleta.MaletaType.Small] = BuildValidList(smallSprites);
    }

    public Sprite TakeWinnerUniqueSprite(Maleta.MaletaType type)
    {
        if (!winnerAvailableByType.TryGetValue(type, out List<Sprite> available) || available == null || available.Count == 0)
        {
            Sprite fallback = GetRandomReusableSprite(type);
            if (fallback != null)
                winnerReservedSprites.Add(fallback);

            return fallback;
        }

        int randomIndex = Random.Range(0, available.Count);
        Sprite selected = available[randomIndex];
        available.RemoveAt(randomIndex);

        if (selected != null)
            winnerReservedSprites.Add(selected);

        return selected;
    }

    public Sprite GetRandomReusableSprite(Maleta.MaletaType type)
    {
        Sprite[] source = GetArrayByType(type);
        if (source == null || source.Length == 0) return null;

        List<Sprite> candidates = new List<Sprite>(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            Sprite sprite = source[i];
            if (sprite == null) continue;
            if (!winnerReservedSprites.Contains(sprite))
                candidates.Add(sprite);
        }

        if (candidates.Count > 0)
        {
            int randomCandidate = Random.Range(0, candidates.Count);
            return ResolveVariantSprite(type, candidates[randomCandidate]);
        }

        int randomIndex = Random.Range(0, source.Length);
        return ResolveVariantSprite(type, source[randomIndex]);
    }

    public bool TryGetSimilarSprite(Maleta.MaletaType type, Sprite baseSprite, out Sprite similarSprite)
    {
        similarSprite = null;
        if (baseSprite == null)
            return false;

        Sprite[] baseArray = GetArrayByType(type);
        Sprite[] similarArray = GetSimilarArrayByType(type);

        if (baseArray == null || similarArray == null)
            return false;

        int index = IndexOfSprite(baseArray, baseSprite);
        if (index < 0 || index >= similarArray.Length)
            return false;

        similarSprite = similarArray[index];
        return similarSprite != null;
    }

    public bool TryGetPairedVariant(Maleta.MaletaType type, Sprite sprite, out Sprite pairedSprite)
    {
        pairedSprite = null;
        if (sprite == null)
            return false;

        Sprite[] baseArray = GetArrayByType(type);
        Sprite[] similarArray = GetSimilarArrayByType(type);

        if (baseArray == null)
            return false;

        int baseIndex = IndexOfSprite(baseArray, sprite);
        if (baseIndex >= 0)
        {
            if (similarArray != null && baseIndex < similarArray.Length && similarArray[baseIndex] != null)
            {
                pairedSprite = similarArray[baseIndex];
                return true;
            }

            return false;
        }

        if (similarArray == null)
            return false;

        int similarIndex = IndexOfSprite(similarArray, sprite);
        if (similarIndex < 0 || similarIndex >= baseArray.Length)
            return false;

        pairedSprite = baseArray[similarIndex];
        return pairedSprite != null;
    }

    public Sprite GetBaseOrSimilarByIndex(Maleta.MaletaType type, int index, bool returnSimilar)
    {
        Sprite[] baseArray = GetArrayByType(type);
        if (baseArray == null || index < 0 || index >= baseArray.Length)
            return null;

        if (!returnSimilar)
            return baseArray[index];

        Sprite[] similarArray = GetSimilarArrayByType(type);
        if (similarArray == null || index >= similarArray.Length)
            return baseArray[index];

        return similarArray[index] != null ? similarArray[index] : baseArray[index];
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

    private Sprite[] GetSimilarArrayByType(Maleta.MaletaType type)
    {
        switch (type)
        {
            case Maleta.MaletaType.Large:
                return largeSimilarSprites;
            case Maleta.MaletaType.Small:
                return smallSimilarSprites;
            default:
                return mediumSimilarSprites;
        }
    }

    private Sprite ResolveVariantSprite(Maleta.MaletaType type, Sprite baseSprite)
    {
        if (!useSimilarVariants || baseSprite == null)
            return baseSprite;

        if (Random.value > Mathf.Clamp01(similarVariantChance))
            return baseSprite;

        if (TryGetSimilarSprite(type, baseSprite, out Sprite similarSprite))
            return similarSprite;

        return baseSprite;
    }

    private static int IndexOfSprite(Sprite[] source, Sprite target)
    {
        if (source == null || source.Length == 0 || target == null)
            return -1;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == target)
                return i;
        }

        return -1;
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

    private List<Sprite> BuildValidList(Sprite[] source)
    {
        List<Sprite> result = new List<Sprite>();
        if (source == null || source.Length == 0) return result;

        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null)
                result.Add(source[i]);
        }

        return result;
    }
}
