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
    [SerializeField] private Color secretEndingColor = new Color(0.6f, 0f, 0.8f, 1f);

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

        if (finalCoinsText != null)
            finalCoinsText.text = $"Total Coins Earned: {dm.totalCoinsEarned}";

        if (finalOrdersText != null)
            finalOrdersText.text = $"Total Orders Delivered: {dm.totalOrdersDelivered}";

        switch (ending)
        {
            case DayManager.EndingType.Secret:
                if (endingTitleText != null)
                    endingTitleText.text = "Full Bloom Legend!";
                if (endingDescriptionText != null)
                    endingDescriptionText.text =
                        "They said it couldn't be done — but you proved them all wrong. " +
                        "Your flower shop has become a legend, whispered about in every " +
                        "corner of the city. Royalty, celebrities, and dreamers all seek " +
                        "out your bouquets. You didn't just build a business... " +
                        "you created something magical.";
                if (endingBackground != null)
                    endingBackground.color = secretEndingColor;
                break;

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
        restartButton.onClick.AddListener(OnRestartClicked);
    }

    void OnRestartClicked()
    {
        Debug.Log("[EndingManager] Restarting game — destroying all persistent managers.");
        DestroyAllPersistentManagers();
        SceneManager.LoadScene("Menu");
    }

    void DestroyAllPersistentManagers()
    {
        // Destroy all DontDestroyOnLoad managers so they
        // get recreated fresh when the game restarts

        if (CustomerSpawner.Instance != null)
        {
            // Force all customer slots empty before destroying
            for (int i = 0; i < 4; i++)
                CustomerSpawner.Instance.OnCustomerLeft(i);
            Destroy(CustomerSpawner.Instance.gameObject);
        }

        if (GameManager.Instance != null)
        {
            // Clear all inventory and orders
            GameManager.Instance.pendingOrders.Clear();
            GameManager.Instance.bouquetInventory.Clear();
            GameManager.Instance.trimmedFlowers.Clear();
            GameManager.Instance.untrimmedFlowers.Clear();
            GameManager.Instance.flowerInventory.Clear();
            GameManager.Instance.collectedFlowers.Clear();
            GameManager.Instance.coins = 0;
            Destroy(GameManager.Instance.gameObject);
        }

        if (OrderSystemController.Instance != null)
            Destroy(OrderSystemController.Instance.gameObject);

        if (GameTimeController.Instance != null)
            Destroy(GameTimeController.Instance.gameObject);

        if (CustomerSpawner.Instance != null)
            Destroy(CustomerSpawner.Instance.gameObject);

        if (FlowerTransferManager.Instance != null)
        {
            FlowerTransferManager.Instance.selectedFlowerPrefabs.Clear();
            FlowerTransferManager.Instance.selectedFlowerStemPrefabs.Clear();
            FlowerTransferManager.Instance.confirmedFlowerNames.Clear();
            Destroy(FlowerTransferManager.Instance.gameObject);
        }

        if (RibbonManager.Instance != null)
            Destroy(RibbonManager.Instance.gameObject);

        if (EndOfDaySplash.Instance != null)
            Destroy(EndOfDaySplash.Instance.gameObject);

        if (DayManager.Instance != null)
            Destroy(DayManager.Instance.gameObject);

        // Destroy InventoryDebugDisplay if present
        var debugDisplay = FindObjectOfType<InventoryDebugDisplay>();
        if (debugDisplay != null)
            Destroy(debugDisplay.gameObject);

        // Destroy GameHUD if present
        var hud = FindObjectOfType<GameHUD>();
        if (hud != null)
            Destroy(hud.gameObject);

        Debug.Log("[EndingManager] All persistent managers destroyed.");
    }
}