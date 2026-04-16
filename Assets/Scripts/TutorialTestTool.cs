using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 教程测试工具 - 用于快速添加教程订单所需的库存
/// </summary>
public class TutorialTestTool : MonoBehaviour
{
    [Header("要添加的花朵")]
    [SerializeField] private string[] tutorialFlowerNames = new string[] { "Rose2", "Daisy2" };
    [SerializeField] private int addCount = 10;

    [Header("丝带")]
    [SerializeField] private string[] tutorialRibbonNames = new string[] { "RibbonRed", "RibbonBlue" };
    [SerializeField] private int ribbonAddCount = 5;

    [Header("UI")]
    [SerializeField] private Button addStockButton;

    void Start()
    {
        if (addStockButton != null)
        {
            addStockButton.onClick.AddListener(AddTutorialStock);
        }
        else
        {
            Debug.LogWarning("[TutorialTestTool] 未设置按钮，请在 Inspector 中配置 addStockButton");
        }
    }

    /// <summary>
    /// 添加教程订单所需的所有库存（花朵 + 丝带）
    /// </summary>
    public void AddTutorialStock()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[TutorialTestTool] GameManager.Instance 为空！");
            return;
        }

        // 添加花朵库存
        if (tutorialFlowerNames != null)
        {
            foreach (string flowerName in tutorialFlowerNames)
            {
                if (string.IsNullOrWhiteSpace(flowerName)) continue;

                for (int i = 0; i < addCount; i++)
                {
                    GameManager.Instance.AddFlowerToInventoryByName(flowerName);
                }
            }
        }

        // 添加丝带库存
        if (tutorialRibbonNames != null)
        {
            foreach (string ribbonName in tutorialRibbonNames)
            {
                if (string.IsNullOrWhiteSpace(ribbonName)) continue;

                for (int i = 0; i < ribbonAddCount; i++)
                {
                    GameManager.Instance.AddRibbonToInventory(ribbonName);
                }
            }
        }

        Debug.Log($"[TutorialTestTool] 教程库存添加完成！");
        ShowCurrentStock();
    }

    /// <summary>
    /// 显示当前库存状态
    /// </summary>
    public void ShowCurrentStock()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager.Instance 为空");
            return;
        }

        string info = "[TutorialTestTool] 当前库存:\n";
        info += "花朵: ";
        if (GameManager.Instance.flowerInventory == null || GameManager.Instance.flowerInventory.Count == 0)
        {
            info += "无";
        }
        else
        {
            var flowerList = new System.Collections.Generic.List<string>();
            foreach (var kvp in GameManager.Instance.flowerInventory)
            {
                flowerList.Add($"{kvp.Key}:{kvp.Value}");
            }
            info += string.Join(", ", flowerList);
        }

        info += "\n丝带: ";
        if (GameManager.Instance.ribbonInventory == null || GameManager.Instance.ribbonInventory.Count == 0)
        {
            info += "无";
        }
        else
        {
            var ribbonList = new System.Collections.Generic.List<string>();
            foreach (var kvp in GameManager.Instance.ribbonInventory)
            {
                ribbonList.Add($"{kvp.Key}:{kvp.Value}");
            }
            info += string.Join(", ", ribbonList);
        }

        Debug.Log(info);
    }
}
