using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfigPanel : MonoBehaviour
{
    [Header("Sliders")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [Header("Value Text")]
    [SerializeField] private TMP_Text sfxValueText;
    [SerializeField] private TMP_Text musicValueText;

    [Header("Mute Icons")]
    [SerializeField] private Image sfxIconImage;
    [SerializeField] private Image musicIconImage;

    [Header("Icon Sprites")]
    [SerializeField] private Sprite unmutedSprite;
    [SerializeField] private Sprite mutedSprite;

    private const float MutedThreshold = 0.0001f;

    private void OnEnable()
    {
        BindSliderEvents(true);
        PullValuesFromAudioSystem();
        RefreshUI();
    }

    private void OnDisable()
    {
        BindSliderEvents(false);
    }

    private void BindSliderEvents(bool bind)
    {
        if (sfxSlider != null)
        {
            if (bind) sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            else sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        }

        if (musicSlider != null)
        {
            if (bind) musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            else musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
        }
    }

    private void PullValuesFromAudioSystem()
    {
        float sfx = SoundManager.GetSfxVolume();
        float music = SoundManager.GetMusicVolume();

        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(sfx);

        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(music);
    }

    private void OnSfxSliderChanged(float value)
    {
        SoundManager.SetSfxVolume(value);
        RefreshSfxUI(value);
    }

    private void OnMusicSliderChanged(float value)
    {
        SoundManager.SetMusicVolume(value);
        RefreshMusicUI(value);
    }

    public void ToggleSfxMute()
    {
        float current = SoundManager.GetSfxVolume();

        if (current <= MutedThreshold)
            SoundManager.UnmuteSfxRestore();
        else
            SoundManager.MuteSfx();

        float newValue = SoundManager.GetSfxVolume();
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(newValue);

        RefreshSfxUI(newValue);
    }

    public void ToggleMusicMute()
    {
        float current = SoundManager.GetMusicVolume();

        if (current <= MutedThreshold)
            SoundManager.UnmuteMusicRestore();
        else
            SoundManager.MuteMusic();

        float newValue = SoundManager.GetMusicVolume();
        if (musicSlider != null)
            musicSlider.SetValueWithoutNotify(newValue);

        RefreshMusicUI(newValue);
    }

    private void RefreshUI()
    {
        RefreshSfxUI(SoundManager.GetSfxVolume());
        RefreshMusicUI(SoundManager.GetMusicVolume());
    }

    private void RefreshSfxUI(float value)
    {
        if (sfxValueText != null)
            sfxValueText.text = ToPercentText(value);

        if (sfxIconImage != null)
            sfxIconImage.sprite = value <= MutedThreshold ? mutedSprite : unmutedSprite;
    }

    private void RefreshMusicUI(float value)
    {
        if (musicValueText != null)
            musicValueText.text = ToPercentText(value);

        if (musicIconImage != null)
            musicIconImage.sprite = value <= MutedThreshold ? mutedSprite : unmutedSprite;
    }

    private static string ToPercentText(float value)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        return percent + "%";
    }
}
