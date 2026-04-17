using UnityEngine;

public class CollectFlower : MonoBehaviour
{
    public GameObject prefabReference; 
    public AudioClip collectSound;

    private bool playerInRange = false;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
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

            if (collectSound != null)
            {
                AudioSource.PlayClipAtPoint(collectSound, transform.position);
            }
        }
        else
        {
            Debug.LogWarning("Missing prefabReference on " + gameObject.name);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            playerInRange = false;
    }

}