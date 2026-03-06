using UnityEngine;


// public class Bone : MonoBehaviour
// {
//     private void OnTriggerEnter2D(Collider2D collision)
//     {
        
//         {
//             Debug.Log("Triggered by " + collision.name);
//             Debug.Log("Treat collected");
//             Destroy(gameObject);
//         }
//     }
//     private void OnCollisionEnter2D(Collision2D collision)
//     {
//         Debug.Log("COLLISION (not trigger) with: " + collision.gameObject.name);
//     }
// }


public class Bone : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Triggered by: " + collision.name);
        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("Collision with: " + collision.gameObject.name);
    }
}