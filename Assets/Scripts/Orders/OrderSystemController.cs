using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 订单 UI：按列表生成 Order 行，每页最多 3 条；超过则复制 Panel Prefab 并链式翻页。
/// </summary>
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

    readonly List<GameObject> _pageInstances = new List<GameObject>();
    int _currentPageIndex;

    void Start()
    {
        if (ordersRoot != null)
            ordersRoot.SetActive(false);
        OpenDebugOrders();
    }

    /// <summary>打开并显示订单（会销毁上次生成的页面后重建）。</summary>
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

    /// <summary>向指定页的固定槽位生成一行 Order。</summary>
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
            Debug.LogError("[OrderSystem] Order Prefab 上需要 OrderRowView。");
            return;
        }

        row.Bind(order.customerNumber, order.GetFlowerNames(), flowerSpriteRegistry);
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
    }

    void OnDestroy()
    {
        ClearPages();
    }
}
