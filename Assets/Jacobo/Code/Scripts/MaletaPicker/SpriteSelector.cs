using UnityEngine;
using System.Collections.Generic;

public class SpriteSelector : MonoBehaviour
{
    [Header("Sprites by Type")]
    [SerializeField] private Sprite[] largeSprites;
    [SerializeField] private Sprite[] mediumSprites;
    [SerializeField] private Sprite[] smallSprites;

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
            return candidates[randomCandidate];
        }

        int randomIndex = Random.Range(0, source.Length);
        return source[randomIndex];
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
