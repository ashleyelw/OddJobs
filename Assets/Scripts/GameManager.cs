using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("已采集花朵（场景中的 GameObject）")]
    public List<GameObject> collectedFlowers = new List<GameObject>();

    [Header("【库存系统】")]
    [Header("未修剪鲜花库存（采摘后、修剪前）")]
    public SerializableDictionary<string, int> untrimmedFlowers = new SerializableDictionary<string, int>();

    [Header("已修剪鲜花库存（修剪后、包装前）")]
    public SerializableDictionary<string, int> trimmedFlowers = new SerializableDictionary<string, int>();

    [Header("花束库存（包装完成后，交付给顾客）")]
    public SerializableDictionary<string, int> bouquetInventory = new SerializableDictionary<string, int>();

    [Header("丝带库存（类型 → 数量）")]
    public SerializableDictionary<string, int> ribbonInventory = new SerializableDictionary<string, int>();

    [Header("待处理订单（供 OrderSystemController 显示）")]
    public List<CustomerOrder> pendingOrders = new List<CustomerOrder>();

    [Header("金币")]
    public int coins = 0;

    [SerializeField]
    private SerializableDictionary<int, int> _activeCustomerSlots = new SerializableDictionary<int, int>();

    // ============================================
    // 库存状态流转说明：
    // 采摘(花园) → untrimmedFlowers(未修剪)
    //           → trimmedFlowers(已修剪) [修剪阶段]
    //           → bouquetInventory(花束) [包装阶段]
    //           → 交付给顾客 [扣除花束库存]
    // ============================================

    // ---- 【新增】基于花束库存的订单检查接口 ----

    /// <summary>检查是否有足够的花束来完成订单（基于 bouquets）</summary>
    /// <param name="bouquetName">订单需要的花束名称</param>
    /// <returns>是否有足够的花束</returns>
    public bool HasEnoughBouquetsForOrder(string bouquetName)
    {
        return GetBouquetCount(bouquetName) > 0;
    }

    /// <summary>检查是否有足够的花束来完成订单（基于花束数组）</summary>
    /// <param name="bouquetNames">订单需要的花束名称数组</param>
    /// <returns>是否有足够的花束</returns>
    public bool HasEnoughBouquetsForOrder(string[] bouquetNames)
    {
        foreach (var name in bouquetNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (GetBouquetCount(name) <= 0) return false;
        }
        return true;
    }

    /// <summary>扣除花束订单所需的花束和丝带（花束模式下交付时调用）</summary>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="ribbonNames">丝带名称数组（与花束一起扣除）</param>
    public void DeductBouquetAndRibbonsForOrder(string bouquetName, string[] ribbonNames)
    {
        if (!string.IsNullOrWhiteSpace(bouquetName))
            RemoveBouquet(bouquetName, 1);

        if (ribbonNames != null)
        {
            foreach (var name in ribbonNames)
            {
                if (string.IsNullOrWhiteSpace(name)) continue;
                RemoveRibbonFromInventory(name, 1);
            }
        }
        Debug.Log($"[Inventory] 花束模式交付：扣除花束 {bouquetName}，丝带 {string.Join(", ", ribbonNames ?? new string[0])}");
    }

    /// <summary>花束模式交付：扣除订单所需的花束和丝带</summary>
    public void DeductBouquetOrderWithRibbons(CustomerOrder order)
    {
        DeductBouquetAndRibbonsForOrder(order.bouquetName, order.GetRibbonNames());
    }

    // ============================================
    // 测试方法
    // ============================================

    /// <summary>测试：添加指定数量的花束和丝带到库存</summary>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="bouquetCount">花束数量</param>
    /// <param name="ribbonNames">丝带名称数组</param>
    /// <param name="ribbonCounts">对应丝带数量数组</param>
    public void Test_AddBouquetWithRibbons(string bouquetName, int bouquetCount, string[] ribbonNames, int[] ribbonCounts)
    {
        AddBouquet(bouquetName, bouquetCount);
        if (ribbonNames != null && ribbonCounts != null && ribbonNames.Length == ribbonCounts.Length)
        {
            for (int i = 0; i < ribbonNames.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(ribbonNames[i]))
                    AddRibbonToInventory(ribbonNames[i], ribbonCounts[i]);
            }
        }
        Debug.Log($"[Test] 测试数据添加完成：花束 {bouquetName} x{bouquetCount}，丝带已添加");
        Debug.Log($"[Test] 当前库存状态：");
        Debug.Log($"[Test]   花束: {GetBouquetInventoryDebugInfo()}");
        Debug.Log($"[Test]   丝带: {GetRibbonInventoryDebugInfo()}");
    }

    /// <summary>测试：模拟一次花束模式订单交付（不真正完成订单，只扣除库存）</summary>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="ribbonNames">丝带名称数组</param>
    public void Test_SimulateBouquetDelivery(string bouquetName, string[] ribbonNames)
    {
        Debug.Log($"[Test] 模拟花束订单交付：");
        Debug.Log($"[Test]   交付前 - 花束 {bouquetName}: {GetBouquetCount(bouquetName)}, 丝带: {GetRibbonInventoryDebugInfo()}");
        DeductBouquetAndRibbonsForOrder(bouquetName, ribbonNames);
        Debug.Log($"[Test]   交付后 - 花束 {bouquetName}: {GetBouquetCount(bouquetName)}, 丝带: {GetRibbonInventoryDebugInfo()}");
    }

    /// <summary>测试：打印当前所有库存状态</summary>
    public void Test_PrintAllInventory()
    {
        Debug.Log($"========== [测试] 库存状态 ==========");
        Debug.Log($"未修剪鲜花: {GetUntrimmedInventoryDebugInfo()}");
        Debug.Log($"已修剪鲜花: {GetTrimmedInventoryDebugInfo()}");
        Debug.Log($"花束: {GetBouquetInventoryDebugInfo()}");
        Debug.Log($"丝带: {GetRibbonInventoryDebugInfo()}");
        Debug.Log($"金币: {coins}");
        Debug.Log($"=====================================");
    }

    string GetUntrimmedInventoryDebugInfo()
    {
        if (untrimmedFlowers == null || untrimmedFlowers.Count == 0)
            return "(空)";
        var parts = new List<string>();
        foreach (var kvp in untrimmedFlowers)
            parts.Add($"{kvp.Key}×{kvp.Value}");
        return string.Join(", ", parts);
    }

    string GetTrimmedInventoryDebugInfo()
    {
        if (trimmedFlowers == null || trimmedFlowers.Count == 0)
            return "(空)";
        var parts = new List<string>();
        foreach (var kvp in trimmedFlowers)
            parts.Add($"{kvp.Key}×{kvp.Value}");
        return string.Join(", ", parts);
    }

    string GetBouquetInventoryDebugInfo()
    {
        if (bouquetInventory == null || bouquetInventory.Count == 0)
            return "(空)";
        var parts = new List<string>();
        foreach (var kvp in bouquetInventory)
            parts.Add($"{kvp.Key}×{kvp.Value}");
        return string.Join(", ", parts);
    }

    /// <summary>扣除订单所需的花束（从花束库存中移除）</summary>
    /// <param name="bouquetNames">订单需要的花束名称数组</param>
    public void DeductBouquetsForOrder(string[] bouquetNames)
    {
        foreach (var name in bouquetNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            DeductBouquetForOrder(name);
        }
    }

    // ============================================

    // ---- 预留接口：未修剪鲜花 ----

    /// <summary>添加未修剪鲜花到库存</summary>
    public void AddUntrimmedFlower(string flowerName, int count = 1)
    {
        if (string.IsNullOrEmpty(flowerName)) return;
        string key = NormalizeKey(flowerName);
        if (untrimmedFlowers.ContainsKey(key))
            untrimmedFlowers[key] += count;
        else
            untrimmedFlowers[key] = count;
        Debug.Log($"[Inventory] 添加未修剪鲜花 {key} x{count}，现有: {untrimmedFlowers[key]}");
    }

    /// <summary>获取未修剪鲜花的数量</summary>
    public int GetUntrimmedFlowerCount(string flowerName)
    {
        string key = NormalizeKey(flowerName);
        return untrimmedFlowers.ContainsKey(key) ? untrimmedFlowers[key] : 0;
    }

    /// <summary>获取所有未修剪鲜花类型</summary>
    public List<string> GetUntrimmedFlowerKeys() => untrimmedFlowers.Keys.ToList();

    /// <summary>消耗指定数量的未修剪鲜花</summary>
    public void RemoveUntrimmedFlowers(string flowerName, int count = 1)
    {
        string key = NormalizeKey(flowerName);
        if (!untrimmedFlowers.ContainsKey(key)) return;
        untrimmedFlowers[key] -= count;
        if (untrimmedFlowers[key] <= 0) untrimmedFlowers.Remove(key);
        Debug.Log($"[Inventory] 消耗未修剪鲜花 {key} x{count}");
    }

    // ---- 预留接口：已修剪鲜花 ----

    /// <summary>添加已修剪鲜花到库存（修剪阶段完成后调用）</summary>
    public void AddTrimmedFlower(string flowerName, int count = 1)
    {
        if (string.IsNullOrEmpty(flowerName)) return;
        string key = NormalizeKey(flowerName);
        if (trimmedFlowers.ContainsKey(key))
            trimmedFlowers[key] += count;
        else
            trimmedFlowers[key] = count;
        Debug.Log($"[Inventory] 添加已修剪鲜花 {key} x{count}，现有: {trimmedFlowers[key]}");
    }

    /// <summary>将未修剪鲜花标记为已修剪（从未修剪库存移到已修剪库存）</summary>
    /// <returns>是否转移成功</returns>
    public bool TransferToTrimmed(string flowerName, int count = 1)
    {
        if (GetUntrimmedFlowerCount(flowerName) < count) return false;
        RemoveUntrimmedFlowers(flowerName, count);
        AddTrimmedFlower(flowerName, count);
        Debug.Log($"[Inventory] {flowerName} 已标记为已修剪");
        return true;
    }

    /// <summary>获取已修剪鲜花的数量</summary>
    public int GetTrimmedFlowerCount(string flowerName)
    {
        string key = NormalizeKey(flowerName);
        return trimmedFlowers.ContainsKey(key) ? trimmedFlowers[key] : 0;
    }

    /// <summary>获取所有已修剪鲜花类型</summary>
    public List<string> GetTrimmedFlowerKeys() => trimmedFlowers.Keys.ToList();

    /// <summary>消耗指定数量的已修剪鲜花</summary>
    public void RemoveTrimmedFlowers(string flowerName, int count = 1)
    {
        string key = NormalizeKey(flowerName);
        if (!trimmedFlowers.ContainsKey(key)) return;
        trimmedFlowers[key] -= count;
        if (trimmedFlowers[key] <= 0) trimmedFlowers.Remove(key);
        Debug.Log($"[Inventory] 消耗已修剪鲜花 {key} x{count}");
    }

    // ---- 预留接口：花束库存 ----

    /// <summary>添加花束到库存（包装阶段完成后调用）</summary>
    /// <param name="bouquetName">花束名称，如 "Rose_Bouquet"</param>
    /// <param name="count">数量</param>
    public void AddBouquet(string bouquetName, int count = 1)
    {
        if (string.IsNullOrEmpty(bouquetName)) return;
        string key = NormalizeKey(bouquetName);
        if (bouquetInventory.ContainsKey(key))
            bouquetInventory[key] += count;
        else
            bouquetInventory[key] = count;
        Debug.Log($"[Inventory] 添加花束 {key} x{count}，现有: {bouquetInventory[key]}");
    }

    /// <summary>将已修剪鲜花包装成花束（从已修剪库存移到花束库存）</summary>
    /// <param name="flowerNames">花束中包含的鲜花名称数组</param>
    /// <param name="bouquetName">花束名称，如 "Rose_Bouquet"</param>
    /// <returns>是否包装成功</returns>
    public bool CreateBouquetFromTrimmed(string[] flowerNames, string bouquetName)
    {
        // 检查是否有足够的已修剪鲜花
        foreach (var name in flowerNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (GetTrimmedFlowerCount(name) <= 0)
            {
                Debug.LogWarning($"[Inventory] 包装失败：缺少已修剪鲜花 {name}");
                return false;
            }
        }

        // 消耗已修剪鲜花
        foreach (var name in flowerNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveTrimmedFlowers(name, 1);
        }

        // 添加花束
        AddBouquet(bouquetName, 1);
        Debug.Log($"[Inventory] 成功创建花束 {bouquetName}");
        return true;
    }

    /// <summary>获取花束数量</summary>
    public int GetBouquetCount(string bouquetName)
    {
        string key = NormalizeKey(bouquetName);
        return bouquetInventory.ContainsKey(key) ? bouquetInventory[key] : 0;
    }

    /// <summary>获取所有花束类型</summary>
    public List<string> GetBouquetKeys() => bouquetInventory.Keys.ToList();

    /// <summary>消耗指定数量的花束（交付时调用）</summary>
    public void RemoveBouquet(string bouquetName, int count = 1)
    {
        string key = NormalizeKey(bouquetName);
        if (!bouquetInventory.ContainsKey(key)) return;
        bouquetInventory[key] -= count;
        if (bouquetInventory[key] <= 0) bouquetInventory.Remove(key);
        Debug.Log($"[Inventory] 消耗花束 {key} x{count}");
    }

    // ---- 兼容旧接口（向后兼容） ----
    [Header("花朵库存（类型 → 数量）【旧接口，建议使用新的分类库存】")]
    public SerializableDictionary<string, int> flowerInventory = new SerializableDictionary<string, int>();

    [System.Obsolete("请使用 AddUntrimmedFlower 或 AddTrimmedFlower")]
    public void AddFlowerToInventory(GameObject flower)
    {
        collectedFlowers.Add(flower);
        string key = NormalizeKey(flower.name);
        if (flowerInventory.ContainsKey(key))
            flowerInventory[key]++;
        else
            flowerInventory[key] = 1;
        AddUntrimmedFlower(key, 1);
        Debug.Log($"[Inventory] (旧接口) Added flower {key}");
    }

    [System.Obsolete("请使用 AddUntrimmedFlower")]
    public void AddFlowerToInventoryByName(string flowerName)
    {
        if (string.IsNullOrEmpty(flowerName)) return;
        string key = NormalizeKey(flowerName);
        if (flowerInventory.ContainsKey(key))
            flowerInventory[key]++;
        else
            flowerInventory[key] = 1;
        AddUntrimmedFlower(key, 1);
        Debug.Log($"[Inventory] (旧接口) Added flower by name {key}");
    }

    // ============================================

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

    public void AddRibbonToInventory(string ribbonName, int count = 1)
    {
        if (string.IsNullOrEmpty(ribbonName)) return;
        string key = NormalizeKey(ribbonName);

        if (ribbonInventory.ContainsKey(key))
            ribbonInventory[key] += count;
        else
            ribbonInventory[key] = count;

        Debug.Log($"[Inventory] Added ribbon {key} x{count}, now have: {ribbonInventory[key]}");
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