using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
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
    [SerializeField] private Animator characterAnimator;

    [Header("Transition")]
    [SerializeField] private ScriptCortina cortina;
    [SerializeField] private Button[] sectionButtons;

    private const string EquiparBool = "Equipar";
    private const string ModelarBool = "Modelar";
    private const string ShopMusicId = "MusicaTienda";

    private List<Skin> allSkins;
    private List<Skin> currentSkins;
    private int currentIndex;
    private SkinCategory currentCategory;
    private AudioSource shopMusicSource;
    private bool isSectionTransitioning;

    private void Start()
    {
        allSkins = ShopDatabase.instance.GetAllSkins();

        GameData.instance.InitializeSkins(allSkins);

        sectionPanel.SetActive(true);
        shopPanel.SetActive(false);

        
        UpdateEquippedVisual();
    }

    private void OnDestroy()
    {
        SoundManager.StopSound(shopMusicSource);
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
        StartSectionTransition(SkinCategory.Head);
    }

    public void SelectChest()
    {
        StartSectionTransition(SkinCategory.Chest);
    }

    private void StartSectionTransition(SkinCategory category)
    {
        if (isSectionTransitioning)
            return;

        OpenSection(category);
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

    private void SetSectionButtonsInteractable(bool interactable)
    {
        if (sectionButtons == null)
            return;

        for (int i = 0; i < sectionButtons.Length; i++)
        {
            if (sectionButtons[i] != null)
                sectionButtons[i].interactable = interactable;
        }
    }

    private void SetActionButtonInteractable(bool interactable)
    {
        if (actionButton == null)
            return;

        actionButton.interactable = interactable;
    }

    // ===== NAVEGACIÓN =====

    public void Next()
    {
        if (currentSkins == null || currentSkins.Count == 0)
            return;

        if (isSectionTransitioning)
            return;

        if (cortina == null)
        {
            PerformNextImmediate();
            UpdateUI();
            return;
        }

        isSectionTransitioning = true;
        SetActionButtonInteractable(false);
        StartCoroutine(PerformNextWithCurtain());
    }

    public void Previous()
    {
        if (currentSkins == null || currentSkins.Count == 0)
            return;

        if (isSectionTransitioning)
            return;

        if (cortina == null)
        {
            PerformPreviousImmediate();
            UpdateUI();
            return;
        }

        isSectionTransitioning = true;
        SetActionButtonInteractable(false);
        StartCoroutine(PerformPreviousWithCurtain());
    }

    private IEnumerator PerformNextWithCurtain()
    {
        yield return StartCoroutine(cortina.PlayCurtainRoutine(() =>
        {
            PerformNextImmediate();
            UpdateUI();
        }));

        SetActionButtonInteractable(true);
        isSectionTransitioning = false;
    }

    private IEnumerator PerformPreviousWithCurtain()
    {
        yield return StartCoroutine(cortina.PlayCurtainRoutine(() =>
        {
            PerformPreviousImmediate();
            UpdateUI();
        }));

        SetActionButtonInteractable(true);
        isSectionTransitioning = false;
    }

    private void PerformNextImmediate()
    {
        currentIndex = (currentIndex + 1) % currentSkins.Count;
        SetAnimatorBool(ModelarBool, true);
    }

    private void PerformPreviousImmediate()
    {
        currentIndex--;
        if (currentIndex < 0) currentIndex = currentSkins.Count - 1;
        SetAnimatorBool(ModelarBool, true);
    }

    // ===== ACCIÓN =====

    public void OnAction()
    {
        if (currentSkins == null || currentSkins.Count == 0)
            return;

        if (isSectionTransitioning)
            return;

        PerformActionImmediate();
        UpdateUI();
    }

    private void PerformActionImmediate()
    {
        Skin skin = GetSelectedSkin();
        bool wasOwned = GameData.instance.IsOwned(skin.id);
        bool wasEquipped = GameData.instance.IsEquipped(skin.id, currentCategory);

        if (!wasOwned)
        {
            GameData.instance.BuySkin(skin.id, skin.price);

            if (GameData.instance.IsOwned(skin.id))
            {
                SoundManager.PlaySound(SoundType.Comprar);
            }
        }
        else if (!wasEquipped)
        {
            GameData.instance.EquipSkin(skin.id, currentCategory);

            if (GameData.instance.IsEquipped(skin.id, currentCategory))
            {
                SoundManager.PlaySound(SoundType.Equipar);
                SetAnimatorBool(EquiparBool, true);
            }
        }
    }

    

    // ===== UI =====

    private void UpdateUI()
    {
        Skin skin = GetSelectedSkin();

        bool owned = GameData.instance.IsOwned(skin.id);
        bool equipped = GameData.instance.IsEquipped(skin.id, currentCategory);

        lockImage.SetActive(!owned);
        costText.text = owned ? "" : "Precio: "+skin.price.ToString();

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

    private void SetAnimatorBool(string parameter, bool value)
    {
        if (characterAnimator == null || string.IsNullOrWhiteSpace(parameter))
            return;

        characterAnimator.SetBool(parameter, value);
    }

    public void ReturnToMenu()
    {
        RoundData.ResetForMainMenu();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}