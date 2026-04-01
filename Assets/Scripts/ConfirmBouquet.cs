using UnityEngine;
using UnityEngine.SceneManagement;

public class ConfirmBouquet : MonoBehaviour
{
    public void OnConfirm()
    {
        SceneManager.LoadScene("FloristMain");
    }
}
