using UnityEngine;

public class PhotoParticleActiver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject angryParticle;

    public void ActivateAngryParticle()
    {
        if (angryParticle == null)
            return;

        angryParticle.SetActive(true);
    }
}
