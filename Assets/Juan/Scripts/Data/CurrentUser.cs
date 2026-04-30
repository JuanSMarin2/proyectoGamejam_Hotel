using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Leaderboards;
using UnityEngine;

public class CurrentUser : MonoBehaviour
{
    private static CurrentUser instance;

    public static CurrentUser Instance
    {
        get
        {
            if (instance != null)
                return instance;

            instance = FindFirstObjectByType<CurrentUser>();

            if (instance != null)
                return instance;

            GameObject currentUserObject = new GameObject(nameof(CurrentUser));
            instance = currentUserObject.AddComponent<CurrentUser>();
            return instance;
        }
    }

    [SerializeField] private string currentUserName = string.Empty;

    private Task servicesInitializationTask;
    private bool servicesReady;

    public string CurrentUserName => currentUserName;

    public bool HasUserName => !string.IsNullOrWhiteSpace(currentUserName);

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }

        if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public async Task SetUserNameAsync(string userName)
    {
        currentUserName = userName == null ? string.Empty : userName.Trim();

        if (!HasUserName)
            return;

        await EnsureServicesReadyAsync();
        await EnsureSignedInWithCurrentUserProfileAsync();
        await SyncAuthenticationPlayerNameAsync();
    }

    public async Task EnsureServicesReadyAsync()
    {
        servicesInitializationTask ??= InitializeServicesAsync();

        try
        {
            await servicesInitializationTask;

            if (HasUserName)
            {
                await EnsureSignedInWithCurrentUserProfileAsync();
                await SyncAuthenticationPlayerNameAsync();
            }
        }
        catch
        {
            servicesInitializationTask = null;
            throw;
        }
    }

    public async Task SubmitInfiniteModeScoreAsync(int completedMinigames)
    {
        if (completedMinigames < 0)
            completedMinigames = 0;

        await EnsureServicesReadyAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            return;

        await LeaderboardsService.Instance.AddPlayerScoreAsync("PerfectTouristLeaderboard", completedMinigames);
    }

    private async Task InitializeServicesAsync()
    {
        if (servicesReady)
            return;

        await UnityServices.InitializeAsync();

        servicesReady = true;
    }

    private async Task EnsureSignedInWithCurrentUserProfileAsync()
    {
        string targetProfile = BuildProfileName(currentUserName);

        if (AuthenticationService.Instance.IsSignedIn)
        {
            if (string.Equals(AuthenticationService.Instance.Profile, targetProfile, StringComparison.Ordinal))
                return;

            AuthenticationService.Instance.SignOut();
        }

        if (!string.Equals(AuthenticationService.Instance.Profile, targetProfile, StringComparison.Ordinal))
        {
            AuthenticationService.Instance.SwitchProfile(targetProfile);
        }

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async Task SyncAuthenticationPlayerNameAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn || !HasUserName)
            return;

        if (string.Equals(AuthenticationService.Instance.PlayerName, currentUserName, StringComparison.Ordinal))
            return;

        await AuthenticationService.Instance.UpdatePlayerNameAsync(currentUserName);
    }

    private string BuildProfileName(string userName)
    {
        string trimmed = userName == null ? string.Empty : userName.Trim().ToLowerInvariant();
        return "hotel_" + trimmed;
    }
}