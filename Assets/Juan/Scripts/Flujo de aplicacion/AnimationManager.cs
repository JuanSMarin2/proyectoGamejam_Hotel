using UnityEngine;
using System.Collections;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private Animator animator;

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
        if (animator == null)
            yield break;


        yield return null;


        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;


        yield return new WaitForSecondsRealtime(animLength);


        Debug.Log("Animation finished, loading next minigame...");
        RoundData.instance.NextMinigame();
    }
}