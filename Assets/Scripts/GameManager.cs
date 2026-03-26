using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("已采集花朵")]
    public List<GameObject> collectedFlowers = new List<GameObject>();

    [Header("花朵库存（类型 → 数量）")]
    public SerializableDictionary<string, int> flowerInventory = new SerializableDictionary<string, int>();

    [Header("待处理订单（供 OrderSystemController 显示）")]
    public List<CustomerOrder> pendingOrders = new List<CustomerOrder>();

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



    public void AddToInventory(GameObject flower)
    {
        collectedFlowers.Add(flower);
        string key = flower.name;

        if (flowerInventory.ContainsKey(key))
            flowerInventory[key]++;
        else
            flowerInventory[key] = 1;

        Debug.Log($"[Inventory] Added {key}, now have: {flowerInventory[key]}");
    }

    public bool HasInInventory(string flowerKey, int count = 1)
    {
        return flowerInventory.ContainsKey(flowerKey) && flowerInventory[flowerKey] >= count;
    }

    public void RemoveFromInventory(string flowerKey, int count = 1)
    {
        if (!flowerInventory.ContainsKey(flowerKey))
            return;

        flowerInventory[flowerKey] -= count;
        if (flowerInventory[flowerKey] <= 0)
            flowerInventory.Remove(flowerKey);

        Debug.Log($"[Inventory] Removed {flowerKey} x{count}");
    }

    public int GetCount(string flowerKey)
    {
        return flowerInventory.ContainsKey(flowerKey) ? flowerInventory[flowerKey] : 0;
    }

    public List<string> GetAvailableFlowerKeys()
    {
        return flowerInventory.Keys.ToList();
    }
}

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField] private List<TKey> keys = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (var kvp in this)
        {
            keys.Add(kvp.Key);
            values.Add(kvp.Value);
        }
    }

    public void OnAfterDeserialize()
    {
        Clear();
        for (int i = 0; i < Mathf.Min(keys.Count, values.Count); i++)
            this[keys[i]] = values[i];
    }
}
