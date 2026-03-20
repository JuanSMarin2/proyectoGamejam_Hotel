using UnityEngine;
using System.Collections.Generic;

public class ShopDatabase : MonoBehaviour
{
    public static ShopDatabase instance;

    [SerializeField] private List<Skin> skins;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);


        }
        else Destroy(gameObject);
    }

    public List<Skin> GetAllSkins()
    {
        return skins;
    }

    public Skin GetSkinByID(string id)
    {
        return skins.Find(s => s.id == id);
    }
}