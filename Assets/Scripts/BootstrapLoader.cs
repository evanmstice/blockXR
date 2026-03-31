using UnityEngine;
using UnityEngine.SceneManagement;
using extOSC.Examples;


public class BootstrapLoader : MonoBehaviour
{
    void Start()
    {
        OSCController osc = FindAnyObjectByType<OSCController>();
        Debug.Log("BootstrapLoader: OSCController found = " + (osc != null));
        Debug.Log("Loading Intro");
        SceneManager.LoadScene("a_Intro_1");
    }
}