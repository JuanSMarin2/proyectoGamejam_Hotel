using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minijuego del recepcionista (CheckIn).
/// El handle del slider sube y baja solo. Se eligen al azar una o dos areas verdes
/// y el jugador debe detener el handle (Espacio o click) dentro de ellas.
/// - Acierta la ultima area verde: gana.
/// - Presiona en zona roja: pierde.
/// - Se acaba el tiempo sin terminar: pierde.
/// - Al ganar, primero se reproduce la animacion de campana y luego la secuencia de victoria.
/// La dificultad sale de MinigameManager.instance.Speed: a mas velocidad, area mas pequeña y handle mas rapido. Cuando el area baja del
/// umbral (35% del slider) pasan a ser dos areas que se aciertan una tras otra.
/// </summary>
public class ReceptionistBalanceController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider balanceSlider;

    [Header("Handle segun skin")]
    [Tooltip("Imagen del handle del slider. Si se deja vacia se usa la del Handle Rect del slider")]
    [SerializeField] private Image handleImage;
    [Tooltip("Sprite del handle cuando el personaje tiene la ropa Base (tambien se usa con cualquier otra skin)")]
    [SerializeField] private Sprite handleSpriteBase;
    [Tooltip("Sprite del handle cuando el personaje tiene la ropa Amarillo")]
    [SerializeField] private Sprite handleSpriteAmarillo;
    [Tooltip("Label de la skin amarilla en ShopDatabase")]
    [SerializeField] private string amarilloLabel = "Amarillo";

    [Header("Zonas - Visual")]
    [SerializeField] private Color areaGreenColor = new Color32(0x91, 0xFE, 0x89, 0xFF);
    [SerializeField] private Color areaRedColor = new Color32(0xFE, 0x80, 0x72, 0xFF);
    [Tooltip("Imagen centrada dentro de cada area verde (donde SI se debe detener)")]
    [SerializeField] private Sprite greenIcon;
    [Tooltip("Imagen centrada dentro de cada area roja (donde NO se debe detener)")]
    [SerializeField] private Sprite redIcon;
    [Tooltip("Tamaño del icono respecto al grosor de la barra (1 = ocupa todo el grosor)")]
    [SerializeField, Range(0.1f, 3f)] private float iconScale = 0.8f;

    [Header("Dificultad")]
    [Tooltip("Velocidad de MinigameManager con la que el juego esta en su punto MAS FACIL")]
    [SerializeField] private float lowerSpeedLimit = 1f;
    [Tooltip("Velocidad de MinigameManager con la que el juego llega a su punto MAS DIFICIL")]
    [SerializeField] private float upperSpeedLimit = 2.5f;
    [Tooltip("Tamaño del area verde (fraccion del slider) en la dificultad mas facil")]
    [SerializeField, Range(0.05f, 1f)] private float areaSizeEasy = 0.55f;
    [Tooltip("Tamaño del area verde (fraccion del slider) en la dificultad mas dificil")]
    [SerializeField, Range(0.05f, 1f)] private float areaSizeHard = 0.15f;
    [Tooltip("Si el area es menor a este valor, el juego pasa a tener DOS areas verdes")]
    [SerializeField, Range(0f, 1f)] private float twoAreasThreshold = 0.35f;
    [Tooltip("Separacion minima entre las dos areas verdes (fraccion del slider)")]
    [SerializeField, Range(0f, 0.5f)] private float minGapBetweenAreas = 0.08f;
    [Tooltip("Velocidad del handle (recorridos completos del slider por segundo) en la dificultad mas facil")]
    [SerializeField] private float handleSpeedEasy = 0.6f;
    [Tooltip("Velocidad del handle en la dificultad mas dificil")]
    [SerializeField] private float handleSpeedHard = 1.4f;

    [Header("Ritmo")]
    [Tooltip("Segundos que el handle queda detenido al acertar la primera de dos areas")]
    [SerializeField] private float pauseAfterHit = 1f;
    [SerializeField] private float fallbackRoundDuration = 10f;

    [Header("Win")]
    [Tooltip("Jugador que toca la campana. Si se deja vacio se busca en la escena")]
    [SerializeField] private CheckInPlayer checkInPlayer;
    [Tooltip("Espera maxima (segundos) a que termine la animacion de campana antes de la secuencia de victoria")]
    [SerializeField] private float maxCampanaWait = 3f;

    [Header("Lose")]
    [Tooltip("Indice en ResultManager.loseDirectors de la secuencia de derrota. 1 = CinematicaPerder2 (PerderCheckIn2)")]
    [SerializeField] private int loseSequenceIndex = 1;

    // Margen para perder por tiempo justo ANTES de que TimeManager dispare su resultado
    // (TimeManager da victoria al acabar el tiempo si losesWithTime esta apagado).
    private const float TimeoutMargin = 0.1f;

    // Areas verdes pendientes (x = inicio, y = fin, en 0..1 del slider), ordenadas por inicio
    private readonly List<Vector2> zones = new List<Vector2>();

    private RectTransform zoneContainer;
    private float handleValue = 1f;   // 1 = arriba, 0 = abajo
    private float direction = -1f;    // empieza bajando
    private float handleSpeed;
    private float pauseTimer;
    private float timeLeft;
    private bool ready;
    private bool finished;
    private TimeManager timeManager;

    private IEnumerator Start()
    {
        timeManager = FindObjectOfType<TimeManager>();
        timeLeft = timeManager != null ? Mathf.Max(1f, timeManager.StartTime) : fallbackRoundDuration;

        if (checkInPlayer == null)
        {
            checkInPlayer = FindObjectOfType<CheckInPlayer>();
        }

        SetupSlider();
        ApplyHandleSkin();
        UpdateUI();

        // MinigameManager fija su Speed en su propio Start: se espera un frame
        // para leer la velocidad ya actualizada sin depender del orden de ejecucion.
        yield return null;

        SetupDifficulty();
        RebuildSegments();
        ready = true;
    }

    private void Update()
    {
        if (!ready || finished)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        if (timeLeft <= TimeoutMargin)
        {
            Lose();
            return;
        }

        if (pauseTimer > 0f)
        {
            // Pausa tras acertar la primera area: al terminar se quita esa area y sigue el juego
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f)
            {
                RemoveZoneAt(handleValue);
                RebuildSegments();
            }
            return;
        }

        MoveHandle();
        UpdateUI();

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            TryStop();
        }
    }

    #region Setup
    private void SetupSlider()
    {
        if (balanceSlider == null)
        {
            return;
        }

        // El slider es solo visual: que el click no lo arrastre
        balanceSlider.transition = Selectable.Transition.None;
        balanceSlider.interactable = false;
        balanceSlider.minValue = 0f;
        balanceSlider.maxValue = 1f;
        balanceSlider.wholeNumbers = false;

        // Contenedor de zonas: ocupa el mismo recorrido que el handle y se dibuja detras de el
        RectTransform parent = balanceSlider.handleRect != null
            ? balanceSlider.handleRect.parent as RectTransform
            : balanceSlider.transform as RectTransform;

        GameObject containerObject = new GameObject("Zonas", typeof(RectTransform));
        zoneContainer = containerObject.GetComponent<RectTransform>();
        zoneContainer.SetParent(parent, false);
        zoneContainer.SetSiblingIndex(0);
        Stretch(zoneContainer, Vector2.zero, Vector2.one);
    }

    /// <summary>
    /// Cambia el sprite del handle segun la ropa (Chest) equipada en GameData.
    /// </summary>
    private void ApplyHandleSkin()
    {
        if (handleImage == null && balanceSlider != null && balanceSlider.handleRect != null)
        {
            handleImage = balanceSlider.handleRect.GetComponent<Image>();
        }

        if (handleImage == null)
        {
            return;
        }

        Sprite sprite = GetEquippedChestLabel() == amarilloLabel ? handleSpriteAmarillo : handleSpriteBase;
        if (sprite != null)
        {
            handleImage.sprite = sprite;
        }
    }

    private string GetEquippedChestLabel()
    {
        // Sin GameData/ShopDatabase (escena probada sola) o sin skin equipada: se asume Base
        if (GameData.instance == null || ShopDatabase.instance == null)
        {
            return null;
        }

        string skinID = GameData.instance.GetEquipped(SkinCategory.Chest);
        if (string.IsNullOrEmpty(skinID))
        {
            return null;
        }

        Skin skin = ShopDatabase.instance.GetAllSkins()
            .Find(s => s.category == SkinCategory.Chest && s.id == skinID);
        return skin != null ? skin.label : null;
    }

    private void SetupDifficulty()
    {
        float t = Mathf.InverseLerp(lowerSpeedLimit, Mathf.Max(lowerSpeedLimit + 0.01f, upperSpeedLimit), GetGameSpeed());

        handleSpeed = Mathf.Lerp(handleSpeedEasy, handleSpeedHard, t);

        float size = Mathf.Lerp(areaSizeEasy, areaSizeHard, t);
        bool twoAreas = size < twoAreasThreshold;

        zones.Clear();

        if (!twoAreas)
        {
            float start = Random.Range(0f, 1f - size);
            zones.Add(new Vector2(start, start + size));
            return;
        }

        // Dos areas del mismo tamaño, sin solaparse y con una separacion minima
        size = Mathf.Min(size, (1f - minGapBetweenAreas) * 0.5f);
        float free = 1f - (2f * size) - minGapBetweenAreas;
        float a = Random.Range(0f, free);
        float b = Random.Range(0f, free);
        float first = Mathf.Min(a, b);
        float second = Mathf.Max(a, b) + size + minGapBetweenAreas;

        zones.Add(new Vector2(first, first + size));
        zones.Add(new Vector2(second, second + size));
    }

    private float GetGameSpeed()
    {
        return MinigameManager.instance != null ? MinigameManager.instance.Speed : lowerSpeedLimit;
    }
    #endregion

    #region Gameplay
    private void MoveHandle()
    {
        handleValue += direction * handleSpeed * Time.deltaTime;

        if (handleValue <= 0f)
        {
            handleValue = 0f;
            direction = 1f;
        }
        else if (handleValue >= 1f)
        {
            handleValue = 1f;
            direction = -1f;
        }
    }

    private void TryStop()
    {
        if (!IsInsideZone(handleValue))
        {
            Lose();
            return;
        }

        if (zones.Count <= 1)
        {
            finished = true;
            StartCoroutine(WinRoutine());
            return;
        }

        // Quedan mas areas: se detiene un momento y luego esta area desaparece
        pauseTimer = Mathf.Max(0.01f, pauseAfterHit);
    }

    /// <summary>
    /// Victoria: primero la animacion de campana completa y, al terminar, la secuencia de ganar.
    /// </summary>
    private IEnumerator WinRoutine()
    {
        // El resultado ya esta decidido: el reloj no debe interrumpir la campana
        if (timeManager != null)
        {
            timeManager.enabled = false;
        }

        if (checkInPlayer != null)
        {
            // Un frame para que CheckInPlayer procese el mismo input (sonido de campana)
            yield return null;

            checkInPlayer.PlayCampanaAnimation();

            float waited = 0f;
            while (checkInPlayer.IsPlayingCampana && waited < maxCampanaWait)
            {
                waited += Time.deltaTime;
                yield return null;
            }
        }

        if (ResultManager.instance != null)
        {
            ResultManager.instance.WinMinigame();
        }
    }

    private void Lose()
    {
        finished = true;
        if (ResultManager.instance != null)
        {
            ResultManager.instance.LoseMinigame(loseSequenceIndex);
        }
    }

    private bool IsInsideZone(float value)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (value >= zones[i].x && value <= zones[i].y)
            {
                return true;
            }
        }

        return false;
    }

    private void RemoveZoneAt(float value)
    {
        for (int i = 0; i < zones.Count; i++)
        {
            if (value >= zones[i].x && value <= zones[i].y)
            {
                zones.RemoveAt(i);
                return;
            }
        }
    }
    #endregion

    #region Visual
    private void UpdateUI()
    {
        if (balanceSlider != null)
        {
            balanceSlider.SetValueWithoutNotify(handleValue);
        }
    }

    /// <summary>
    /// Redibuja la barra: tramos verdes en las areas pendientes y rojos en el resto,
    /// cada tramo con su icono centrado.
    /// </summary>
    private void RebuildSegments()
    {
        if (zoneContainer == null)
        {
            return;
        }

        for (int i = zoneContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(zoneContainer.GetChild(i).gameObject);
        }

        Canvas.ForceUpdateCanvases();

        const float epsilon = 0.001f;
        float cursor = 0f;

        for (int i = 0; i < zones.Count; i++)
        {
            if (zones[i].x > cursor + epsilon)
            {
                CreateSegment(cursor, zones[i].x, false);
            }

            CreateSegment(zones[i].x, zones[i].y, true);
            cursor = zones[i].y;
        }

        if (cursor < 1f - epsilon)
        {
            CreateSegment(cursor, 1f, false);
        }
    }

    private void CreateSegment(float start, float end, bool isGreen)
    {
        GameObject segmentObject = new GameObject(isGreen ? "Verde" : "Rojo", typeof(RectTransform), typeof(Image));
        RectTransform segmentRect = segmentObject.GetComponent<RectTransform>();
        segmentRect.SetParent(zoneContainer, false);
        Stretch(segmentRect, new Vector2(start, 0f), new Vector2(end, 1f));

        Image segmentImage = segmentObject.GetComponent<Image>();
        segmentImage.color = isGreen ? areaGreenColor : areaRedColor;
        segmentImage.raycastTarget = false;

        Sprite icon = isGreen ? greenIcon : redIcon;
        if (icon == null)
        {
            return;
        }

        GameObject iconObject = new GameObject("Icono", typeof(RectTransform), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(segmentRect, false);
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;

        // El icono cabe en el tramo: limitado por el grosor de la barra y por el largo del tramo
        Rect containerSize = zoneContainer.rect;
        float segmentLength = containerSize.width * (end - start);
        float side = Mathf.Min(containerSize.height * iconScale, segmentLength);
        iconRect.sizeDelta = new Vector2(side, side);

        // El slider esta rotado para verse vertical: el icono se mantiene derecho
        Canvas canvas = zoneContainer.GetComponentInParent<Canvas>();
        iconRect.rotation = canvas != null ? canvas.rootCanvas.transform.rotation : Quaternion.identity;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
    }

    private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }
    #endregion
}
