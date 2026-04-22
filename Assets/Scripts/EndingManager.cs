using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EndingManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingDescriptionText;
    [SerializeField] private TMP_Text finalCoinsText;
    [SerializeField] private TMP_Text finalOrdersText;
    [SerializeField] private Image endingBackground;
    [SerializeField] private Button restartButton;

    [Header("Ending Content")]
    [SerializeField] private Color goodEndingColor = Color.green;
    [SerializeField] private Color neutralEndingColor = Color.yellow;
    [SerializeField] private Color badEndingColor = Color.red;

    void Start()
    {
        if (DayManager.Instance == null)
        {
            Debug.LogError("[EndingManager] DayManager not found!");
            return;
        }

        ShowEnding();
        SetupRestartButton();
    }

    void ShowEnding()
    {
        var dm = DayManager.Instance;
        var ending = dm.GetEnding();

        // Final stats
        if (finalCoinsText != null)
            finalCoinsText.text = $"Total Coins Earned: {dm.totalCoinsEarned}";

        if (finalOrdersText != null)
            finalOrdersText.text = $"Total Orders Delivered: {dm.totalOrdersDelivered}";

        // Ending specific content
        switch (ending)
        {
            case DayManager.EndingType.Good:
                if (endingTitleText != null)
                    endingTitleText.text = "Flourishing Florist!";
                if (endingDescriptionText != null)
                    endingDescriptionText.text =
                        "Your flower shop is the talk of the town! " +
                        "Customers come from far and wide to buy your beautiful bouquets. " +
                        "The future looks bright for your blooming business!";
                if (endingBackground != null)
                    endingBackground.color = goodEndingColor;
                break;

            case DayManager.EndingType.Neutral:
                if (endingTitleText != null)
                    endingTitleText.text = "Steady Stems";
                if (endingDescriptionText != null)
                    endingDescriptionText.text =
                        "Your shop is getting by, but there's room to grow. " +
                        "With a bit more hustle and some better bouquets, " +
                        "you could really make this business bloom!";
                if (endingBackground != null)
                    endingBackground.color = neutralEndingColor;
                break;

            case DayManager.EndingType.Bad:
                if (endingTitleText != null)
                    endingTitleText.text = "Wilting Away...";
                if (endingDescriptionText != null)
                    endingDescriptionText.text =
                        "The shop couldn't keep up with the bills. " +
                        "Customers left disappointed and the flowers wilted unsold. " +
                        "Maybe next time you'll stop and smell the roses before it's too late.";
                if (endingBackground != null)
                    endingBackground.color = badEndingColor;
                break;
        }
    }

    void SetupRestartButton()
    {
        if (restartButton == null) return;
        restartButton.onClick.RemoveAllListeners();
        restartButton.onClick.AddListener(() =>
        {
            // Destroy DayManager so it resets on next play
            Destroy(DayManager.Instance.gameObject);
            SceneManager.LoadScene("Menu");
        });
    }
}