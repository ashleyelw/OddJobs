using UnityEngine;

public class RibbonManager : MonoBehaviour
{
    public static RibbonManager Instance { get; private set; }

    public GameObject selectedRibbonPrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SelectRibbon(GameObject ribbonPrefab)
    {
        selectedRibbonPrefab = ribbonPrefab;
        Debug.Log($"[RibbonManager] 选择了丝带: {ribbonPrefab?.name}");
    }

    public void ClearSelection()
    {
        selectedRibbonPrefab = null;
    }
}