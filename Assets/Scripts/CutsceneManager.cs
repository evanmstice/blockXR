using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CutsceneManager : MonoBehaviour
{
    public enum AnimationMode
    {
        WalkLoop,
        Idle,
        MoveForwardLoop,
        None
    }

    [Header("References")]
    public PlayerMovement playerMovement;
    public Button nextButton;

    [Header("Cutscene Settings")]
    public AnimationMode animationMode = AnimationMode.WalkLoop;
    public float walkLeftDuration = 0.6f;
    public float walkRightDuration = 0.6f;

    [Header("Walk Up Loop Settings")]
    public float walkUpDuration = 1.0f;   // how long the walk up animation plays
    public float walkUpDistance = 2.0f;   // how far up the player moves
    public float snapBackDelay = 0.2f;

    private Coroutine animationCoroutine;
    private Vector3 initialPosition;


    void Start()
    {
        GameManager.Instance.SetRunButton(false);
        if (playerMovement != null)
            initialPosition = playerMovement.transform.position;

        switch (animationMode)
        {
            case AnimationMode.WalkLoop:
                animationCoroutine = StartCoroutine(CutsceneWalkLoop());
                break;
            case AnimationMode.Idle:
                animationCoroutine = StartCoroutine(PlayIdleAnimation());
                break;
            case AnimationMode.MoveForwardLoop:
                animationCoroutine = StartCoroutine(MoveForwardLoop());
                break;
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(OnNextClicked);
        else
            Debug.LogWarning("CutsceneManager: No next button assigned!");
    }

    private IEnumerator CutsceneWalkLoop()
    {
        while (true)
        {
            playerMovement.PlayCutsceneAnimation("Walk_Left");
            yield return new WaitForSeconds(walkLeftDuration);

            playerMovement.PlayCutsceneAnimation("Walk_Right");
            yield return new WaitForSeconds(walkRightDuration);
        }
    }

    private IEnumerator PlayIdleAnimation()
    {
        while (true)
        {
            playerMovement.PlayCutsceneAnimation("Walk_Down");
            yield return new WaitForSeconds(walkLeftDuration);
        }
    }

    private IEnumerator MoveForwardLoop()
    {
        while (true)
        {
            // play walk up animation and move upward
            playerMovement.PlayCutsceneAnimation("Walk_Up");
 
            float elapsed = 0f;
            Vector3 targetPosition = initialPosition + Vector3.up * walkUpDistance;
 
            while (elapsed < walkUpDuration)
            {
                playerMovement.transform.position = Vector3.Lerp(
                    initialPosition,
                    targetPosition,
                    elapsed / walkUpDuration
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
 
            // brief pause at the top
            yield return new WaitForSeconds(snapBackDelay);
 
            // snap back to start instantly
            playerMovement.transform.position = initialPosition;
            playerMovement.PlayCutsceneAnimation("Idle_Up");
 
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator None()
    {
        yield return null;
    }

    private void OnNextClicked()
    {
        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);

        if (playerMovement != null)
            playerMovement.PlayCutsceneAnimation("Idle_Down");

        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex + 1
        );
    }
}