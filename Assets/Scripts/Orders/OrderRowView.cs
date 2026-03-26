using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 挂在 Order 行 Prefab 上：第一个 Image 为头像（不修改），后三个为花槽，Text 为客户编号。
/// </summary>
public class OrderRowView : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] Text customerLabel;
    [SerializeField] Image flowerSlot0;
    [SerializeField] Image flowerSlot1;
    [SerializeField] Image flowerSlot2;

    [Header("空槽")]
    [SerializeField] Sprite emptySlotSprite;

    [Header("交付按钮")]
    [SerializeField] Button deliverButton;
    public UnityEvent onDeliverClicked;

    Image[] FlowerSlots => new[] { flowerSlot0, flowerSlot1, flowerSlot2 };

    void Awake()
    {
        if (deliverButton != null)
            deliverButton.onClick.AddListener(() => onDeliverClicked?.Invoke());
    }

    public void Bind(int customerNumber, string[] flowerPrefabNames, FlowerSpriteRegistry registry)
    {
        if (customerLabel != null)
            customerLabel.text = $"客户{customerNumber}";

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
}
