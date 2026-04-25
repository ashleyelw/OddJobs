using UnityEngine;
using TMPro;

public class TalkPrompt : MonoBehaviour
{
    public GameObject talkPrompt;
    
    private bool playerInRange = false;
    

    void Start()
    {
        if (talkPrompt != null)
            talkPrompt.SetActive(false);   
    }

    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (talkPrompt != null)
                talkPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (talkPrompt != null)
                talkPrompt.SetActive(false);
        }
    }    
}
