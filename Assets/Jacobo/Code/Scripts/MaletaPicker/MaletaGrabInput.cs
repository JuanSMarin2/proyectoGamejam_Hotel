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

    [Header("Keyboard Fallback")]
    [SerializeField] private bool allowAnyKeyboardKey = true;

    private float lastGrabTime = -999f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (maletaManager == null)
            maletaManager = FindAnyObjectByType<MaletaManager>();
    }

    private void Update()
    {
        if (!allowAnyKeyboardKey)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current.anyKey.wasPressedThisFrame)
            return;

        TriggerGrab();
    }

    public void OnGrab(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        TriggerGrab();
    }

    public void TriggerGrab()
    {
        if (Time.time < lastGrabTime + grabCooldown)
            return;

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
