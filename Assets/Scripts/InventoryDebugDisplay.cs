using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryDebugDisplay : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Text inventoryText;
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Auto-create UI if references not set")]
    [SerializeField] private bool autoCreateUI = true;

    private bool _isVisible = false;

    void Start()
    {
        if (autoCreateUI && panelRoot == null)
            CreateUIAutomatically();

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();

        // Refresh display every frame while open so it stays live
        if (_isVisible)
            RefreshDisplay();
    }

    public void Toggle()
    {
        _isVisible = !_isVisible;
        if (panelRoot != null)
            panelRoot.SetActive(_isVisible);
    }

    void RefreshDisplay()
    {
        if (inventoryText == null || GameManager.Instance == null) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== INVENTORY ===");

        // --- Untrimmed Flowers ---
        sb.AppendLine("\n[Untrimmed Flowers]");
        if (GameManager.Instance.untrimmedFlowers == null || GameManager.Instance.untrimmedFlowers.Count == 0)
        {
            sb.AppendLine("  (empty)");
        }
        else
        {
            foreach (var kvp in GameManager.Instance.untrimmedFlowers)
                sb.AppendLine($"  {kvp.Key}: x{kvp.Value}");
        }

        // --- Trimmed Flowers ---
        sb.AppendLine("\n[Trimmed Flowers]");
        if (GameManager.Instance.trimmedFlowers == null || GameManager.Instance.trimmedFlowers.Count == 0)
        {
            sb.AppendLine("  (empty)");
        }
        else
        {
            foreach (var kvp in GameManager.Instance.trimmedFlowers)
                sb.AppendLine($"  {kvp.Key}: x{kvp.Value}");
        }

        // --- Bouquet Inventory ---
        sb.AppendLine("\n[Bouquets]");
        if (GameManager.Instance.bouquetInventory == null || GameManager.Instance.bouquetInventory.Count == 0)
        {
            sb.AppendLine("  (empty)");
        }
        else
        {
            // Group bouquets by name for cleaner display
            var grouped = new Dictionary<string, int>();
            foreach (var b in GameManager.Instance.bouquetInventory)
            {
                string key = b.bouquetName ?? "unnamed";
                grouped[key] = grouped.ContainsKey(key) ? grouped[key] + 1 : 1;
            }
            foreach (var kvp in grouped)
                sb.AppendLine($"  {kvp.Key}: x{kvp.Value}");
        }

        // --- Ribbons (from RibbonManager) ---
        sb.AppendLine("\n[Ribbons]");
        if (RibbonManager.Instance == null || !RibbonManager.Instance.HasAnyRibbons())
        {
            sb.AppendLine("  (empty)");
        }
        else
        {
            foreach (var key in RibbonManager.Instance.GetRibbonKeys())
                sb.AppendLine($"  {key}: x{RibbonManager.Instance.GetRibbonCount(key)}");
        }

        // --- Coins ---
        sb.AppendLine($"\n[Coins]  {GameManager.Instance.coins}");

        sb.AppendLine("\n(Press I to close)");
        inventoryText.text = sb.ToString();
    }

    void CreateUIAutomatically()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[InventoryDebugDisplay] No Canvas found in scene.");
            return;
        }

        // Panel background
        panelRoot = new GameObject("InventoryDebugPanel");
        panelRoot.transform.SetParent(canvas.transform, false);

        var panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(0.35f, 1f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);

        var panelImage = panelRoot.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        // Scroll text
        var textGo = new GameObject("InventoryText");
        textGo.transform.SetParent(panelRoot.transform, false);

        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 10);
        textRect.offsetMax = new Vector2(-10, -10);

        inventoryText = textGo.AddComponent<Text>();
        inventoryText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        inventoryText.fontSize = 14;
        inventoryText.color = Color.white;
        inventoryText.alignment = TextAnchor.UpperLeft;
    }
}