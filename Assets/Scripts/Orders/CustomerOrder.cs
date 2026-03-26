using System;
using UnityEngine;

/// <summary>
/// 单笔订单：客户序号 + 最多 3 种花（填 Prefab 根物体名，如 Rose2、Daisy2）。
/// </summary>
[Serializable]
public class CustomerOrder
{
    [Tooltip("显示为「客户N」，从 1 开始")]
    public int customerNumber = 1;

    [Tooltip("需要的花")]
    public string flowerPrefabName0;
    public string flowerPrefabName1;
    public string flowerPrefabName2;

    public string[] GetFlowerNames()
    {
        return new[] { flowerPrefabName0, flowerPrefabName1, flowerPrefabName2 };
    }
}
