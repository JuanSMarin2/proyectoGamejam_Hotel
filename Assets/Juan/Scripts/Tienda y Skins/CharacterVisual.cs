using UnityEngine;
using UnityEngine.U2D.Animation;

public class CharacterVisual : MonoBehaviour
{
    [Header("Resolvers")]
    [SerializeField] private SpriteResolver head;
    [SerializeField] private SpriteResolver chest;
    [SerializeField] private SpriteResolver derarm;
    [SerializeField] private SpriteResolver izqarm;
    [SerializeField] private SpriteResolver izqhand;
    [SerializeField] private SpriteResolver derhand;
    [SerializeField] private SpriteResolver izqleg;
    [SerializeField] private SpriteResolver derleg;
    [SerializeField] private SpriteResolver izqfoot;
    [SerializeField] private SpriteResolver derfoot;

    [SerializeField] private HeadFixer headFixer;

    private const string BASE_LABEL = "Base";

    private void Start()
    {
        ApplyEquipped();
    }

    public void ApplyEquipped()
    {
        string headLabel = GetLabel(SkinCategory.Head);
        string chestLabel = GetLabel(SkinCategory.Chest);

        // Reset base primero
        ApplyLabelToAll(BASE_LABEL);

        // Aplicar chest
        ApplyChest(chestLabel);

        // Aplicar head (override)
        ApplyHead(headLabel);
    }

    private string GetLabel(SkinCategory category)
    {
        string skinID = GameData.instance.GetEquipped(category);
        Skin skin = ShopDatabase.instance.GetSkinByID(skinID);

        return skin != null ? skin.label : BASE_LABEL;
    }

    private void ApplyLabelToAll(string label)
    {
        head.SetCategoryAndLabel("Head", label);
        chest.SetCategoryAndLabel("chest", label);

        derarm.SetCategoryAndLabel("derarm", label);
        izqarm.SetCategoryAndLabel("izqarm", label);

        derhand.SetCategoryAndLabel("derhand", label);
        izqhand.SetCategoryAndLabel("izqhand", label);

        derleg.SetCategoryAndLabel("derleg", label);
        izqleg.SetCategoryAndLabel("izqleg", label);

        derfoot.SetCategoryAndLabel("derfoot", label);
        izqfoot.SetCategoryAndLabel("izqfoot", label);
    }

    private void ApplyChest(string label)
    {
        chest.SetCategoryAndLabel("chest", label);

        derleg.SetCategoryAndLabel("derleg", label);
        izqleg.SetCategoryAndLabel("izqleg", label);
    }

    public void SetSkin(SkinCategory category, string label)
    {
   
        ApplyEquipped();

  
        switch (category)
        {
            case SkinCategory.Head:
                ApplyHead(label);
                break;

            case SkinCategory.Chest:
                ApplyChest(label);
                break;
        }
    }
  

    private void ApplyHead(string label)
    {
        head.SetCategoryAndLabel("Head", label);

        // SpriteResolver de Head fallaba en modelo 3/4 NO TENGO IDEA DE POR QUE
        // Se fuerza sprite manualmente para evitar inconsistencias
        if (headFixer != null)
            headFixer.ApplyHead(label);


        derarm.SetCategoryAndLabel("derarm", label);
        izqarm.SetCategoryAndLabel("izqarm", label);

        derhand.SetCategoryAndLabel("derhand", label);
        izqhand.SetCategoryAndLabel("izqhand", label);

        derfoot.SetCategoryAndLabel("derfoot", label);
        izqfoot.SetCategoryAndLabel("izqfoot", label);
    }
}