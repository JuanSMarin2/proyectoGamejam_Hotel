using UnityEngine;
using System.Collections.Generic;


[System.Serializable]
public enum SkinCategory
{
    Head,
    Chest,
    FullBody
}

public class GameData : MonoBehaviour
{
    public static GameData instance;

    [SerializeField] private int money;

    private HashSet<string> ownedSkins = new HashSet<string>();

    private Dictionary<SkinCategory, string> equippedSkins =
        new Dictionary<SkinCategory, string>();

    private bool skinsInitialized = false;

    public int Money
    {
        get => money;
        set => money = Mathf.Max(0, value);
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

     
        }
        else Destroy(gameObject);
    }
    
    public string GetEquipped(SkinCategory category)
{
    return equippedSkins.ContainsKey(category) 
        ? equippedSkins[category] 
        : null;
}


    public void InitializeSkins(List<Skin> allSkins)
    {
        if (skinsInitialized) return;

        foreach (SkinCategory category in System.Enum.GetValues(typeof(SkinCategory)))
        {
            Skin defaultSkin = allSkins.Find(s => s.category == category && s.id == "0");

            if (defaultSkin != null)
            {
                ownedSkins.Add(defaultSkin.id);
                equippedSkins[category] = defaultSkin.id;
            }
        }

        skinsInitialized = true;
    }

    public bool IsOwned(string id)
    {
        return ownedSkins.Contains(id);
    }

    public bool IsEquipped(string id, SkinCategory category)
    {
        return equippedSkins.ContainsKey(category) &&
               equippedSkins[category] == id;
    }

    public void BuySkin(string id, int price)
    {
        if (money < price) return;

        money -= price;
        ownedSkins.Add(id);
    }

    public void EquipSkin(string id, SkinCategory category)
    {
        if (!IsOwned(id)) return;

        equippedSkins[category] = id;
    }
}