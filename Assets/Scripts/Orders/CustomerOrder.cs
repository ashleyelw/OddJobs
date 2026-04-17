using System;
using UnityEngine;


[Serializable]
public class CustomerOrder
{
    [Tooltip("显示为「客户N」，从 1 开始")]
    public int customerNumber = 1;

    [Tooltip("客户 GameObject 名字")]
    public string customerName;

    [Tooltip("客户唯一实例ID（跨场景标识）")]
    public string instanceId;

    [Tooltip("需要的花")]
    public string flowerPrefabName0;
    public string flowerPrefabName1;
    public string flowerPrefabName2;

    [Tooltip("需要的丝带（与花一一对应）")]
    public string ribbonPrefabName0;
    public string ribbonPrefabName1;
    public string ribbonPrefabName2;

    // ============================================
    // 【新增】花束模式订单支持
    // 如果设置了 bouquetNames，则此订单使用花束库存进行检查和扣除
    // ============================================

    [Tooltip("【花束模式】订单需要的花束名称数组（设置后优先使用花束库存）")]
    public string[] bouquetNames = new string[0];

    [Tooltip("【花束模式】是否使用花束库存（如果为true，检查bouquetNames而非鲜花）")]
    public bool useBouquetInventory = false;

    /// <summary>获取第一个花束名称（兼容旧代码）</summary>
    public string GetBouquetName()
    {
        return useBouquetInventory && bouquetNames != null && bouquetNames.Length > 0 ? bouquetNames[0] : null;
    }

    /// <summary>获取所有花束名称</summary>
    public string[] GetBouquetNames()
    {
        return useBouquetInventory ? bouquetNames : new string[0];
    }

    /// <summary>获取花束数量</summary>
    public int GetBouquetCount()
    {
        return bouquetNames != null ? bouquetNames.Length : 0;
    }

    /// <summary>是否使用花束模式</summary>
    public bool IsUsingBouquetMode()
    {
        return useBouquetInventory && bouquetNames != null && bouquetNames.Length > 0;
    }

    public string[] GetFlowerNames()
    {
        // 【花束模式】从 bouquetNames 解析鲜花名
        if (useBouquetInventory && bouquetNames != null && bouquetNames.Length > 0)
        {
            string[] flowers = new string[bouquetNames.Length];
            for (int i = 0; i < bouquetNames.Length; i++)
            {
                flowers[i] = ParseFlowerFromBouquet(bouquetNames[i]);
            }
            Debug.Log($"[CustomerOrder] 花束模式 - 鲜花数组: [{string.Join(", ", flowers)}]");
            return flowers;
        }
        return new[] { flowerPrefabName0, flowerPrefabName1, flowerPrefabName2 };
    }

    public string[] GetRibbonNames()
    {
        // 【花束模式】从 bouquetNames 解析丝带名
        if (useBouquetInventory && bouquetNames != null && bouquetNames.Length > 0)
        {
            string[] ribbons = new string[bouquetNames.Length];
            for (int i = 0; i < bouquetNames.Length; i++)
            {
                ribbons[i] = ParseRibbonFromBouquet(bouquetNames[i]);
            }
            return ribbons;
        }
        return new[] { ribbonPrefabName0, ribbonPrefabName1, ribbonPrefabName2 };
    }

    /// <summary>从花束名称解析鲜花名（格式：Flower_Ribbon）</summary>
    private string ParseFlowerFromBouquet(string bouquetName)
    {
        if (string.IsNullOrEmpty(bouquetName)) return "";
        int underscoreIndex = bouquetName.LastIndexOf('_');
        string flowerName = underscoreIndex > 0 ? bouquetName.Substring(0, underscoreIndex) : bouquetName;
        
        // 尝试添加 "2" 后缀（如果预制体名称带有 2）
        if (!IsKnownFlower(flowerName) && !IsKnownFlower(flowerName + "2"))
        {
            Debug.LogWarning($"[CustomerOrder] 未找到鲜花「{flowerName}」，尝试添加2后缀: {flowerName}2");
        }
        
        return flowerName;
    }

    private bool IsKnownFlower(string name)
    {
        // 这个方法用于检测鲜花名称是否已知
        // 简化处理：假设名称不是空的就返回 true
        return !string.IsNullOrEmpty(name);
    }

    /// <summary>从花束名称解析丝带名（格式：Flower_Ribbon）</summary>
    private string ParseRibbonFromBouquet(string bouquetName)
    {
        if (string.IsNullOrEmpty(bouquetName)) return "";
        int underscoreIndex = bouquetName.LastIndexOf('_');
        return underscoreIndex >= 0 && underscoreIndex < bouquetName.Length - 1
            ? bouquetName.Substring(underscoreIndex + 1)
            : "";
    }

    [Tooltip("订单时限（秒）")]
    public float timeLimitMinutes = 30f;

    [Tooltip("下单时的游戏累计分钟数（用于计算超时）")]
    public int orderStartGameMinutes;

    [Tooltip("订单是否已超时")]
    public bool isTimedOut = false;

    [Tooltip("订单是否已完成交付")]
    public bool isDelivered = false;

    [Tooltip("是否为教程订单（无限时间，不触发超时）")]
    public bool isTutorialOrder = false;

    public float GetRemainingMinutes(int currentGameMinutes)
    {
        if (isDelivered) return float.MaxValue;
        return timeLimitMinutes - (currentGameMinutes - orderStartGameMinutes);
    }

    public bool CheckTimeout(int currentGameMinutes)
    {
        if (isDelivered || isTimedOut) return false;
        if (orderStartGameMinutes <= 0) return false;

        float elapsedMinutes = currentGameMinutes - orderStartGameMinutes;

        if (elapsedMinutes >= timeLimitMinutes)
        {
            isTimedOut = true;
            return true;
        }
        return false;
    }

    [NonSerialized]
    public int debugCustomerNumber;

    public bool RequiresDialogueDelivery()
    {
        int sqrt = (int)Mathf.Sqrt(customerNumber);
        return sqrt * sqrt == customerNumber;
    }
}