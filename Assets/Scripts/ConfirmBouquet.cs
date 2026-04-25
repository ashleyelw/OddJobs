using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfirmBouquet : MonoBehaviour
{
    public void OnConfirm()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[ConfirmBouquet] GameManager not found!");
            SceneManager.LoadScene("FloristMain");
            return;
        }

        // Gather ribbon name
        string ribbonRaw = null;
        if (RibbonManager.Instance == null)
        {
            Debug.Log("RibbonManager.Instance=null");
        }
        if (RibbonManager.Instance.selectedRibbonPrefab == null)
        {
            Debug.Log("RibbonManager.Instance.selectedRibbonPrefab=null");
        }
        if (RibbonManager.Instance != null && RibbonManager.Instance.selectedRibbonPrefab != null)
        {
            Debug.Log("添加丝带");
            ribbonRaw = GameManager.NormalizeKey(RibbonManager.Instance.selectedRibbonPrefab.name);
        }
    
        Debug.Log("丝带名字"+ribbonRaw);
        // Gather flower names from trimmed prefabs
        var flowerNames = new System.Collections.Generic.List<string>();
        if (FlowerTransferManager.Instance != null)
        {
            Debug.Log("flower manager!=null");
            var source = FlowerTransferManager.Instance.selectedFlowerPrefabs;
            foreach (var prefab in source)
            {Debug.Log("遍历花束prefab名字"+prefab.name);
                if (prefab == null) continue;
                string name = GameManager.Instance.NormalizeFlowerName(
                                GameManager.NormalizeKey(prefab.name));
                Debug.Log("添加花束名字为"+name);
                flowerNames.Add(name);
            }
        }

        if (flowerNames.Count == 0)
        {
            Debug.LogWarning("[ConfirmBouquet] No flowers selected.");
            SceneManager.LoadScene("FloristMain");
            return;
        }

        // AssembleBouquet handles name generation AND consumes trimmed inventory
        string bouquetName = GameManager.Instance.AssembleBouquet(flowerNames, ribbonRaw);
        Debug.Log(bouquetName);
        if (bouquetName == null)
        {
            Debug.LogWarning("[ConfirmBouquet] AssembleBouquet failed — check trimmed flower inventory.");
            // Fallback: force-add anyway so player isn't stuck
            string normalizedRibbon = string.IsNullOrEmpty(ribbonRaw) ? null
                : GameManager.Instance.NormalizeRibbonName(ribbonRaw);
            bouquetName = string.IsNullOrEmpty(normalizedRibbon)
                ? flowerNames[0]
                : $"{flowerNames[0]}_{normalizedRibbon}";
            GameManager.Instance.AddBouquet(bouquetName, flowerNames, normalizedRibbon);
            Debug.Log("花的信息"+flowerNames+""+normalizedRibbon);
            Debug.LogWarning($"[ConfirmBouquet] Fallback: force-added bouquet {bouquetName}");
        }

        Debug.Log($"[ConfirmBouquet] Bouquet confirmed: {bouquetName}");
        GameManager.Instance.Test_PrintAllInventory();

        // Clear transfer state
        if (FlowerTransferManager.Instance != null)
        {
            FlowerTransferManager.Instance.selectedFlowerPrefabs.Clear();
            FlowerTransferManager.Instance.selectedFlowerStemPrefabs.Clear();
        }
        if (RibbonManager.Instance != null)
            RibbonManager.Instance.ClearSelection();

        SceneManager.LoadScene("FloristMain");
    }
}