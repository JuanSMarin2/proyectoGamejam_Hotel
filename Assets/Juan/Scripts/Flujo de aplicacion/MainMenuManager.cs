using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StoryMode()
    {
        var order = GameOrderManager.instance.GetSceneOrder();

        RoundData.instance.SetGameOrder(order);



       SceneManager.LoadScene("IntroScene");
    }
}