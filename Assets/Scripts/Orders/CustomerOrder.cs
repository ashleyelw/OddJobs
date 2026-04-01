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

    public string[] GetFlowerNames()
    {
        return new[] { flowerPrefabName0, flowerPrefabName1, flowerPrefabName2 };
    }

    [Tooltip("订单时限（秒）")]
    public float timeLimitMinutes = 30f;

    [Tooltip("下单时的游戏累计分钟数（用于计算超时）")]
    public int orderStartGameMinutes;

    [Tooltip("订单是否已超时")]
    public bool isTimedOut = false;

    [Tooltip("订单是否已完成交付")]
    public bool isDelivered = false;

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
}
