using UnityEngine;
using UnityEngine.UI;

public class RibbonSpawner : MonoBehaviour
{
    public Transform bouquetPoint;

    [Header("Ribbon Prefabs and their corresponding Button names")]
    [SerializeField] private RibbonButtonEntry[] ribbonEntries;

    [System.Serializable]
    public class RibbonButtonEntry
    {
        public GameObject ribbonPrefab;
        public string buttonName; // exact name of the Button GameObject in the scene
    }

    void OnEnable()
    {
        AssignButtonListeners();
    }

    void AssignButtonListeners()
    {
        if (ribbonEntries == null || ribbonEntries.Length == 0)
        {
            Debug.LogWarning("[RibbonSpawner] No ribbon entries assigned.");
            return;
        }

        foreach (var entry in ribbonEntries)
        {
            if (entry.ribbonPrefab == null || string.IsNullOrEmpty(entry.buttonName))
                continue;

            // Find button by name in scene
            GameObject btnGo = GameObject.Find(entry.buttonName);
            if (btnGo == null)
            {
                Debug.LogWarning($"[RibbonSpawner] Could not find button named: {entry.buttonName}");
                continue;
            }

            Button btn = btnGo.GetComponent<Button>();
            if (btn == null)
            {
                Debug.LogWarning($"[RibbonSpawner] GameObject {entry.buttonName} has no Button component.");
                continue;
            }

            // Capture for lambda
            GameObject prefab = entry.ribbonPrefab;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SpawnRibbon(prefab));
            Debug.Log($"[RibbonSpawner] Button '{entry.buttonName}' assigned to ribbon: {prefab.name}");
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