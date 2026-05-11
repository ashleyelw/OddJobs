using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneChange : MonoBehaviour
{
    static int levl2 = 0;
   public void GoToFlowerAssembly()
   {
      SceneManager.LoadScene("FlowerAssembly");
   }

   public void GoToFlowerWrap()
   {
      SceneManager.LoadScene("FlowerWrap");
   }

   public void GoToFloristMain()
   {
        var dm = DayManager.Instance;
         bool canEnterLevel2 = dm != null
            && dm.Day3Settled
            && dm.totalCoinsEarned >= DayManager.Level2UnlockCoins;
        if (canEnterLevel2&& levl2 !=1)
        {
           // ClearAllGameStateForNewLevel();
            levl2++;
            Debug.Log($"清除了一次{levl2}");

        }
        SceneManager.LoadScene(canEnterLevel2 ? "Level2" : "FloristMain");
    }
    public void GoToLevel2()
    {
        ClearAllGameStateForNewLevel();
        SceneManager.LoadScene("Level2");
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
    public void GoToFlowerGarden()
   {
      SceneManager.LoadScene("FlowerGarden");
   }

   public void GoToFlowerWater()
   {
      SceneManager.LoadScene("FlowerWater");
   }

   public void GoToThornPicki()
   {
      SceneManager.LoadScene("ThornPicki");
   }
}
