using UnityEngine;

public class VaseManager : MonoBehaviour
{
    public GameObject smallVase;
    public GameObject bigVase;

    public void UseSmallVase()
    {
        smallVase.SetActive(true);
        bigVase.SetActive(false);
    }

    public void UseBigVase()
    {
        smallVase.SetActive(false);
        bigVase.SetActive(true);
    }
}