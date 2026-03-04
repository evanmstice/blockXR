using UnityEngine;
using System.Collections;
using UnityEngine.Tilemaps;

public class PlayerMovement : MonoBehaviour
{
    // can adjust how big the step are within the inspector
    public float step = 1f;
    public float speed = 1f;
    public Tilemap pathTilemap;

    private Animator animator;
    

    public enum Direction
    {
        Up,
        Right,
        Down,
        Left
    }

    private Direction currentDirection = Direction.Up;

    // this runs when the object is first loaded, grabs the animator component attached
    // to the same object, stores it in the animator variable
    void Awake(){
        animator = GetComponent<Animator>();
    }

    public IEnumerator MoveForward()
    {

        Vector3 movementDirection = Vector3.zero;

        switch (currentDirection)
        {
            case Direction.Up:
                movementDirection = Vector3.up;
                PlayAnimation("Walk_Up");
                break;
            case Direction.Right:
                movementDirection = Vector3.right;
                PlayAnimation("Walk_Right");
                break;
            case Direction.Down:
                movementDirection = Vector3.down;
                PlayAnimation("Walk_Down");
                break;
            case Direction.Left:
                movementDirection = Vector3.left;
                PlayAnimation("Walk_Left");
                break;
            
        }


    
        // transform.right moves the dog in the position that it is facing
        Vector3 endPos = transform.position + movementDirection * step;
        
        
        // runs every frame until the dog reaches the end position
        while(Vector3.Distance(transform.position, endPos) > 0.01f){
            // movetowards moves the dog from the start position to the end position by a fixed amount per frame
            transform.position = Vector3.MoveTowards(transform.position, endPos, speed * Time.deltaTime);
            yield return null;
        }

        // sets the dog's position to the end position
        transform.position = endPos;

        Vector3Int cellPosition = pathTilemap.WorldToCell(transform.position);
        TileBase tile = pathTilemap.GetTile(cellPosition);

        if (tile == null){
            Debug.Log("Dog has left the path");

            // TODO: Add a wrong block order try again
        }

    UpdateIdleAnimation();
    }
    public void TurnRight(){
        currentDirection = (Direction)(((int)currentDirection + 1) % 4);
        UpdateIdleAnimation();
    }

    public void TurnLeft(){
        currentDirection = (Direction)(((int)currentDirection - 1 + 4) % 4);
        UpdateIdleAnimation();
    }

    private void PlayAnimation(string animationName){
        if(animator != null)
            animator.Play(animationName);
    }

    private void UpdateIdleAnimation(){

        if(animator == null) return;

        switch (currentDirection)
        {
            case Direction.Up:
                PlayAnimation("Idle_Up");
                break;
            case Direction.Right:
                PlayAnimation("Idle_Right");
                break;
            case Direction.Down:
                PlayAnimation("Idle_Down");
                break;
            case Direction.Left:
                PlayAnimation("Idle_Left");
                break;
        }
    }
}