using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    void OnMouseDown()
    {
        Debug.Log("Arrow clicked");
        SceneManager.LoadScene("FlowerWrap");
    }
}
