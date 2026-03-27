using UnityEngine;

public class CustomerOrderCoordinator : InteractionZone
{
    [Header("客户设置")]
    [Tooltip("客户编号，用于显示为「客户N」")]
    [SerializeField] private int customerNumber = 1;

    [Header("订单设置")]
    [Tooltip("可选的花朵预制体名称列表")]
    [SerializeField] private string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };

    [Tooltip("每个订单需要多少种花 (1-3)")]
    [Range(1, 3)]
    [SerializeField] private int flowersPerOrder = 2;

    private bool hasOrderedThisSession = false;

    protected override void Interact()
    {
        if (hasOrderedThisSession)
        {
            Debug.Log($"[Customer {customerNumber}] 本次已下过单，无需重复下单。");
            return;
        }

        hasOrderedThisSession = true;

        // 创建新订单
        CustomerOrder order = new CustomerOrder
        {
            customerNumber = this.customerNumber,
            customerName = gameObject.name
        };

        // 随机分配花朵
        string[] randomFlowers = GetRandomFlowers(flowersPerOrder);
        order.flowerPrefabName0 = randomFlowers[0];
        order.flowerPrefabName1 = randomFlowers[1];
        order.flowerPrefabName2 = randomFlowers[2];

        // 添加到 GameManager 的待处理订单列表
        if (GameManager.Instance != null)
        {
            GameManager.Instance.pendingOrders.Add(order);
            Debug.Log($"[Customer {customerNumber}] 已下单: {order.flowerPrefabName0}, {order.flowerPrefabName1}, {order.flowerPrefabName2}");

            // 同步刷新订单面板
            OrderSystemController.Instance?.NotifyOrderAdded();
        }
        else
        {
            Debug.LogError("[CustomerOrderCoordinator] 未找到 GameManager 实例！");
        }
    }

    private string[] GetRandomFlowers(int count)
    {
        if (availableFlowers == null || availableFlowers.Length == 0)
        {
            Debug.LogWarning("[CustomerOrderCoordinator] availableFlowers 为空！");
            return new string[] { "", "", "" };
        }

        count = Mathf.Min(count, availableFlowers.Length);

        // Fisher-Yates 洗牌
        string[] shuffled = (string[])availableFlowers.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        string[] result = new string[3];
        for (int i = 0; i < 3; i++)
        {
            result[i] = i < count ? shuffled[i] : "";
        }
        return result;
    }
}
