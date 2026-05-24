using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    public const int TotalDays = 3;
    public const int DayDurationSeconds = 45;
    public const int Level2UnlockCoins = 150;

    [Header("Level2 Settings")]
    [Tooltip("Level2 的最大游戏时间（秒）")]
    public const int Level2DurationSeconds = 45;

    [Tooltip("Level2 的过关金币目标")]
    public const int Level2TargetCoins = 200;

    [Header("Ending Thresholds")]
    [Tooltip("Coins needed per day to avoid bad ending")]
    [SerializeField] private int minDailyCoins = 100;

    [Tooltip("Total coins over all 3 days needed for good ending")]
    [SerializeField] private int totalHighCoins = 500;

    // Public getters so other scripts can still read them
    public int MinDailyCoins => minDailyCoins;
    public int TotalHighCoins => totalHighCoins;

    [Header("Day Tracking")]
    public int currentDay = 1;
    public int totalCoinsEarned = 0;
    public int todayCoinsEarned = 0;
    public int todayOrdersDelivered = 0;
    public int totalOrdersDelivered = 0;
    private bool _dayStarted = false;
    private bool _day3Settled = false;
    private bool _level2Entered = false;
    private bool _isInLevel2 = false;   // tracks whether the current run is Level2
    public bool Day3Settled => _day3Settled;
    public bool IsInLevel2  => _isInLevel2;
    public List<DaySummary> daySummaries = new List<DaySummary>();

    [System.Serializable]
    public class DaySummary
    {
        public int day;
        public int coinsEarned;
        public int ordersDelivered;
        public bool metDailyMinimum;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "FloristMain")
        {
            // Only reset timer if this is a fresh day, not a mid-day scene return
            if (!_dayStarted)
            {
                _dayStarted = true;
                if (GameTimeController.Instance != null)
                {
                    GameTimeController.Instance.ResetDayTimer();
                    Debug.Log($"[DayManager] Day {currentDay} timer started.");
                }
                else
                {
                    StartCoroutine(ResetTimerNextFrame());
                }
            }
            else
            {
                Debug.Log($"[DayManager] Returned to FloristMain mid-day, timer continues.");
            }
        }
        else if (scene.name == "Level2")
        {
            _isInLevel2 = true;
            if (GameTimeController.Instance != null)
            {
                if (!GameTimeController.Instance.DayTimerActive)
                {
                    GameTimeController.Instance.ResetDayTimer();
                    Debug.Log($"[DayManager] Level2 entered, timer started.");
                }
                else
                {
                    Debug.Log($"[DayManager] Level2 entered, timer already active.");
                }
            }
        }
    }

    System.Collections.IEnumerator ResetTimerNextFrame()
    {
        yield return null;
        if (GameTimeController.Instance != null)
        {
            GameTimeController.Instance.ResetDayTimer();
            Debug.Log($"[DayManager] Day timer reset (delayed) for day {currentDay}");
        }
        _dayStarted = true;
    }

    public void OnOrderDelivered(int coinsEarned)
    {
        todayCoinsEarned += coinsEarned;
        totalCoinsEarned += coinsEarned;
        todayOrdersDelivered++;
        totalOrdersDelivered++;
        Debug.Log($"[DayManager] Order delivered. Today: {todayCoinsEarned} coins, " +
                  $"{todayOrdersDelivered} orders. Total: {totalCoinsEarned} coins.");
    }

    public void TriggerEndOfDay()
    {
        Debug.Log($"[DayManager] Day {currentDay} ended. " +
                $"Coins: {todayCoinsEarned}, Orders: {todayOrdersDelivered}");

        daySummaries.Add(new DaySummary
        {
            day = currentDay,
            coinsEarned = todayCoinsEarned,
            ordersDelivered = todayOrdersDelivered,
            metDailyMinimum = todayCoinsEarned >= MinDailyCoins
        });

        if (currentDay >= TotalDays)
            _day3Settled = true;

        // Show splash before transitioning
        if (EndOfDaySplash.Instance != null)
            EndOfDaySplash.Instance.Show(currentDay, todayCoinsEarned);
        else
            SceneManager.LoadScene("EndOfDay");
    }

    public void StartNextDay()
    {
        currentDay++;
        todayCoinsEarned = 0;
        todayOrdersDelivered = 0;
        _dayStarted = false;
        Debug.Log($"[DayManager] Starting day {currentDay} (Level2={_isInLevel2})");

        CloseAllOrderUIs();
        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.ResetAllSlots();

        // Return to whichever level the player is currently playing
        SceneManager.LoadScene(_isInLevel2 ? "Level2" : "FloristMain");
    }

    /// <summary>
    /// Resets day tracking back to Day 1 when entering Level2.
    /// Keeps totalCoinsEarned intact for the ending calculation.
    /// </summary>
    public void ResetForLevel2()
    {
        currentDay           = 1;
        todayCoinsEarned     = 0;
        todayOrdersDelivered = 0;
        _dayStarted          = false;
        _day3Settled         = false;
        _isInLevel2          = true;
        daySummaries.Clear();
        Debug.Log("[DayManager] Reset for Level2 — day counter back to 1.");

        CloseAllOrderUIs();
        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.ResetAllSlots();
    }

    void CloseAllOrderUIs()
    {
        OrderSystemController.CloseAllCustomerOrderUIs();
        if (OrderSystemController.Instance != null)
            OrderSystemController.Instance.CloseAll();
        if (GameManager.Instance != null)
            GameManager.Instance.ClearPendingOrders();
    }

    // ── Level2 outcomes ───────────────────────────────────────────────────────

    public void TriggerLevel2Success()
    {
        Debug.Log($"[DayManager] Level2 成功！金币: {todayCoinsEarned}，触发成功结算。");
        if (EndOfDaySplash.Instance != null)
            EndOfDaySplash.Instance.ShowLevel2Success(todayCoinsEarned);
        else
            SceneManager.LoadScene("Ending");
    }

    public void TriggerLevel2Fail()
    {
        Debug.Log($"[DayManager] Level2 失败！金币: {todayCoinsEarned}/{Level2TargetCoins}，触发失败结算。");
        if (EndOfDaySplash.Instance != null)
            EndOfDaySplash.Instance.ShowLevel2Fail(todayCoinsEarned);
        else
            SceneManager.LoadScene("Ending");
    }

    public void TriggerEnding()
    {
        SceneManager.LoadScene("Ending");
    }

    public EndingType GetEnding()
    {
        // Secret ending — checked first, overrides everything else
        if (totalCoinsEarned >= totalHighCoins)
            return EndingType.Secret;

        // If any day failed the minimum, always bad ending
        foreach (var summary in daySummaries)
        {
            if (!summary.metDailyMinimum)
                return EndingType.Bad;
        }

        // All minimums met — check total for good vs neutral
        if (totalCoinsEarned >= minDailyCoins * TotalDays)
            return EndingType.Neutral;
        else
            return EndingType.Bad;
    }

    public int GetCoinsNeededForGoodEnding()
    {
        return Mathf.Max(0, totalHighCoins - totalCoinsEarned);
    }

    public int GetCoinsNeededForMinimum()
    {
        return Mathf.Max(0, minDailyCoins - todayCoinsEarned);
    }

    public enum EndingType { Bad, Neutral, Good, Secret }
}