using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;

public class EndAnimationManager : MonoBehaviour
{
    [Header("Directors")]
    [SerializeField] private GameObject goodDirector;
    [SerializeField] private GameObject neutralDirector;
    [SerializeField] private GameObject badDirector;

    [Header("Scene")]
    [SerializeField] private string finalSceneName = "FinalScene";

    private void Start()
    {
        StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        int money = RoundData.instance.Money;
        int startMoney = RoundData.instance.StartMoney;

        GameObject activeDirectorObject;
        if (money >= startMoney)
            activeDirectorObject = goodDirector;
        else if (money > 0)
            activeDirectorObject = neutralDirector;
        else
            activeDirectorObject = badDirector;

        if (goodDirector != null) goodDirector.SetActive(false);
        if (neutralDirector != null) neutralDirector.SetActive(false);
        if (badDirector != null) badDirector.SetActive(false);

        PlayableDirector director = null;
        if (activeDirectorObject != null)
        {
            activeDirectorObject.SetActive(true);
            director = activeDirectorObject.GetComponent<PlayableDirector>();
            if (director == null)
                director = activeDirectorObject.GetComponentInChildren<PlayableDirector>(true);
        }

        if (director != null)
        {
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.Play();

            yield return null;

            while (director.state == PlayState.Playing)
                yield return null;
        }
        else
        {
            Debug.LogWarning("EndAnimationManager: Director no asignado/no encontrado; cargando escena sin esperar.");
            yield return null;
        }

        SceneManager.LoadScene(finalSceneName);
    }
}