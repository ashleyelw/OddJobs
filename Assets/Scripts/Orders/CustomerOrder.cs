using System;
using UnityEngine;


[Serializable]
public class CustomerOrder
{
    [Tooltip("显示为「客户N」，从 1 开始")]
    public int customerNumber = 1;

    [Tooltip("客户 GameObject 名字")]
    public string customerName;

    [Tooltip("需要的花")]
    public string flowerPrefabName0;
    public string flowerPrefabName1;
    public string flowerPrefabName2;

    public string[] GetFlowerNames()
    {
        return new[] { flowerPrefabName0, flowerPrefabName1, flowerPrefabName2 };
    }
}
