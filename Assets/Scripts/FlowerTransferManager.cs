using UnityEngine;
using System.Collections.Generic;

public class FlowerTransferManager : MonoBehaviour
{
    public static FlowerTransferManager Instance;

    public List<GameObject> selectedFlowerPrefabs = new List<GameObject>();
    public List<GameObject> selectedFlowerStemPrefabs = new List<GameObject>();

    // ADD THIS: persists flower names across scene loads for bouquet creation
    public List<string> confirmedFlowerNames = new List<string>();

    void Awake()
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
}