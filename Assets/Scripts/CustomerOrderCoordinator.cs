using UnityEngine;

public class CustomerOrderCoordinator : InteractionZone
{
    [Header("")]
    [SerializeField] private string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };

    [Header("")]
    [Range(1, 3)]
    [SerializeField] private int flowersPerOrder = 2;

    int _slotIndex = -1;
    int _customerNumber = 1;
    bool _hasOrderedThisSession = false;
    CustomerSpawner _spawner;

    public int SlotIndex => _slotIndex;

    public void Initialize(int slotIndex, int customerNumber, string[] flowers, int perOrder, CustomerSpawner spawner)
    {
        _slotIndex = slotIndex;
        _customerNumber = customerNumber;
        availableFlowers = flowers;
        flowersPerOrder = perOrder;
        _spawner = spawner;
        _hasOrderedThisSession = false;
    }

    public void SetCustomerNumber(int number)
    {
        _customerNumber = number;
    }

    protected override void Interact()
    {
        if (_hasOrderedThisSession)
        {
            Debug.Log($"[Customer {_customerNumber}] 本次已下过单。");
            return;
        }

        _hasOrderedThisSession = true;

        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerOrdered(_slotIndex);

        CustomerOrder order = new CustomerOrder
        {
            customerNumber = _customerNumber,
            customerName = gameObject.name
        };

        string[] randomFlowers = GetRandomFlowers(flowersPerOrder);
        order.flowerPrefabName0 = randomFlowers[0];
        order.flowerPrefabName1 = randomFlowers[1];
        order.flowerPrefabName2 = randomFlowers[2];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterActiveCustomer(gameObject.name, _slotIndex);
            GameManager.Instance.pendingOrders.Add(order);
            Debug.Log($"[Customer {_customerNumber}] 已下单: {order.flowerPrefabName0}, {order.flowerPrefabName1}, {order.flowerPrefabName2}");
            OrderSystemController.Instance?.NotifyOrderAdded();
        }
    }

    public void NotifyOrderCompleted()
    {
        Debug.Log($"[Customer {_customerNumber}] 订单完成，离开。");
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
