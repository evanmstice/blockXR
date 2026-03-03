using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // can adjust how big the step are within the inspector
    public float step = 1f;

    public void MoveForward()
    {
        // player will move forward in the direction that it is facing
        transform.position += transform.right * step;

        Debug.Log("Player moved forward by " + step);
    }
    public void TurnRight(){
        transform.Rotate(0, 90, 0);
    }
}