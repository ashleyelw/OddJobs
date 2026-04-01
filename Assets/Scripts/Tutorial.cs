using UnityEngine;

public class Tutorial : MonoBehaviour
{
    public GameObject Instructions;

    public void OpenPopup()
    {
        Instructions.SetActive(true);
    }

    public void ClosePopup()
    {
        Instructions.SetActive(false);
    }
}
