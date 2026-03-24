using UnityEngine;
using UnityEngine.SceneManagement;
using extOSC.Examples;


public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        OSCController osc = FindAnyObjectByType<OSCController>();
        Debug.Log("BootstrapLoader: OSCController found = " + (osc != null));
        Debug.Log("Loading Level_01...");
        SceneManager.LoadScene("Level_01");
    }
}