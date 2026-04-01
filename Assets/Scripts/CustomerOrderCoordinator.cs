using UnityEngine;

public class CustomerOrderCoordinator : InteractionZone
{
    [Header("花朵配置")]
    [SerializeField] private string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };

    [Header("订单配置")]
    [Range(1, 3)]
    [SerializeField] private int flowersPerOrder = 2;

    int _slotIndex = -1;
    int _customerNumber = 1;
    
    // 关键：标记此客户是否已下单（跨场景持久化）
    bool _hasOrderedThisSession = false;
    
    CustomerSpawner _spawner;

    // 唯一实例ID（用于跨场景标识）
    string _instanceId;

    public int SlotIndex => _slotIndex;
    public string InstanceId => _instanceId;

    public void Initialize(int slotIndex, int customerNumber, string[] flowers, int perOrder, CustomerSpawner spawner, string instanceId = null)
    {
        _slotIndex = slotIndex;
        _customerNumber = customerNumber;
        availableFlowers = flowers;
        flowersPerOrder = perOrder;
        _spawner = spawner;
        _instanceId = instanceId ?? $"{customerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        // 重要：不在这里重置下单状态，由外部决定是否需要重置
    }

    /// <summary>
    /// 恢复客户的下单状态（场景切换后调用）
    /// </summary>
    public void RestoreHasOrderedState(bool hasOrdered)
    {
        _hasOrderedThisSession = hasOrdered;
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 恢复下单状态: {hasOrdered}");
    }

    public void SetCustomerNumber(int number)
    {
        _customerNumber = number;
    }

    protected override void Interact()
    {
        // 关键检查：如果已经下过单，拒绝再次下单
        if (_hasOrderedThisSession)
        {
            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 本次已下过单，拒绝重复下单。");
            return;
        }

        _hasOrderedThisSession = true;

        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerOrdered(_slotIndex);

        CustomerOrder order = new CustomerOrder
        {
            customerNumber = _customerNumber,
            customerName = gameObject.name,
            instanceId = _instanceId
        };

        string[] randomFlowers = GetRandomFlowers(flowersPerOrder);
        order.flowerPrefabName0 = randomFlowers[0];
        order.flowerPrefabName1 = randomFlowers[1];
        order.flowerPrefabName2 = randomFlowers[2];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterActiveCustomer(gameObject.name, _slotIndex);
            GameManager.Instance.pendingOrders.Add(order);
            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 已下单: {order.flowerPrefabName0}, {order.flowerPrefabName1}, {order.flowerPrefabName2}");
            OrderSystemController.Instance?.NotifyOrderAdded();
        }
    }

    public void NotifyOrderCompleted()
    {
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 订单完成，离开。");
        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerLeft(_slotIndex);
        else
            Destroy(gameObject);
    }

    string[] GetRandomFlowers(int count)
    {
        if (availableFlowers == null || availableFlowers.Length == 0)
        {
            Debug.LogWarning("[CustomerOrderCoordinator] availableFlowers 为空！");
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
            result[i] = i < count ? shuffled[i] : "";
        return result;
    }
}
