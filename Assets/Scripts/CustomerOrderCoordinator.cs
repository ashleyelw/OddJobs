using UnityEngine;
using System.Linq;

public class CustomerOrderCoordinator : InteractionZone
{
    [Header("花朵配置")]
    [SerializeField] private string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };

    [Header("丝带配置")]
    [SerializeField] private string[] availableRibbons = new string[] { "RibbonRed", "RibbonBlue", "RibbonYellow" };

    [Header("订单配置")]
    [Range(1, 3)]
    [SerializeField] private int flowersPerOrder = 2;

    [Header("教程模式")]
    [SerializeField] private bool isTutorialCustomer = false;

    [Header("单独订单 UI")]
    [SerializeField] private GameObject singleOrderUIPrefab;

    int _slotIndex = -1;
    int _customerNumber = 1;
    bool _hasOrderedThisSession = false;
    CustomerSpawner _spawner;
    string _instanceId;

    private GameObject _singleOrderUIInstance;
    private CustomerOrder _currentOrder;
    private bool _isTutorialCustomer = false;

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

    public void InitializeRibbons(string[] ribbons)
    {
        availableRibbons = ribbons;
    }

    public void SetTutorialCustomer(bool isTutorial)
    {
        _isTutorialCustomer = isTutorial;
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
            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 本次已下过单，跳过。");
            return;
        }

        _hasOrderedThisSession = true;

        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 开始下单流程");

        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerOrdered(_slotIndex);

        if (string.IsNullOrEmpty(_instanceId))
        {
            _instanceId = $"{_customerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        float orderTimeLimit = _isTutorialCustomer ? float.MaxValue :
            (OrderSystemController.Instance != null ? OrderSystemController.Instance.defaultOrderTimeLimit : 30f);

        CustomerOrder order = new CustomerOrder
        {
            customerNumber = _customerNumber,
            customerName = gameObject.name,
            instanceId = _instanceId,
            orderStartGameMinutes = GameTimeController.Instance != null
                ? GameTimeController.Instance.GetTotalMinutes()
                : 0,
            timeLimitMinutes = orderTimeLimit,
            isTutorialOrder = _isTutorialCustomer
        };

        string[] randomFlowers = GetRandomItems(availableFlowers, flowersPerOrder);
        order.flowerPrefabName0 = randomFlowers.Length > 0 ? randomFlowers[0] : "";
        order.flowerPrefabName1 = randomFlowers.Length > 1 ? randomFlowers[1] : "";
        order.flowerPrefabName2 = randomFlowers.Length > 2 ? randomFlowers[2] : "";

        string[] randomRibbons = GetRandomItems(availableRibbons, flowersPerOrder);
        order.ribbonPrefabName0 = randomRibbons.Length > 0 ? randomRibbons[0] : "";
        order.ribbonPrefabName1 = randomRibbons.Length > 1 ? randomRibbons[1] : "";
        order.ribbonPrefabName2 = randomRibbons.Length > 2 ? randomRibbons[2] : "";

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterActiveCustomer(gameObject.name, _slotIndex);
            GameManager.Instance.pendingOrders.Add(order);
            _currentOrder = order;

            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 已下单{( _isTutorialCustomer ? "(教程)" : "")}");
            OrderSystemController.Instance?.NotifyOrderAdded();
        }
    }

    void Update()
    {
        base.Update();

        if (isPlayerInside && _hasOrderedThisSession && _currentOrder != null)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                Debug.Log("调用了");
                ToggleSingleOrderUI();
            }
        }
    }

    void ToggleSingleOrderUI()
    {
        if (_singleOrderUIInstance == null)
        {
            ShowSingleOrderUI();
        }
        else
        {
            CloseSingleOrderUI();
        }
    }

    void ShowSingleOrderUI()
    {
        if (singleOrderUIPrefab == null)
        {
            Debug.LogWarning("[CustomerOrderCoordinator] 未设置 singleOrderUIPrefab");
            return;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CustomerOrderCoordinator] 场景中找不到 Canvas！");
            return;
        }

        _singleOrderUIInstance = Instantiate(singleOrderUIPrefab, canvas.transform);

        var view = _singleOrderUIInstance.GetComponent<SingleOrderView>();
        if (view != null)
        {
            view.Bind(_currentOrder);
            view.onClose.AddListener(CloseSingleOrderUI);
            view.onDeliver.AddListener(OnDeliverFromSingleUI);
        }
        else
        {
            Debug.LogWarning("[CustomerOrderCoordinator] 预制体上没有 SingleOrderView 组件");
        }
    }

    public void CloseSingleOrderUI()
    {
        if (_singleOrderUIInstance != null)
        {
            Destroy(_singleOrderUIInstance);
            _singleOrderUIInstance = null;
        }
    }

    void OnDeliverFromSingleUI()
    {
        if (OrderSystemController.Instance != null && _currentOrder != null)
        {
            OrderSystemController.Instance.TryDeliverOrder(_currentOrder);
        }
        CloseSingleOrderUI();
    }

    public void NotifyOrderCompleted()
    {
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 订单完成，离开。");
        CloseSingleOrderUI();
        if (_spawner != null && _slotIndex >= 0)
        {
            if (_isTutorialCustomer && _spawner.IsTutorialMode)
            {
                _spawner.SetTutorialCompleted();
            }
            _spawner.OnCustomerLeft(_slotIndex);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ForceCustomerLeave()
    {
        Debug.Log($"[CustomerOrderCoordinator] ForceCustomerLeave 被调用");
        CloseSingleOrderUI();
        if (_spawner != null && _slotIndex >= 0)
        {
            _spawner.OnCustomerLeft(_slotIndex);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    string[] GetRandomItems(string[] source, int count)
    {
        if (source == null || source.Length == 0)
        {
            Debug.LogWarning("[CustomerOrderCoordinator] 源数组为空！");
            return new string[3];
        }

        count = Mathf.Min(count, source.Length);

        string[] shuffled = (string[])source.Clone();
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