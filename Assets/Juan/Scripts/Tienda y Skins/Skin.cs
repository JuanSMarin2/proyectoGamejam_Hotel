using UnityEngine;

[System.Serializable]
public class Skin
{
    public string id;
    public SkinCategory category;

    [Header("Sprite Labels")]
    public string label;

    [Header("Shop")]
    public int price;
}