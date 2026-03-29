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

    [Header("空槽")]
    [SerializeField] Sprite emptySlotSprite;

    [SerializeField] Button deliverButton;
    public UnityEvent onDeliverClicked;


    private CustomerOrder _boundOrder;

    private System.Action<CustomerOrder> _onDeliverRequested;

    Image[] FlowerSlots => new[] { flowerSlot0, flowerSlot1, flowerSlot2 };

    void Awake()
    {
        if (deliverButton != null)
            deliverButton.onClick.AddListener(() => onDeliverClicked?.Invoke());
    }

    public void Bind(int customerNumber, string[] flowerPrefabNames, FlowerSpriteRegistry registry)
    {
        if (customerLabel != null)
            customerLabel.text = $"client{customerNumber}";

        var slots = FlowerSlots;
        var names = flowerPrefabNames ?? Array.Empty<string>();

        for (int i = 0; i < slots.Length; i++)
        {
            var img = slots[i];
            if (img == null)
                continue;

            string name = i < names.Length ? names[i] : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
                continue;
            }

            if (registry != null && registry.TryGetSprite(name, out var sp))
            {
                img.sprite = sp;
                img.enabled = true;
            }
            else
            {
                Debug.LogWarning($"[OrderRowView] 未找到花的 Sprite：「{name}」，请检查 FlowerSpriteRegistry 是否包含该预制体。");
                img.sprite = emptySlotSprite;
                img.enabled = emptySlotSprite != null;
            }
        }
    }

    public void BindWithDeliver(int customerNumber, string[] flowerPrefabNames,
        FlowerSpriteRegistry registry, CustomerOrder order, System.Action<CustomerOrder> onDeliver)
    {
        _boundOrder = order;
        _onDeliverRequested = onDeliver;

        Bind(customerNumber, flowerPrefabNames, registry);

        onDeliverClicked.RemoveAllListeners();
        onDeliverClicked.AddListener(RequestDeliver);
    }

    void RequestDeliver()
    {
        if (_onDeliverRequested != null && _boundOrder != null)
            _onDeliverRequested.Invoke(_boundOrder);
    }
}
