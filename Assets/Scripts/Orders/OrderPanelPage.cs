using UnityEngine;
using UnityEngine.UI;


public class OrderPanelPage : MonoBehaviour
{
    [Header("固定三个 Order 槽位")]
    [SerializeField] RectTransform orderSlot0;
    [SerializeField] RectTransform orderSlot1;
    [SerializeField] RectTransform orderSlot2;

    [Header("翻页")]
    public Button nextPageButton;
    public Button prevPageButton;

    RectTransform[] I => new[] { orderSlot0, orderSlot1, orderSlot2 };

    public RectTransform GetSlot(int index)
    {
        if (index < 0 || index >= 3)
            return null;
        return I[index];
    }

    public int GetFilledSlotCount()
    {
        int count = 0;
        foreach (var slot in I)
            if (slot != null) count++;
        return count;
    }
}
