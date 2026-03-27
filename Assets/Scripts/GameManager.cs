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

    [Header("金币")]
    public int coins = 0;

    [SerializeField]
    private SerializableDictionary<string, bool> _completedCustomers = new SerializableDictionary<string, bool>();

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

    public static string NormalizeFlowerKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Trim();
        const string clone = "(Clone)";
        if (name.EndsWith(clone))
            name = name.Substring(0, name.Length - clone.Length).TrimEnd();
        return name;
    }

    string ResolveInventoryKey(string flowerKey)
    {
        if (string.IsNullOrEmpty(flowerKey)) return flowerKey;
        if (flowerInventory.ContainsKey(flowerKey)) return flowerKey;
        string norm = NormalizeFlowerKey(flowerKey);
        if (flowerInventory.ContainsKey(norm)) return norm;
        foreach (var k in flowerInventory.Keys)
        {
            if (NormalizeFlowerKey(k) == norm)
                return k;
        }
        return norm;
    }

    public void AddToInventory(GameObject flower)
    {
        collectedFlowers.Add(flower);
        string key = NormalizeFlowerKey(flower.name);

        if (flowerInventory.ContainsKey(key))
            flowerInventory[key]++;
        else
            flowerInventory[key] = 1;

        Debug.Log($"[Inventory] Added {key}, now have: {flowerInventory[key]}");
    }

    public bool HasInInventory(string flowerKey, int count = 1)
    {
        return GetCount(flowerKey) >= count;
    }

    public void RemoveFromInventory(string flowerKey, int count = 1)
    {
        string key = ResolveInventoryKey(flowerKey);
        if (!flowerInventory.ContainsKey(key))
            return;

        flowerInventory[key] -= count;
        if (flowerInventory[key] <= 0)
            flowerInventory.Remove(key);

        Debug.Log($"[Inventory] Removed {key} x{count}");
    }

    public int GetCount(string flowerKey)
    {
        string key = ResolveInventoryKey(flowerKey);
        return flowerInventory.ContainsKey(key) ? flowerInventory[key] : 0;
    }

    public List<string> GetAvailableFlowerKeys()
    {
        return flowerInventory.Keys.ToList();
    }

    public Dictionary<string, int> GetMissingFlowers(CustomerOrder order)
    {
        var missing = new Dictionary<string, int>();
        var required = new Dictionary<string, int>();

        foreach (var name in order.GetFlowerNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string norm = NormalizeFlowerKey(name.Trim());
            if (required.ContainsKey(norm))
                required[norm]++;
            else
                required[norm] = 1;
        }

        foreach (var kvp in required)
        {
            int have = GetCount(kvp.Key);
            if (have < kvp.Value)
                missing[kvp.Key] = kvp.Value - have;
        }

        return missing;
    }

    public bool HasEnoughForOrder(CustomerOrder order)
    {
        return GetMissingFlowers(order).Count == 0;
    }

    public void DeductOrderFlowers(CustomerOrder order)
    {
        foreach (var name in order.GetFlowerNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveFromInventory(name, 1);
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        Debug.Log($"[GameManager] 金币 +{amount}，当前: {coins}");
    }

    public void MarkCustomerCompleted(string customerName)
    {
        if (string.IsNullOrEmpty(customerName)) return;
        _completedCustomers[customerName] = true;
        HideCustomerByName(customerName);
        Debug.Log($"[GameManager] 客户已完成订单: {customerName}");
    }

    void HideCustomerByName(string customerName)
    {
        var transforms = FindObjectsOfType<Transform>();
        foreach (var t in transforms)
        {
            if (t.name == customerName)
            {
                t.gameObject.SetActive(false);
                Debug.Log($"[GameManager] 隐藏客户: {customerName}");
                return;
            }
        }
    }

    void Update()
    {
        foreach (var name in _completedCustomers.Keys)
            HideCustomerByName(name);
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
