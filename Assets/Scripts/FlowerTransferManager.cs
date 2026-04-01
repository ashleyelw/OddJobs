using UnityEngine;
using System.Collections.Generic;

public class FlowerTransferManager : MonoBehaviour
{
    public static FlowerTransferManager Instance;

    public List<GameObject> selectedFlowerPrefabs = new List<GameObject>();

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
