using UnityEngine;

// Place this on the SAME GameObject that has the Animator (the one you edit animation events on).
public class HeadFixerProxy : MonoBehaviour
{
    [SerializeField] private HeadFixer target;
    [SerializeField] private string animatorBoolName = "sentado";
    private Animator _animator;

    private void Start()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_animator != null && !string.IsNullOrEmpty(animatorBoolName))
            _animator.SetBool(animatorBoolName, true);
        else if (_animator == null)
            Debug.LogWarning("[HeadFixerProxy] No Animator found to set '" + animatorBoolName + "'.");
    }

    // AnimationEvent-friendly public method
    public void SwapFace()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (target == null)
            target = GetComponentInChildren<HeadFixer>();

        if (target != null)
            target.SwapFace();
        else
            Debug.LogWarning("[HeadFixerProxy] No HeadFixer found in children to SwapFace().");
    }
}