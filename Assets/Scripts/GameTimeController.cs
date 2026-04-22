using UnityEngine;
using UnityEngine.UI;
using System;

public class GameTimeController : MonoBehaviour
{
    public static GameTimeController Instance { get; private set; }

    [Header("")]
    [SerializeField] int startYear = 2025;
    [SerializeField] int startMonth = 3;
    [SerializeField] int startDay = 27;
    [SerializeField] int startHour = 8;
    [SerializeField] int startMinute = 5;

    [Header("")]
    [SerializeField] Text dateText;
    [SerializeField] Text timeText;
    [SerializeField] Text dateTimeText;

    [Header("")]
    [SerializeField] string datePrefix = "";
    [SerializeField] string dateSuffix = "";
    [SerializeField] string timePrefix = "";
    [SerializeField] string timeSuffix = "";

    DateTime _currentTime;
    int _lastNotifiedTotalMinutes;
    float _timer;
    // ADD this field at the top with other fields
    float _dayTimer = 0f;
    bool _dayEnded = false;
    bool _dayTimerActive = false;

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
            return;
        }

        _currentTime = new DateTime(startYear, startMonth, startDay, startHour, startMinute, 0);
        _lastNotifiedTotalMinutes = TotalMinutes(_currentTime);
        AutoFindUI();
        RefreshUI();
    }

    void AutoFindUI()
    {
        if (dateTimeText == null)
            dateTimeText = GetComponentInChildren<Text>();
    }

    void Update()
    {
        if (!_dayTimerActive) return;
        if (_dayEnded) return;

        _dayTimer += Time.deltaTime;
        _timer += Time.deltaTime;

        if (_dayTimer >= DayManager.DayDurationSeconds)
        {
            _dayEnded = true;
            _dayTimerActive = false;
            Debug.Log($"[GameTimeController] Day timer reached {DayManager.DayDurationSeconds}s, triggering end of day.");
            DayManager.Instance?.TriggerEndOfDay();
            return;
        }

        if (_timer >= 1f)
        {
            int minutesToAdd = Mathf.FloorToInt(_timer);
            _timer -= minutesToAdd;
            _currentTime = _currentTime.AddMinutes(minutesToAdd);
            RefreshUI();
            int currentMins = TotalMinutes(_currentTime);
            if (currentMins > _lastNotifiedTotalMinutes)
            {
                _lastNotifiedTotalMinutes = currentMins;
                CustomerSpawner.Instance?.OnGameMinuteChanged();
            }
        }
    }

    public void ResetDayTimer()
    {
        _dayTimer = 0f;
        _dayEnded = false;
        _dayTimerActive = true;
        Debug.Log("[GameTimeController] Day timer started.");
    }
    int TotalMinutes(DateTime dt) => dt.Day * 1440 + dt.Hour * 60 + dt.Minute;

    public int GetTotalMinutes() => TotalMinutes(_currentTime);

    void RefreshUI()
    {
        string d = _currentTime.ToString("yyyy/MM/dd");
        string t = _currentTime.ToString("HH:mm");

        if (dateText != null)
            dateText.text = datePrefix + d + dateSuffix;

        if (timeText != null)
            timeText.text = timePrefix + t + timeSuffix;

        if (dateTimeText != null)
            dateTimeText.text = datePrefix + d + dateSuffix + " " + timePrefix + t + timeSuffix;
    }

    public DateTime Now => _currentTime;

    public void SetTime(int year, int month, int day, int hour, int minute)
    {
        _currentTime = new DateTime(year, month, day, hour, minute, 0);
        _timer = 0f;
        _lastNotifiedTotalMinutes = TotalMinutes(_currentTime);
        RefreshUI();
    }

    public void AddMinutes(int minutes)
    {
        _currentTime = _currentTime.AddMinutes(minutes);
        int currentMins = TotalMinutes(_currentTime);
        if (currentMins > _lastNotifiedTotalMinutes)
        {
            _lastNotifiedTotalMinutes = currentMins;
            CustomerSpawner.Instance?.OnGameMinuteChanged();
        }
        RefreshUI();
    }
}
