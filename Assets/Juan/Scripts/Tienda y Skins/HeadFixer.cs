using UnityEngine;
using System.Collections.Generic;

public class HeadFixer : MonoBehaviour
{
    public enum Face
    {
        Neutral,
        Happy,
        Sad,
        Angry,
        Back
    }

    [System.Serializable]
    public class FaceSpriteEntry
    {
        public Face face;
        public Sprite sprite;
    }

    [System.Serializable]
    public class HeadSprite
    {
        public string label;
        public List<FaceSpriteEntry> faceSprites = new List<FaceSpriteEntry>();

        private Dictionary<Face, Sprite> faceDict;

        public void BuildCache()
        {
            faceDict = new Dictionary<Face, Sprite>();

            for (int i = 0; i < faceSprites.Count; i++)
            {
                FaceSpriteEntry entry = faceSprites[i];
                if (entry == null || faceDict.ContainsKey(entry.face))
                    continue;

                faceDict.Add(entry.face, entry.sprite);
            }
        }

        public Sprite GetSprite(Face face)
        {
            if (faceDict == null)
            {
                BuildCache();
            }

            if (faceDict.TryGetValue(face, out Sprite sprite))
            {
                return sprite;
            }

            if (faceDict.TryGetValue(Face.Neutral, out Sprite neutralSprite))
            {
                return neutralSprite;
            }

            return null;
        }
    }

    [Header("Config")]
    [SerializeField] private SpriteRenderer headRenderer;
    [SerializeField] private List<HeadSprite> sprites;
    [SerializeField] private Face startFace = Face.Neutral;

    private Dictionary<string, HeadSprite> spriteDict;
    private string currentLabel;
    private Face currentFace;

    private void Awake()
    {
        spriteDict = new Dictionary<string, HeadSprite>();

        foreach (var s in sprites)
        {
            if (!spriteDict.ContainsKey(s.label))
            {
                s.BuildCache();
                spriteDict.Add(s.label, s);
            }
        }

        currentFace = startFace;
    }

    public void ApplyHead(string label)
    {
        if (spriteDict.TryGetValue(label, out HeadSprite headSprite))
        {
            currentLabel = label;

            Sprite selectedSprite = headSprite.GetSprite(currentFace);
            if (selectedSprite != null)
            {
                headRenderer.sprite = selectedSprite;
            }
            else
            {
                Debug.LogWarning("No sprite para face: " + currentFace + " en label: " + label);
            }
        }
        else
        {
            Debug.LogWarning("No sprite para label: " + label);
        }
    }

    public void SwapFace(Face newFace)
    {
        currentFace = newFace;

        if (!string.IsNullOrEmpty(currentLabel))
        {
            ApplyHead(currentLabel);
        }
    }

    public void SwapFace()
    {
        int nextFace = ((int)currentFace + 1) % System.Enum.GetValues(typeof(Face)).Length;
        SwapFace((Face)nextFace);
    }

    public Face CurrentFace => currentFace;
}