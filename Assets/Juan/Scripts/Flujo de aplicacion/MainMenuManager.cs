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

    [Header("User Flow")]
    [Tooltip("Si esta activo, el modo infinito SIEMPRE pide usuario (reemplaza al anterior) aunque ya haya uno registrado")]
    [SerializeField] private bool changeUserEveryTime = false;

    // Bloquea segundas solicitudes mientras una confirmacion esta en curso.
    // Solo se libera cuando la solicitud termina (error o exito).
    private bool isConfirmingUser;

    public void StoryMode()
    {
        var order = GameOrderManager.instance.GetSceneOrder();

        RoundData.instance.SetGameOrder(order);

        RoundData.instance.SetStoryMode(true);



       SceneManager.LoadScene("IntroScene");
    }

    public void InfiniteMode()
    {
        // Con changeUserEveryTime activo, SIEMPRE se pide usuario:
        // el nuevo registro reemplaza al anterior (mismo flujo de registro)
        if (changeUserEveryTime || !CurrentUser.Instance.HasUserName)
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
        // Si ya hay una solicitud en curso, ignorar esta llamada
        if (isConfirmingUser)
            return;

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

        isConfirmingUser = true;

        try
        {
            SetStatus(statusText, "Registrando...");
            await CurrentUser.Instance.SetUserNameAsync(userName);
        }
        catch (Exception exception)
        {
            // Log del error real para diagnosticar fallos intermitentes de sign-in
            Debug.LogWarning("MainMenuManager: fallo el registro de usuario. " + exception);
            SetStatus(statusText, "No se pudo cambiar de usuario.");
            return;
        }
        finally
        {
            // Siempre se libera el estado, haya error o exito.
            // Si hubo exito con cambio de escena, este objeto se destruye de todos modos.
            isConfirmingUser = false;
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