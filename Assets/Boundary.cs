using UnityEngine;

public class Boundary : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Debug.Log("Player hit boundary");

            Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
            PlayerMovement movement = collision.GetComponent<PlayerMovement>();
            // if the player collides with the bound
            // move the player back to the nearest tile, stop movement
            if (rb != null)
            {
                float x = Mathf.Round(rb.position.x);
                float y = Mathf.Round(rb.position.y);
                rb.MovePosition(new Vector2(x, y));
            }
        }
    }
}