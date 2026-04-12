using UnityEngine;

public class Bone : MonoBehaviour
{
    private bool canCollect = false;

    private void Start()
    {
        // tells gamemanager to register the bone, so it can be tracked in case of reset
        GameManager.Instance.RegisterBone(this);
        Invoke("EnableCollection", 0.5f);

    }
    private void EnableCollection(){
        canCollect = true;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!canCollect) return;
        if (collision.CompareTag("Player"))
        {
            Debug.Log("Player collected bone");
        // setting it to false hides the bone, and can be brought back 
            gameObject.SetActive(false);
        }
        
    }
}