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
    // 如果设置了 bouquetName，则此订单使用花束库存进行检查和扣除
    // ============================================

    [Tooltip("【花束模式】订单需要的花束名称（设置后优先使用花束库存）")]
    public string bouquetName;

    [Tooltip("【花束模式】是否使用花束库存（如果为true，检查bouquetName而非鲜花）")]
    public bool useBouquetInventory = false;

    /// <summary>获取花束名称（如果 useBouquetInventory 为 true）</summary>
    public string GetBouquetName()
    {
        return useBouquetInventory ? bouquetName : null;
    }

    /// <summary>是否使用花束模式</summary>
    public bool IsUsingBouquetMode()
    {
        return useBouquetInventory && !string.IsNullOrEmpty(bouquetName);
    }

    public string[] GetFlowerNames()
    {
        return new[] { flowerPrefabName0, flowerPrefabName1, flowerPrefabName2 };
    }

    public string[] GetRibbonNames()
    {
        return new[] { ribbonPrefabName0, ribbonPrefabName1, ribbonPrefabName2 };
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