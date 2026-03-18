using UnityEngine;
using System.Collections.Generic;

public class GameOrderManager : MonoBehaviour
{
    public static GameOrderManager instance;

    [System.Serializable]
    public class MinigameData
    {
        public string sceneName;
    }

    [SerializeField] private List<MinigameData> minigameOrder = new List<MinigameData>();

    public List<string> GetSceneOrder()
    {
        List<string> order = new List<string>();

        foreach (var minigame in minigameOrder)
        {
            order.Add(minigame.sceneName);
        }

        return order;
    }

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }
}