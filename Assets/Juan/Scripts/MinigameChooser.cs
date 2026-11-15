using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class MinigameChooser : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject minigameChooserPanel;

    private void Start()
    {
        // Asegura que el panel empiece desactivado
        SetPanelActive(false);
    }

    private void Update()
    {
        // Al presionar M se activa/desactiva el panel
        if (Input.GetKeyDown(KeyCode.M))
        {
            TogglePanel();
        }
    }

    public void TogglePanel()
    {
        if (minigameChooserPanel == null) return;
        SetPanelActive(!minigameChooserPanel.activeSelf);
    }

    /// <summary>
    /// Funcion publica para los botones de escoger minijuego.
    /// Recibe el nombre de la escena e inicia el modo historia
    /// solo con ese minijuego.
    /// </summary>
    public void ChoseMinigame(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("ChoseMinigame recibio un nombre de escena vacio.");
            return;
        }

        // La lista del modo historia contiene solo el minijuego elegido
        List<string> order = new List<string> { sceneName };

        RoundData.instance.SetGameOrder(order);
        RoundData.instance.SetStoryMode(false);

        SetPanelActive(false);

        SceneManager.LoadScene("IntroScene");
    }

    private void SetPanelActive(bool isActive)
    {
        if (minigameChooserPanel != null)
        {
            minigameChooserPanel.SetActive(isActive);
        }
    }
}