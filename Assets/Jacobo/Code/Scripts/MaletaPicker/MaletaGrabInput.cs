using UnityEngine;
using UnityEngine.InputSystem;

public class MaletaGrabInput : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private MaletaManager maletaManager;

    [Header("Animation")]
    [SerializeField] private string grabTriggerName = "grab";
    [SerializeField] private float grabCooldown = 0.1f;

    private float lastGrabTime = -999f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (maletaManager == null)
            maletaManager = FindAnyObjectByType<MaletaManager>();
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (Time.time < lastGrabTime + grabCooldown) return;

        lastGrabTime = Time.time;

        if (animator != null)
            animator.SetTrigger(grabTriggerName);
    }

    public void TryGrabMaleta(Maleta maleta)
    {
        if (maletaManager == null || maleta == null) return;
        maletaManager.TryPickMaleta(maleta);
    }
}
