using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance { get; private set; }

    public const int TotalDays = 3;
    public const int DayDurationSeconds = 180;
    public const int MinDailyCoins = 100;
    public const int TotalHighCoins = 500;

    [Header("Day Tracking")]
    public int currentDay = 1;
    public int totalCoinsEarned = 0;
    public int todayCoinsEarned = 0;
    public int todayOrdersDelivered = 0;
    public int totalOrdersDelivered = 0;

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
        // Reset the day timer whenever FloristMain loads (i.e. start of each new day)
        if (scene.name == "FloristMain")
        {
            if (GameTimeController.Instance != null)
            {
                GameTimeController.Instance.ResetDayTimer();
                Debug.Log($"[DayManager] Day timer reset for day {currentDay}");
            }
            else
            {
                // GameTimeController may not be ready yet, retry next frame
                StartCoroutine(ResetTimerNextFrame());
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

        SceneManager.LoadScene("EndOfDay");
    }

    public void StartNextDay()
    {
        currentDay++;
        todayCoinsEarned = 0;
        todayOrdersDelivered = 0;

        Debug.Log($"[DayManager] Starting day {currentDay}");

        // Timer reset is handled by OnSceneLoaded when FloristMain loads
        SceneManager.LoadScene("FloristMain");
    }

    public void TriggerEnding()
    {
        SceneManager.LoadScene("Ending");
    }

    public EndingType GetEnding()
    {
        if (totalCoinsEarned >= TotalHighCoins)
            return EndingType.Good;
        else if (totalCoinsEarned >= MinDailyCoins * TotalDays)
            return EndingType.Neutral;
        else
            return EndingType.Bad;
    }

    public int GetCoinsNeededForGoodEnding()
    {
        return Mathf.Max(0, TotalHighCoins - totalCoinsEarned);
    }

    public int GetCoinsNeededForMinimum()
    {
        return Mathf.Max(0, MinDailyCoins - todayCoinsEarned);
    }

    public enum EndingType { Bad, Neutral, Good }
}