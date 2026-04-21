using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RibbonManager : MonoBehaviour
{
    public static RibbonManager Instance { get; private set; }

    public GameObject selectedRibbonPrefab;

    [Header("丝带库存")]
    [SerializeField] private SerializableDictionary<string, int> ribbonInventory = new SerializableDictionary<string, int>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ADD THIS
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

    // ============================================
    // 丝带库存接口
    // ============================================

    /// <summary>
    /// 添加丝带到库存
    /// </summary>
    public void AddRibbonToInventory(string ribbonName, int count = 1)
    {
        if (string.IsNullOrEmpty(ribbonName)) return;
        string key = NormalizeKey(ribbonName);
        if (ribbonInventory.ContainsKey(key))
            ribbonInventory[key] += count;
        else
            ribbonInventory[key] = count;
        Debug.Log($"[RibbonManager] 添加丝带 {key} x{count}，现有: {ribbonInventory[key]}");
    }

    /// <summary>
    /// 获取丝带库存数量
    /// </summary>
    public int GetRibbonCount(string ribbonName)
    {
        string key = NormalizeKey(ribbonName);
        return ribbonInventory.ContainsKey(key) ? ribbonInventory[key] : 0;
    }

    /// <summary>
    /// 检查是否有足够的丝带
    /// </summary>
    public bool HasEnoughRibbon(string ribbonName, int count = 1)
    {
        return GetRibbonCount(ribbonName) >= count;
    }

    /// <summary>
    /// 消耗丝带库存
    /// </summary>
    public void RemoveRibbon(string ribbonName, int count = 1)
    {
        string key = NormalizeKey(ribbonName);
        if (!ribbonInventory.ContainsKey(key)) return;
        ribbonInventory[key] -= count;
        if (ribbonInventory[key] <= 0) ribbonInventory.Remove(key);
        Debug.Log($"[RibbonManager] 消耗丝带 {key} x{count}");
    }

    /// <summary>
    /// 获取所有丝带类型
    /// </summary>
    public List<string> GetRibbonKeys()
    {
        return ribbonInventory.Keys.ToList();
    }

    /// <summary>
    /// 检查是否有任何丝带
    /// </summary>
    public bool HasAnyRibbons()
    {
        return ribbonInventory != null && ribbonInventory.Count > 0;
    }

    /// <summary>
    /// 随机获取一个库存中的丝带名称
    /// </summary>
    public string GetRandomRibbonFromInventory()
    {
        if (ribbonInventory == null || ribbonInventory.Count == 0) return null;
        var keys = ribbonInventory.Keys.ToList();
        return keys[Random.Range(0, keys.Count)];
    }

    /// <summary>
    /// 消耗并获取一个随机丝带（用于订单生成）
    /// </summary>
    public string ConsumeRandomRibbon()
    {
        if (ribbonInventory == null || ribbonInventory.Count == 0) return null;
        var keys = ribbonInventory.Keys.ToList();
        string ribbon = keys[Random.Range(0, keys.Count)];
        RemoveRibbon(ribbon, 1);
        return ribbon;
    }

    private string NormalizeKey(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Trim();
        const string clone = "(Clone)";
        if (name.EndsWith(clone))
            name = name.Substring(0, name.Length - clone.Length).TrimEnd();
        return name;
    }
}