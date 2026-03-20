using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject sectionPanel;
    [SerializeField] private GameObject shopPanel;

    [Header("UI")]
  [Header("UI")]
[SerializeField] private Image headImage;
[SerializeField] private Image chestImage;
[SerializeField] private Image fullBodyImage;

[SerializeField] private Button actionButton;
[SerializeField] private TextMeshProUGUI buttonText;
[SerializeField] private TextMeshProUGUI costText;
[SerializeField] private TextMeshProUGUI moneyText;
[SerializeField] private GameObject lockImage;

[SerializeField] private Image headFrame;
[SerializeField] private Image chestFrame;
[SerializeField] private Image fullFrame;


    [Header("Data")]
    [SerializeField] private List<Skin> allSkins;

    private List<Skin> currentSkins;
    private int currentIndex;
    private SkinCategory currentCategory;

private void Start()
{
    GameData.instance.InitializeSkins(allSkins);

    sectionPanel.SetActive(true);
    shopPanel.SetActive(false);

    UpdateCharacterVisual(); 
}
private Skin GetSelectedSkin()
{
    return currentSkins[currentIndex];
}

    private void UpdateCategoryHighlight()
{
    headFrame.color = currentCategory == SkinCategory.Head ? Color.white : Color.gray;
    chestFrame.color = currentCategory == SkinCategory.Chest ? Color.white : Color.gray;
    fullFrame.color = currentCategory == SkinCategory.FullBody ? Color.white : Color.gray;
}

 private void UpdateCharacterVisual()
{

    string headID = GameData.instance.GetEquipped(SkinCategory.Head);
    Skin headSkin = allSkins.Find(s => s.id == headID && s.category == SkinCategory.Head);

 
    string chestID = GameData.instance.GetEquipped(SkinCategory.Chest);
    Skin chestSkin = allSkins.Find(s => s.id == chestID && s.category == SkinCategory.Chest);


    string fullID = GameData.instance.GetEquipped(SkinCategory.FullBody);
    Skin fullSkin = allSkins.Find(s => s.id == fullID && s.category == SkinCategory.FullBody);


    if (headSkin != null) headImage.sprite = headSkin.icon;
    if (chestSkin != null) chestImage.sprite = chestSkin.icon;
    if (fullSkin != null) fullBodyImage.sprite = fullSkin.icon;

   
    if (currentSkins != null && currentSkins.Count > 0)
    {
        Skin selected = GetSelectedSkin();

        if (selected.category == SkinCategory.Head)
            headImage.sprite = selected.icon;

        else if (selected.category == SkinCategory.Chest)
            chestImage.sprite = selected.icon;

        else if (selected.category == SkinCategory.FullBody)
            fullBodyImage.sprite = selected.icon;
    }
}
private void Update(){
    moneyText.text = GameData.instance.Money.ToString();
}

    // ===== SELECCIÓN DE SECCIÓN =====

    public void SelectHead()
    {
        OpenSection(SkinCategory.Head);
    }

    public void SelectChest()
    {
        OpenSection(SkinCategory.Chest);
    }

    public void SelectFullBody()
    {
        OpenSection(SkinCategory.FullBody);
    }

   private void OpenSection(SkinCategory category)
{
    currentCategory = category;

    currentSkins = allSkins
        .Where(s => s.category == category)
        .OrderBy(s => int.Parse(s.id)) 
        .ToList();


    string equippedID = GameData.instance.GetEquipped(category);

    currentIndex = currentSkins.FindIndex(s => s.id == equippedID);

    if (currentIndex < 0)
        currentIndex = 0;

    sectionPanel.SetActive(false);
    shopPanel.SetActive(true);

    UpdateUI();
}

 public void BackToSections()
{
    sectionPanel.SetActive(true);
    shopPanel.SetActive(false);

    currentSkins = null;

    UpdateCharacterVisual(); 
}

    // ===== NAVEGACIÓN =====

    public void Next()
    {
        currentIndex = (currentIndex + 1) % currentSkins.Count;
        UpdateUI();
    }

    public void Previous()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentSkins.Count - 1;
        UpdateUI();
    }

    // ===== ACCIÓN =====

    public void OnAction()
{
    Skin skin = currentSkins[currentIndex];

    if (!GameData.instance.IsOwned(skin.id))
    {
        GameData.instance.BuySkin(skin.id, skin.price);
    }
    else if (!GameData.instance.IsEquipped(skin.id, currentCategory))
    {
        GameData.instance.EquipSkin(skin.id, currentCategory);
    }

    UpdateUI();
}

    // ===== UI =====

    private void UpdateUI()
{


    Skin skin = currentSkins[currentIndex];

    bool owned = GameData.instance.IsOwned(skin.id);
    bool equipped = GameData.instance.IsEquipped(skin.id, currentCategory);

    // LOCK
    lockImage.SetActive(!owned);

    // COST
    costText.text = owned ? "" : skin.price.ToString();

    if (!owned)
    {
        buttonText.text = "Comprar";
        actionButton.interactable = GameData.instance.Money >= skin.price;
        SetButtonAlpha(1f);
    }
    else if (!equipped)
    {
        buttonText.text = "Equipar";
        actionButton.interactable = true;
        SetButtonAlpha(1f);
    }
    else
    {
        buttonText.text = "Equipado";
        actionButton.interactable = false;
        SetButtonAlpha(0.5f);
    }

  
    UpdateCharacterVisual();
}

    private void SetButtonAlpha(float alpha)
    {
        Color c = actionButton.image.color;
        c.a = alpha;
        actionButton.image.color = c;
    }

    public void ReturnToMenu(){
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}