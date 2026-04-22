using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EndOfDayUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text dayTitleText;
    [SerializeField] private TMP_Text coinsEarnedText;
    [SerializeField] private TMP_Text ordersDeliveredText;
    [SerializeField] private TMP_Text minimumStatusText;
    [SerializeField] private TMP_Text coinsNeededText;
    [SerializeField] private TMP_Text totalCoinsText;
    [SerializeField] private Button continueButton;
    [SerializeField] private TMP_Text continueButtonText;

    [Header("Colors")]
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private Color badColor = Color.red;
    [SerializeField] private Color neutralColor = Color.yellow;

    void Start()
    {
        if (DayManager.Instance == null)
        {
            Debug.LogError("[EndOfDayUI] DayManager not found!");
            return;
        }

        PopulateSummary();
        SetupContinueButton();
    }

    void PopulateSummary()
    {
        var dm = DayManager.Instance;
        var summary = dm.daySummaries[dm.daySummaries.Count - 1];

        // Day title
        if (dayTitleText != null)
            dayTitleText.text = $"Day {summary.day} Summary";

        // Coins earned today
        if (coinsEarnedText != null)
            coinsEarnedText.text = $"Coins Earned Today: {summary.coinsEarned}";

        // Orders delivered today
        if (ordersDeliveredText != null)
            ordersDeliveredText.text = $"Orders Delivered: {summary.ordersDelivered}";

        // Met daily minimum?
        if (minimumStatusText != null)
        {
            if (summary.metDailyMinimum)
            {
                minimumStatusText.text = "Daily Minimum: MET ✓";
                minimumStatusText.color = goodColor;
            }
            else
            {
                minimumStatusText.text = "Daily Minimum: NOT MET ✗";
                minimumStatusText.color = badColor;
            }
        }

        // Coins still needed
        if (coinsNeededText != null)
        {
            int needed = dm.GetCoinsNeededForGoodEnding();
            if (needed <= 0)
            {
                coinsNeededText.text = "Good Ending: ON TRACK ✓";
                coinsNeededText.color = goodColor;
            }
            else
            {
                coinsNeededText.text = $"Need {needed} more coins for good ending";
                coinsNeededText.color = neutralColor;
            }
        }

        // Running total
        if (totalCoinsText != null)
            totalCoinsText.text = $"Total Coins (all days): {dm.totalCoinsEarned}";
    }

    void SetupContinueButton()
    {
        if (continueButton == null) return;

        var dm = DayManager.Instance;
        bool isLastDay = dm.currentDay >= DayManager.TotalDays;

        if (continueButtonText != null)
            continueButtonText.text = isLastDay ? "See Ending" : $"Start Day {dm.currentDay + 1}";

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(() =>
        {
            if (isLastDay)
                dm.TriggerEnding();
            else
                dm.StartNextDay();
        });
    }
}