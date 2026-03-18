using UnityEngine;

public class RoundData : MonoBehaviour
{
    public static RoundData instance { get;  set; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {

            Destroy(this.gameObject);
        }
        else
        {

            instance = this;

            DontDestroyOnLoad(this.gameObject);
        }
    }
    void Start()
    {

    }


    void Update()
    {

    }

    public void MinigameResult(bool won)
    {
        if (won)
        {
            Debug.Log("Minijuego ganado");
        }
        else
        {
            Debug.Log("Minijuego perdido");
        }
    }
}
