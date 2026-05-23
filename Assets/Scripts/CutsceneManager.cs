using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneManager : MonoBehaviour
{
    public float cutsceneLength = 15f;

    void Start()
    {
        Invoke("LoadGame", cutsceneLength);
    }

    void LoadGame()
    {
        SceneManager.LoadScene("FloristMain");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadGame();
        }
    }
}