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
            DontDestroyOnLoad(gameObject); // ADD THIS
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
        if (!_dayTimerActive)
        {
            // 每隔10秒打印一次等待状态，避免刷屏
            if (Time.frameCount % 600 == 0)
                Debug.Log($"[GameTimeController] 时间未运行 | _dayTimerActive={_dayTimerActive}, _dayEnded={_dayEnded}, _dayTimer={_dayTimer:F1}s/{DayManager.DayDurationSeconds}s");
            return;
        }
        if (_dayEnded)
        {
            if (Time.frameCount % 600 == 0)
                Debug.Log("[GameTimeController] 今日已结束，等待结算");
            return;
        }

        _dayTimer += Time.deltaTime;
        _timer += Time.deltaTime;

        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (_dayTimer >= DayManager.DayDurationSeconds)
        {
            if (currentSceneName == "Level2")
            {
                _dayTimer = DayManager.DayDurationSeconds;
            }
            else
            {
                _dayEnded = true;
                _dayTimerActive = false;
                Debug.Log($"[GameTimeController] 一天结束！已运行 {_dayTimer:F1}s，触发 TriggerEndOfDay()");
                DayManager.Instance?.TriggerEndOfDay();
                return;
            }
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
                Debug.Log($"[GameTimeController] 游戏时间推进 +{minutesToAdd}分钟，当前: {currentMins}（每分钟通知一次CustomerSpawner）");
                CustomerSpawner.Instance?.OnGameMinuteChanged();
            }
        }
    }

    public void ResetDayTimer()
    {
        _dayTimer = 0f;
        _dayEnded = false;
        _dayTimerActive = true;
        _timer = 0f;
        Debug.Log("[GameTimeController] ResetDayTimer() 被调用！计时器已启动。");
        Debug.Log($"[GameTimeController] 计时器状态: _dayTimerActive={_dayTimerActive}, _dayEnded={_dayEnded}, _dayTimer={_dayTimer}");
    }
    int TotalMinutes(DateTime dt) => dt.Day * 1440 + dt.Hour * 60 + dt.Minute;

    // ADD this public property so other scripts can read the timer
    public float DayTimer => _dayTimer;
    public bool DayTimerActive => _dayTimerActive;
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
