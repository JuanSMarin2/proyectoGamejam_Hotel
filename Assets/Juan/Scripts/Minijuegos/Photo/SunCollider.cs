using UnityEngine;
using System.Collections.Generic;

public class SunCollider : MonoBehaviour
{

    public bool isBlocked = false;
    private readonly HashSet<Collider2D> collidersInside = new HashSet<Collider2D>();
    private readonly Dictionary<PhotoParticleActiver, int> activersInside = new Dictionary<PhotoParticleActiver, int>();

 private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
            return;

        collidersInside.Add(other);
        isBlocked = collidersInside.Count > 0;

        PhotoParticleActiver activer = other.GetComponentInParent<PhotoParticleActiver>();
        if (activer != null)
        {
            if (!activersInside.ContainsKey(activer))
                activersInside[activer] = 0;

            activersInside[activer]++;
        }
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
            return;

        collidersInside.Remove(other);
        isBlocked = collidersInside.Count > 0;

        PhotoParticleActiver activer = other.GetComponentInParent<PhotoParticleActiver>();
        if (activer != null && activersInside.TryGetValue(activer, out int count))
        {
            count--;
            if (count <= 0)
                activersInside.Remove(activer);
            else
                activersInside[activer] = count;
        }
        
    }

    public void ActivateAngryParticlesInside()
    {
        foreach (KeyValuePair<PhotoParticleActiver, int> entry in activersInside)
        {
            PhotoParticleActiver activer = entry.Key;
            if (activer == null)
                continue;

            activer.ActivateAngryParticle();
        }
    }
}
