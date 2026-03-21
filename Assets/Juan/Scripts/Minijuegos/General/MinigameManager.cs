using UnityEngine;
using TMPro; 
using System.Collections;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager instance;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI gameDescriptionText;

    [Header("Description Config")]
    [SerializeField] private string gameDescription;
    [SerializeField] private float gameDescriptionTime = 3f;

    [Header("General Config")]
    [SerializeField] private float speed = 1f;
    [SerializeField] private bool losesWithTime = false;

    public bool LosesWithTime => losesWithTime;

    public float Speed
    {
        get => speed;
        set => speed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {

        if (RoundData.instance != null)
        {
            Speed = RoundData.instance.GetCurrentMinigameSpeed();
        }

   
        if (gameDescriptionText != null)
        {
            StartCoroutine(ShowDescriptionRoutine());
        }
    }

    private IEnumerator ShowDescriptionRoutine()
    {
  
        gameDescriptionText.text = gameDescription;

  
        yield return new WaitForSeconds(gameDescriptionTime);


        gameDescriptionText.text = "";
    }
}