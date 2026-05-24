using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Level2UnlockUI : MonoBehaviour
{
    [Header("跳关按钮")]
    [SerializeField] private Button unlockLevel2Button;
    [SerializeField] private TMP_Text unlockLevel2Text;
    [SerializeField] private int coinThreshold = 120;

    private bool _hasUnlocked = false;

    void Start()
    {
        if (unlockLevel2Button != null)
            unlockLevel2Button.gameObject.SetActive(false);
    }

    void Update()
    {
        if (_hasUnlocked) return;
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.coins >= coinThreshold)
        {
            _hasUnlocked = true;
            ShowUnlockButton();
        }
    }

    void ShowUnlockButton()
    {
        if (unlockLevel2Button != null)
        {
            unlockLevel2Button.gameObject.SetActive(true);
            if (unlockLevel2Text != null)
                unlockLevel2Text.text = $"Next Level";
        }
        Debug.Log($"[Level2UnlockUI] 金币已达 {GameManager.Instance.coins}，解锁 Level2 按钮！");
    }

    public void OnUnlockLevel2Clicked()
    {
        DayManager.Instance?.ResetForLevel2();
        ClearAllGameStateForNewLevel();
        UnityEngine.SceneManagement.SceneManager.LoadScene("Level2");
    }

    void ClearAllGameStateForNewLevel()
    {
        OrderSystemController.CloseAllCustomerOrderUIs();
        if (OrderSystemController.Instance != null)
            OrderSystemController.Instance.CloseAll();
        if (GameManager.Instance != null)
            GameManager.Instance.ClearPendingOrders();
        if (CustomerSpawner.Instance != null)
            CustomerSpawner.Instance.ResetAllSlots();
    }
}