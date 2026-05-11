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

    [Header("Level2 Unlock Button")]
    [SerializeField] private Button unlockLevel2Button;
    [SerializeField] private TMP_Text unlockLevel2Text;

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
        SetupUnlockLevel2Button();
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
        var latestSummary = dm.daySummaries[dm.daySummaries.Count - 1];
        bool metDailyMinimum = latestSummary.metDailyMinimum;

        // Failed the daily minimum — bad ending regardless of day
        if (!metDailyMinimum)
        {
            if (continueButtonText != null)
                continueButtonText.text = "See Ending...";

            // Update minimum status text to make it clear why
            if (minimumStatusText != null)
            {
                minimumStatusText.text = "Daily minimum not met — ending the run!";
                minimumStatusText.color = badColor;
            }

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(() =>
            {
                Debug.Log($"[EndOfDayUI] Daily minimum not met on day {dm.currentDay}," +
                        $" triggering bad ending.");
                dm.TriggerEnding();
            });

            return;
        }

        // Met minimum — continue to next day or final ending
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

    void SetupUnlockLevel2Button()
    {
        if (unlockLevel2Button == null) return;

        var dm = DayManager.Instance;
        bool isDay3 = dm.currentDay == DayManager.TotalDays;
        bool hasEnoughCoins = dm.totalCoinsEarned >= DayManager.Level2UnlockCoins;

        if (isDay3 && hasEnoughCoins)
        {
            unlockLevel2Button.gameObject.SetActive(true);
            if (unlockLevel2Text != null)
                unlockLevel2Text.text = "Next Level";

            unlockLevel2Button.onClick.RemoveAllListeners();
            unlockLevel2Button.onClick.AddListener(() =>
            {
                Debug.Log($"[EndOfDayUI] Unlocking Level2! Total coins: {dm.totalCoinsEarned}");
                ClearAllGameStateForNewLevel();
                UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
            });

    void ClearAllGameStateForNewLevel()
    {
        OrderSystemController.CloseAllCustomerOrderUIs();
        if (OrderSystemController.Instance != null)
            OrderSystemController.Instance.CloseAll();
        if (GameManager.Instance != null)
            GameManager.Instance.ClearPendingOrders();
        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.ResetAllSlots();
    }

            Debug.Log($"[EndOfDayUI] Day 3 complete with {dm.totalCoinsEarned} total coins — Level2 unlocked!");
        }
        else
        {
            unlockLevel2Button.gameObject.SetActive(false);
        }
    }
}