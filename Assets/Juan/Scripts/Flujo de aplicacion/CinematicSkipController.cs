using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

/// <summary>
/// Allows the player to skip a playing cinematic after confirming with a second click/tap.
/// Attach it to any active object in a scene containing a PlayableDirector.
/// </summary>
public class CinematicSkipController : MonoBehaviour
{
    [Header("Scene transition")]
    [Tooltip("Leave empty to continue with the minigame order configured by GameOrderManager.")]
    [SerializeField] private string nextSceneName;

    [Header("Confirmation")]
    [SerializeField, Min(0.1f)] private float confirmationDuration = 3f;
    [SerializeField] private string confirmationMessage = "Pulsa una vez más para saltar";
    [Tooltip("Optional. If empty, the message is created on the active Canvas.")]
    [SerializeField] private TextMeshProUGUI confirmationText;

    private bool waitingForConfirmation;
    private float confirmationEndsAt;
    private bool sceneIsLoading;

    private void Start()
    {
        SetConfirmationVisible(false);
    }

    private void Update()
    {
        if (sceneIsLoading)
            return;

        PlayableDirector playingDirector = FindPlayingDirector();
        if (playingDirector == null)
        {
            CancelConfirmation();
            return;
        }

        if (waitingForConfirmation && Time.unscaledTime >= confirmationEndsAt)
            CancelConfirmation();

        if (!SkipClickPressed())
            return;

        if (!waitingForConfirmation)
        {
            waitingForConfirmation = true;
            confirmationEndsAt = Time.unscaledTime + confirmationDuration;
            SetConfirmationVisible(true);
            return;
        }

        SkipCinematic(playingDirector);
    }

    private static PlayableDirector FindPlayingDirector()
    {
        PlayableDirector[] directors = FindObjectsByType<PlayableDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (PlayableDirector director in directors)
        {
            if (director.isActiveAndEnabled && director.state == PlayState.Playing)
                return director;
        }

        return null;
    }

    private static bool SkipClickPressed()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
    }

    private void CancelConfirmation()
    {
        waitingForConfirmation = false;
        SetConfirmationVisible(false);
    }

    private void SkipCinematic(PlayableDirector director)
    {
        sceneIsLoading = true;
        director.Stop();
        CancelConfirmation();

        if (!string.IsNullOrWhiteSpace(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
            return;
        }

        if (RoundData.instance == null)
        {
            Debug.LogWarning("CinematicSkipController: RoundData was not found, so the GameOrderManager sequence cannot continue.", this);
            sceneIsLoading = false;
            return;
        }

        // RoundData stores the order received from GameOrderManager and knows the current
        // position, game mode and the appropriate end scene.
        RoundData.instance.NextMinigame();
    }

    private void SetConfirmationVisible(bool visible)
    {
        if (confirmationText == null && visible)
            CreateConfirmationText();

        if (confirmationText == null)
            return;

        confirmationText.text = confirmationMessage;
        confirmationText.gameObject.SetActive(visible);
    }

    private void CreateConfirmationText()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("CinematicSkipController: No Canvas found to display the skip message.", this);
            return;
        }

        GameObject textObject = new GameObject("Cinematic Skip Confirmation", typeof(RectTransform));
        textObject.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.12f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.12f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(700f, 70f);

        confirmationText = textObject.AddComponent<TextMeshProUGUI>();
        confirmationText.font = TMP_Settings.defaultFontAsset;
        confirmationText.alignment = TextAlignmentOptions.Center;
        confirmationText.fontSize = 28f;
        confirmationText.color = Color.white;
        confirmationText.raycastTarget = false;
    }
}
