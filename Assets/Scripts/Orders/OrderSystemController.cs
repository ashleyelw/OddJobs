using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class OrderSystemController : MonoBehaviour
{
    public const int OrdersPerPage = 3;

    [Header("根节点（打开/关闭整个订单界面）")]
    [SerializeField] GameObject ordersRoot;

    [Header("Prefab")]
    [SerializeField] GameObject panelPagePrefab;
    [SerializeField] GameObject orderRowPrefab;

    [Header("生成到的父节点")]
    [SerializeField] Transform panelPagesParent;

    [Header("依赖")]
    [SerializeField] FlowerSpriteRegistry flowerSpriteRegistry;

    [Header("调试")]
    [SerializeField] List<CustomerOrder> debugOrders = new List<CustomerOrder>();

    [Header("金币奖励（每完成一个订单获得的金币）")]
    [SerializeField] int coinRewardPerOrder = 10;

    [Header("提示 UI（运行时显示不足/成功信息）")]
    [SerializeField] GameObject tipRoot;
    [SerializeField] Text tipText;
    [SerializeField] float tipDuration = 2.5f;

    [Header("金币显示（当前金币）")]
    [SerializeField] Text coinDisplayText;

    [Header("时限设置")]
    public float defaultOrderTimeLimit = 20f;

    public static OrderSystemController Instance { get; private set; }

    private bool _isPanelShowing = false;

    private OrderPanelPage _lastPage;

    readonly List<GameObject> _pageInstances = new List<GameObject>();
    int _currentPageIndex;

    readonly List<OrderRowView> _activeOrderRows = new List<OrderRowView>();

    int _lastCheckedGameMinutes = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (ordersRoot != null)
            ordersRoot.SetActive(false);
        OpenDebugOrders();
    }

    void Update()
    {
        if (GameTimeController.Instance != null)
        {
            int currentMinutes = GameTimeController.Instance.GetTotalMinutes();
            if (currentMinutes != _lastCheckedGameMinutes)
            {
                _lastCheckedGameMinutes = currentMinutes;
                CheckAllOrdersTimeout(currentMinutes);
            }
        }

        UpdateAllOrderTimeDisplays();
    }

    void UpdateAllOrderTimeDisplays()
    {
        if (GameTimeController.Instance == null) return;
        int currentMinutes = GameTimeController.Instance.GetTotalMinutes();

        foreach (var row in _activeOrderRows)
        {
            if (row == null) continue;

            var order = row.GetBoundOrder();
            if (order == null) continue;

            row.UpdateTimeDisplay(order);

            if (!order.isTimedOut && !row.IsClosed() && order.orderStartGameMinutes > 0)
            {
                if (order.CheckTimeout(currentMinutes))
                {
                    Debug.Log($"[OrderSystem] 帧检测到订单超时: 客户{order.customerNumber}");
                    HandleOrderTimeout(order);
                }
            }
        }
    }

    public void OpenWithOrders(IReadOnlyList<CustomerOrder> orders)
    {
        ClearPages();
        if (orders == null || orders.Count == 0)
        {
            Debug.LogWarning("[OrderSystem] 订单列表为空。");
            if (ordersRoot != null)
                ordersRoot.SetActive(false);
            return;
        }

        if (panelPagePrefab == null || orderRowPrefab == null || panelPagesParent == null)
        {
            Debug.LogError("[OrderSystem] 请指定 panelPagePrefab、orderRowPrefab、panelPagesParent。");
            return;
        }

        int pageCount = Mathf.CeilToInt(orders.Count / (float)OrdersPerPage);
        for (int p = 0; p < pageCount; p++)
        {
            var pageGo = Instantiate(panelPagePrefab, panelPagesParent);
            _pageInstances.Add(pageGo);

            var page = pageGo.GetComponent<OrderPanelPage>();
            if (page == null)
            {
                Debug.LogError("[OrderSystem] Panel Prefab 上需要 OrderPanelPage 组件。");
                continue;
            }

            int start = p * OrdersPerPage;
            for (int k = 0; k < OrdersPerPage && start + k < orders.Count; k++)
                CreateOrderRowInSlot(page, k, orders[start + k]);
        }

        SetupNavigation(pageCount);
        _currentPageIndex = 0;
        ShowPage(0);

        if (ordersRoot != null)
            ordersRoot.SetActive(true);

        _isPanelShowing = true;
        UpdateCoinDisplay();
        HideTip();
        if (_pageInstances.Count > 0)
            _lastPage = _pageInstances[^1].GetComponent<OrderPanelPage>();
    }

    public void OpenDebugOrders()
    {
        OpenWithOrders(debugOrders);
    }

    public void OpenPendingOrdersFromGameManager()
    {
        if (GameManager.Instance != null && GameManager.Instance.pendingOrders != null && GameManager.Instance.pendingOrders.Count > 0)
            OpenWithOrders(GameManager.Instance.pendingOrders);
        else
            OpenWithOrders(debugOrders);
    }

    public void Close()
    {
        if (ordersRoot != null)
            ordersRoot.SetActive(false);
        _isPanelShowing = false;
        _lastPage = null;
    }

    public void Toggle()
    {
        if (ordersRoot == null)
            return;
        bool show = !ordersRoot.activeSelf;
        if (show)
            OpenPendingOrdersFromGameManager();
        else
            ordersRoot.SetActive(false);
    }

    public void AppendOrder(CustomerOrder order)
    {
        if (order == null) return;

        if (!_isPanelShowing || ordersRoot == null || !ordersRoot.activeSelf)
        {
            OpenPendingOrdersFromGameManager();
            return;
        }

        if (panelPagePrefab == null || orderRowPrefab == null || flowerSpriteRegistry == null)
        {
            Debug.LogError("[OrderSystem] AppendOrder 所需引用未设置。");
            return;
        }

        bool appended = false;
        if (_lastPage != null)
        {
            for (int i = 0; i < OrdersPerPage; i++)
            {
                var slot = _lastPage.GetSlot(i);
                if (slot != null && slot.childCount == 0)
                {
                    CreateOrderRowInSlot(_lastPage, i, order);
                    appended = true;
                    break;
                }
            }
        }

        if (!appended)
        {
            var newPageGo = Instantiate(panelPagePrefab, panelPagesParent);
            _pageInstances.Add(newPageGo);
            var newPage = newPageGo.GetComponent<OrderPanelPage>();
            if (newPage == null)
            {
               
                Destroy(newPageGo);
                return;
            }

            CreateOrderRowInSlot(newPage, 0, order);
            SetupNavigation(_pageInstances.Count);

            ShowPage(_pageInstances.Count - 1);
            _lastPage = newPage;
        }

        Debug.Log($"[OrderSystem] 追加订单: 客户{order.customerNumber}");
    }

    public void NotifyOrderAdded()
    {
        Debug.Log($"[OrderSystem] NotifyOrderAdded 被调用，pendingOrders数量: {GameManager.Instance?.pendingOrders?.Count ?? -1}");

        if (GameManager.Instance != null &&
            GameManager.Instance.pendingOrders != null &&
            GameManager.Instance.pendingOrders.Count > 0)
        {
            OpenPendingOrdersFromGameManager();

            int orderCount = GameManager.Instance.pendingOrders.Count;
            int lastPageIndex = Mathf.CeilToInt(orderCount / (float)OrdersPerPage) - 1;
            if (lastPageIndex < 0) lastPageIndex = 0;

            Debug.Log($"[OrderSystem] 新订单在第{lastPageIndex}页（{orderCount}个订单，每页{OrdersPerPage}个）");

            if (_pageInstances.Count > 0)
            {
                ShowPage(lastPageIndex);
            }
        }
    }

    void CreateOrderRowInSlot(OrderPanelPage page, int slotIndex, CustomerOrder order)
    {
        var slot = page.GetSlot(slotIndex);
        if (slot == null)
        {
            Debug.LogWarning($"[OrderSystem] 第 {slotIndex} 号槽位未设置，已跳过该行。");
            return;
        }

        var rowGo = Instantiate(orderRowPrefab, slot);
        var row = rowGo.GetComponent<OrderRowView>();
        if (row == null)
        {
            
            return;
        }

        if (order.orderStartGameMinutes <= 0)
        {
            int gameMinutes = GameTimeController.Instance != null
                ? GameTimeController.Instance.GetTotalMinutes()
                : 0;
            order.orderStartGameMinutes = gameMinutes;
            order.timeLimitMinutes = defaultOrderTimeLimit;
            Debug.Log($"[OrderSystem] 订单时限已设置 - 客户{order.customerNumber}: " +
                      $"开始时间={gameMinutes}, 时限={defaultOrderTimeLimit}分钟");
        }

        row.BindWithDeliver(order.customerNumber, order.GetFlowerNames(),
            flowerSpriteRegistry, order, TryDeliverOrder, OnOrderCloseClicked);

        if (order.isTimedOut)
        {
            row.MarkAsTimedOut();
        }

        _activeOrderRows.Add(row);
    }

    void SetupNavigation(int pageCount)
    {
        for (int i = 0; i < _pageInstances.Count; i++)
        {
            var page = _pageInstances[i].GetComponent<OrderPanelPage>();
            if (page == null)
                continue;

            int idx = i;
            if (page.nextPageButton != null)
            {
                page.nextPageButton.onClick.RemoveAllListeners();
                bool hasNext = i < pageCount - 1;
                page.nextPageButton.gameObject.SetActive(hasNext);
                if (hasNext)
                    page.nextPageButton.onClick.AddListener(() => ShowPage(idx + 1));
            }

            if (page.prevPageButton != null)
            {
                page.prevPageButton.onClick.RemoveAllListeners();
                bool hasPrev = i > 0;
                page.prevPageButton.gameObject.SetActive(hasPrev);
                if (hasPrev)
                    page.prevPageButton.onClick.AddListener(() => ShowPage(idx - 1));
            }
        }
    }

    void ShowPage(int index)
    {
        index = Mathf.Clamp(index, 0, Mathf.Max(0, _pageInstances.Count - 1));
        _currentPageIndex = index;

        for (int i = 0; i < _pageInstances.Count; i++)
        {
            if (_pageInstances[i] != null)
                _pageInstances[i].SetActive(i == index);
        }
    }

    void ClearPages()
    {
        foreach (var go in _pageInstances)
        {
            if (go != null)
                Destroy(go);
        }
        _pageInstances.Clear();
        _lastPage = null;
        _activeOrderRows.Clear();
    }

    void CheckAllOrdersTimeout(int currentGameMinutes)
    {
        if (GameManager.Instance == null || GameManager.Instance.pendingOrders == null)
            return;

        foreach (var order in GameManager.Instance.pendingOrders.ToList())
        {
            if (order == null || order.isDelivered) continue;
            if (order.orderStartGameMinutes <= 0) continue;

            bool timedOut = order.CheckTimeout(currentGameMinutes);
            if (timedOut)
            {
                Debug.Log($"[OrderSystem] 订单超时: 客户{order.customerNumber}, " +
                          $"开始={order.orderStartGameMinutes}, 当前={currentGameMinutes}, " +
                          $"经过={currentGameMinutes - order.orderStartGameMinutes}, " +
                          $"时限={order.timeLimitMinutes}");
                HandleOrderTimeout(order);
            }
        }
    }

    void HandleOrderTimeout(CustomerOrder order)
    {
        Debug.Log($"[OrderSystem] HandleOrderTimeout 被调用 - 订单: {order.customerNumber}, instanceId: {order.instanceId}, customerName: {order.customerName}");

        order.isTimedOut = true;

        foreach (var row in _activeOrderRows)
        {
            if (row.GetBoundOrder() == order)
            {
                row.MarkAsTimedOut();
                break;
            }
        }

        NotifyCustomerLeft(order);

        ShowTip($"Order timeout! Customer {order.customerNumber} left.");

        Debug.Log($"[OrderSystem] 订单超时处理完成: 客户{order.customerNumber}, " +
                  $"客户已消失，等待用户点击Close关闭订单");
    }

    void OnOrderCloseClicked(CustomerOrder order)
    {
        Debug.Log($"[OrderSystem] 关闭订单: 客户{order.customerNumber}");
        RemoveOrder(order);
    }

    void NotifyCustomerLeft(CustomerOrder order)
    {
        Debug.Log($"[OrderSystem] NotifyCustomerLeft 被调用 - 订单: {order?.customerNumber}, instanceId: {order?.instanceId}, customerName: {order?.customerName}");

        if (CustomerSpawner.Instance == null)
        {
            Debug.LogWarning("[OrderSystem] CustomerSpawner.Instance 为 null!");
            TryForceCustomerLeaveByName(order);
            return;
        }

        bool removedBySlot = TryRemoveCustomerBySlotIndex(order);

        if (!removedBySlot && !string.IsNullOrEmpty(order.instanceId))
        {
            Debug.Log($"[OrderSystem] 尝试通过 instanceId 查找槽位: {order.instanceId}");
            for (int i = 0; i < 4; i++)
            {
                var slotData = CustomerSpawner.Instance.GetSlotData(i);
                if (slotData != null && slotData.instanceId == order.instanceId)
                {
                    CustomerSpawner.Instance.OnCustomerLeft(i);
                    Debug.Log($"[OrderSystem] 已通过 instanceId 通知客户离开槽位: {i}");
                    return;
                }
            }
            Debug.LogWarning($"[OrderSystem] 未找到匹配的 instanceId: {order.instanceId}");
        }

        if (!removedBySlot)
        {
            TryForceCustomerLeaveByName(order);
        }
    }

    bool TryRemoveCustomerBySlotIndex(CustomerOrder order)
    {
        if (order == null || CustomerSpawner.Instance == null) return false;

        for (int i = 0; i < 4; i++)
        {
            var slotData = CustomerSpawner.Instance.GetSlotData(i);
            if (slotData != null && slotData.customerNumber == order.customerNumber)
            {
                if (!string.IsNullOrEmpty(order.instanceId) &&
                    !string.IsNullOrEmpty(slotData.instanceId) &&
                    order.instanceId != slotData.instanceId)
                {
                    continue;
                }

                Debug.Log($"[OrderSystem] 通过 customerNumber={order.customerNumber} 匹配到槽位 {i}，调用 OnCustomerLeft");
                CustomerSpawner.Instance.OnCustomerLeft(i);
                return true;
            }
        }

        return false;
    }

    void TryForceCustomerLeaveByName(CustomerOrder order)
    {
        if (string.IsNullOrEmpty(order.customerName))
        {
            Debug.LogWarning("[OrderSystem] customerName 也为空，无法找到客户");
            return;
        }

        var coordinator = FindObjectsOfType<CustomerOrderCoordinator>()
            .FirstOrDefault(c => c.gameObject.name == order.customerName);

        if (coordinator != null)
        {
            Debug.Log($"[OrderSystem] 通过 customerName 找到客户，调用 ForceCustomerLeave");
            coordinator.ForceCustomerLeave();
        }
        else
        {
            Debug.LogWarning($"[OrderSystem] 未找到客户 GameObject: {order.customerName}");
        }
    }

    public void RemoveOrder(CustomerOrder order)
    {
        if (order == null) return;

        if (GameManager.Instance != null)
            GameManager.Instance.pendingOrders.Remove(order);

        ClearPages();
        OpenPendingOrdersFromGameManager();
        UpdateCoinDisplay();

        Debug.Log($"[OrderSystem] 订单已删除: 客户{order.customerNumber}");


        if (GameManager.Instance != null && !string.IsNullOrEmpty(order.customerName))
        {
            var coordinator = FindObjectsOfType<CustomerOrderCoordinator>()
                .FirstOrDefault(c => c.gameObject.name == order.customerName);
            if (coordinator != null)
                coordinator.NotifyOrderCompleted();
            else
                GameManager.Instance.MarkCustomerCompleted(order.customerName);
        }
    }

    public void TryDeliverOrder(CustomerOrder order)
    {
        if (GameManager.Instance == null || order == null) return;

        var missing = GameManager.Instance.GetMissingFlowers(order);

        if (missing.Count > 0)
        {

            var parts = new List<string>();
            foreach (var kvp in missing)
                parts.Add($"{kvp.Key} x{kvp.Value}");
            ShowTip($"Out of stock! Shortage of: {string.Join(", ", parts)}");
            return;
        }


        GameManager.Instance.DeductOrderFlowers(order);
        GameManager.Instance.AddCoins(coinRewardPerOrder);
        UpdateCoinDisplay();
        ShowTip($"Payment successful! +{coinRewardPerOrder} coins");


        Invoke(nameof(RemoveOrderDelayed), 0.1f);
        _pendingDeliverOrder = order;
    }

    private CustomerOrder _pendingDeliverOrder;

    void RemoveOrderDelayed()
    {
        if (_pendingDeliverOrder != null)
        {
            RemoveOrder(_pendingDeliverOrder);
            _pendingDeliverOrder = null;
        }
    }

    public void UpdateCoinDisplay()
    {
        if (coinDisplayText == null) return;
        if (GameManager.Instance != null)
            coinDisplayText.text = $"coin: {GameManager.Instance.coins}";
        else
            coinDisplayText.text = "coin: 0";
    }

    public void ShowTip(string message)
    {
        if (tipRoot == null || tipText == null) return;

        tipText.text = message;
        tipRoot.SetActive(true);

        CancelInvoke(nameof(HideTip));
        Invoke(nameof(HideTip), tipDuration);
    }

    void HideTip()
    {
        if (tipRoot != null)
            tipRoot.SetActive(false);
    }

    void OnDestroy()
    {
        ClearPages();
    }
}
