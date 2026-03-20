using UnityEngine;

public class PlayFootstepSound : MonoBehaviour
{
    [Header("Tags")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string softGroundTag = "SoftGround";
    [SerializeField] private string hardGroundTag = "HardGround";

    [Header("Audio")]
    [Tooltip("Optional AudioSource to play the sound through (e.g., on the player). If null, SoundManager uses its own AudioSource.")]
    [SerializeField] private AudioSource source;

   //  private SoundType currentFootstepType = SoundType.PisadaDura;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        UpdateFootstepTypeFromCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        UpdateFootstepTypeFromCollision(collision);
    }

    private void UpdateFootstepTypeFromCollision(Collision2D collision)
    {
        // This script is expected to be on the Player.
        // Only react when the player is the one colliding.
        if (!CompareTag(playerTag))
            return;

        // Determine which terrain tag we are standing on.
        GameObject other = collision.collider != null ? collision.collider.gameObject : null;
        if (other == null)
            return;

        if (other.CompareTag(softGroundTag))
        {
           // currentFootstepType = SoundType.PisadaArena;
        }
        else if (other.CompareTag(hardGroundTag))
        {
           // currentFootstepType = SoundType.PisadaDura;
        }
    }

    // Call this from an Animation Event when the foot hits the ground.
    public void PlayFootstep()
    {
      //  SoundManager.PlaySound(currentFootstepType, source);
    }
}
