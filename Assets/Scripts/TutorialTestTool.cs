using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 教程测试工具 - 用于快速添加教程订单所需的库存
/// </summary>
public class TutorialTestTool : MonoBehaviour
{
    [Header("要添加的花朵")]
    [SerializeField] private string[] tutorialFlowerNames = new string[] { "Rose2", "Daisy2" };
    [SerializeField] private int addCount = 10;

    [Header("UI")]
    [SerializeField] private Button addStockButton;

    [Header("花束测试配置")]
    [SerializeField] private string testBouquetName = "Rose_Bouquet";
    [SerializeField] private List<string> testFlowersForBouquet = new List<string> { "Rose", "Tulip" };
    [SerializeField] private string testRibbonForBouquet = "Red_Ribbon";

    [Header("花束库存配置")]
    [Tooltip("CustomerSpawner引用（从中获取鲜花和丝带列表）")]
    [SerializeField] private CustomerSpawner customerSpawner;
    [Tooltip("每种花束添加的数量")]
    [SerializeField] private int bouquetCountPerType = 10;

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
    /// 添加教程订单所需的所有库存（花朵）
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

        Debug.Log("[TutorialTestTool] 教程库存添加完成！");
        ShowCurrentStock();
    }

    /// <summary>
    /// 完整测试：添加花束库存（采摘→修剪→包装）
    /// </summary>
    public void Btn_AddFlowersForBouquet()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager 未找到");
            return;
        }

        // 1. 先添加未修剪鲜花
        foreach (string flower in testFlowersForBouquet)
        {
            GameManager.Instance.AddUntrimmedFlower(flower, 2);
        }
        Debug.Log($"[TutorialTestTool] 添加未修剪鲜花: {string.Join(", ", testFlowersForBouquet)}");

        // 2. 全部修剪
        foreach (string flower in testFlowersForBouquet)
        {
            GameManager.Instance.TransferToTrimmed(flower, 2);
        }
        Debug.Log("[TutorialTestTool] 修剪完成");

        // 3. 包装成花束
        bool success = GameManager.Instance.CreateBouquetFromTrimmed(
            testFlowersForBouquet,
            testBouquetName,
            testRibbonForBouquet
        );

        if (success)
        {
            Debug.Log($"[TutorialTestTool] 花束创建成功: {testBouquetName}[{string.Join(",", testFlowersForBouquet)}+{testRibbonForBouquet}]");
        }

        ShowCurrentStock();
    }

    /// <summary>
    /// 直接添加测试花束（跳过采摘修剪，用于快速测试）
    /// </summary>
    public void Btn_AddTestBouquet()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager 未找到");
            return;
        }

        List<string> flowers = new List<string> { "Rose" };
        GameManager.Instance.Test_AddBouquet("Rose_Bouquet", flowers, "Red_Ribbon");
        GameManager.Instance.Test_AddBouquet("Rose_Bouquet", flowers, "Red_Ribbon");
        GameManager.Instance.Test_AddBouquet("Tulip_Bouquet", new List<string> { "Tulip" }, "Blue_Ribbon");

        Debug.Log("[TutorialTestTool] 直接添加测试花束 x3");
        ShowCurrentStock();
    }

    /// <summary>
    /// 打印当前库存状态
    /// </summary>
    public void Btn_PrintInventory()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager 未找到");
            return;
        }

        GameManager.Instance.Test_PrintAllInventory();
    }

    /// <summary>
    /// 模拟订单交付（扣除一个花束）
    /// </summary>
    public void Btn_SimulateDelivery()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager 未找到");
            return;
        }

        Debug.Log($"[TutorialTestTool] 模拟花束交付: {testBouquetName}");
        GameManager.Instance.Test_SimulateBouquetDelivery(testBouquetName);
        ShowCurrentStock();
    }

    public void Test()
    {
        // 1. 添加测试数据：花束 x2（包含 Rose 鲜花和 Red_Ribbon 丝带）
        System.Collections.Generic.List<string> flowers = new System.Collections.Generic.List<string> { "Rose" };
        GameManager.Instance.Test_AddBouquet("Rose_Bouquet", flowers, "Red_Ribbon");
        GameManager.Instance.Test_AddBouquet("Rose_Bouquet", flowers, "Red_Ribbon");

        // 2. 查看当前库存
        GameManager.Instance.Test_PrintAllInventory();

        // 3. 模拟一次花束订单交付（扣除1个 Rose_Bouquet 花束）
        GameManager.Instance.Test_SimulateBouquetDelivery("Rose_Bouquet");

        // 4. 查看扣除后的库存
        GameManager.Instance.Test_PrintAllInventory();
    }

    /// <summary>
    /// 添加所有种类花束库存（从CustomerSpawner获取鲜花和丝带类型）
    /// 鲜花 + 丝带 → 生成花束名称
    /// </summary>
    public void Btn_AddAllBouquetTypes()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[TutorialTestTool] GameManager 未找到");
            return;
        }

        // 从 CustomerSpawner 获取鲜花列表
        var flowerList = new System.Collections.Generic.List<string>();
        if (customerSpawner != null)
        {
            string[] flowers = customerSpawner.GetAvailableFlowers();
            if (flowers != null)
                flowerList.AddRange(flowers);
        }

        // 从 CustomerSpawner 获取丝带列表
        var ribbonList = new System.Collections.Generic.List<string>();
        if (customerSpawner != null)
        {
            string[] ribbons = customerSpawner.GetAvailableRibbons();
            if (ribbons != null)
                ribbonList.AddRange(ribbons);
        }

        // 如果 CustomerSpawner 为空，使用备用配置
        if (flowerList.Count == 0)
        {
            Debug.LogWarning("[TutorialTestTool] CustomerSpawner未配置或为空，使用备用鲜花列表");
            flowerList = new System.Collections.Generic.List<string>(new[] { "Rose2", "Daisy2", "Tulip2" });
        }

        if (ribbonList.Count == 0)
        {
            Debug.LogWarning("[TutorialTestTool] CustomerSpawner未配置或为空，使用备用丝带列表");
            ribbonList = new System.Collections.Generic.List<string>(new[] { "RibbonRed", "RibbonBlue", "RibbonYellow" });
        }

        int addedCount = 0;

        // 遍历所有鲜花和丝带组合
        // 一个花束 = 1种鲜花 + 1种丝带
        foreach (string flower in flowerList)
        {
            foreach (string ribbon in ribbonList)
            {
                // 生成花束名称（与CustomerOrderCoordinator.GenerateBouquetName保持一致）
                string bouquetName = GenerateBouquetName(flower, ribbon);

                // 提取鲜花基础名称（移除 "2" 后缀）
                string flowerBaseName = flower;
                if (flower.EndsWith("2"))
                    flowerBaseName = flower.Substring(0, flower.Length - 1);

                // 提取丝带名称（移除 "Ribbon" 前缀）
                string ribbonBaseName = ribbon;
                if (ribbon.StartsWith("Ribbon"))
                    ribbonBaseName = ribbon.Substring(6);

                // 添加指定数量的花束
                for (int i = 0; i < bouquetCountPerType; i++)
                {
                    var flowers = new System.Collections.Generic.List<string> { flowerBaseName };
                    GameManager.Instance.Test_AddBouquet(bouquetName, flowers, ribbonBaseName);
                    addedCount++;
                }

                Debug.Log($"[TutorialTestTool] 添加花束: {bouquetName} x{bouquetCountPerType}");
            }
        }

        Debug.Log($"[TutorialTestTool] 共添加 {addedCount} 个花束（{flowerList.Count} 种鲜花 × {ribbonList.Count} 种丝带 × {bouquetCountPerType} 个）");
        ShowCurrentStock();
    }

    /// <summary>
    /// 生成花束名称（与CustomerOrderCoordinator保持一致）
    /// 格式: FlowerName_RibbonName
    /// </summary>
    private string GenerateBouquetName(string flowerName, string ribbonName)
    {
        // 提取鲜花基础名称
        string flower = flowerName;
        if (flower.EndsWith("2"))
            flower = flower.Substring(0, flower.Length - 1);

        // 提取丝带名称
        string ribbon = ribbonName;
        if (ribbon.StartsWith("Ribbon"))
            ribbon = ribbon.Substring(6);

        return $"{flower}_{ribbon}";
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

        GameManager.Instance.Test_PrintAllInventory();
    }
}
