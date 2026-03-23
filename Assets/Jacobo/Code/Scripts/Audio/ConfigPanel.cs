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

    [Header("Icon Sprites (SFX)")]
    [SerializeField] private Sprite sfxUnmutedSprite;
    [SerializeField] private Sprite sfxMutedSprite;

    [Header("Icon Sprites (Music)")]
    [SerializeField] private Sprite musicUnmutedSprite;
    [SerializeField] private Sprite musicMutedSprite;

    [Header("Legacy Shared Fallback")]
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
            UnmuteSfx();
        else
            MuteSfx();
    }

    public void ToggleMusicMute()
    {
        float current = SoundManager.GetMusicVolume();

        if (current <= MutedThreshold)
            UnmuteMusic();
        else
            MuteMusic();
    }

    // Explicit channel controls (useful for separate buttons/actions)
    public void MuteSfx()
    {
        SoundManager.MuteSfx();
        SyncSfxUIFromSystem();
    }

    public void UnmuteSfx()
    {
        SoundManager.UnmuteSfxRestore();
        SyncSfxUIFromSystem();
    }

    public void MuteMusic()
    {
        SoundManager.MuteMusic();
        SyncMusicUIFromSystem();
    }

    public void UnmuteMusic()
    {
        SoundManager.UnmuteMusicRestore();
        SyncMusicUIFromSystem();
    }

    private void SyncSfxUIFromSystem()
    {
        float newValue = SoundManager.GetSfxVolume();
        if (sfxSlider != null)
            sfxSlider.SetValueWithoutNotify(newValue);

        RefreshSfxUI(newValue);
    }

    private void SyncMusicUIFromSystem()
    {
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
        {
            Sprite muted = sfxMutedSprite != null ? sfxMutedSprite : mutedSprite;
            Sprite unmuted = sfxUnmutedSprite != null ? sfxUnmutedSprite : unmutedSprite;
            sfxIconImage.sprite = value <= MutedThreshold ? muted : unmuted;
        }
    }

    private void RefreshMusicUI(float value)
    {
        if (musicValueText != null)
            musicValueText.text = ToPercentText(value);

        if (musicIconImage != null)
        {
            Sprite muted = musicMutedSprite != null ? musicMutedSprite : mutedSprite;
            Sprite unmuted = musicUnmutedSprite != null ? musicUnmutedSprite : unmutedSprite;
            musicIconImage.sprite = value <= MutedThreshold ? muted : unmuted;
        }
    }

    private static string ToPercentText(float value)
    {
        int percent = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f);
        return percent + "%";
    }
}
