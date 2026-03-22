using UnityEngine;

public class PlayFootstepSound : MonoBehaviour
{
  

   

   //  private SoundType currentFootstepType = SoundType.PisadaDura;
    [SerializeField] private string SoundId  = "FootStepSounds";
    public void PlayFootstep()
    {
        SoundManager.PlaySound(SoundId);
    }
}
