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

        // Save flower names before clearing (for ConfirmBouquet to use)
        FlowerTransferManager.Instance.confirmedFlowerNames.Clear();
        foreach (GameObject prefab in FlowerTransferManager.Instance.selectedFlowerStemPrefabs)
        {
            if (prefab == null) continue;
            string name = GameManager.Instance.NormalizeFlowerName(
                              GameManager.NormalizeKey(prefab.name));
            FlowerTransferManager.Instance.confirmedFlowerNames.Add(name);
            Debug.Log($"[FlowerWrapSpawn] Saved flower name for bouquet: {name}");
        }

        // Also check prefabs list as fallback
        if (FlowerTransferManager.Instance.confirmedFlowerNames.Count == 0)
        {
            foreach (GameObject prefab in FlowerTransferManager.Instance.selectedFlowerPrefabs)
            {
                if (prefab == null) continue;
                string name = GameManager.Instance.NormalizeFlowerName(
                                  GameManager.NormalizeKey(prefab.name));
                FlowerTransferManager.Instance.confirmedFlowerNames.Add(name);
                Debug.Log($"[FlowerWrapSpawn] Saved flower name (prefabs) for bouquet: {name}");
            }
        }

        float offsetX = 0f;
        foreach (GameObject prefab in FlowerTransferManager.Instance.selectedFlowerPrefabs)
        {
            if (prefab == null) continue;
            Instantiate(prefab, spawnArea.position + new Vector3(offsetX, 0f, 0f), Quaternion.identity);
            offsetX += 1.5f;
        }

        FlowerTransferManager.Instance.selectedFlowerPrefabs.Clear();
    }
}