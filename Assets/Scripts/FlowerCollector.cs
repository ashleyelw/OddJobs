using UnityEngine;

public class FlowerCollector : MonoBehaviour
{
    [Header("检测设置")]
    public float harvestRange = 1.5f;
    public KeyCode harvestKey = KeyCode.E;

    public AudioClip harvestSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && harvestSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(harvestKey))
        {
            TryHarvest();
        }
    }

    void TryHarvest()
    {
        if (FlowerSpawner.Instance == null)
        {
            Debug.LogWarning("FlowerSpawner not found in scene!");
            return;
        }

        Vector2 playerPos = transform.position;
        FlowerSpawner.SpawnPoint nearestPoint = FlowerSpawner.Instance.GetNearestSpawnPoint(playerPos, harvestRange);

        if (nearestPoint == null)
        {
            Debug.Log("No flower in range to harvest.");
            return;
        }

        HarvestFlower(nearestPoint);
    }

    void HarvestFlower(FlowerSpawner.SpawnPoint point)
    {
        if (point.currentFlower == null)
            return;

        GameObject flower = point.currentFlower;

        if (harvestSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(harvestSound);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddToInventory(flower);
            Debug.Log($"Flower harvested! Total collected: {GameManager.Instance.collectedFlowers.Count}");
        }

        FlowerSpawner.Instance.OnFlowerHarvested(point);
        flower.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, harvestRange);
    }
}
