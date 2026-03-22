using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using System.Collections;
using System.Collections.Generic;

public class ResultManager : MonoBehaviour
{
    public static ResultManager instance;

    
    
    [Header("Directors")]
    [SerializeField] private List<GameObject> winDirectors = new List<GameObject>();
    [SerializeField] private List<GameObject> loseDirectors = new List<GameObject>();
    [SerializeField] private int winDirectorIndex = 0;
    [SerializeField] private int loseDirectorIndex = 0;

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

        StartCoroutine(HandleResult(true, 0));
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

        StartCoroutine(HandleResult(false, loseDirectorIndex));
    }

    public void WinMinigame(int directorIndex)
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(true, directorIndex));
    }

    public void LoseMinigame(int directorIndex)
    {
        if (hasFinished) return;
        hasFinished = true;

        Debug.Log("Minijuego perdido, delay de " +loseDelay +" segundos");
        StartCoroutine(LoseWithDelay(directorIndex));
    }

    public void DirectLose(int directorIndex)
    {
        if (hasFinished) return;
        hasFinished = true;

        StartCoroutine(HandleResult(false, directorIndex));
    }

    private IEnumerator LoseWithDelay()
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(loseDelay);

        if (SwimController.IsSwimSceneActive())
        {
            SoundManager.PlaySound(SoundType.Frenado);
            SoundManager.PlaySound(SoundType.Motores);
        }

        yield return HandleResult(false, loseDirectorIndex);
    }

    private IEnumerator LoseWithDelay(int directorIndex)
    {
        Time.timeScale = 0f;

        yield return new WaitForSecondsRealtime(loseDelay);

        if (SwimController.IsSwimSceneActive())
        {
            SoundManager.PlaySound(SoundType.Frenado);
            SoundManager.PlaySound(SoundType.Motores);
        }

        yield return HandleResult(false, directorIndex);
    }

    private IEnumerator HandleResult(bool won, int directorIndex)
    {
    
        Time.timeScale = 0f;

    
        

        GameObject activeDirectorObject = GetDirector(won, directorIndex);

        SetDirectorsActive(won, false);

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

    private GameObject GetDirector(bool won, int index)
    {
        List<GameObject> list = won ? winDirectors : loseDirectors;
        if (list == null || list.Count == 0)
            return null;

        if (index < 0 || index >= list.Count)
        {
            Debug.LogWarning("ResultManager: indice de director fuera de rango; usando 0.");
            index = 0;
        }

        return list[index];
    }

    private void SetDirectorsActive(bool won, bool isActive)
    {
        List<GameObject> list = won ? winDirectors : loseDirectors;
        if (list == null)
            return;

        for (int i = 0; i < list.Count; i++)
        {
            GameObject directorObject = list[i];
            if (directorObject != null)
                directorObject.SetActive(isActive);
        }
    }
}