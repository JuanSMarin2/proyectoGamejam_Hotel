using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LeaderboardManager : MonoBehaviour
{
    [Header("Leaderboard")]
    [SerializeField] private string leaderboardId = "PerfectTouristLeaderboard";

    [Header("Text References")]
    [SerializeField] private TextMeshProUGUI firstPlaceText;
    [SerializeField] private TextMeshProUGUI secondPlaceText;
    [SerializeField] private TextMeshProUGUI thirdPlaceText;
    [SerializeField] private TextMeshProUGUI fourthPlaceText;
    [SerializeField] private TextMeshProUGUI fifthPlaceText;

    [Header("Colors")]
    [SerializeField] private Color userTextColor = Color.yellow;
    [SerializeField] private Color othersTextColor = Color.white;

    private void Start()
    {
        _ = RefreshLeaderboardAsync();
    }

    public async Task RefreshLeaderboardAsync()
    {
        ResetTexts();

        try
        {
            await CurrentUser.Instance.EnsureServicesReadyAsync();

            string currentPlayerId = AuthenticationService.Instance.PlayerId;
            string currentUserName = CurrentUser.Instance.CurrentUserName;

            LeaderboardScoresPage topPage = await LeaderboardsService.Instance.GetScoresAsync(
                leaderboardId,
                new GetScoresOptions { Offset = 0, Limit = 5 });

            List<LeaderboardEntry> topEntries = topPage?.Results ?? new List<LeaderboardEntry>();

            int currentIndexInTop = FindIndexByPlayerId(topEntries, currentPlayerId);

            SetTopTexts(topEntries, currentIndexInTop, currentUserName);

            if (currentIndexInTop >= 0 && currentIndexInTop <= 3)
            {
                LeaderboardEntry fifthEntry = topEntries.Count > 4 ? topEntries[4] : null;
                SetTextForEntry(fifthPlaceText, fifthEntry, false, currentUserName);
                return;
            }

            await SetCurrentUserAsFifthAsync(currentPlayerId, currentUserName);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("LeaderboardManager: no se pudo cargar el leaderboard. " + exception.Message);
            SetFallbackErrorText();
        }
    }

    private void SetTopTexts(List<LeaderboardEntry> entries, int currentIndexInTop, string currentUserName)
    {
        TextMeshProUGUI[] topTexts = { firstPlaceText, secondPlaceText, thirdPlaceText, fourthPlaceText };

        for (int i = 0; i < topTexts.Length; i++)
        {
            bool isCurrentUser = i == currentIndexInTop;
            LeaderboardEntry entry = entries.Count > i ? entries[i] : null;
            SetTextForEntry(topTexts[i], entry, isCurrentUser, currentUserName);
        }
    }

    private async Task SetCurrentUserAsFifthAsync(string currentPlayerId, string currentUserName)
    {
        LeaderboardEntry currentEntry = null;

        try
        {
            currentEntry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
        }
        catch (Exception exception)
        {
            Debug.Log("LeaderboardManager: usuario sin score en leaderboard o no disponible. " + exception.Message);
        }

        bool hasCurrentEntry = currentEntry != null
                               && !string.IsNullOrWhiteSpace(currentEntry.PlayerId)
                               && string.Equals(currentEntry.PlayerId, currentPlayerId, StringComparison.Ordinal);

        if (!hasCurrentEntry)
        {
            SetTextForEntry(fifthPlaceText, null, false, currentUserName);
            return;
        }

        string userDisplayName = ResolveDisplayName(currentEntry, true, currentUserName);
        int score = Mathf.RoundToInt((float)currentEntry.Score);
        string rankLabel = (currentEntry.Rank + 1).ToString();

        SetFormattedText(fifthPlaceText, rankLabel, userDisplayName, score, true);
    }

    private void SetTextForEntry(TextMeshProUGUI textField, LeaderboardEntry entry, bool isCurrentUser, string currentUserName)
    {
        if (textField == null)
            return;

        if (entry == null)
        {
            textField.text = "-";
            textField.color = othersTextColor;
            return;
        }

        string rankLabel = (entry.Rank + 1).ToString();
        string displayName = ResolveDisplayName(entry, isCurrentUser, currentUserName);
        int score = Mathf.RoundToInt((float)entry.Score);

        SetFormattedText(textField, rankLabel, displayName, score, isCurrentUser);
    }

    private void SetFormattedText(TextMeshProUGUI textField, string rankLabel, string name, int score, bool isCurrentUser)
    {
        textField.text = rankLabel + ". " + name + " - Puntos: " + score;
        textField.color = isCurrentUser ? userTextColor : othersTextColor;
    }

    private string ResolveDisplayName(LeaderboardEntry entry, bool isCurrentUser, string currentUserName)
    {
        if (isCurrentUser && !string.IsNullOrWhiteSpace(currentUserName))
            return currentUserName;

        if (!string.IsNullOrWhiteSpace(entry.PlayerName))
            return entry.PlayerName;

        return "Jugador";
    }

    private int FindIndexByPlayerId(List<LeaderboardEntry> entries, string playerId)
    {
        if (entries == null || string.IsNullOrWhiteSpace(playerId))
            return -1;

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            if (entry == null)
                continue;

            if (string.Equals(entry.PlayerId, playerId, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private void ResetTexts()
    {
        SetPending(firstPlaceText);
        SetPending(secondPlaceText);
        SetPending(thirdPlaceText);
        SetPending(fourthPlaceText);
        SetPending(fifthPlaceText);
    }

    private void SetPending(TextMeshProUGUI textField)
    {
        if (textField == null)
            return;

        textField.text = "...";
        textField.color = othersTextColor;
    }

    private void SetFallbackErrorText()
    {
        SetError(firstPlaceText);
        SetError(secondPlaceText);
        SetError(thirdPlaceText);
        SetError(fourthPlaceText);
        SetError(fifthPlaceText);
    }

    private void SetError(TextMeshProUGUI textField)
    {
        if (textField == null)
            return;

        textField.text = "No disponible";
        textField.color = othersTextColor;
    }
      public void MainMenuScene(){
        SceneManager.LoadScene("MainMenu");
    }
}