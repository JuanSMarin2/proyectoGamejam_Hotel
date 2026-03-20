using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void StoryMode()
    {
        var order = GameOrderManager.instance.GetSceneOrder();

        RoundData.instance.SetGameOrder(order);

        RoundData.instance.SetStoryMode(true);



       SceneManager.LoadScene("IntroScene");
    }

    public void LoadShopScene(){
        SceneManager.LoadScene("Shop");
    }
}