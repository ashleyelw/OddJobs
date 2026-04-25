using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("Auto Create UI if not assigned")]
    [SerializeField] private bool autoCreateUI = true;

    // Track timer ourselves since GameTimeController._dayTimer is private
    private float _displayTimer = 0f;
    private bool _timerRunning = false;

    void Start()
    {
        if (autoCreateUI && dayText == null)
            CreateHUDAutomatically();
    }

    void Update()
    {
        RefreshHUD();
    }

    void RefreshHUD()
    {
        if (DayManager.Instance == null) return;

        int totalSeconds = DayManager.DayDurationSeconds;
        float elapsed = GetElapsedSeconds();
        float remaining = Mathf.Max(0, totalSeconds - elapsed);

        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.FloorToInt(remaining % 60f);

        // Day
        if (dayText != null)
            dayText.text = $"Day {DayManager.Instance.currentDay} / {DayManager.TotalDays}";

        // Timer
        if (timerText != null)
        {
            timerText.text = $"Time Left: {minutes:00}:{seconds:00}";

            // Change color as time runs out
            if (remaining <= 20f)
                timerText.color = criticalColor;
            else if (remaining <= 45f)
                timerText.color = warningColor;
            else
                timerText.color = normalColor;
        }

        // Timer bar
        if (timerBarFill != null)
        {
            float progress = remaining / totalSeconds;
            timerBarFill.fillAmount = progress;

            if (progress <= 0.15f)
                timerBarFill.color = criticalColor;
            else if (progress <= 0.35f)
                timerBarFill.color = warningColor;
            else
                timerBarFill.color = normalColor;
        }

        // Coins
        if (coinsText != null && GameManager.Instance != null)
            coinsText.text = $"Coins Today: {DayManager.Instance.todayCoinsEarned} " +
                             $"(Total: {DayManager.Instance.totalCoinsEarned})";

        // Threshold status
        if (thresholdText != null)
        {
            int neededMin = DayManager.Instance.GetCoinsNeededForMinimum();
            int neededGood = DayManager.Instance.GetCoinsNeededForGoodEnding();

            if (neededMin <= 0 && neededGood <= 0)
            {
                thresholdText.text = "Good Ending: ON TRACK ✓";
                thresholdText.color = normalColor;
            }
            else if (neededMin <= 0)
            {
                thresholdText.text = $"Good Ending: need {neededGood} more coins";
                thresholdText.color = warningColor;
            }
            else
            {
                thresholdText.text = $"Daily Min: need {neededMin} more coins";
                thresholdText.color = criticalColor;
            }
        }

        // Debug info — shows whether timer is actually running
        // Remove this once transition is working
        //Debug.Log($"[GameHUD] DayManager={DayManager.Instance != null}, " +
        //          $"GameTimeController={GameTimeController.Instance != null}, " +
        //          $"Remaining={remaining:F1}s, Elapsed={elapsed:F1}s");
    }

    // Mirror the timer from GameTimeController since _dayTimer is private
    float GetElapsedSeconds()
    {
        if (GameTimeController.Instance != null)
            return GameTimeController.Instance.DayTimer;
        return _elapsedSeconds;
    }

    private float _elapsedSeconds = 0f;

    void FixedUpdate()
    {
        // Only count if GameTimeController exists and day hasn't ended
        if (GameTimeController.Instance != null && DayManager.Instance != null)
            _elapsedSeconds += Time.fixedDeltaTime;
    }

    void CreateHUDAutomatically()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[GameHUD] No Canvas found.");
            return;
        }

        // HUD Panel — top of screen
        var panel = new GameObject("HUDPanel");
        panel.transform.SetParent(canvas.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.sizeDelta = new Vector2(0, 80);
        panelRect.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.75f);

        // Day text — top left
        dayText = CreateTMPText(panel.transform, "DayText",
            new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(10, -10), new Vector2(200, -50), 20);

        // Timer text — top center
        timerText = CreateTMPText(panel.transform, "TimerText",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-100, -10), new Vector2(200, -50), 22);

        // Coins text — below timer
        coinsText = CreateTMPText(panel.transform, "CoinsText",
            new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(-150, -40), new Vector2(300, -70), 16);

        // Threshold text — top right
        thresholdText = CreateTMPText(panel.transform, "ThresholdText",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-210, -10), new Vector2(-10, -50), 16);

        // Timer bar — thin bar below the panel
        var barBg = new GameObject("TimerBarBG");
        barBg.transform.SetParent(canvas.transform, false);
        var barBgRect = barBg.AddComponent<RectTransform>();
        barBgRect.anchorMin = new Vector2(0, 1);
        barBgRect.anchorMax = new Vector2(1, 1);
        barBgRect.pivot = new Vector2(0.5f, 1f);
        barBgRect.sizeDelta = new Vector2(0, 8);
        barBgRect.anchoredPosition = new Vector2(0, -80);
        var barBgImg = barBg.AddComponent<Image>();
        barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        var barFill = new GameObject("TimerBarFill");
        barFill.transform.SetParent(barBg.transform, false);
        var barFillRect = barFill.AddComponent<RectTransform>();
        barFillRect.anchorMin = Vector2.zero;
        barFillRect.anchorMax = Vector2.one;
        barFillRect.offsetMin = Vector2.zero;
        barFillRect.offsetMax = Vector2.zero;
        timerBarFill = barFill.AddComponent<Image>();
        timerBarFill.color = normalColor;
        timerBarFill.type = Image.Type.Filled;
        timerBarFill.fillMethod = Image.FillMethod.Horizontal;
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