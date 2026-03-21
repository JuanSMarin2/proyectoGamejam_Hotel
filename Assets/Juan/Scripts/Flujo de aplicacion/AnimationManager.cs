using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class AnimationManager : MonoBehaviour
{
    [Header("Director")]
    [SerializeField] private GameObject introDirector;

    private bool hasPlayed = false;
    private void Start()
    {
        PlayAndLoadNext();
    }

    public void PlayAndLoadNext()
    {
        if (hasPlayed) return;
        hasPlayed = true;

        StartCoroutine(WaitForAnimation());
    }

    private IEnumerator WaitForAnimation()
    {
        if (introDirector == null)
        {
            Debug.LogWarning("AnimationManager: introDirector no asignado; continuando sin esperar.");
            RoundData.instance.NextMinigame();
            yield break;
        }

        introDirector.SetActive(true);

        var director = introDirector.GetComponent<PlayableDirector>();
        if (director == null)
            director = introDirector.GetComponentInChildren<PlayableDirector>(true);

        if (director == null)
        {
            Debug.LogWarning("AnimationManager: No se encontró PlayableDirector en introDirector; continuando sin esperar.");
            RoundData.instance.NextMinigame();
            yield break;
        }

        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        director.Play();

        yield return null;

        while (director.state == PlayState.Playing)
        {
            if (SkipRequested())
            {
                director.Stop();
                break;
            }

            yield return null;
        }

        Debug.Log("Director finished, loading next minigame...");
        RoundData.instance.NextMinigame();
    }

    private bool SkipRequested()
    {
        return Input.anyKeyDown ||
               Input.GetMouseButtonDown(0) ||
               Input.GetMouseButtonDown(1) ||
               Input.GetMouseButtonDown(2);
    }
}