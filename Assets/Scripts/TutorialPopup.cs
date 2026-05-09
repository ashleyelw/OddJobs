using UnityEngine;

public class TutorialPopup : MonoBehaviour
{
    public GameObject popupPanel;
    public string popupKey = "Popup_Scissors";

    private static System.Collections.Generic.HashSet<string> seenPopups = new();

    void Start()
    {
        if (!seenPopups.Contains(popupKey))
        {
            popupPanel.SetActive(true);
            Time.timeScale = 0f; //pauses time while popup is active
        }
        else
        {
            popupPanel.SetActive(false);
        }
    }

    public void ClosePopup()
    {
        seenPopups.Add(popupKey); 
        popupPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}