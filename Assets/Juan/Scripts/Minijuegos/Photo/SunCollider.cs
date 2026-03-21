using UnityEngine;

public class SunCollider : MonoBehaviour
{

    public bool isBlocked = false;
 private void OnTriggerEnter2D(Collider2D other)
    {
      
          isBlocked = true;
        
    }

    private void OnTriggerExit2D(Collider2D other)
    {
       
            isBlocked = false;
        
    }
}
