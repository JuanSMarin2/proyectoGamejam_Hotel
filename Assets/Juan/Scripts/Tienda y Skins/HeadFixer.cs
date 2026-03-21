using UnityEngine;
using System.Collections.Generic;

public class HeadFixer : MonoBehaviour
{
    [System.Serializable]
    public class HeadSprite
    {
        public string label;
        public Sprite sprite;
        public Sprite backHeadSprite;
    }

    [Header("Config")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private List<HeadSprite> sprites;
    [SerializeField] private bool UseBackwardsHead;

    private Dictionary<string, HeadSprite> spriteDict;
    private string currentLabel;

    private void Awake()
    {
        spriteDict = new Dictionary<string, HeadSprite>();

        foreach (var s in sprites)
        {
            if (!spriteDict.ContainsKey(s.label))
                spriteDict.Add(s.label, s);
        }
    }

    public void ApplyHead(string label)
    {
        if (spriteDict.TryGetValue(label, out HeadSprite headSprite))
        {
            currentLabel = label;
            headRenderer.sprite = UseBackwardsHead ? headSprite.backHeadSprite : headSprite.sprite;
        }
        else
        {
            Debug.LogWarning("No sprite para label: " + label);
        }
    }

    public void SwapFace()
    {
        UseBackwardsHead = !UseBackwardsHead;

        if (!string.IsNullOrEmpty(currentLabel))
        {
            ApplyHead(currentLabel);
        }
    }
}