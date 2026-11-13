using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSpecificSound : MonoBehaviour
{
    [SerializeField] private SoundType soundType = SoundType.UiBoton;
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool autoPlayOnClick = true;

    [Header("Camera Flash")]
    [SerializeField] private CanvasGroup flashCanvasGroup;
    [SerializeField] private float flashPeakAlpha = 1f;
    [SerializeField] private float flashInDuration = 0.05f;
    [SerializeField] private float flashOutDuration = 0.35f;

    private Button cachedButton;
    private Coroutine flashCoroutine;

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

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
    }

    public void PlayConfiguredSound()
    {
        SoundManager.PlaySound(soundType, null, volume);
    }

    public void SetSoundType(SoundType newSoundType)
    {
        soundType = newSoundType;
    }

    /// <summary>
    /// Dispara el efecto de flash de camara utilizando el CanvasGroup asignado en el Inspector.
    /// </summary>
    public void TriggerCameraFlash()
    {
        TriggerCameraFlash(flashCanvasGroup, flashInDuration, flashOutDuration, flashPeakAlpha);
    }

    /// <summary>
    /// Dispara el efecto de flash de camara permitiendo pasar un CanvasGroup especifico y tiempos personalizados.
    /// Usa unscaledDeltaTime para funcionar incluso si Time.timeScale es 0 (por ejemplo durante la foto).
    /// </summary>
    public void TriggerCameraFlash(CanvasGroup targetCanvasGroup, float inDuration = 0.05f, float outDuration = 0.35f, float peakAlpha = 1f)
    {
        if (targetCanvasGroup == null)
        {
            Debug.LogWarning("UIButtonSpecificSound: No se asigno CanvasGroup para el flash.", this);
            return;
        }

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine = StartCoroutine(CameraFlashRoutine(targetCanvasGroup, inDuration, outDuration, peakAlpha));
    }

    private IEnumerator CameraFlashRoutine(CanvasGroup targetCanvasGroup, float inDuration, float outDuration, float peakAlpha)
    {
        // 1. Subida rápida al peak alpha (aparición instantánea/súbita típica de flash)
        if (inDuration > 0f)
        {
            float elapsedIn = 0f;
            float startAlpha = targetCanvasGroup.alpha;

            while (elapsedIn < inDuration)
            {
                elapsedIn += Time.unscaledDeltaTime;
                targetCanvasGroup.alpha = Mathf.Lerp(startAlpha, peakAlpha, elapsedIn / inDuration);
                yield return null;
            }
        }

        targetCanvasGroup.alpha = peakAlpha;

        // 2. Desvanecimiento suave (fade out)
        if (outDuration > 0f)
        {
            float elapsedOut = 0f;
            while (elapsedOut < outDuration)
            {
                elapsedOut += Time.unscaledDeltaTime;
                targetCanvasGroup.alpha = Mathf.Lerp(peakAlpha, 0f, elapsedOut / outDuration);
                yield return null;
            }
        }

        targetCanvasGroup.alpha = 0f;
        flashCoroutine = null;
    }
}
