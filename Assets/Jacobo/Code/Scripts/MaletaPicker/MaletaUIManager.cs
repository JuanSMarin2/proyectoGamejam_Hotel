using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MaletaUIManager : MonoBehaviour
{
    [System.Serializable]
    private class WinnerUiSlot
    {
        public Image holderImage;
        public GameObject pickedPanel;
    }

    [Header("References")]
    [SerializeField] private MaletaManager maletaManager;

    [Header("UI")]
    [SerializeField] private List<WinnerUiSlot> winnerSlots = new List<WinnerUiSlot>();
    [SerializeField] private Sprite iconOverride;
    [SerializeField] private Color pendingColor = Color.white;
    [SerializeField] private Color pickedColor = Color.green;

    private readonly Dictionary<int, Image> imageByPoolId = new Dictionary<int, Image>();
    private readonly Dictionary<int, GameObject> pickedPanelByPoolId = new Dictionary<int, GameObject>();

    private void OnEnable()
    {
        if (maletaManager == null) return;

        maletaManager.WinnersAssigned += HandleWinnersAssigned;
        maletaManager.MaletaPicked += HandleMaletaPicked;

        if (maletaManager.WinnerTargets != null && maletaManager.WinnerTargets.Count > 0)
            HandleWinnersAssigned(maletaManager.WinnerTargets);
    }

    private void OnDisable()
    {
        if (maletaManager == null) return;

        maletaManager.WinnersAssigned -= HandleWinnersAssigned;
        maletaManager.MaletaPicked -= HandleMaletaPicked;
    }

    private void HandleWinnersAssigned(IReadOnlyList<MaletaManager.WinnerTarget> targets)
    {
        imageByPoolId.Clear();
        pickedPanelByPoolId.Clear();

        for (int i = 0; i < winnerSlots.Count; i++)
        {
            WinnerUiSlot slot = winnerSlots[i];
            if (slot == null || slot.holderImage == null) continue;

            slot.holderImage.gameObject.SetActive(false);
            slot.holderImage.color = pendingColor;
            slot.holderImage.sprite = null;

            if (slot.pickedPanel != null)
                slot.pickedPanel.SetActive(false);
        }

        int limit = Mathf.Min(targets.Count, winnerSlots.Count);
        for (int i = 0; i < limit; i++)
        {
            WinnerUiSlot slot = winnerSlots[i];
            if (slot == null || slot.holderImage == null) continue;

            slot.holderImage.gameObject.SetActive(true);
            slot.holderImage.sprite = iconOverride != null ? iconOverride : targets[i].Sprite;
            slot.holderImage.color = pendingColor;

            imageByPoolId[targets[i].PoolId] = slot.holderImage;

            if (slot.pickedPanel != null)
            {
                slot.pickedPanel.SetActive(false);
                pickedPanelByPoolId[targets[i].PoolId] = slot.pickedPanel;
            }
        }
    }

    private void HandleMaletaPicked(Maleta maleta, bool countedAsWinner, int collectedCount, int targetCount)
    {
        if (!countedAsWinner || maleta == null) return;

        if (imageByPoolId.TryGetValue(maleta.PoolId, out Image winnerImage) && winnerImage != null)
            winnerImage.color = pickedColor;

        if (pickedPanelByPoolId.TryGetValue(maleta.PoolId, out GameObject pickedPanel) && pickedPanel != null)
            pickedPanel.SetActive(true);
    }
}
