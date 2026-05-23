using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class GameHUD : MonoBehaviour
{
    [Header("HUD References")]
    [SerializeField] private TMP_Text dayText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text thresholdText;
    [SerializeField] private Image timerBarFill;

    [Header("Timer Bar Colors")]
    [SerializeField] private Color normalColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;

    [Header("Pulse Effect")]
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseMinAlpha = 0.4f;
    [SerializeField] private float pulseMaxAlpha = 1f;

    [Header("Auto Create UI if not assigned")]
    [SerializeField] private bool autoCreateUI = true;

    // Scenes where the HUD should be completely hidden
    private static readonly string[] HiddenScenes = { "EndOfDay", "Ending", "Menu" };

    // Runtime state
    private bool _isPulsing = false;
    private float _pulseTimer = 0f;
    private float _elapsedSeconds = 0f;

    // Kept to avoid breaking any serialized references
    private float _displayTimer = 0f;
    private bool _timerRunning = false;

    // References to the auto-created root objects so we can hide/show them
    private GameObject _hudPanel;
    private GameObject _timerBarBG;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
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
        bool shouldHide = IsHiddenScene(scene.name);
        SetHUDVisible(!shouldHide);

        // If entering a gameplay scene and UI hasn't been built yet, build it now
        if (!shouldHide && autoCreateUI && dayText == null)
            CreateHUDAutomatically();
    }

    void Start()
    {
        if (autoCreateUI && dayText == null)
            CreateHUDAutomatically();

        // Check the current scene immediately on start
        bool shouldHide = IsHiddenScene(SceneManager.GetActiveScene().name);
        SetHUDVisible(!shouldHide);
    }

    void Update()
    {
        if (IsHiddenScene(SceneManager.GetActiveScene().name)) return;
        RefreshHUD();
        UpdatePulse();
    }

    // ─── Visibility ───────────────────────────────────────────────────────────

    bool IsHiddenScene(string sceneName)
    {
        foreach (var s in HiddenScenes)
            if (s == sceneName) return true;
        return false;
    }

    void SetHUDVisible(bool visible)
    {
        // Hide/show the auto-created root objects if they exist
        if (_hudPanel != null)     _hudPanel.SetActive(visible);
        if (_timerBarBG != null)   _timerBarBG.SetActive(visible);

        // Also hide/show manually assigned UI text references
        if (dayText != null)       dayText.gameObject.SetActive(visible);
        if (timerText != null)     timerText.gameObject.SetActive(visible);
        if (coinsText != null)     coinsText.gameObject.SetActive(visible);
        if (thresholdText != null) thresholdText.gameObject.SetActive(visible);
        if (timerBarFill != null)  timerBarFill.gameObject.SetActive(visible);

        if (!visible) StopPulse();
    }

    // ─── Pulse ────────────────────────────────────────────────────────────────

    void UpdatePulse()
    {
        if (!_isPulsing || timerBarFill == null) return;

        _pulseTimer += Time.deltaTime * pulseSpeed;
        float t = (Mathf.Sin(_pulseTimer) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, t);

        Color c = timerBarFill.color;
        c.a = alpha;
        timerBarFill.color = c;
    }

    void StartPulse()
    {
        if (_isPulsing) return;
        _isPulsing = true;
        _pulseTimer = 0f;
    }

    void StopPulse()
    {
        if (!_isPulsing) return;
        _isPulsing = false;

        if (timerBarFill != null)
        {
            Color c = timerBarFill.color;
            c.a = 1f;
            timerBarFill.color = c;
        }
    }

    // ─── HUD Refresh ──────────────────────────────────────────────────────────

    void RefreshHUD()
    {
        if (DayManager.Instance == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel2 = sceneName == "Level2";

        if (isLevel2)
        {
            RefreshLevel2HUD();
        }
        else
        {
            RefreshFloristMainHUD();
        }
    }

    void RefreshFloristMainHUD()
    {
        int totalSeconds = DayManager.DayDurationSeconds;
        float elapsed = GetElapsedSeconds();
        float remaining = Mathf.Max(0, totalSeconds - elapsed);
        float progress = remaining / totalSeconds;

        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        // Day label
        if (dayText != null)
            dayText.text = $"Day {DayManager.Instance.currentDay} / {DayManager.TotalDays}";

        // ── Timer bar ──
        if (timerBarFill != null)
        {
            timerBarFill.enabled = true;
            timerBarFill.fillAmount = progress;

            bool isCritical = progress <= 0.15f;
            bool isWarning  = progress <= 0.35f;

            Color baseColor = isCritical ? criticalColor
                            : isWarning  ? warningColor
                                         : normalColor;
            baseColor.a = timerBarFill.color.a;
            timerBarFill.color = baseColor;

            if (isCritical) StartPulse();
            else            StopPulse();
        }

        // ── Timer text ──
        if (timerText != null)
        {
            timerText.enabled = true;
            timerText.text  = $"Time Left: {minutes:00}:{seconds:00}";
            timerText.color = remaining <= 20f ? criticalColor
                            : remaining <= 45f ? warningColor
                                               : normalColor;
        }

        // ── Coins ──
        if (coinsText != null && GameManager.Instance != null)
            coinsText.text = $"Coins Today: {DayManager.Instance.todayCoinsEarned} " +
                             $"(Total: {DayManager.Instance.totalCoinsEarned})";

        // ── Threshold status ──
        if (thresholdText != null)
        {
            int neededMin  = DayManager.Instance.GetCoinsNeededForMinimum();
            int neededGood = DayManager.Instance.GetCoinsNeededForGoodEnding();

            if (neededMin <= 0 && neededGood <= 0)
            {
                thresholdText.text  = "Good Ending: ON TRACK ✓";
                thresholdText.color = normalColor;
            }
            else if (neededMin <= 0)
            {
                thresholdText.text  = $"Good Ending: need {neededGood} more coins";
                thresholdText.color = warningColor;
            }
            else
            {
                thresholdText.text  = $"Daily Min: need {neededMin} more coins";
                thresholdText.color = criticalColor;
            }
        }
    }

    void RefreshLevel2HUD()
    {
        if (dayText != null)
            dayText.text = "Level 2";

        // ── Level2 倒计时 ──
        if (timerBarFill != null)
        {
            timerBarFill.enabled = true;
            float elapsed = GetLevel2ElapsedSeconds();
            int total = DayManager.Level2DurationSeconds;
            float remaining = Mathf.Max(0, total - elapsed);
            float progress = remaining / total;
            timerBarFill.fillAmount = progress;

            bool isCritical = progress <= 0.15f;
            bool isWarning  = progress <= 0.35f;

            Color baseColor = isCritical ? criticalColor
                            : isWarning  ? warningColor
                                         : normalColor;
            baseColor.a = timerBarFill.color.a;
            timerBarFill.color = baseColor;

            if (isCritical) StartPulse();
            else            StopPulse();
        }

        // ── Level2 计时器文本 ──
        if (timerText != null)
        {
            timerText.enabled = true;
            float elapsed = GetLevel2ElapsedSeconds();
            int remaining = Mathf.FloorToInt(DayManager.Level2DurationSeconds - elapsed);
            int minutes = remaining / 60;
            int seconds = remaining % 60;
            timerText.text = $"Time Left: {minutes:00}:{seconds:00}";
            timerText.color = remaining <= 45 ? criticalColor
                            : remaining <= 105 ? warningColor
                                               : normalColor;
        }

        // ── 金币目标进度 ──
        if (coinsText != null && GameManager.Instance != null)
        {
            int current = DayManager.Instance.todayCoinsEarned;
            int target = DayManager.Level2TargetCoins;
            bool passed = current >= target;
            coinsText.text = $"Coins: {current} / {target} {(passed ? "✓" : "")}";
            coinsText.color = passed ? normalColor : warningColor;
        }

        // ── 目标状态 ──
        if (thresholdText != null)
        {
            int current = DayManager.Instance.todayCoinsEarned;
            int needed = Mathf.Max(0, DayManager.Level2TargetCoins - current);
            if (needed <= 0)
            {
                thresholdText.text  = "Target Reached! Keep going! ✓";
                thresholdText.color = normalColor;
            }
            else
            {
                thresholdText.text  = $"Need {needed} more coins";
                thresholdText.color = criticalColor;
            }
        }
    }

    float GetLevel2ElapsedSeconds()
    {
        if (GameTimeController.Instance != null)
            return GameTimeController.Instance.Level2Timer;
        return 0f;
            dayText.text = "Level 2";

        // ── Level2 倒计时 ──
        if (timerBarFill != null)
        {
            timerBarFill.enabled = true;
            float elapsed = GetLevel2ElapsedSeconds();
            int total = DayManager.Level2DurationSeconds;
            float remaining = Mathf.Max(0, total - elapsed);
            float progress = remaining / total;
            timerBarFill.fillAmount = progress;

            bool isCritical = progress <= 0.15f;
            bool isWarning  = progress <= 0.35f;

            Color baseColor = isCritical ? criticalColor
                            : isWarning  ? warningColor
                                         : normalColor;
            baseColor.a = timerBarFill.color.a;
            timerBarFill.color = baseColor;

            if (isCritical) StartPulse();
            else            StopPulse();
        }

        // ── Level2 计时器文本 ──
        if (timerText != null)
        {
            timerText.enabled = true;
            float elapsed = GetLevel2ElapsedSeconds();
            int remaining = Mathf.FloorToInt(DayManager.Level2DurationSeconds - elapsed);
            int minutes = remaining / 60;
            int seconds = remaining % 60;
            timerText.text = $"Time Left: {minutes:00}:{seconds:00}";
            timerText.color = remaining <= 45 ? criticalColor
                            : remaining <= 105 ? warningColor
                                               : normalColor;
        }

        // ── 金币目标进度 ──
        if (coinsText != null && GameManager.Instance != null)
        {
            int current = DayManager.Instance.todayCoinsEarned;
            int target = DayManager.Level2TargetCoins;
            bool passed = current >= target;
            coinsText.text = $"Coins: {current} / {target} {(passed ? "✓" : "")}";
            coinsText.color = passed ? normalColor : warningColor;
        }

        // ── 目标状态 ──
        if (thresholdText != null)
        {
            int current = DayManager.Instance.todayCoinsEarned;
            int needed = Mathf.Max(0, DayManager.Level2TargetCoins - current);
            if (needed <= 0)
            {
                thresholdText.text  = "Target Reached! Keep going! ✓";
                thresholdText.color = normalColor;
            }
            else
            {
                thresholdText.text  = $"Need {needed} more coins";
                thresholdText.color = criticalColor;
            }
        }
    }



    // ─── Helpers ──────────────────────────────────────────────────────────────

    float GetElapsedSeconds()
    {
        if (GameTimeController.Instance != null)
            return GameTimeController.Instance.DayTimer;
        return _elapsedSeconds;
    }

    void FixedUpdate()
    {
        if (GameTimeController.Instance != null && DayManager.Instance != null)
            _elapsedSeconds += Time.fixedDeltaTime;
    }

    // ─── Auto-create UI ───────────────────────────────────────────────────────

    void CreateHUDAutomatically()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameHUD] No Canvas found.");
            return;
        }

        // HUD Panel — top of screen
        _hudPanel = new GameObject("HUDPanel");
        _hudPanel.transform.SetParent(canvas.transform, false);
        var panelRect = _hudPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(0, 80);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImg = _hudPanel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.75f);

        // Day text — top left
        dayText = CreateTMPText(_hudPanel.transform, "DayText",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(200, -50), 20);

        // Timer text — top center
        timerText = CreateTMPText(_hudPanel.transform, "TimerText",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-100, -10), new Vector2(200, -50), 22);

        // Coins text — below timer
        coinsText = CreateTMPText(_hudPanel.transform, "CoinsText",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-150, -40), new Vector2(300, -70), 16);

        // Threshold text — top right
        thresholdText = CreateTMPText(_hudPanel.transform, "ThresholdText",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-210, -10), new Vector2(-10, -50), 16);

        // Timer bar background
        _timerBarBG = new GameObject("TimerBarBG");
        _timerBarBG.transform.SetParent(canvas.transform, false);
        var barBgRect = _timerBarBG.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0, 1);
        barBgRect.anchorMax = new Vector2(1, 1);
        barBgRect.pivot = new Vector2(0.5f, 1f);
        barBgRect.sizeDelta = new Vector2(0, 8);
        barBgRect.anchoredPosition = new Vector2(0, -80);
        var barBgImg = _timerBarBG.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Timer bar fill
        var barFill = new GameObject("TimerBarFill");
        barFill.transform.SetParent(_timerBarBG.transform, false);
        var barFillRect = barFill.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        timerBarFill = barFill.AddComponent<Image>();
        timerBarFill.color = normalColor;
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;
        timerBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        timerBarFill.fillAmount = 1f;
    }

    TMP_Text CreateTMPText(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
        return tmp;
    }
}