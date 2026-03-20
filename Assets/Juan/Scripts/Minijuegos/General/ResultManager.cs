using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;

public class ResultManager : MonoBehaviour
{
    public static ResultManager instance;

    
    
    [Header("Directors")]
    [SerializeField] private GameObject winDirector;
    [SerializeField] private GameObject loseDirector;

    [Header("Timing")]
    [SerializeField] private float loseDelay = 0f;

    [Header("Scene")]
    [SerializeField] private string nextSceneName = "MinigameResults";

    private bool hasFinished = false;



    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

 
    public void WinMinigame()
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(true));
    }

   
    public void LoseMinigame()
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("Minijuego perdido, delay de " +loseDelay +" segundos");
        StartCoroutine(LoseWithDelay());
    }

 
    public void DirectLose()
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(false));
    }

    private IEnumerator LoseWithDelay()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(loseDelay);

        yield return HandleResult(false);
    }

    private IEnumerator HandleResult(bool won)
    {
    
        Time.timeScale = 0f;

    
        

        GameObject activeDirectorObject = won ? winDirector : loseDirector;
        GameObject inactiveDirectorObject = won ? loseDirector : winDirector;

        if (inactiveDirectorObject != null)
            inactiveDirectorObject.SetActive(false);

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
            Debug.LogWarning("ResultManager: Director no asignado/no encontrado; continuando sin esperar secuencia.");
            yield return null;
        }

   
        RoundData.instance.MinigameResult(won);

      
        Time.timeScale = 1f;

   
        SceneManager.LoadScene(nextSceneName);
    }
}