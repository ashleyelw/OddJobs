using UnityEngine;

public class FlowerWrapSpawn : MonoBehaviour
{
    public Transform spawnArea;

    void Start()
    {
        if (FlowerTransferManager.Instance == null)
        {
            Debug.LogError("Missing Transfer Manager");
            return;
        }

        float offsetX = 0f;

        foreach (GameObject prefab in FlowerTransferManager.Instance.selectedFlowerPrefabs)
        {
            Instantiate(prefab, spawnArea.position + new Vector3(offsetX, 0f, 0f), Quaternion.identity);
            offsetX += 1.5f;
        }

        FlowerTransferManager.Instance.selectedFlowerPrefabs.Clear();
    }
}

