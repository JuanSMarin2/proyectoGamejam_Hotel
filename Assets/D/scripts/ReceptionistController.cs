using UnityEngine;
using UnityEngine.U2D.Animation;

public class ReceptionistController : MonoBehaviour
{
    #region States
    public enum ReceptionistState
    {
        Working,
        Mixed,
        Playing,
        Angry
    }
    #endregion

    #region References
    [SerializeField] private SpriteResolver headSpriteResolver;
    [SerializeField] private HeadFixer headFixer;
    [SerializeField] private SpriteRenderer bubbleSpriteRendererA;
    [SerializeField] private SpriteRenderer bubbleSpriteRendererB;
    [SerializeField] private SpriteRenderer bubbleContentRendererA;
    [SerializeField] private SpriteRenderer bubbleContentRendererB;
    [SerializeField] private Sprite[] workBubbleSprites = new Sprite[2]; // A/B
    [SerializeField] private Sprite[] playingBubbleSprites = new Sprite[2]; // A/B (cartas)
    [SerializeField] private ParticleSystem angryParticles;
    [SerializeField] private float workingMinTime = 1f;
    [SerializeField] private float workingMaxTime = 3f;
    [SerializeField] private Color32 workBubbleColor = new Color32(0x79, 0xD7, 0x51, 0xFF);
    [SerializeField] private Color32 playingBubbleColor = new Color32(0xF8, 0x47, 0x47, 0xFF);
    #endregion

    #region State Management
    private ReceptionistState currentState = ReceptionistState.Working;
    private bool hasLastHeadFixerFace;
    private HeadFixer.Face lastHeadFixerFace;
    #endregion

    #region Input & Gameplay
    private int clickCounter = 0;
    private int requiredClicksMixed;
    private int requiredClicksPlaying;
    private float playingTimer = 0f;
    private float playingTimeLimit;
    private float workingTimer = 0f;
    private float workingTimeLimit = 0f;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        SetState(ReceptionistState.Working);
    }

    private void Update()
    {
        HandleInput();

        if (currentState == ReceptionistState.Playing)
        {
            UpdatePlayingTimer();
        }
        else if (currentState == ReceptionistState.Working)
        {
            UpdateWorkingTimer();
        }
    }
    #endregion

    #region Input Handling
    private void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnSpacePressed();
        }
    }

    private void OnSpacePressed()
    {
        switch (currentState)
        {
            case ReceptionistState.Working:
                WorkingStateInput();
                break;
            case ReceptionistState.Mixed:
                MixedStateInput();
                break;
            case ReceptionistState.Playing:
                PlayingStateInput();
                break;
            case ReceptionistState.Angry:
                // No hacer nada en estado Angry
                break;
        }
    }

    private void WorkingStateInput()
    {
        SetState(ReceptionistState.Angry);
        ResultManager.instance.LoseMinigame(1);
    }

    private void MixedStateInput()
    {
        clickCounter++;

        if (clickCounter >= requiredClicksMixed)
        {
            // Transición a Working
            SetState(ReceptionistState.Working);
        }
        else if (clickCounter > 3)
        {
            // Superar 3 clicks = perder
            SetState(ReceptionistState.Angry);
            ResultManager.instance.LoseMinigame();
        }
    }

    private void PlayingStateInput()
    {
        clickCounter++;

        if (clickCounter >= requiredClicksPlaying)
        {
            // Pasar primero a Mixed
            SetState(ReceptionistState.Mixed);
        }
    }
    #endregion

    #region State Management
    public void SetState(ReceptionistState newState)
    {
        currentState = newState;
        clickCounter = 0;
        playingTimer = 0f;
        workingTimer = 0f;

        switch (currentState)
        {
            case ReceptionistState.Working:
                InitializeWorkingState();
                break;
            case ReceptionistState.Mixed:
                InitializeMixedState();
                break;
            case ReceptionistState.Playing:
                InitializePlayingState();
                break;
            case ReceptionistState.Angry:
                InitializeAngryState();
                break;
        }

        UpdateFace();
        UpdateBubble();
        UpdateAngryParticles();
    }

    private void InitializeWorkingState()
    {
        // El recepcionista trabaja un rato y luego cambia a Mixed o Playing.
        workingTimeLimit = Random.Range(workingMinTime, workingMaxTime);
    }

    private void InitializeMixedState()
    {
        // Generar número aleatorio entre 1 y 3
        requiredClicksMixed = Random.Range(1, 4);
    }

    private void InitializePlayingState()
    {
        // Generar número aleatorio entre 5 y 10
        requiredClicksPlaying = Random.Range(5, 8);
        playingTimeLimit = 3.5f;
    }

    private void InitializeAngryState()
    {
        // Estado de pérdida, solo mostramos expresión enojada
    }
    #endregion

    #region Visual Updates
    private void UpdateFace()
    {
        if (headFixer != null)
        {
            HeadFixer.Face targetFace = GetHeadFixerFace();
            PlayHeadFixerMoodSoundIfNeeded(targetFace);
            headFixer.SwapFace(targetFace);
            return;
        }

        if (headSpriteResolver == null)
        {
            return;
        }

        string label = GetFaceLabel();
        headSpriteResolver.SetCategoryAndLabel("Head", label);
    }

    private void PlayHeadFixerMoodSoundIfNeeded(HeadFixer.Face targetFace)
    {
        if (!hasLastHeadFixerFace || targetFace != lastHeadFixerFace)
        {
            switch (targetFace)
            {
                case HeadFixer.Face.Angry:
                    SoundManager.PlaySound(SoundType.PjEnojado);
                    break;
                case HeadFixer.Face.Sad:
                    SoundManager.PlaySound(SoundType.PjTriste);
                    break;
                case HeadFixer.Face.Happy:
                    SoundManager.PlaySound(SoundType.PjRiendo);
                    break;
            }

            lastHeadFixerFace = targetFace;
            hasLastHeadFixerFace = true;
        }
    }

    private HeadFixer.Face GetHeadFixerFace()
    {
        switch (currentState)
        {
            case ReceptionistState.Working:
                return HeadFixer.Face.Happy;
            case ReceptionistState.Mixed:
                return HeadFixer.Face.Angry;
            case ReceptionistState.Playing:
                return HeadFixer.Face.Angry;
            case ReceptionistState.Angry:
                return HeadFixer.Face.Sad;
            default:
                return HeadFixer.Face.Neutral;
        }
    }

    private string GetFaceLabel()
    {
        switch (currentState)
        {
            case ReceptionistState.Working:
                return "Triste";
            case ReceptionistState.Mixed:
                return "Neutral";
            case ReceptionistState.Playing:
                return "Feliz";
            case ReceptionistState.Angry:
                return "Enojado";
            default:
                return "Neutral";
        }
    }

    private void UpdateBubble()
    {
        switch (currentState)
        {
            case ReceptionistState.Working:
                ShowWorkingBubbles();
                break;
            case ReceptionistState.Playing:
                ShowPlayingBubbles();
                break;
            case ReceptionistState.Mixed:
                // Opcional: mezcla visual o alternancia
                ShowMixedBubbles();
                break;
            case ReceptionistState.Angry:
                HideBubble();
                break;
        }
    }

    private void UpdateAngryParticles()
    {
        if (angryParticles == null)
        {
            return;
        }

        if (currentState == ReceptionistState.Working || currentState == ReceptionistState.Mixed)
        {
            if (!angryParticles.gameObject.activeSelf)
            {
                angryParticles.gameObject.SetActive(true);
            }
            angryParticles.Play();
        }
        else
        {
            angryParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (angryParticles.gameObject.activeSelf)
            {
                angryParticles.gameObject.SetActive(false);
            }
        }
    }

    private void ShowWorkingBubbles()
    {
        SetBubbleSprite(bubbleContentRendererA, workBubbleSprites);
        SetBubbleSprite(bubbleContentRendererB, workBubbleSprites);
        SetBubbleColor(bubbleSpriteRendererA, workBubbleColor);
        SetBubbleColor(bubbleSpriteRendererB, workBubbleColor);
    }

    private void ShowPlayingBubbles()
    {
        SetBubbleSprite(bubbleContentRendererA, playingBubbleSprites);
        SetBubbleSprite(bubbleContentRendererB, playingBubbleSprites);
        SetBubbleColor(bubbleSpriteRendererA, playingBubbleColor);
        SetBubbleColor(bubbleSpriteRendererB, playingBubbleColor);
    }

    private void ShowMixedBubbles()
    {
        SetBubbleSprite(bubbleContentRendererA, workBubbleSprites);
        SetBubbleSprite(bubbleContentRendererB, playingBubbleSprites);
        SetBubbleColor(bubbleSpriteRendererA, workBubbleColor);
        SetBubbleColor(bubbleSpriteRendererB, playingBubbleColor);
    }

    private void HideBubble()
    {
        if (bubbleSpriteRendererA != null)
        {
            bubbleSpriteRendererA.enabled = false;
        }
        if (bubbleSpriteRendererB != null)
        {
            bubbleSpriteRendererB.enabled = false;
        }
        if (bubbleContentRendererA != null)
        {
            bubbleContentRendererA.enabled = false;
        }
        if (bubbleContentRendererB != null)
        {
            bubbleContentRendererB.enabled = false;
        }
    }

    private void SetBubbleSprite(SpriteRenderer target, Sprite[] options)
    {
        if (target == null || options == null || options.Length == 0)
        {
            return;
        }
        int randomIndex = Random.Range(0, options.Length);
        target.sprite = options[randomIndex];
        target.enabled = true;
    }

    private void SetBubbleColor(SpriteRenderer target, Color32 color)
    {
        if (target == null)
        {
            return;
        }

        target.color = color;
        target.enabled = true;
    }
    #endregion

    #region Timer Management
    private void UpdatePlayingTimer()
    {
        if (playingTimeLimit <= 0f)
        {
            return;
        }
        playingTimer += Time.deltaTime;

        // Verificar si se agotó el tiempo
        if (playingTimer >= playingTimeLimit)
        {
            SetState(ReceptionistState.Angry);
            ResultManager.instance.LoseMinigame(0);
        }
    }

    private void UpdateWorkingTimer()
    {
        if (workingTimeLimit <= 0f)
        {
            return;
        }
        workingTimer += Time.deltaTime;

        if (workingTimer >= workingTimeLimit)
        {
            ReceptionistState nextState = Random.Range(0, 2) == 0
                ? ReceptionistState.Mixed
                : ReceptionistState.Playing;
            SetState(nextState);
        }
    }

    public float GetPlayingTimerProgress()
    {
        return playingTimer / playingTimeLimit;
    }
    #endregion
}
