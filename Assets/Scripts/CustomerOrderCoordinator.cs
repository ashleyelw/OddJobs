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
    bool _hasOrderedThisSession = false;
    CustomerSpawner _spawner;
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
    }

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
        if (_hasOrderedThisSession)
        {
            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 本次已下过单，拒绝重复下单。");
            return;
        }

        _hasOrderedThisSession = true;

        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 开始下单流程, _instanceId={_instanceId}, _slotIndex={_slotIndex}");

        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerOrdered(_slotIndex);

        if (string.IsNullOrEmpty(_instanceId))
        {
            _instanceId = $"{_customerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            Debug.LogWarning($"[CustomerOrderCoordinator] _instanceId 为空，已重新生成: {_instanceId}");
        }

        float orderTimeLimit = OrderSystemController.Instance != null
            ? OrderSystemController.Instance.defaultOrderTimeLimit
            : 30f;

        CustomerOrder order = new CustomerOrder
        {
            customerNumber = _customerNumber,
            customerName = gameObject.name,
            instanceId = _instanceId,
            orderStartGameMinutes = GameTimeController.Instance != null
                ? GameTimeController.Instance.GetTotalMinutes()
                : 0,
            timeLimitMinutes = orderTimeLimit
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
            Debug.Log($"[CustomerOrderCoordinator] 准备调用 OrderSystemController.Instance?.NotifyOrderAdded()");
            OrderSystemController.Instance?.NotifyOrderAdded();
            Debug.Log($"[CustomerOrderCoordinator] NotifyOrderAdded 调用完成");
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

    public void ForceCustomerLeave()
    {
        Debug.Log($"[CustomerOrderCoordinator] ForceCustomerLeave 被调用 - 客户: {gameObject.name}, _slotIndex: {_slotIndex}, _instanceId: {_instanceId}, _spawner: {_spawner != null}");
        if (_spawner != null && _slotIndex >= 0)
        {
            Debug.Log($"[CustomerOrderCoordinator] 调用 _spawner.OnCustomerLeft({_slotIndex})");
            _spawner.OnCustomerLeft(_slotIndex);
        }
        else
        {
            Debug.Log($"[CustomerOrderCoordinator] _spawner 或 _slotIndex 无效，直接 Destroy");
            Destroy(gameObject);
        }
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
