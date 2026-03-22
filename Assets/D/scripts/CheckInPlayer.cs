using UnityEngine;

public class CheckInPlayer : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleBoolName = "idlecheckin";
    [SerializeField] private string campanaBoolName = "Campana";
    [SerializeField] private string campanaStateName = "Campana";
    [SerializeField] private int campanaLayer = 0;

    private bool isPlayingCampana;

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (animator == null)
        {
            return;
        }

        animator.SetBool(idleBoolName, true);
        animator.SetBool(campanaBoolName, false);
    }

    private void Update()
    {
        if (animator == null)
        {
            return;
        }

        if (!isPlayingCampana && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
        {
            isPlayingCampana = true;
            animator.SetBool(campanaBoolName, true);
            animator.SetBool(idleBoolName, false);

            if (!string.IsNullOrEmpty(campanaStateName))
            {
                animator.Play(campanaStateName, campanaLayer, 0f);
            }
        }

        if (!isPlayingCampana)
        {
            return;
        }

        var stateInfo = animator.GetCurrentAnimatorStateInfo(campanaLayer);
        var isCampanaState = string.IsNullOrEmpty(campanaStateName) || stateInfo.IsName(campanaStateName);
        if (isCampanaState && stateInfo.normalizedTime >= 1f && !animator.IsInTransition(campanaLayer))
        {
            isPlayingCampana = false;
            animator.SetBool(campanaBoolName, false);
            animator.SetBool(idleBoolName, true);
        }
    }
}
