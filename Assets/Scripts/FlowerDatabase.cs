using UnityEngine;
using System.Collections.Generic;

public class FlowerDatabase : MonoBehaviour
{
    public static FlowerDatabase Instance;

    [System.Serializable]
    public class FlowerEntry
    {
            public string id;
            public GameObject prefab;
    }

    public List<FlowerEntry> flowers;

    void Awake()
    {
        Instance=this;
    }
    public GameObject GetPrefab(string id)
    {
        foreach(var flower in flowers)
        {
            if(flower.id==id)
            return flower.prefab;
        }
        Debug.LogError("No prefab found for ID: "+id);
        return null;
    }
}
