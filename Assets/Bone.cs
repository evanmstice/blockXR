using UnityEngine;


public class Bone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        
        {
            Debug.Log("Triggered by " + collision.name);
            Debug.Log("Treat collected");
            Destroy(gameObject);
        }
    }
}