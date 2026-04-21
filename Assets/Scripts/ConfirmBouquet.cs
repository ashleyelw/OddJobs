using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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

        // --- Get selected ribbon ---
        string ribbonRaw = null;
        if (RibbonManager.Instance != null && RibbonManager.Instance.selectedRibbonPrefab != null)
            ribbonRaw = GameManager.NormalizeKey(RibbonManager.Instance.selectedRibbonPrefab.name);

        if (string.IsNullOrEmpty(ribbonRaw))
        {
            Debug.LogWarning("[ConfirmBouquet] No ribbon selected.");
            return;
        }

        string ribbonNormalized = GameManager.Instance.NormalizeRibbonName(ribbonRaw);

        // --- Read from confirmedFlowerNames (set by FlowerWrapSpawn) ---
        List<string> flowerNames = FlowerTransferManager.Instance?.confirmedFlowerNames;

        if (flowerNames == null || flowerNames.Count == 0)
        {
            Debug.LogWarning("[ConfirmBouquet] confirmedFlowerNames is empty.");
            return;
        }

        // --- Create one bouquet per flower ---
        int saved = 0;
        foreach (var flowerName in flowerNames)
        {
            if (string.IsNullOrEmpty(flowerName)) continue;

            string bouquetName = $"{flowerName}_{ribbonNormalized}";

            GameManager.Instance.AddBouquet(
                bouquetName,
                new List<string> { flowerName },
                ribbonNormalized
            );

            Debug.Log($"[ConfirmBouquet] Saved bouquet: '{bouquetName}'");
            saved++;
        }

        Debug.Log($"[ConfirmBouquet] Total bouquets saved: {saved}");
        GameManager.Instance.Test_PrintAllInventory();

        // --- Clear everything ---
        FlowerTransferManager.Instance.confirmedFlowerNames.Clear();
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