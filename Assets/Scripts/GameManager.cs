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
    public List<BouquetData> bouquetInventory = new List<BouquetData>();

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
        if (string.IsNullOrWhiteSpace(bouquetName)) return false;
        string key = NormalizeKey(bouquetName);
        return bouquetInventory.Exists(b => NormalizeKey(b.bouquetName) == key);
    }

    /// <summary>检查是否有足够的花束来完成订单（基于花束数组）</summary>
    /// <param name="bouquetNames">订单需要的花束名称数组</param>
    /// <returns>是否有足够的花束</returns>
    public bool HasEnoughBouquetsForOrder(string[] bouquetNames)
    {
        foreach (var name in bouquetNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!HasEnoughBouquetsForOrder(name)) return false;
        }
        return true;
    }

    /// <summary>扣除花束订单所需的花束（花束模式下交付时调用）</summary>
    /// <param name="bouquetName">花束名称</param>
    public void DeductBouquetForOrder(string bouquetName)
    {
        if (!string.IsNullOrWhiteSpace(bouquetName))
            RemoveBouquet(bouquetName, 1);
        Debug.Log($"[Inventory] 花束模式交付：扣除花束 {bouquetName}");
    }

    /// <summary>花束模式交付：扣除订单所需的花束（支持多个花束）</summary>
    public void DeductBouquetOrder(CustomerOrder order)
    {
        if (order.bouquetNames != null)
        {
            foreach (var name in order.bouquetNames)
            {
                DeductBouquetForOrder(name);
            }
        }
        else
        {
            // 兼容旧代码：单个花束名称
            DeductBouquetForOrder(order.GetBouquetName());
        }
    }

    // ============================================
    // 测试方法
    // ============================================

    /// <summary>测试：添加花束到库存（带花朵和丝带信息）</summary>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="flowers">使用的修剪后花朵列表</param>
    /// <param name="ribbonName">使用的丝带名称</param>
    public void Test_AddBouquet(string bouquetName, List<string> flowers, string ribbonName)
    {
        BouquetData bouquet = new BouquetData(bouquetName, flowers, ribbonName);
        bouquetInventory.Add(bouquet);
        Debug.Log($"[Test] 测试数据添加完成：{bouquet.GetDescription()}");
        Debug.Log($"[Test] 当前花束库存: {GetBouquetInventoryDebugInfo()}");
    }

    /// <summary>测试：模拟一次花束模式订单交付（不真正完成订单，只扣除库存）</summary>
    /// <param name="bouquetName">花束名称</param>
    public void Test_SimulateBouquetDelivery(string bouquetName)
    {
        Debug.Log($"[Test] 模拟花束订单交付：");
        Debug.Log($"[Test]   交付前 - 花束库存: {GetBouquetInventoryDebugInfo()}");
        DeductBouquetForOrder(bouquetName);
        Debug.Log($"[Test]   交付后 - 花束库存: {GetBouquetInventoryDebugInfo()}");
    }

    /// <summary>测试：打印当前所有库存状态</summary>
    public void Test_PrintAllInventory()
    {
        Debug.Log($"========== [测试] 库存状态 ==========");
        Debug.Log($"未修剪鲜花: {GetUntrimmedInventoryDebugInfo()}");
        Debug.Log($"已修剪鲜花: {GetTrimmedInventoryDebugInfo()}");
        Debug.Log($"花束: {GetBouquetInventoryDebugInfo()}");
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
        foreach (var bouquet in bouquetInventory)
            parts.Add($"{bouquet.GetSimpleDescription()}");
        return string.Join(", ", parts);
    }

    /// <summary>扣除订单所需的花束（从花束库存中移除）</summary>
    /// <param name="bouquetNames">订单需要的花束名称数组</param>
    public void DeductBouquetsForOrder(string[] bouquetNames)
    {
        foreach (var name in bouquetNames)
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveBouquet(name, 1);
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

    // ============================================
    // 【新增】修剪状态检查接口
    // ============================================

    /// <summary>
    /// 检查鲜花库存中是否有已修剪状态的鲜花
    /// </summary>
    /// <param name="flowerName">鲜花名称</param>
    /// <returns>是否有已修剪的鲜花</returns>
    public bool HasTrimmedFlower(string flowerName)
    {
        return GetTrimmedFlowerCount(flowerName) > 0;
    }

    /// <summary>
    /// 检查是否有任何已修剪的鲜花
    /// </summary>
    public bool HasAnyTrimmedFlowers()
    {
        return trimmedFlowers != null && trimmedFlowers.Count > 0;
    }

    /// <summary>
    /// 获取已修剪鲜花的信息（包括状态）
    /// </summary>
    public string GetTrimmedFlowerInfo(string flowerName)
    {
        int count = GetTrimmedFlowerCount(flowerName);
        return count > 0 ? $"已修剪 x{count}" : "无已修剪鲜花";
    }

    // ============================================
    // 【新增】花束组装接口（基于Flower注册和Ribbon注册）
    // 用于包装阶段将已修剪鲜花和丝带组装成花束
    // ============================================

    /// <summary>
    /// 从FlowerTransferManager和RibbonManager组装花束
    /// 将 FlowerTransferManager 中选择的已修剪鲜花与 RibbonManager 中的丝带组合成花束
    /// </summary>
    /// <param name="bouquetName">花束名称，如 "Rose_Bouquet"</param>
    /// <returns>是否组装成功</returns>
    public bool AssembleBouquetFromTransferManager(string bouquetName)
    {
        if (FlowerTransferManager.Instance == null)
        {
            Debug.LogWarning("[GameManager] FlowerTransferManager.Instance 为空，无法组装花束");
            return false;
        }

        // 获取当前选择的鲜花列表（应该是已修剪的）
        var flowerPrefabs = FlowerTransferManager.Instance.selectedFlowerPrefabs;
        var stemPrefabs = FlowerTransferManager.Instance.selectedFlowerStemPrefabs;
        
        if ((flowerPrefabs == null || flowerPrefabs.Count == 0) && 
            (stemPrefabs == null || stemPrefabs.Count == 0))
        {
            Debug.LogWarning("[GameManager] 没有选择的鲜花可以组装花束");
            return false;
        }

        // 提取鲜花名称
        var flowerNames = new List<string>();
        
        // 从 stemPrefabs 提取（优先使用）
        foreach (var stemPrefab in stemPrefabs)
        {
            if (stemPrefab != null)
            {
                string name = NormalizeKey(stemPrefab.name);
                flowerNames.Add(name);
            }
        }
        
        // 从 flowerPrefabs 提取
        foreach (var flowerPrefab in flowerPrefabs)
        {
            if (flowerPrefab != null)
            {
                string name = NormalizeKey(flowerPrefab.name);
                if (!flowerNames.Contains(name))
                    flowerNames.Add(name);
            }
        }

        // 获取当前选择的丝带
        string ribbonName = null;
        if (RibbonManager.Instance != null && RibbonManager.Instance.selectedRibbonPrefab != null)
        {
            ribbonName = NormalizeKey(RibbonManager.Instance.selectedRibbonPrefab.name);
        }

        // 检查鲜花是否已修剪（如果FlowerData组件存在）
        foreach (var stemPrefab in stemPrefabs)
        {
            if (stemPrefab != null)
            {
                var flowerData = stemPrefab.GetComponent<FlowerData>();
                if (flowerData != null && !flowerData.IsTrimmed)
                {
                    Debug.LogWarning($"[GameManager] 鲜花 {stemPrefab.name} 尚未修剪，无法组装花束");
                    return false;
                }
            }
        }

        // 消耗已修剪鲜花库存
        foreach (var name in flowerNames)
        {
            RemoveTrimmedFlowers(name, 1);
        }

        // 创建花束（包含鲜花名称和丝带信息）
        AddBouquet(bouquetName, flowerNames, ribbonName);

        // 清理选择列表
        FlowerTransferManager.Instance.selectedFlowerPrefabs.Clear();
        FlowerTransferManager.Instance.selectedFlowerStemPrefabs.Clear();
        
        // 清理丝带选择
        if (RibbonManager.Instance != null)
        {
            RibbonManager.Instance.ClearSelection();
        }

        Debug.Log($"[GameManager] 成功组装花束 {bouquetName}（鲜花: {string.Join(", ", flowerNames)}，丝带: {ribbonName ?? "(无)"})");
        return true;
    }

    /// <summary>
    /// 手动组装花束（使用鲜花名称列表和丝带）
    /// </summary>
    /// <param name="flowerNames">鲜花名称列表</param>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="ribbonName">丝带名称（可选）</param>
    /// <returns>是否组装成功</returns>
    public bool AssembleBouquet(List<string> flowerNames, string bouquetName, string ribbonName = null)
    {
        if (flowerNames == null || flowerNames.Count == 0)
        {
            Debug.LogWarning("[GameManager] 组装花束失败：鲜花列表为空");
            return false;
        }

        // 检查所有鲜花是否已修剪
        foreach (var name in flowerNames)
        {
            if (GetTrimmedFlowerCount(name) <= 0)
            {
                Debug.LogWarning($"[GameManager] 组装花束失败：鲜花 {name} 未修剪或库存不足");
                return false;
            }
        }

        // 消耗已修剪鲜花
        foreach (var name in flowerNames)
        {
            RemoveTrimmedFlowers(name, 1);
        }

        // 创建花束
        AddBouquet(bouquetName, flowerNames, ribbonName);
        Debug.Log($"[GameManager] 成功组装花束 {bouquetName}（鲜花: {string.Join(", ", flowerNames)}，丝带: {ribbonName ?? "(无)"})");
        return true;
    }

    /// <summary>
    /// 简化的花束组装方法（只传鲜花和丝带，自动生成花束名称）
    /// </summary>
    /// <param name="flowerName">鲜花名称（单个）</param>
    /// <param name="ribbonName">丝带名称</param>
    /// <returns>花束名称，组装失败返回 null</returns>
    public string AssembleBouquet(string flowerName, string ribbonName)
    {
        return AssembleBouquet(new List<string> { flowerName }, ribbonName);
    }

    /// <summary>
    /// 简化的花束组装方法（只传鲜花列表和丝带，自动生成花束名称）
    /// </summary>
    /// <param name="flowerNames">鲜花名称列表</param>
    /// <param name="ribbonName">丝带名称</param>
    /// <returns>花束名称，组装失败返回 null</returns>
    public string AssembleBouquet(List<string> flowerNames, string ribbonName)
    {
        if (flowerNames == null || flowerNames.Count == 0)
        {
            Debug.LogWarning("[GameManager] 组装花束失败：鲜花列表为空");
            return null;
        }

        if (string.IsNullOrEmpty(ribbonName))
        {
            Debug.LogWarning("[GameManager] 组装花束失败：丝带名称为空");
            return null;
        }

        // 检查所有鲜花是否已修剪
        foreach (var name in flowerNames)
        {
            if (GetTrimmedFlowerCount(name) <= 0)
            {
                Debug.LogWarning($"[GameManager] 组装花束失败：鲜花 {name} 未修剪或库存不足");
                return null;
            }
        }

        // 生成花束名称（格式：FlowerName_RibbonName，如 Rose_Pink）
        string normalizedRibbon = ribbonName;
        if (normalizedRibbon.StartsWith("Ribbon"))
            normalizedRibbon = normalizedRibbon.Substring(6); // 移除 "Ribbon" 前缀
        
        string bouquetName = $"{flowerNames[0]}_{normalizedRibbon}";

        // 消耗已修剪鲜花
        foreach (var name in flowerNames)
        {
            RemoveTrimmedFlowers(name, 1);
        }

        // 创建花束
        AddBouquet(bouquetName, flowerNames, ribbonName);
        Debug.Log($"[GameManager] 成功组装花束 {bouquetName}（鲜花: {string.Join(", ", flowerNames)}，丝带: {ribbonName}）");
        return bouquetName;
    }

    // ============================================
    // 【新增】订单生成时自动组装花束
    // 从鲜花库存和丝带库存组装成花束，生成花束订单
    // ============================================

    /// <summary>
    /// 订单生成时自动组装花束（从库存获取鲜花和丝带）
    /// </summary>
    /// <param name="requiredFlowers">订单需要的鲜花名称列表</param>
    /// <param name="requiredRibbons">订单需要的丝带名称列表</param>
    /// <param name="bouquetName">生成的花束名称（如果为空则自动生成）</param>
    /// <returns>组装后的花束数据，如果失败返回null</returns>
    public BouquetData AutoAssembleBouquetForOrder(List<string> requiredFlowers, List<string> requiredRibbons, string bouquetName = null)
    {
        if (requiredFlowers == null || requiredFlowers.Count == 0)
        {
            Debug.LogWarning("[GameManager] 自动组装花束失败：鲜花列表为空");
            return null;
        }

        // 检查鲜花库存
        foreach (var flower in requiredFlowers)
        {
            if (string.IsNullOrWhiteSpace(flower)) continue;
            if (GetTrimmedFlowerCount(flower) <= 0)
            {
                Debug.LogWarning($"[GameManager] 自动组装花束失败：已修剪鲜花库存不足 - {flower}");
                return null;
            }
        }

        // 消耗已修剪鲜花
        var consumedFlowers = new List<string>();
        foreach (var flower in requiredFlowers)
        {
            if (string.IsNullOrWhiteSpace(flower)) continue;
            RemoveTrimmedFlowers(flower, 1);
            consumedFlowers.Add(flower);
        }

        // 处理丝带
        string ribbonName = null;
        if (requiredRibbons != null && requiredRibbons.Count > 0)
        {
            foreach (var ribbon in requiredRibbons)
            {
                if (string.IsNullOrWhiteSpace(ribbon)) continue;
                // 尝试从 RibbonManager 获取丝带
                if (RibbonManager.Instance != null && RibbonManager.Instance.HasEnoughRibbon(ribbon, 1))
                {
                    RibbonManager.Instance.RemoveRibbon(ribbon, 1);
                    ribbonName = ribbon;
                    break;
                }
            }
        }

        // 如果没有指定丝带，尝试从 RibbonManager 随机获取一个
        if (string.IsNullOrEmpty(ribbonName) && RibbonManager.Instance != null && RibbonManager.Instance.HasAnyRibbons())
        {
            ribbonName = RibbonManager.Instance.ConsumeRandomRibbon();
        }

        // 生成花束名称
        string finalBouquetName = bouquetName;
        if (string.IsNullOrEmpty(finalBouquetName))
        {
            finalBouquetName = GenerateBouquetName(consumedFlowers, ribbonName);
        }

        // 创建花束
        BouquetData bouquet = new BouquetData(finalBouquetName, consumedFlowers, ribbonName);
        bouquetInventory.Add(bouquet);

        Debug.Log($"[GameManager] 订单生成自动组装花束: {bouquet.GetDescription()}");
        return bouquet;
    }

    /// <summary>
    /// 根据鲜花和丝带列表生成花束名称
    /// 格式：Flower1_Flower2_RibbonName
    /// </summary>
    public string GenerateBouquetName(List<string> flowers, string ribbonName = null)
    {
        if (flowers == null || flowers.Count == 0)
            return "Custom_Bouquet";

        // 归一化鲜花名称（移除可能的 "2" 后缀）
        var normalizedFlowers = flowers
            .Where(f => !string.IsNullOrWhiteSpace(f))
            .Select(f => NormalizeFlowerName(f))
            .ToList();

        if (normalizedFlowers.Count == 0)
            return "Custom_Bouquet";

        // 生成花束名称
        string flowerPart = string.Join("_", normalizedFlowers);
        string ribbonPart = !string.IsNullOrEmpty(ribbonName) ? NormalizeRibbonName(ribbonName) : "NoRibbon";

        return $"{flowerPart}_{ribbonPart}";
    }

    /// <summary>
    /// 归一化鲜花名称（移除 "2" 后缀）
    /// </summary>
    public string NormalizeFlowerName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        if (name.EndsWith("2"))
            name = name.Substring(0, name.Length - 1);
        return name;
    }

    /// <summary>
    /// 归一化丝带名称（移除 "Ribbon" 前缀）
    /// </summary>
    public string NormalizeRibbonName(string name)
    {
        if (string.IsNullOrEmpty(name)) return "Unknown";
        if (name.StartsWith("Ribbon"))
            name = name.Substring(6);
        return name;
    }

    /// <summary>
    /// 订单生成时获取随机鲜花（从已修剪库存）
    /// </summary>
    /// <param name="count">需要的数量</param>
    /// <returns>鲜花名称列表</returns>
    public List<string> GetRandomFlowersFromTrimmedInventory(int count)
    {
        var result = new List<string>();
        var available = GetTrimmedFlowerKeys();

        if (available.Count == 0)
        {
            Debug.LogWarning("[GameManager] 已修剪鲜花库存为空");
            return result;
        }

        // 随机打乱并选择
        var shuffled = new List<string>(available);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            string temp = shuffled[i];
            shuffled[i] = shuffled[j];
            shuffled[j] = temp;
        }

        int actualCount = Mathf.Min(count, shuffled.Count);
        for (int i = 0; i < actualCount; i++)
        {
            result.Add(shuffled[i]);
        }

        return result;
    }

    /// <summary>
    /// 检查是否可以从库存生成订单所需的鲜花
    /// </summary>
    public bool CanGenerateFlowerOrder(List<string> requiredFlowers)
    {
        if (requiredFlowers == null) return false;
        foreach (var flower in requiredFlowers)
        {
            if (string.IsNullOrWhiteSpace(flower)) continue;
            if (GetTrimmedFlowerCount(flower) <= 0)
            {
                return false;
            }
        }
        return true;
    }

    // ---- 预留接口：花束库存 ----

    /// <summary>添加花束到库存（包装阶段完成后调用）</summary>
    /// <param name="bouquetName">花束名称</param>
    /// <param name="flowers">使用的修剪后花朵列表</param>
    /// <param name="ribbonName">使用的丝带</param>
    public void AddBouquet(string bouquetName, List<string> flowers, string ribbonName)
    {
        if (string.IsNullOrEmpty(bouquetName)) return;
        BouquetData bouquet = new BouquetData(bouquetName, flowers, ribbonName);
        bouquetInventory.Add(bouquet);
        Debug.Log($"[Inventory] 添加花束 {bouquet.GetDescription()}");
    }

    /// <summary>将已修剪鲜花包装成花束（从已修剪库存移到花束库存，丝带信息包含在花束中）</summary>
    /// <param name="flowerNames">花束中包含的鲜花名称列表</param>
    /// <param name="bouquetName">花束名称，如 "Rose_Bouquet"</param>
    /// <param name="ribbonName">使用的丝带名称（会记录在花束中）</param>
    /// <returns>是否包装成功</returns>
    public bool CreateBouquetFromTrimmed(List<string> flowerNames, string bouquetName, string ribbonName = null)
    {
        if (flowerNames == null || flowerNames.Count == 0)
        {
            Debug.LogWarning("[Inventory] 包装失败：花朵列表为空");
            return false;
        }

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

        // 添加花束（包含丝带信息）
        AddBouquet(bouquetName, flowerNames, ribbonName);
        Debug.Log($"[Inventory] 成功创建花束 {bouquetName}（消耗鲜花: {string.Join(", ", flowerNames)}，丝带: {ribbonName ?? "(无)"})");
        return true;
    }

    /// <summary>获取花束数量（同名花束）</summary>
    public int GetBouquetCount(string bouquetName)
    {
        if (string.IsNullOrWhiteSpace(bouquetName)) return 0;
        string key = NormalizeKey(bouquetName);
        return bouquetInventory.FindAll(b => NormalizeKey(b.bouquetName) == key).Count;
    }

    /// <summary>获取所有花束的详细信息列表</summary>
    public List<BouquetData> GetAllBouquets()
    {
        return new List<BouquetData>(bouquetInventory);
    }

    /// <summary>消耗指定花束（交付时调用，根据名称移除一个匹配的花束）</summary>
    public bool RemoveBouquet(string bouquetName)
    {
        return RemoveBouquet(bouquetName, 1);
    }

    /// <summary>消耗指定数量的花束</summary>
    public bool RemoveBouquet(string bouquetName, int count)
    {
        if (string.IsNullOrWhiteSpace(bouquetName)) return false;
        if (count <= 0) return false;
        
        string key = NormalizeKey(bouquetName);
        int removed = 0;
        
        for (int i = 0; i < count && bouquetInventory.Count > 0; i++)
        {
            int index = bouquetInventory.FindIndex(b => NormalizeKey(b.bouquetName) == key);
            if (index < 0) break;
            
            var bouquet = bouquetInventory[index];
            bouquetInventory.RemoveAt(index);
            removed++;
        }
        
        if (removed > 0)
        {
            Debug.Log($"[Inventory] 消耗花束 {bouquetName} x{removed}");
            return true;
        }
        
        Debug.LogWarning($"[Inventory] 移除花束失败：库存中没有 {bouquetName}");
        return false;
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

        Debug.Log($"[GameManager] 当前库存 - 未修剪鲜花: {GetUntrimmedInventoryDebugInfo()}, 已修剪鲜花: {GetTrimmedInventoryDebugInfo()}, 花束: {GetBouquetInventoryDebugInfo()}, 金币: {coins}");

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

    // ============================================

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

    // ---- Ribbon 已移除：Ribbon 信息现在存储在 BouquetData 内部 ----

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

    public int GetFlowerCount(string flowerKey)
    {
        string key = ResolveFlowerKey(flowerKey);
        return flowerInventory.ContainsKey(key) ? flowerInventory[key] : 0;
    }

    public List<string> GetAvailableFlowerKeys()
    {
        return flowerInventory.Keys.ToList();
    }

    public Dictionary<string, int> GetMissingFlowers(CustomerOrder order)
    {
        // 花束模式订单不需要检查鲜花
        if (order.useBouquetInventory)
            return new Dictionary<string, int>();
        
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
            int have = GetTrimmedFlowerCount(kvp.Key);
            if (have < kvp.Value)
                missing[kvp.Key] = kvp.Value - have;
        }

        return missing;
    }

    public Dictionary<string, int> GetMissingRibbons(CustomerOrder order)
    {
        // Ribbon 不再单独存储：
        // - 花束模式：Ribbon 在花束内部，不需要检查
        // - 普通模式：Ribbon 不再需要库存检查（玩家可直接使用）
        return new Dictionary<string, int>();
    }

    public bool HasEnoughForOrder(CustomerOrder order)
    {
        // 花束模式：只检查花束
        if (order.useBouquetInventory)
        {
            return HasEnoughBouquetsForOrder(order.GetBouquetNames());
        }
        
        // 普通模式：检查鲜花
        return GetMissingFlowers(order).Count == 0;
    }

    public void DeductOrderFlowers(CustomerOrder order)
    {
        // 花束模式：扣除花束（内部包含丝带）
        if (order.useBouquetInventory)
        {
            DeductBouquetOrder(order);
            return;
        }
        
        // 普通模式：扣除鲜花
        foreach (var name in order.GetFlowerNames())
        {
            if (string.IsNullOrWhiteSpace(name)) continue;
            RemoveTrimmedFlowers(name, 1);
        }
    }

    /// <summary>
    /// 扣除订单所需的丝带
    /// 注意：Ribbon 现在存储在 BouquetData 内部，花束交付时会一起扣除
    /// </summary>
    public void DeductOrderRibbons(CustomerOrder order)
    {
        // Ribbon 已移至 BouquetData 内部，花束交付时一起扣除
        if (order.useBouquetInventory)
            return;
        
        // 普通模式：不再需要单独扣除 Ribbon
        Debug.Log("[GameManager] 普通模式订单无需扣除 Ribbon");
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