using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
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
    SceneManager.LoadScene("FloristMain");
   }
}
