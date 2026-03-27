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

    public void SetCustomerNumber(int number)
    {
        customerNumber = number;
    }

    protected override void Interact()
    {
        if (isOnCooldown)
        {
            Debug.Log($"[Customer {customerNumber}] 冷却中，请稍候...");
            return;
        }
        Debug.Log("按下了");
        CustomerOrder order = new CustomerOrder
        {
            customerNumber = this.customerNumber,
            customerName = gameObject.name
        };

        string[] randomFlowers = GetRandomFlowers(flowersPerOrder);
        order.flowerPrefabName0 = randomFlowers[0];
        order.flowerPrefabName1 = randomFlowers[1];
        order.flowerPrefabName2 = randomFlowers[2];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterActiveCustomer(gameObject.name, -1);
            GameManager.Instance.pendingOrders.Add(order);
            Debug.Log($"[Customer {customerNumber}] 已下单: {order.flowerPrefabName0}, {order.flowerPrefabName1}, {order.flowerPrefabName2}");
        }
        else
        {
            Debug.LogError("[CustomerInteraction] 未找到 GameManager 实例！");
            return;
        }

        StartCooldown();
    }

    private string[] GetRandomFlowers(int count)
    {
        if (availableFlowers == null || availableFlowers.Length == 0)
        {
            Debug.LogWarning("[CustomerInteraction] availableFlowers 为空！");
            return new string[] { "", "", "" };
        }

        count = Mathf.Min(count, availableFlowers.Length);

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

    private void StartCooldown()
    {
        isOnCooldown = true;
        cooldownTimer = orderCooldown;
    }

    private void Update()
    {
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
}
