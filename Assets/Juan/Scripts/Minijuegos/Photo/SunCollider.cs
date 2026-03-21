using UnityEngine;

public class SunCollider : MonoBehaviour
{

    public bool isBlocked = false;
 private void OnTriggerEnter2D(Collider2D other)
    {
      
        Debug.Log("Colision con: " + other.gameObject.name);
          isBlocked = true;
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
         Debug.Log("Colision con: " + other.gameObject.name);
            isBlocked = false;
        
    }
}
