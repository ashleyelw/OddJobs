using UnityEngine;

public class CustomerInteraction : InteractionZone
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

    [Tooltip("下单后冷却时间（秒），防止重复下单")]
    [SerializeField] private float orderCooldown = 2f;

    private bool isOnCooldown = false;
    private float cooldownTimer = 0f;

    protected override void Interact()
    {
        if (isOnCooldown)
        {
            Debug.Log($"[Customer {customerNumber}] 冷却中，请稍候...");
            return;
        }
        Debug.Log("按下了");
        // 创建新订单
        CustomerOrder order = new CustomerOrder
        {
            customerNumber = this.customerNumber
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
        }
        else
        {
            Debug.LogError("[CustomerInteraction] 未找到 GameManager 实例！");
            return;
        }

        // 开始冷却
        StartCooldown();
    }

    private string[] GetRandomFlowers(int count)
    {
        if (availableFlowers == null || availableFlowers.Length == 0)
        {
            Debug.LogWarning("[CustomerInteraction] availableFlowers 为空！");
            return new string[] { "", "", "" };
        }

        // 如果可用花朵少于需要数量，返回所有可用花朵
        count = Mathf.Min(count, availableFlowers.Length);

        // Fisher-Yates 洗牌算法打乱顺序
        string[] shuffled = (string[])availableFlowers.Clone();
        for (int i = shuffled.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        // 取前 count 个，其余填空
        string[] result = new string[3];
        for (int i = 0; i < 3; i++)
        {
            result[i] = i < count ? shuffled[i] : "";
        }

        return result;
    }

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = orderCooldown;
    }

    private void Update()
    {
        // 处理冷却计时
        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                isOnCooldown = false;
                cooldownTimer = 0f;
            }
        }
    }

    // public void ResetCooldown() { isOnCooldown = false; cooldownTimer = 0f; }
}
