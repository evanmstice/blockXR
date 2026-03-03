using UnityEngine;
using extOSC.Examples;

public class RunButtonHandler : MonoBehaviour
{
    // allows for reference to OSCController so that messages can be sent to python and can be received back
    public OSCController osc;
    
    public void OnClick()
    {
        Debug.Log("Run Button Clicked... sending OSC message");
        string[] message = {"scan"};
        // /req is the address we want to send the message to
        // osc controller sends /req "scan" to python, python will receive it because it is listening for /req
        osc.MessageSent("/req", message, 0);
        // state being set to receive tells unity to wait for a response
        osc.UpdateState("RECEIVE");
    }
}
