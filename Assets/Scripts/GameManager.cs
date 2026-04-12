// this file helps with the game aspect like switching levels, triggering try again screens, etc.
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour { 
    // game manager can be accessed from anywhere
    public static GameManager Instance;
    private GameObject tryAgainPanel;
    private TextMeshProUGUI tryAgainText;
    private Button runButton;
    private Transform player;
    private Vector3 playerInitialPosition;
    private bool goalReached = false;
    private List<Bone> registeredBones = new List<Bone>();


    // this sets up the game manager... if there is no game manger, it creates one... if one already exists, it destroys it
    private void Awake () { 
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        } else {
            Destroy(gameObject);
        }
    }

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // resets references every time a new scene loads
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        goalReached = false;
        registeredBones.Clear();
    }

    // called by Player on Start()
    public void RegisterPlayer(Transform p)
    {
        player = p;
        playerInitialPosition = player.position;
    }

    // called by each on Start()
    public void RegisterBone(Bone bone)
    {
        if(!registeredBones.Contains(bone)){
            registeredBones.Add(bone);
        }

    }

    public void ResetBones(){
        foreach (Bone bone in registeredBones){
            if (bone != null) 
                bone.gameObject.SetActive(true);
        }
    }

    // called by TryAgainPanel on Start()
    public void RegisterTryAgainPanel(GameObject panel)
    {
        Debug.Log("RegisterTryAgainPanel called with: " + panel.name);
        tryAgainPanel = panel;
        tryAgainText = panel.GetComponentInChildren<TextMeshProUGUI>();
        tryAgainPanel.SetActive(false);
        Debug.Log("tryAgainPanel is now: " + (tryAgainPanel != null ? "set" : "null"));

    }

    // called by RunButton on Start()
    public void RegisterRunButton(Button button)
    {
        runButton = button;
    }

    // called by PlayerMovement instead of accessing runButton directly
    public void SetRunButton(bool interactable)
    {
        if (runButton != null)
            runButton.interactable = interactable;
    }

    // wjen the player collides with the goal, this will be called
    public void GoalReached() {
        goalReached = true;
        Debug.Log("Goal reached");
    }

    public void ShowTryAgainPanel(string message) {
        Debug.Log("ShowTryAgainPanel called, panel is: " + (tryAgainPanel != null ? "found" : "null"));
        if (tryAgainPanel == null) return;
        tryAgainText.text = message;
        tryAgainPanel.SetActive(true);
    }

    public void Result() {
        if (runButton != null) runButton.interactable = false;
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Debug.Log("offPath: " + movement.offPath + " | goalReached: " + goalReached);

        if (movement.isMoving) return;

        if (movement.offPath) {
            Debug.Log("Player moved off path");
            ShowTryAgainPanel("OOPS!\nYOU LEFT THE PATH\nTRY AGAIN!");
        } else if (!goalReached) {
            Debug.Log("Try again, player did not reach the goal");
            ShowTryAgainPanel("OOPS!\nYOU DID NOT\nREACH THE GOAL\nTRY AGAIN!");
        } else {
            Debug.Log("Goal reached, NEXT LEVEL");
            StartCoroutine(LoadCutscene());
        }
    }

    private IEnumerator LoadCutscene() {
        yield return new WaitForSeconds(1f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void TriggerTryAgain() {
        tryAgainPanel.SetActive(true);
    }

    public void ClickedTryAgain() {
        // hiding the try again panel
        if (tryAgainPanel != null) tryAgainPanel.SetActive(false);
        if (runButton != null) runButton.interactable = true;

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
        // resets the bones
        ResetBones();

    }

}


