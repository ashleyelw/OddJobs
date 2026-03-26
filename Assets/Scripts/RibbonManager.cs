using UnityEngine;

public class RibbonManager : MonoBehaviour
{
    public static RibbonManager Instance;
    public GameObject selectedRibbonPrefab;

    private void Awake()
    {
        Instance=this;
    }

    public void SelectRibbon(GameObject ribbonPrefab)
    {
        selectedRibbonPrefab=ribbonPrefab;
    }
}
