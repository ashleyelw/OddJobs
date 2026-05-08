using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class CustomerOrderCoordinator : InteractionZone
{
    [Header("花束配置")]
    [Tooltip("是否使用花束模式订单（玩家需要先包装花束再交付）")]
    [SerializeField] private bool useBouquetMode = false;

    [Header("订单配置")]
    [Range(1, 3)]
    [SerializeField] private int flowersPerOrder = 2;
    [Tooltip("启用后每次订单随机生成花束数量；关闭后使用固定的 flowersPerOrder 数量")]
    [SerializeField] private bool enableRandomBouquetCount = false;

    [Header("【FloristMain专属】花束数量范围")]
    [Tooltip("仅在 FloristMain 场景生效。启用后每次订单随机生成花束数量（可配置范围）；关闭后使用固定的 flowersPerOrder 数量")]
    [SerializeField] private bool useFloristMainBouquetRange = false;
    [Tooltip("FloristMain 花束数量范围（最小值），默认1")]
    [Range(1, 6)]
    [SerializeField] private int bouquetCountMinFloristMain = 1;
    [Tooltip("FloristMain 花束数量范围（最大值），默认3")]
    [Range(1, 6)]
    [SerializeField] private int bouquetCountMaxFloristMain = 3;

    [Header("【Level2专属】花束数量范围")]
    [Tooltip("仅在 Level2 场景生效。启用后每次订单随机生成4-6束花束；否则使用通用的1-6范围")]
    [SerializeField] private bool useLevel2BouquetRange = true;
    [Tooltip("Level2 花束数量范围（最小值），默认4")]
    [Range(4, 5)]
    [SerializeField] private int bouquetCountMinLevel2 = 4;
    [Tooltip("Level2 花束数量范围（最大值），默认5")]
    [Range(4, 5)]
    [SerializeField] private int bouquetCountMaxLevel2 = 5;

    [Header("教程模式")]
    [SerializeField] private bool isTutorialCustomer = false;

    [Header("单独订单 UI")]
    [SerializeField] private GameObject singleOrderUIPrefab;

    int _slotIndex = -1;
    int _customerNumber = 1;
    bool _hasOrderedThisSession = false;
    CustomerSpawner _spawner;
    string _instanceId;

    // 通过 Initialize 方法传入的鲜花和丝带列表
    private string[] _availableFlowers;
    private string[] _availableRibbons;

    private GameObject _singleOrderUIInstance;
    private CustomerOrder _currentOrder;
    private bool _isTutorialCustomer = false;

    /// <summary>获取所有可用的鲜花名称列表（从CustomerSpawner获取）</summary>
    public string[] GetAvailableFlowers()
    {
        if (_spawner != null)
            return _spawner.GetAvailableFlowers();
        return _availableFlowers ?? new string[0];
    }

    /// <summary>获取所有可用的丝带名称列表（从CustomerSpawner获取）</summary>
    public string[] GetAvailableRibbons()
    {
        if (_spawner != null)
            return _spawner.GetAvailableRibbons();
        return _availableRibbons ?? new string[0];
    }

    public int SlotIndex => _slotIndex;
    public string InstanceId => _instanceId;

    public void Initialize(int slotIndex, int customerNumber, string[] flowers, 
                       int perOrder, CustomerSpawner spawner, string instanceId = null)
    {
        _slotIndex = slotIndex;
        _customerNumber = customerNumber;
        _availableFlowers = flowers;
        flowersPerOrder = perOrder;
        _spawner = spawner;
        _instanceId = instanceId ?? 
                    $"{customerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        // ADD THIS: restore order if one already exists for this customer
        RestoreCurrentOrder();
    }

    public void InitializeRibbons(string[] ribbons)
    {
        _availableRibbons = ribbons;
    }

    public void SetTutorialCustomer(bool isTutorial)
    {
        _isTutorialCustomer = isTutorial;
    }

    public void RestoreHasOrderedState(bool hasOrdered)
    {
        _hasOrderedThisSession = hasOrdered;
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 恢复下单状态: {hasOrdered}");

        // ADD THIS: restore order reference if customer has already ordered
        if (hasOrdered)
            RestoreCurrentOrder();
    }
    public void RestoreCurrentOrder()
    {
        if (GameManager.Instance == null) return;

        // Find the matching order in pendingOrders by customerNumber
        foreach (var order in GameManager.Instance.pendingOrders)
        {
            if (order.customerNumber == _customerNumber)
            {
                _currentOrder = order;
                Debug.Log($"[CustomerOrderCoordinator] Restored _currentOrder for " +
                        $"customer {_customerNumber}: {string.Join(", ", order.bouquetNames)}");
                return;
            }
        }

        Debug.LogWarning($"[CustomerOrderCoordinator] Could not find order for " +
                        $"customer {_customerNumber} in pendingOrders");
    }

    public void SetCustomerNumber(int number)
    {
        _customerNumber = number;
    }

        /// <summary>
    /// 生成花束名称
    /// 格式：flowerName_RibbonName，多个花用And连接
    /// 示例：Rose_RedAndDaisy_Blue
    /// <summary>
    /// 生成花束名称
    /// 格式：FlowerName_RibbonName
    /// 例如：Rose_Red、Daisy_Blue、Tulip_Yellow
    /// </summary>
    private string GenerateBouquetName(string flowerName, string ribbonName)
    {
        // 提取鲜花基础名称（移除 "2" 后缀，如 Rose2 -> Rose）
        string flower = NormalizeFlowerName(flowerName);
        
        // 提取丝带名称（移除 "Ribbon" 前缀，如 RibbonRed -> Red）
        string ribbon = NormalizeRibbonName(ribbonName);
        
        return $"{flower}_{ribbon}";
    }

    /// <summary>
    /// 获取当前花束模式是否启用
    /// </summary>
    public bool IsBouquetModeEnabled => useBouquetMode;

    private string NormalizeFlowerName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        // 移除可能的 "2" 后缀（如 Rose2 -> Rose）
        if (name.EndsWith("2"))
            name = name.Substring(0, name.Length - 1);
        return name;
    }

    private string NormalizeRibbonName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        // 移除 Ribbon 前缀（如 RibbonRed -> Red）
        if (name.StartsWith("Ribbon"))
            name = name.Substring(6);
        return name;
    }

    protected override void Interact()
    {
        // If already ordered, treat interaction as delivery attempt instead
        if (_hasOrderedThisSession)
        {
            if (_currentOrder != null)
            {
                Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 尝试交付订单。");
                TryDeliverCurrentOrder();
            }
            return;
        }

        _hasOrderedThisSession = true;
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 开始下单流程");

        if (_spawner != null && _slotIndex >= 0)
            _spawner.OnCustomerOrdered(_slotIndex);

        if (string.IsNullOrEmpty(_instanceId))
            _instanceId = $"{_customerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

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

        string[] allFlowers = GetAvailableFlowers();
        string[] allRibbons = GetAvailableRibbons();

        int bouquetsPerOrder;
        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel2 = sceneName == "Level2";

        if (isLevel2 && useLevel2BouquetRange)
        {
            bouquetsPerOrder = Random.Range(bouquetCountMinLevel2, bouquetCountMaxLevel2 + 1);
            Debug.Log($"[CustomerOrderCoordinator] 场景={sceneName}，Level2花束范围: {bouquetCountMinLevel2}-{bouquetCountMaxLevel2}，实际生成: {bouquetsPerOrder}");
        }
        else if (useFloristMainBouquetRange)
        {
            bouquetsPerOrder = Random.Range(bouquetCountMinFloristMain, bouquetCountMaxFloristMain + 1);
            Debug.Log($"[CustomerOrderCoordinator] 场景={sceneName}，FloristMain花束范围: {bouquetCountMinFloristMain}-{bouquetCountMaxFloristMain}，实际生成: {bouquetsPerOrder}");
        }
        else
        {
            bouquetsPerOrder = flowersPerOrder;
            Debug.Log($"[CustomerOrderCoordinator] 场景={sceneName}，固定花束数量: {bouquetsPerOrder}");
        }
        string[] chosenBouquets = new string[bouquetsPerOrder];
        
        // 填充订单的花朵和丝带字段（用于显示，取第一个花束）
        string[] firstFlowers = GetRandomItems(allFlowers, 1);
        string[] firstRibbons = GetRandomItems(allRibbons, 1);

        order.flowerPrefabName0 = firstFlowers.Length > 0 ? firstFlowers[0] : "";
        order.flowerPrefabName1 = "";
        order.flowerPrefabName2 = "";
        order.ribbonPrefabName0 = firstRibbons.Length > 0 ? firstRibbons[0] : "";
        order.ribbonPrefabName1 = "";
        order.ribbonPrefabName2 = "";

        var bouquetList = new System.Collections.Generic.List<string>();
        string[] fixedRibbon = GetRandomItems(allRibbons, 1);
        for (int i = 0; i < bouquetsPerOrder; i++)
        {
            string[] randomFlowers = GetRandomItems(allFlowers, 1);
            string bouquet = GenerateBouquetName(randomFlowers[0], fixedRibbon[0]);
            bouquetList.Add(bouquet);
        }

        order.bouquetNames = bouquetList.ToArray();
        order.useBouquetInventory = true;

        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 生成花束订单（{bouquetsPerOrder}个）: " +
                $"{string.Join(", ", order.bouquetNames)}");

        Debug.Log($"[CustomerOrderCoordinator] 下一步：注册到 GameManager 和 OrderSystemController");
        Debug.Log($"[CustomerOrderCoordinator]   GameManager.Instance = {GameManager.Instance != null}");
        Debug.Log($"[CustomerOrderCoordinator]   OrderSystemController.Instance = {OrderSystemController.Instance != null}");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterActiveCustomer(gameObject.name, _slotIndex);
            GameManager.Instance.pendingOrders.Add(order);
            _currentOrder = order;

            Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 已下单并注册到 GameManager（pendingOrders.Count={GameManager.Instance.pendingOrders.Count}）" +
                    $"{(_isTutorialCustomer ? "(教程)" : "")}");
            OrderSystemController.Instance?.NotifyOrderAdded();
        }
        else
        {
            Debug.LogError($"[CustomerOrderCoordinator] GameManager.Instance 为 null！无法注册订单！");
        }
    }

    // ADD this new method
    void Update()
    {
        // Log every frame when player is inside so we can see state
        if (isPlayerInside)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                Debug.Log($"[CustomerOrderCoordinator] E pressed - " +
                        $"hasOrdered={_hasOrderedThisSession}, " +
                        $"currentOrder={_currentOrder != null}, " +
                        $"isPlayerInside={isPlayerInside}");

                if (_hasOrderedThisSession && _currentOrder != null)
                {
                    Debug.Log("[CustomerOrderCoordinator] Routing to TryDeliverCurrentOrder");
                    TryDeliverCurrentOrder();
                }
                else if (!_hasOrderedThisSession)
                {
                    Debug.Log("[CustomerOrderCoordinator] Routing to Interact (place order)");
                    Interact();
                }
                else
                {
                    Debug.Log($"[CustomerOrderCoordinator] E pressed but no action taken - " +
                            $"hasOrdered={_hasOrderedThisSession}, " +
                            $"currentOrder is null={_currentOrder == null}");
                }
            }
        }

        // Q key to toggle single order UI
        if (isPlayerInside && _hasOrderedThisSession && _currentOrder != null)
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                ToggleSingleOrderUI();
            }
        }
    }

    void TryDeliverCurrentOrder()
    {
        Debug.Log($"[CustomerOrderCoordinator] TryDeliverCurrentOrder called");
        Debug.Log($"[CustomerOrderCoordinator] _currentOrder null={_currentOrder == null}");
        Debug.Log($"[CustomerOrderCoordinator] OrderSystemController null={OrderSystemController.Instance == null}");

        if (_currentOrder == null)
        {
            Debug.LogWarning("[CustomerOrderCoordinator] _currentOrder is null, cannot deliver");
            return;
        }

        Debug.Log($"[CustomerOrderCoordinator] Order bouquets: " +
                $"{string.Join(", ", _currentOrder.bouquetNames)}");
        Debug.Log($"[CustomerOrderCoordinator] Order isDelivered={_currentOrder.isDelivered}, " +
                $"isTimedOut={_currentOrder.isTimedOut}");

        if (OrderSystemController.Instance != null)
        {
            Debug.Log("[CustomerOrderCoordinator] Calling TryDeliverOrder");
            OrderSystemController.Instance.TryDeliverOrder(_currentOrder);
        }
        else
        {
            Debug.LogWarning("[CustomerOrderCoordinator] OrderSystemController.Instance is null!");
        }
    }

    void ToggleSingleOrderUI()
    {
        if (_singleOrderUIInstance == null)
        {
            ShowSingleOrderUI();
            Debug.Log("还有订单");
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
        Debug.Log($"[CustomerOrderCoordinator] 客户 {gameObject.name} 订单完成，开始离开流程。");
        Debug.Log($"[CustomerOrderCoordinator] _spawner={_spawner != null}, _slotIndex={_slotIndex}, _isTutorialCustomer={_isTutorialCustomer}");
        if (_spawner != null)
            Debug.Log($"[CustomerOrderCoordinator] _spawner.IsTutorialMode={_spawner.IsTutorialMode}");
        
        CloseSingleOrderUI();
        
        if (_spawner != null && _slotIndex >= 0)
        {
            Debug.Log($"[CustomerOrderCoordinator] 调用 _spawner.OnCustomerLeft({_slotIndex})");
            if (_isTutorialCustomer && _spawner.IsTutorialMode)
            {
                _spawner.SetTutorialCompleted();
            }
            _spawner.OnCustomerLeft(_slotIndex);
        }
        else
        {
            Debug.LogWarning($"[CustomerOrderCoordinator] 条件不满足，_spawner={_spawner != null}, _slotIndex={_slotIndex}");
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