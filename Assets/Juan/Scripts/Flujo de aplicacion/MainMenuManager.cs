using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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

    // Input que debe recibir el foco en el proximo Update (al abrir un panel)
    private TMP_InputField pendingFocusInput;

    // Frame en el que un input dejo de editarse. Evita que el mismo Enter
    // que cierra la edicion la vuelva a abrir en ese frame.
    private int lastEndEditFrame = -1;

    private void Awake()
    {
        SetupNameInput(registerNameInput);
        SetupNameInput(changeUserNameInput);

        // Enter mientras se escribe equivale a dar click al boton de confirmar
        if (registerNameInput != null)
            registerNameInput.onSubmit.AddListener(OnRegisterNameSubmit);

        if (changeUserNameInput != null)
            changeUserNameInput.onSubmit.AddListener(OnChangeUserNameSubmit);
    }

    private void OnDestroy()
    {
        if (registerNameInput != null)
        {
            registerNameInput.onEndEdit.RemoveListener(OnNameInputEndEdit);
            registerNameInput.onSubmit.RemoveListener(OnRegisterNameSubmit);
        }

        if (changeUserNameInput != null)
        {
            changeUserNameInput.onEndEdit.RemoveListener(OnNameInputEndEdit);
            changeUserNameInput.onSubmit.RemoveListener(OnChangeUserNameSubmit);
        }
    }

    private void OnRegisterNameSubmit(string _)
    {
        ConfirmRegisterUser();
    }

    private void OnChangeUserNameSubmit(string _)
    {
        ConfirmChangeUser();
    }

    private void Update()
    {
        // Foco automatico al abrir el panel: se escribe sin dar click
        if (pendingFocusInput != null)
        {
            if (pendingFocusInput.gameObject.activeInHierarchy)
            {
                FocusInput(pendingFocusInput);
                pendingFocusInput = null;
            }

            return;
        }

        // Enter vuelve a entrar al input si se habia dejado de escribir
        if (!Input.GetKeyDown(KeyCode.Return) && !Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        TMP_InputField openInput = GetOpenNameInput();

        if (openInput == null || openInput.isFocused || Time.frameCount == lastEndEditFrame)
            return;

        FocusInput(openInput);
    }

    private void SetupNameInput(TMP_InputField inputField)
    {
        if (inputField == null)
            return;

        // Al volver a entrar el cursor queda al final, sin seleccionar todo el texto
        inputField.onFocusSelectAll = false;
        inputField.onEndEdit.AddListener(OnNameInputEndEdit);
    }

    private void OnNameInputEndEdit(string _)
    {
        lastEndEditFrame = Time.frameCount;
    }

    private TMP_InputField GetOpenNameInput()
    {
        if (registerNameInput != null && registerNameInput.gameObject.activeInHierarchy)
            return registerNameInput;

        if (changeUserNameInput != null && changeUserNameInput.gameObject.activeInHierarchy)
            return changeUserNameInput;

        return null;
    }

    private void FocusInput(TMP_InputField inputField)
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(inputField.gameObject);

        inputField.ActivateInputField();
        inputField.MoveTextEnd(false);
    }

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
        SetStatus(registerStatusText, "Ingresa Tu nombre:");
        SetInputText(registerNameInput, string.Empty);
        pendingFocusInput = registerNameInput;
    }

    public void OpenChangeUserPanel()
    {
        SetPanelActive(changeUserPanel, true);
        SetStatus(changeUserStatusText, "Ingresa Tu nombre:");
        SetInputText(changeUserNameInput, string.Empty);
        pendingFocusInput = changeUserNameInput;
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
    if (Application.platform == RuntimePlatform.WebGLPlayer)
        return;

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
            pendingFocusInput = inputField; // sigue escribiendo sin dar click
            return;
        }

        if (userName.Length > 8)
        {
            SetStatusWithColor(statusText, "Maximo 8 caracteres.", "B64636");
            pendingFocusInput = inputField;
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
            pendingFocusInput = inputField;
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

    private void SetStatusWithColor(TMP_Text statusText, string message, string hexColor)
    {
        if (statusText != null)
        {
            statusText.text = message;
            if (ColorUtility.TryParseHtmlString("#" + hexColor, out Color color))
            {
                statusText.color = color;
            }
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