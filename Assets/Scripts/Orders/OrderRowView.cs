using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


public class OrderRowView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] Text customerLabel;
    [SerializeField] Image flowerSlot0;
    [SerializeField] Image flowerSlot1;
    [SerializeField] Image flowerSlot2;

    [Header("丝带 UI")]
    [SerializeField] Image ribbonSlot0;
    [SerializeField] Image ribbonSlot1;
    [SerializeField] Image ribbonSlot2;

    [Header("空槽")]
    [SerializeField] Sprite emptySlotSprite;

    [SerializeField] Button deliverButton;
    public UnityEvent onDeliverClicked;

    [Header("时限显示")]
    [SerializeField] Text timeLimitText;
    [SerializeField] Image timerBarFill;
    [SerializeField] Color normalTimeColor = Color.white;
    [SerializeField] Color warningTimeColor = Color.yellow;
    [SerializeField] Color criticalTimeColor = Color.red;

    public UnityEvent onCloseClicked;


    private CustomerOrder _boundOrder;
    private System.Action<CustomerOrder> _onDeliverRequested;
    private System.Action<CustomerOrder> _onCloseRequested;
    private bool _isClosed = false;

    Image[] FlowerSlots => new[] { flowerSlot0, flowerSlot1, flowerSlot2 };
    Image[] RibbonSlots => new[] { ribbonSlot0, ribbonSlot1, ribbonSlot2 };

    void Awake()
    {
        if (deliverButton != null)
            deliverButton.onClick.AddListener(() => onDeliverClicked?.Invoke());
    }

    public void Bind(int customerNumber, string[] flowerPrefabNames, FlowerSpriteRegistry flowerRegistry,
        string[] ribbonPrefabNames, RibbonSpriteRegistry ribbonRegistry)
    {
        if (customerLabel != null)
            customerLabel.text = $"client{customerNumber}";

        var flowerSlots = FlowerSlots;
        var flowerNames = flowerPrefabNames ?? Array.Empty<string>();

        for (int i = 0; i < flowerSlots.Length; i++)
        {
            var img = flowerSlots[i];
            if (img == null)
                continue;

            string name = i < flowerNames.Length ? flowerNames[i] : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
                continue;
            }

            if (flowerRegistry != null && flowerRegistry.TryGetSprite(name, out var sp))
            {
                img.sprite = sp;
                img.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[OrderRowView] 未找到花的 Sprite：「{name}」");
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
            }
        }

        var ribbonSlots = RibbonSlots;
        var ribbonNames = ribbonPrefabNames ?? Array.Empty<string>();

        for (int i = 0; i < ribbonSlots.Length; i++)
        {
            var img = ribbonSlots[i];
            if (img == null)
                continue;

            string name = i < ribbonNames.Length ? ribbonNames[i] : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
                continue;
            }

            if (ribbonRegistry != null && ribbonRegistry.TryGetSprite(name, out var sp))
            {
                img.sprite = sp;
                img.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[OrderRowView] 未找到丝带的 Sprite：「{name}」");
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
            }
        }
    }

    public void BindWithDeliver(int customerNumber, string[] flowerPrefabNames,
        FlowerSpriteRegistry flowerRegistry, string[] ribbonPrefabNames, RibbonSpriteRegistry ribbonRegistry,
        CustomerOrder order, System.Action<CustomerOrder> onDeliver,
        System.Action<CustomerOrder> onClose = null)
    {
        _boundOrder = order;
        _onDeliverRequested = onDeliver;
        _onCloseRequested = onClose;
        _isClosed = false;

        Bind(customerNumber, flowerPrefabNames, flowerRegistry, ribbonPrefabNames, ribbonRegistry);

        onDeliverClicked.RemoveAllListeners();
        onDeliverClicked.AddListener(RequestDeliver);

        UpdateTimeDisplay(order);
    }

    public void UpdateTimeDisplay(CustomerOrder order)
    {
        if (order == null || _isClosed) return;

        if (GameTimeController.Instance != null)
        {
            int currentMinutes = GameTimeController.Instance.GetTotalMinutes();
            float remaining = order.GetRemainingMinutes(currentMinutes);

            if (timeLimitText != null)
            {
                if (remaining <= 0)
                {
                    timeLimitText.text = "TIMEOUT";
                    timeLimitText.color = criticalTimeColor;
                }
                else
                {
                    timeLimitText.text = $"{Mathf.Max(0, remaining):F1}m";
                    if (remaining <= 0.5f)
                        timeLimitText.color = criticalTimeColor;
                    else if (remaining <= 1f)
                        timeLimitText.color = warningTimeColor;
                    else
                        timeLimitText.color = normalTimeColor;
                }
            }

            if (timerBarFill != null && order.timeLimitMinutes > 0)
            {
                float progress = Mathf.Clamp01(remaining / order.timeLimitMinutes);
                timerBarFill.fillAmount = progress;

                if (progress <= 0.25f)
                    timerBarFill.color = criticalTimeColor;
                else if (progress <= 0.5f)
                    timerBarFill.color = warningTimeColor;
                else
                    timerBarFill.color = normalTimeColor;
            }
        }
    }

    public void MarkAsTimedOut()
    {
        if (_isClosed) return;
        _isClosed = true;

        if (timeLimitText != null)
        {
            timeLimitText.text = "FAILED";
            timeLimitText.color = criticalTimeColor;
        }

        if (timerBarFill != null)
        {
            timerBarFill.fillAmount = 0;
            timerBarFill.color = criticalTimeColor;
        }

        if (deliverButton != null)
        {
            var buttonText = deliverButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Close";

            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(RequestClose);
        }
    }

    void RequestDeliver()
    {
        if (_isClosed) return;
        if (_onDeliverRequested != null && _boundOrder != null)
            _onDeliverRequested.Invoke(_boundOrder);
    }

    void RequestClose()
    {
        if (_isClosed && _onCloseRequested != null && _boundOrder != null)
            _onCloseRequested.Invoke(_boundOrder);
    }

    public void CloseRow()
    {
        _isClosed = true;
        if (deliverButton != null)
        {
            var buttonText = deliverButton.GetComponentInChildren<Text>();
            if (buttonText != null)
                buttonText.text = "Close";

            deliverButton.onClick.RemoveAllListeners();
            deliverButton.onClick.AddListener(RequestClose);
        }
    }

    public CustomerOrder GetBoundOrder() => _boundOrder;
    public bool IsClosed() => _isClosed;
}