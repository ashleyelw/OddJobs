using UnityEngine;

public class RibbonSpawner : MonoBehaviour
{
    public Transform bouquetPoint;

    public void SpawnRibbon(GameObject ribbonPrefab)
    {
        if (ribbonPrefab != null)
        {
            Instantiate(ribbonPrefab, bouquetPoint.position, Quaternion.identity);

            // Tell RibbonManager which ribbon was selected
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