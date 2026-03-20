using UnityEngine;
using System.Collections.Generic;

public class HeadFixer : MonoBehaviour
{
    [System.Serializable]
    public class HeadSprite
    {
        public string label;
        public Sprite sprite;
    }

    [Header("Config")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private List<HeadSprite> sprites;

    private Dictionary<string, Sprite> spriteDict;

    private void Awake()
    {
        spriteDict = new Dictionary<string, Sprite>();

        foreach (var s in sprites)
        {
            if (!spriteDict.ContainsKey(s.label))
                spriteDict.Add(s.label, s.sprite);
        }
    }

    public void ApplyHead(string label)
    {
        if (spriteDict.TryGetValue(label, out Sprite sprite))
        {
            headRenderer.sprite = sprite;
        }
        else
        {
            Debug.LogWarning("No sprite para label: " + label);
        }
    }
}