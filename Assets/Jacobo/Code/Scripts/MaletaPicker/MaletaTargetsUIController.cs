using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MaletaTargetsUIController : MonoBehaviour
{
    [System.Serializable]
    private class WinnerUiSlot
    {
        public Image holderImage;
    }

    [Header("References")]
    [SerializeField] private MaletaManager maletaManager;

    [Header("UI")]
    [SerializeField] private List<WinnerUiSlot> winnerSlots = new List<WinnerUiSlot>();
    [SerializeField] private GameObject pickedPanelPrefab;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string singleTargetTitle = "Mi maleta:";
    [SerializeField] private string multipleTargetsTitle = "Mis maletas:";

    private readonly Dictionary<int, GameObject> pickedPanelByPoolId = new Dictionary<int, GameObject>();
    private readonly List<GameObject> spawnedPickedPanels = new List<GameObject>();

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
        if (maletaManager != null)
        {
            maletaManager.WinnersAssigned -= HandleWinnersAssigned;
            maletaManager.MaletaPicked -= HandleMaletaPicked;
        }

        ClearSpawnedPanels();
    }

    private void HandleWinnersAssigned(IReadOnlyList<MaletaManager.WinnerTarget> targets)
    {
        pickedPanelByPoolId.Clear();
        ClearSpawnedPanels();

        int limit = Mathf.Min(targets.Count, winnerSlots.Count);

        if (titleText != null)
            titleText.text = limit > 1 ? multipleTargetsTitle : singleTargetTitle;

        for (int i = 0; i < winnerSlots.Count; i++)
        {
            WinnerUiSlot slot = winnerSlots[i];
            if (slot == null || slot.holderImage == null) continue;

            bool isActive = i < limit;
            slot.holderImage.gameObject.SetActive(isActive);

            if (!isActive)
            {
                slot.holderImage.sprite = null;
                continue;
            }

            slot.holderImage.sprite = targets[i].Sprite;

            if (pickedPanelPrefab == null) continue;

            GameObject panelInstance = Instantiate(pickedPanelPrefab, slot.holderImage.transform);
            panelInstance.SetActive(false);
            spawnedPickedPanels.Add(panelInstance);
            pickedPanelByPoolId[targets[i].PoolId] = panelInstance;
        }
    }

    private void HandleMaletaPicked(Maleta maleta, bool countedAsWinner, int collectedCount, int targetCount)
    {
        if (!countedAsWinner || maleta == null) return;

        if (pickedPanelByPoolId.TryGetValue(maleta.PoolId, out GameObject pickedPanel) && pickedPanel != null)
            pickedPanel.SetActive(true);
    }

    private void ClearSpawnedPanels()
    {
        for (int i = 0; i < spawnedPickedPanels.Count; i++)
        {
            if (spawnedPickedPanels[i] != null)
                Destroy(spawnedPickedPanels[i]);
        }

        spawnedPickedPanels.Clear();
    }
}
