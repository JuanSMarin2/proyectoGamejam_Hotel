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
        string previousUserName = currentUserName;
        currentUserName = userName == null ? string.Empty : userName.Trim();

        if (!HasUserName)
            return;

        try
        {
            await EnsureServicesReadyAsync();
            await EnsureSignedInWithCurrentUserProfileAsync();
            await SyncAuthenticationPlayerNameAsync();
        }
        catch
        {
            // ROLLBACK: si el registro falla, el nombre NO queda guardado.
            // Sin esto, HasUserName quedaba en true y el juego trataba al
            // usuario como registrado aunque el sign-in hubiera fallado.
            currentUserName = previousUserName;
            throw;
        }
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

    /// <summary>
    /// Envia el puntaje del modo infinito al leaderboard guardando SOLO el mejor
    /// resultado (new best). Si el puntaje nuevo no supera al registrado, no se envia.
    ///
    /// Nota: esta version del SDK solo tiene AddPlayerScoreAsync, que SUMA al score
    /// existente. Por eso se envia solo la DIFERENCIA entre el nuevo mejor puntaje
    /// y el registrado, de modo que el total quede igual al new best.
    /// </summary>
    public async Task SubmitInfiniteModeScoreAsync(int completedMinigames)
    {
        if (completedMinigames < 0)
            completedMinigames = 0;

        await EnsureServicesReadyAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            return;

        double currentBest = 0;
        bool hasPreviousScore = false;

        try
        {
            var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync("PerfectTouristLeaderboard");

            if (entry != null)
            {
                currentBest = entry.Score;
                hasPreviousScore = true;
            }
        }
        catch (Exception)
        {
            // El jugador aun no tiene score en el leaderboard
            hasPreviousScore = false;
        }

        // Solo se envia si es un nuevo mejor puntaje
        if (hasPreviousScore && completedMinigames <= currentBest)
            return;

        // AddPlayerScoreAsync suma: enviamos solo la diferencia para que
        // el total quede exactamente en el nuevo mejor puntaje
        double delta = hasPreviousScore
            ? completedMinigames - currentBest
            : completedMinigames;

        await LeaderboardsService.Instance.AddPlayerScoreAsync("PerfectTouristLeaderboard", delta);
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