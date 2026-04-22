using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    public const int TotalDays = 3;
    public const int DayDurationSeconds = 120;

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

        // Show splash before transitioning
        if (EndOfDaySplash.Instance != null)
            EndOfDaySplash.Instance.Show(currentDay, todayCoinsEarned);
        else
            SceneManager.LoadScene("EndOfDay"); // Fallback if splash not found
    }

    public void StartNextDay()
    {
        currentDay++;
        todayCoinsEarned = 0;
        todayOrdersDelivered = 0;
        _dayStarted = false; // ADD THIS so next day's timer resets properly
        Debug.Log($"[DayManager] Starting day {currentDay}");
        SceneManager.LoadScene("FloristMain");
    }

    public void TriggerEnding()
    {
        SceneManager.LoadScene("Ending");
    }

    public EndingType GetEnding()
    {
        // If any day failed the minimum, always bad ending
        foreach (var summary in daySummaries)
        {
            if (!summary.metDailyMinimum)
                return EndingType.Bad;
        }

        // All minimums met — check total for good vs neutral
        if (totalCoinsEarned >= totalHighCoins)
            return EndingType.Good;
        else if (totalCoinsEarned >= minDailyCoins * TotalDays)
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

    public enum EndingType { Bad, Neutral, Good }
}