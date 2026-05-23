using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
    float _pausedTime = 0f;
    bool _dayEnded = false;
    bool _dayTimerActive = false;
    bool _skipDayEnd = false;

    float _level2Timer = 0f;
    bool _level2Ended = false;

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
        Debug.Log($"[GameTimeController] OnSceneLoaded: scene={scene.name}, Day3Settled={DayManager.Instance?.Day3Settled}");
        if (scene.name == "Level2" && DayManager.Instance != null && DayManager.Instance.Day3Settled)
        {
            _skipDayEnd = true;
            Debug.Log("[GameTimeController] Level2 entered after Day3 settled, daily timer check disabled.");
        }

        if (scene.name == "Level2")
        {
            _level2Timer = 0f;
            _level2Ended = false;
            Debug.Log("[GameTimeController] Level2 entered, Level2 timer reset.");
        }
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

        // ── Level2 独立计时逻辑 ──
        if (currentSceneName == "Level2")
        {
            if (!_level2Ended && DayManager.Instance != null)
            {
                _level2Timer += Time.deltaTime;

                // 倒计时到 0 时结算
                if (_level2Timer >= DayManager.Level2DurationSeconds)
                {
                    _level2Ended = true;
                    if (DayManager.Instance.todayCoinsEarned >= DayManager.Level2TargetCoins)
                        DayManager.Instance.TriggerLevel2Success();
                    else
                        DayManager.Instance.TriggerLevel2Fail();
                    return;
                }
            }
        }
        // ── FloristMain 计时逻辑（原逻辑）──
        else if (_dayTimer >= DayManager.DayDurationSeconds)
        {
            if (_skipDayEnd)
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
                //Debug.Log($"[GameTimeController] 游戏时间推进 +{minutesToAdd}分钟，当前: {currentMins}（每分钟通知一次CustomerSpawner）");
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
        _level2Timer = 0f;
        _level2Ended = false;
        Debug.Log("[GameTimeController] ResetDayTimer() 被调用！计时器已启动。");
        Debug.Log($"[GameTimeController] 计时器状态: _dayTimerActive={_dayTimerActive}, _dayEnded={_dayEnded}, _dayTimer={_dayTimer}");
    }

    public void PauseDayTimer()
    {
        if (!_dayTimerActive) return;
        _pausedTime = _dayTimer;
        _dayTimerActive = false;
        Debug.Log($"[GameTimeController] 暂停计时器，当前 _dayTimer={_pausedTime:F1}s");
    }

    public void ResumeDayTimer()
    {
        if (_dayTimerActive) return;
        _dayTimer = _pausedTime;
        _dayTimerActive = true;
        Debug.Log($"[GameTimeController] 恢复计时器，_dayTimer={_dayTimer:F1}s");
    }
    int TotalMinutes(DateTime dt) => dt.Day * 1440 + dt.Hour * 60 + dt.Minute;

    // ADD this public property so other scripts can read the timer
    public float DayTimer => _dayTimer;
    public float Level2Timer => _level2Timer;
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
