using UnityEngine;
using UnityEngine.SceneManagement;

public class Animaciones : MonoBehaviour
{
    [System.Serializable]
    private class BoolEntry
    {
        public Animator animator;
        public string boolParameterName = "";
        public bool boolValue = true;
    }

    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private BoolEntry[] bools;

    private void Start()
    {
        if (!applyOnStart)
        {
            return;
        }

        ApplyBools();
    }

    public void ApplyBools()
    {
        if (bools == null)
        {
            return;
        }

        foreach (var entry in bools)
        {
            if (entry == null)
            {
                continue;
            }

            var targetAnimator = entry.animator != null ? entry.animator : GetComponent<Animator>();
            if (targetAnimator == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(entry.boolParameterName))
            {
                targetAnimator.SetBool(entry.boolParameterName, entry.boolValue);
            }
        }
    }
}
