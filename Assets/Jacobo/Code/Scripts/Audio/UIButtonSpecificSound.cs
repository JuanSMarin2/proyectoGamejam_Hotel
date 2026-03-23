using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSpecificSound : MonoBehaviour
{
    [SerializeField] private SoundType soundType = SoundType.UiBoton;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool autoPlayOnClick = true;

    private Button cachedButton;

    private void Awake()
    {
        cachedButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (!autoPlayOnClick)
            return;

        if (cachedButton == null)
            cachedButton = GetComponent<Button>();

        if (cachedButton != null)
            cachedButton.onClick.AddListener(PlayConfiguredSound);
    }

    private void OnDisable()
    {
        if (cachedButton != null)
            cachedButton.onClick.RemoveListener(PlayConfiguredSound);
    }

    public void PlayConfiguredSound()
    {
        SoundManager.PlaySound(soundType, null, volume);
    }

    public void SetSoundType(SoundType newSoundType)
    {
        soundType = newSoundType;
    }
}
