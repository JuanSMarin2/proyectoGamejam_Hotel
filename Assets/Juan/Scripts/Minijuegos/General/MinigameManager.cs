using UnityEngine;

public class MinigameManager : MonoBehaviour
{
    public static MinigameManager instance;

    [Header("Config")]
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
}