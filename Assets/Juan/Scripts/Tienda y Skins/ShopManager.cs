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
    [SerializeField] private Button actionButton;
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private GameObject lockImage;

    [Header("Character")]
    [SerializeField] private CharacterVisual characterVisual;

    private List<Skin> allSkins;
    private List<Skin> currentSkins;
    private int currentIndex;
    private SkinCategory currentCategory;

    private void Start()
    {
        allSkins = ShopDatabase.instance.GetAllSkins();

        GameData.instance.InitializeSkins(allSkins);

        sectionPanel.SetActive(true);
        shopPanel.SetActive(false);

        UpdateEquippedVisual();
    }

    private void Update()
    {
        moneyText.text = GameData.instance.Money.ToString();
    }

    private Skin GetSelectedSkin()
    {
        return currentSkins[currentIndex];
    }

    // ===== VISUAL =====

    private void UpdateEquippedVisual()
    {
        characterVisual.ApplyEquipped();
    }

    private void UpdatePreviewVisual()
    {
        characterVisual.ApplyEquipped();

        if (currentSkins == null || currentSkins.Count == 0) return;

        Skin selected = GetSelectedSkin();
        characterVisual.SetSkin(selected.category, selected.label);
    }

    // ===== SELECCIÓN =====

    public void SelectHead()
    {
        OpenSection(SkinCategory.Head);
    }

    public void SelectChest()
    {
        OpenSection(SkinCategory.Chest);
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

        UpdateEquippedVisual();
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
        Skin skin = GetSelectedSkin();

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
        Skin skin = GetSelectedSkin();

        bool owned = GameData.instance.IsOwned(skin.id);
        bool equipped = GameData.instance.IsEquipped(skin.id, currentCategory);

        lockImage.SetActive(!owned);
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

        UpdatePreviewVisual();
    }

    private void SetButtonAlpha(float alpha)
    {
        Color c = actionButton.image.color;
        c.a = alpha;
        actionButton.image.color = c;
    }

    public void ReturnToMenu()
    {
        RoundData.ResetForMainMenu();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}