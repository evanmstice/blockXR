using UnityEngine;
// THIS SCRIPT IS TO HELP TRIGGER THE CUTSCENE
public class Goal : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // there is an empty game object called goal, when it collides with the player
        // it triggers the cutscene
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player reached goal");
            GameManager.Instance.GoalReached();
        }
    }
}