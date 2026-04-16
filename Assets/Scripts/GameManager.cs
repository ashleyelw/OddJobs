using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("已采集花朵")]
    public List<GameObject> collectedFlowers = new List<GameObject>();

    [Header("花朵库存（类型 → 数量）")]
    public SerializableDictionary<string, int> flowerInventory = new SerializableDictionary<string, int>();

    [Header("丝带库存（类型 → 数量）")]
    public SerializableDictionary<string, int> ribbonInventory = new SerializableDictionary<string, int>();

    [Header("待处理订单（供 OrderSystemController 显示）")]
    public List<CustomerOrder> pendingOrders = new List<CustomerOrder>();

    [Header("金币")]
    public int coins = 0;

    [SerializeField]
    private SerializableDictionary<int, int> _activeCustomerSlots = new SerializableDictionary<int, int>();

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
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GameManager] 场景切换: {scene.name}");

        // 调试日志：检查库存是否保留
        Debug.Log($"[GameManager] 当前库存 - 花: {GetInventoryDebugInfo()}, 丝带: {GetRibbonInventoryDebugInfo()}, 金币: {coins}");

        if (scene.name == "FloristMain")
        {
            CleanupInvalidOrders();
        }
    }

    string GetInventoryDebugInfo()
    {
        if (flowerInventory == null || flowerInventory.Count == 0)
            return "(空)";
        var parts = new List<string>();
        foreach (var kvp in flowerInventory)
            parts.Add($"{kvp.Key}×{kvp.Value}");
        return string.Join(", ", parts);
    }

    string GetRibbonInventoryDebugInfo()
    {
        if (ribbonInventory == null || ribbonInventory.Count == 0)
            return "(空)";
        var parts = new List<string>();
        foreach (var kvp in ribbonInventory)
            parts.Add($"{kvp.Key}×{kvp.Value}");
        return string.Join(", ", parts);
    }

    void CleanupInvalidOrders()
    {
        if (pendingOrders == null || pendingOrders.Count == 0) return;

        Debug.Log($"[GameManager] 开始清理订单，清理前数量: {pendingOrders.Count}");

        var activeCustomerNames = new HashSet<string>();
        var activeCoordinators = Object.FindObjectsOfType<CustomerOrderCoordinator>();
        foreach (var coord in activeCoordinators)
        {
            if (coord != null && !string.IsNullOrEmpty(coord.gameObject.name))
            {
                activeCustomerNames.Add(coord.gameObject.name);
            }
        }

        if (CustomerSpawner.Instance != null)
        {
            for (int i = 0; i < 4; i++)
            {
                var slotData = CustomerSpawner.Instance.GetSlotData(i);
                if (slotData != null && slotData.prefabIndex >= 0)
                {
                    string customerName = $"Customer_{i}_{slotData.customerNumber}";
                    activeCustomerNames.Add(customerName);
                }
            }
        }

        int removedCount = 0;
        pendingOrders.RemoveAll(order =>
        {
            if (order == null) return true;

            bool shouldRemove = false;

            if (!string.IsNullOrEmpty(order.customerName))
            {
                if (!activeCustomerNames.Contains(order.customerName))
                {
                    Debug.Log($"[GameManager] 订单无效（客户不存在）: {order.customerName}");
                    shouldRemove = true;
                }
            }

            if (shouldRemove)
            {
                removedCount++;
                return true;
            }
            return false;
        });

        if (removedCount > 0)
        {
            Debug.Log($"[GameManager] 已清理 {removedCount} 个无效订单，剩余: {pendingOrders.Count}");
        }
    }

    public static string NormalizeKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Trim();
        const string clone = "(Clone)";
        if (name.EndsWith(clone))
            name = name.Substring(0, name.Length - clone.Length).TrimEnd();
        return name;
    }

    string ResolveFlowerKey(string flowerKey)
    {
        if (string.IsNullOrEmpty(flowerKey)) return flowerKey;
        if (flowerInventory.ContainsKey(flowerKey)) return flowerKey;
        string norm = NormalizeKey(flowerKey);
        if (flowerInventory.ContainsKey(norm)) return norm;
        foreach (var k in flowerInventory.Keys)
        {
            if (NormalizeKey(k) == norm)
                return k;
        }
        return norm;
    }

    string ResolveRibbonKey(string ribbonKey)
    {
        if (string.IsNullOrEmpty(ribbonKey)) return ribbonKey;
        if (ribbonInventory.ContainsKey(ribbonKey)) return ribbonKey;
        string norm = NormalizeKey(ribbonKey);
        if (ribbonInventory.ContainsKey(norm)) return norm;
        foreach (var k in ribbonInventory.Keys)
        {
            if (NormalizeKey(k) == norm)
                return k;
        }
        return norm;
    }

    public void AddFlowerToInventory(GameObject flower)
    {
        collectedFlowers.Add(flower);
        string key = NormalizeKey(flower.name);

        if (flowerInventory.ContainsKey(key))
            flowerInventory[key]++;
        else
            flowerInventory[key] = 1;

        Debug.Log($"[Inventory] Added flower {key}, now have: {flowerInventory[key]}");
    }

    public void AddRibbonToInventory(GameObject ribbon)
    {
        if (ribbon == null) return;
        string key = NormalizeKey(ribbon.name);

        if (ribbonInventory.ContainsKey(key))
            ribbonInventory[key]++;
        else
            ribbonInventory[key] = 1;

        Debug.Log($"[Inventory] Added ribbon {key}, now have: {ribbonInventory[key]}");
    }

    public void AddRibbonToInventory(string ribbonName)
    {
        if (string.IsNullOrEmpty(ribbonName)) return;
        string key = NormalizeKey(ribbonName);

        if (ribbonInventory.ContainsKey(key))
            ribbonInventory[key]++;
        else
            ribbonInventory[key] = 1;

        Debug.Log($"[Inventory] Added ribbon {key}, now have: {ribbonInventory[key]}");
    }

    public void RemoveFromInventory(string flowerKey, int count = 1)
    {
        string key = ResolveFlowerKey(flowerKey);
        if (!flowerInventory.ContainsKey(key))
            return;

        flowerInventory[key] -= count;
        if (flowerInventory[key] <= 0)
            flowerInventory.Remove(key);

        Debug.Log($"[Inventory] Removed flower {key} x{count}");
    }

    public void RemoveRibbonFromInventory(string ribbonKey, int count = 1)
    {
        string key = ResolveRibbonKey(ribbonKey);
        if (!ribbonInventory.ContainsKey(key))
            return;

        ribbonInventory[key] -= count;
        if (ribbonInventory[key] <= 0)
            ribbonInventory.Remove(key);

        Debug.Log($"[Inventory] Removed ribbon {key} x{count}");
    }

    public int GetFlowerCount(string flowerKey)
    {
        string key = ResolveFlowerKey(flowerKey);
        return flowerInventory.ContainsKey(key) ? flowerInventory[key] : 0;
    }

    public int GetRibbonCount(string ribbonKey)
    {
        string key = ResolveRibbonKey(ribbonKey);
        return ribbonInventory.ContainsKey(key) ? ribbonInventory[key] : 0;
    }

    public List<string> GetAvailableFlowerKeys()
    {
        return flowerInventory.Keys.ToList();
    }

    public List<string> GetAvailableRibbonKeys()
    {
        return ribbonInventory.Keys.ToList();
    }

    public Dictionary<string, int> GetMissingFlowers(CustomerOrder order)
    {
        var missing = new Dictionary<string, int>();
        var required = new Dictionary<string, int>();

        foreach (var name in order.GetFlowerNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string norm = NormalizeKey(name.Trim());
            if (required.ContainsKey(norm))
                required[norm]++;
            else
                required[norm] = 1;
        }

        foreach (var kvp in required)
        {
            int have = GetFlowerCount(kvp.Key);
            if (have < kvp.Value)
                missing[kvp.Key] = kvp.Value - have;
        }

        return missing;
    }

    public Dictionary<string, int> GetMissingRibbons(CustomerOrder order)
    {
        var missing = new Dictionary<string, int>();
        var required = new Dictionary<string, int>();

        foreach (var name in order.GetRibbonNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            string norm = NormalizeKey(name.Trim());
            if (required.ContainsKey(norm))
                required[norm]++;
            else
                required[norm] = 1;
        }

        foreach (var kvp in required)
        {
            int have = GetRibbonCount(kvp.Key);
            if (have < kvp.Value)
                missing[kvp.Key] = kvp.Value - have;
        }

        return missing;
    }

    public bool HasEnoughForOrder(CustomerOrder order)
    {
        return GetMissingFlowers(order).Count == 0 && GetMissingRibbons(order).Count == 0;
    }

    public void DeductOrderFlowers(CustomerOrder order)
    {
        foreach (var name in order.GetFlowerNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveFromInventory(name, 1);
        }
    }

    public void DeductOrderRibbons(CustomerOrder order)
    {
        foreach (var name in order.GetRibbonNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveRibbonFromInventory(name, 1);
        }
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        Debug.Log($"[GameManager] 金币 +{amount}，当前: {coins}");
    }

    public void RegisterActiveCustomer(string customerName, int slotIndex)
    {
        int instanceId = GameObject.Find(customerName)?.GetInstanceID() ?? 0;
        _activeCustomerSlots[instanceId] = slotIndex;
    }

    public void MarkCustomerCompleted(string customerName)
    {
        var go = GameObject.Find(customerName);
        if (go == null) return;
        int instanceId = go.GetInstanceID();
        if (_activeCustomerSlots.ContainsKey(instanceId))
        {
            int slot = _activeCustomerSlots[instanceId];
            _activeCustomerSlots.Remove(instanceId);
            CustomerSpawner.Instance?.OnCustomerLeft(slot);
        }
        go.SetActive(false);
        Debug.Log($"[GameManager] 客户已完成订单: {customerName}");
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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