using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIButtonSfxAutoBinder : MonoBehaviour
{
    [SerializeField] private float clickVolume = 1f;
    [SerializeField] private bool includeInactiveButtons = false;
    [SerializeField] private bool rescanRuntimeButtons = true;
    [SerializeField] private float rescanInterval = 0.5f;

    private readonly HashSet<Button> boundButtons = new HashSet<Button>();
    private float nextScanTime;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindAllButtonsInScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        boundButtons.Clear();
    }

    private void Update()
    {
        if (!rescanRuntimeButtons)
            return;

        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + Mathf.Max(0.05f, rescanInterval);
        BindAllButtonsInScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindAllButtonsInScene();
    }

    private void BindAllButtonsInScene()
    {
        CleanupNullButtons();

        Button[] buttons = FindObjectsOfType<Button>(includeInactiveButtons);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            if (boundButtons.Contains(button))
                continue;

            if (button.GetComponent<NoUiClickSfx>() != null)
                continue;

            if (button.GetComponent<UIButtonSpecificSound>() != null)
                continue;

            button.onClick.AddListener(HandleAnyButtonClick);
            boundButtons.Add(button);
        }
    }

    private void HandleAnyButtonClick()
    {
        SoundManager.PlayUiClick(clickVolume);
    }

    private void CleanupNullButtons()
    {
        if (boundButtons.Count == 0)
            return;

        List<Button> toRemove = null;
        foreach (Button b in boundButtons)
        {
            if (b != null)
                continue;

            if (toRemove == null)
                toRemove = new List<Button>();

            toRemove.Add(b);
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            boundButtons.Remove(toRemove[i]);
    }
}
