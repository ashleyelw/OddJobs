using UnityEngine;
using System.Collections;

public class ReactionManager : MonoBehaviour
{
    public static ReactionManager Instance;

    public GameObject happyPopup;
    public GameObject sadPopup;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
    }

    public void ShowHappy()
    {
        StartCoroutine(ShowPopup(happyPopup));
    }

    public void ShowSad()
    {
        StartCoroutine(ShowPopup(sadPopup));
    }

    IEnumerator ShowPopup(GameObject popup)
    {
        popup.SetActive(true);

        yield return new WaitForSeconds(2f);

        popup.SetActive(false);
    }
}
