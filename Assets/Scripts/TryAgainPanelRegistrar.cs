using UnityEngine;
using UnityEngine.UI;

public class TryAgainPanelRegistrar : MonoBehaviour
{
    void Start()
    {
        Debug.Log("TryAgainPanelRegistrar: Registering panel with GameManager");

        Debug.Log("TryAgainPanelRegistrar: Start() called on " + gameObject.name);
        Debug.Log("TryAgainPanelRegistrar: GameManager.Instance is " + (GameManager.Instance != null ? "found" : "null"));
        

        GameManager.Instance.RegisterTryAgainPanel(gameObject);

        // wire up the try again button click in code
        Button tryAgainButton = GetComponentInChildren<Button>();
        if (tryAgainButton != null) {
            Debug.Log("TryAgainPanelRegistrar: Button found, wiring up click");

            tryAgainButton.onClick.AddListener(() => GameManager.Instance.ClickedTryAgain());
        }else
            Debug.LogWarning("TryAgainPanelRegistrar: Button not found");
    }
}