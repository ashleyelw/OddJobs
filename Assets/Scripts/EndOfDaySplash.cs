using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class EndOfDaySplash : MonoBehaviour
{
    public static EndOfDaySplash Instance { get; private set; }

    [Header("Splash Settings")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeDuration = 0.5f;

    // UI built at runtime so it works across all scenes
    private Canvas _splashCanvas;
    private CanvasGroup _canvasGroup;
    private TMP_Text _splashText;
    private TMP_Text _subText;
    private Image _background;

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

    void Start()
    {
        BuildSplashUI();
        HideImmediate();
    }

    void BuildSplashUI()
    {
        // Create a canvas that renders on top of everything
        var canvasGo = new GameObject("EndOfDaySplashCanvas");
        canvasGo.transform.SetParent(transform);
        DontDestroyOnLoad(canvasGo);

        _splashCanvas = canvasGo.AddComponent<Canvas>();
        _splashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _splashCanvas.sortingOrder = 999; // Always on top

        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        _canvasGroup = canvasGo.AddComponent<CanvasGroup>();

        // Dark background
        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(canvasGo.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        _background = bgGo.AddComponent<Image>();
        _background.color = new Color(0.1f, 0.05f, 0.2f, 0.95f);

        // Main "END OF DAY!" text
        var mainTextGo = new GameObject("SplashText");
        mainTextGo.transform.SetParent(canvasGo.transform, false);
        var mainTextRect = mainTextGo.AddComponent<RectTransform>();
        mainTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        mainTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        mainTextRect.sizeDelta = new Vector2(800, 150);
        mainTextRect.anchoredPosition = new Vector2(0, 40);
        _splashText = mainTextGo.AddComponent<TextMeshProUGUI>();
        _splashText.text = "END OF DAY!";
        _splashText.fontSize = 72;
        _splashText.fontStyle = FontStyles.Bold;
        _splashText.color = Color.yellow;
        _splashText.alignment = TextAlignmentOptions.Center;

        // Sub text
        var subTextGo = new GameObject("SubText");
        subTextGo.transform.SetParent(canvasGo.transform, false);
        var subTextRect = subTextGo.AddComponent<RectTransform>();
        subTextRect.anchorMin = new Vector2(0.5f, 0.5f);
        subTextRect.anchorMax = new Vector2(0.5f, 0.5f);
        subTextRect.sizeDelta = new Vector2(600, 60);
        subTextRect.anchoredPosition = new Vector2(0, -40);
        _subText = subTextGo.AddComponent<TextMeshProUGUI>();
        _subText.fontSize = 28;
        _subText.color = Color.white;
        _subText.alignment = TextAlignmentOptions.Center;
    }

    void HideImmediate()
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
        if (_splashCanvas != null)
            _splashCanvas.gameObject.SetActive(false);
    }

    public void Show(int dayNumber, int coinsEarned)
    {
        if (_splashCanvas == null) BuildSplashUI();

        _splashText.text = "END OF DAY!";
        _subText.text = $"Day {dayNumber} complete — {coinsEarned} coins earned today";

        _splashCanvas.gameObject.SetActive(true);
        StartCoroutine(SplashRoutine());
    }

    IEnumerator SplashRoutine()
    {
        // Fade in
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            _canvasGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        HideImmediate();

        // Now transition to EndOfDay scene
        SceneManager.LoadScene("EndOfDay");
    }
}