using UnityEngine;
using TMPro;

public class CollectFlower : MonoBehaviour
{
    public GameObject prefabReference;
    public AudioClip collectSound;
    public GameObject collectPrompt;

    private bool playerInRange = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (collectPrompt != null)
            collectPrompt.SetActive(false);
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            Collect();
        }
    }

    void Collect()
    {
        if (prefabReference != null)
        {
            FlowerTransferManager.Instance.selectedFlowerStemPrefabs.Add(prefabReference);
            Debug.Log("Collected: " + prefabReference.name);

            // ADD THIS: save to untrimmed inventory
            if (GameManager.Instance != null)
            {
                string flowerName = GameManager.NormalizeKey(prefabReference.name);
                GameManager.Instance.AddUntrimmedFlower(flowerName);
            }

            if (collectSound != null)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            if (collectPrompt != null)
                collectPrompt.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Missing prefabReference on " + gameObject.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            if (collectPrompt != null)
                collectPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (collectPrompt != null)
                collectPrompt.SetActive(false);
        }
    }

}