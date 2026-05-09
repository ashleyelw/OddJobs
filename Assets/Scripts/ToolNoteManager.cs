using UnityEngine;

public class TrimTutorialManager : MonoBehaviour
{
    public static TrimTutorialManager Instance;

    public GameObject tutorialNote;

    private bool thornUsed = false;
    private bool scissorsUsed = false;

    private void Awake()
    {
        Instance = this;
    }

    public void ThornToolUsed()
    {
        thornUsed = true;
        CheckTutorialComplete();
    }

    public void ScissorsUsed()
    {
        scissorsUsed = true;
        CheckTutorialComplete();
    }

    void CheckTutorialComplete()
    {
        if (thornUsed && scissorsUsed)
        {
            tutorialNote.SetActive(false);
        }
    }
}
