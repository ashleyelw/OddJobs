using UnityEngine;
using UnityEngine.UI;

public class SingleOrderView : MonoBehaviour
{
    [Header("UI 元素")]
    [SerializeField] private Text customerNameText;
    [SerializeField] private Text timerText;
    [SerializeField] private Image timerBarFill;
    [SerializeField] private Image flowerIcon0;
    [SerializeField] private Image flowerIcon1;
    [SerializeField] private Image flowerIcon2;
    [SerializeField] private Image ribbonIcon0;
    [SerializeField] private Image ribbonIcon1;
    [SerializeField] private Image ribbonIcon2;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deliverButton;

    [Header("颜色")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    private CustomerOrder _order;
    private Sprite emptySlotSprite;

    public UnityEngine.Events.UnityEvent onClose = new UnityEngine.Events.UnityEvent();
    public UnityEngine.Events.UnityEvent onDeliver = new UnityEngine.Events.UnityEvent();

    void Start()
    {
        emptySlotSprite = Resources.Load<Sprite>("UI/EmptySlot");

        closeButton?.onClick.AddListener(() => onClose?.Invoke());
        deliverButton?.onClick.AddListener(() => onDeliver?.Invoke());
    }

    void Update()
    {
        if (_order != null)
        {
            UpdateTimer();
        }
    }

    public void Bind(CustomerOrder order)
    {
        _order = order;

        if (customerNameText != null)
            customerNameText.text = $"Client{order.customerNumber}";

        DisplayFlowers();
        DisplayRibbons();
    }

    void DisplayFlowers()
    {
        var names = new[] { _order.flowerPrefabName0, _order.flowerPrefabName1, _order.flowerPrefabName2 };
        var icons = new[] { flowerIcon0, flowerIcon1, flowerIcon2 };

        Sprite sp;
        var registry = FindObjectOfType<FlowerSpriteRegistry>();
        if (registry == null)
        {
            Debug.LogWarning("[SingleOrderView] 未找到 FlowerSpriteRegistry");
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;

            if (!string.IsNullOrEmpty(names[i]) && registry.TryGetSprite(names[i], out sp))
            {
                icons[i].sprite = sp;
                icons[i].enabled = true;
            }
            else
            {
                icons[i].sprite = emptySlotSprite;
                icons[i].enabled = emptySlotSprite != null;
            }
        }
    }

    void DisplayRibbons()
    {
        var names = new[] { _order.ribbonPrefabName0, _order.ribbonPrefabName1, _order.ribbonPrefabName2 };
        var icons = new[] { ribbonIcon0, ribbonIcon1, ribbonIcon2 };

        Sprite sp;
        var registry = FindObjectOfType<RibbonSpriteRegistry>();
        if (registry == null)
        {
            Debug.LogWarning("[SingleOrderView] 未找到 RibbonSpriteRegistry");
            return;
        }

        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i] == null) continue;

            if (!string.IsNullOrEmpty(names[i]) && registry.TryGetSprite(names[i], out sp))
            {
                icons[i].sprite = sp;
                icons[i].enabled = true;
            }
            else
            {
                icons[i].sprite = emptySlotSprite;
                icons[i].enabled = emptySlotSprite != null;
            }
        }
    }

    void UpdateTimer()
    {
        if (timerText == null || GameTimeController.Instance == null) return;

        int current = GameTimeController.Instance.GetTotalMinutes();
        float remaining = _order.GetRemainingMinutes(current);

        float fillAlpha = 0.4f;

        if (_order.isTutorialOrder)
        {
            timerText.text = "TUTORIAL";
            timerText.color = Color.cyan;
            if (timerBarFill != null)
            {
                timerBarFill.fillAmount = 1f;
                timerBarFill.color = new Color(0, 1, 1, fillAlpha);
            }
        }
        else if (remaining <= 0)
        {
            timerText.text = "TIMEOUT";
            timerText.color = criticalColor;
            if (timerBarFill != null)
            {
                timerBarFill.fillAmount = 0;
                timerBarFill.color = new Color(criticalColor.r, criticalColor.g, criticalColor.b, fillAlpha);
            }
        }
        else
        {
            timerText.text = $"{remaining:F1}m";
            timerText.color = remaining <= 1f ? warningColor : normalColor;

            if (timerBarFill != null && _order.timeLimitMinutes > 0)
            {
                timerBarFill.fillAmount = Mathf.Clamp01(remaining / _order.timeLimitMinutes);
                Color targetColor = remaining <= 0.5f ? warningColor : normalColor;
                timerBarFill.color = new Color(targetColor.r, targetColor.g, targetColor.b, fillAlpha);
            }
        }
    }
}