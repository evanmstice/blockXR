// this file helps with the game aspect like switching levels, triggering try again screens, etc.
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour 
{ 
    // game manager can be accessed from anywhere
    public static GameManager Instance;

    [Header("UI")]
    // this panel is dragged in from the inspector, it get activated when player goes off path
    public GameObject tryAgainPanel;

    [Header("Player")]
    public Transform player;
    // this will save the initial position of the player when the game starts
    private Vector3 playerInitialPosition;
    // at the beginning of each run this will be set to false
    private bool goalReached = false;


    // this sets up the game manager... if there is no game manger, it creates one... if one already exists, it destroys it
    private void Awake () 
    { 
        if (Instance == null)
            Instance = this; 
        else
            Destroy(gameObject);
    }

    void Start () 
    {
        // set the initial position of the player, this will be the reset position
        playerInitialPosition = player.position;

        // hide the try again panel at the start of the game
        tryAgainPanel.SetActive(false);

    }

    // wjen the player collides with the goal, this will be called
    public void GoalReached()
    {
        goalReached = true;
        Debug.Log("Goal reached");
    }

    public void Result()
    {
        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement.offPath || !goalReached)
        {
            Debug.Log("Did not reach goal, TRY AGAIN");
            tryAgainPanel.SetActive(true);
        }
        else{
            Debug.Log("Goal reached, NEXT LEVEL");
            StartCoroutine(LoadCutscene());
        }
    }

    private IEnumerator LoadCutscene(){
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void TriggerTryAgain()
    {
        tryAgainPanel.SetActive(true);
    }

    public void ClickedTryAgain(){
        // hiding the try again panel
        tryAgainPanel.SetActive(false);

        // to snap the player back to the initial position
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.MovePosition(playerInitialPosition);
        }else{
            player.position = playerInitialPosition;
        }
        // goal is reset to false
        goalReached = false;
        // resets the player direction
        player.GetComponent<PlayerMovement>().ResetDirection();

    }

}


