using UnityEngine;
using UnityEngine.UI;
using extOSC.Examples;

public class RunButtonHandler : MonoBehaviour
{
    // allows for reference to OSCController so that messages can be sent to python and can be received back
    private OSCController osc;

    void Start()
    {
        StartCoroutine(Initialize());
    }

    private System.Collections.IEnumerator Initialize()
    {
        // wait one frame for _Bootstrap objects to fully initialize
        yield return null;

        OSCController osc = FindAnyObjectByType<OSCController>();
        if (osc == null)
            Debug.LogWarning("RunButtonHandler: OSCController not found");

        GameManager.Instance.RegisterRunButton(GetComponent<Button>());
    }
    
    public void OnClick()
    {
        // try to find osc again if it's null
        if (osc == null)
            osc = FindAnyObjectByType<OSCController>();

        if (osc == null)
        {
            Debug.LogError("RunButtonHandler: OSCController not found on click");
            return;
        }
        Debug.Log("Run Button Clicked... sending OSC message");
        string[] message = {"scan"};
        // /req is the address we want to send the message to
        // osc controller sends /req "scan" to python, python will receive it because it is listening for /req
        osc.MessageSent("/req", message, 0);
        // state being set to receive tells unity to wait for a response
        osc.UpdateState("RECEIVE");
    }
}
