using UnityEngine;

public class GameData : MonoBehaviour
{
    public static GameData instance;

    [SerializeField] private int money;

    public int Money
    {
        get => money;
        set => money = Mathf.Max(0, value);
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }
}