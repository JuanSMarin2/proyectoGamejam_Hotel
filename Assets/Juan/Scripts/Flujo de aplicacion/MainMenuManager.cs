using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class MainMenuManager : MonoBehaviour
{
    [Header("Register Panel")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private TMP_InputField registerNameInput;
    [SerializeField] private TMP_Text registerStatusText;

    [Header("Change User Panel")]
    [SerializeField] private GameObject changeUserPanel;
    [SerializeField] private TMP_InputField changeUserNameInput;
    [SerializeField] private TMP_Text changeUserStatusText;

    public void StoryMode()
    {
        var order = GameOrderManager.instance.GetSceneOrder();

        RoundData.instance.SetGameOrder(order);

        RoundData.instance.SetStoryMode(true);



       SceneManager.LoadScene("IntroScene");
    }

    public void InfiniteMode()
    {
        if (!CurrentUser.Instance.HasUserName)
        {
            OpenRegisterPanel();
            return;
        }

        StartInfiniteMode();
    }

    public void StartInfiniteMode()
    {
        var order = GameOrderManager.instance.GetSceneOrder();

        RoundData.instance.SetGameOrder(order);
        RoundData.instance.SetStoryMode(false);

        SceneManager.LoadScene("IntroScene");
    }

    public void ChangeUser()
    {
        OpenChangeUserPanel();
    }

    public void OpenRegisterPanel()
    {
        SetPanelActive(registerPanel, true);
        ClearStatus(registerStatusText);
        SetInputText(registerNameInput, string.Empty);
    }

    public void OpenChangeUserPanel()
    {
        SetPanelActive(changeUserPanel, true);
        ClearStatus(changeUserStatusText);
        SetInputText(changeUserNameInput, string.Empty);
    }

    public async void ConfirmRegisterUser()
    {
        await ConfirmUserNameAsync(registerNameInput, registerStatusText, true);
    }

    public async void ConfirmChangeUser()
    {
        await ConfirmUserNameAsync(changeUserNameInput, changeUserStatusText, false);
    }

    public void LoadShopScene(){
        SceneManager.LoadScene("Shop");
    }

    public void LoadLeaderboardScene(){
        SceneManager.LoadScene("Leaderboard");
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    private async System.Threading.Tasks.Task ConfirmUserNameAsync(TMP_InputField inputField, TMP_Text statusText, bool startInfiniteMode)
    {
        string userName = inputField != null ? inputField.text.Trim() : string.Empty;

        if (string.IsNullOrWhiteSpace(userName))
        {
            SetStatus(statusText, "Ingresa un nombre.");
            return;
        }

        if (userName.Length > 8)
        {
            SetStatus(statusText, "Maximo 8 caracteres.");
            return;
        }

        try
        {
            await CurrentUser.Instance.SetUserNameAsync(userName);
        }
        catch (Exception)
        {
            SetStatus(statusText, "No se pudo cambiar de usuario.");
            return;
        }

        if (startInfiniteMode)
        {
            SetPanelActive(registerPanel, false);
            ClearStatus(registerStatusText);
            StartInfiniteMode();
            return;
        }

        SetPanelActive(changeUserPanel, false);
        ClearStatus(changeUserStatusText);
    }

    private void SetPanelActive(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    private void SetInputText(TMP_InputField inputField, string value)
    {
        if (inputField != null)
        {
            inputField.text = value;
        }
    }

    private void SetStatus(TMP_Text statusText, string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void ClearStatus(TMP_Text statusText)
    {
        if (statusText != null)
        {
            statusText.text = string.Empty;
        }
    }
}