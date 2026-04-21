using UnityEngine;
using UnityEngine.UI;

public class RibbonSpawner : MonoBehaviour
{
    public Transform bouquetPoint;

    [Header("Ribbon Buttons — assign prefabs only, listeners set in code")]
    [SerializeField] private RibbonButtonEntry[] ribbonButtons;

    [System.Serializable]
    public class RibbonButtonEntry
    {
        public Button button;
        public GameObject ribbonPrefab;
    }

    void Start()
    {
        // Reassign all button listeners on every scene load
        if (ribbonButtons == null) return;

        foreach (var entry in ribbonButtons)
        {
            if (entry.button == null || entry.ribbonPrefab == null) continue;

            // Capture for lambda
            GameObject prefab = entry.ribbonPrefab;

            entry.button.onClick.RemoveAllListeners();
            entry.button.onClick.AddListener(() => SpawnRibbon(prefab));
            Debug.Log($"[RibbonSpawner] Listener assigned for ribbon: {prefab.name}");
        }
    }

    public void SpawnRibbon(GameObject ribbonPrefab)
    {
        if (ribbonPrefab != null)
        {
            Instantiate(ribbonPrefab, bouquetPoint.position, Quaternion.identity);

            if (RibbonManager.Instance != null)
            {
                RibbonManager.Instance.SelectRibbon(ribbonPrefab);
                Debug.Log($"[RibbonSpawner] Ribbon selected: {ribbonPrefab.name}");
            }
            else
            {
                Debug.LogWarning("[RibbonSpawner] RibbonManager.Instance is null!");
            }
        }
    }
}