using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfirmFlowers : MonoBehaviour

{
    public void OnConfirm()
    {
        SceneManager.LoadScene("FlowerWrap");
    }
}
