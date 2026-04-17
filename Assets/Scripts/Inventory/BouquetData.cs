using System;
using System.Collections.Generic;

/// <summary>
/// 花束数据结构
/// 包含：花束ID、使用的修剪后花朵列表、使用的Ribbon信息
/// </summary>
[Serializable]
public class BouquetData
{
    /// <summary>花束唯一ID（用于追踪）</summary>
    public string bouquetId;
    
    /// <summary>花束显示名称（如 "Rose_Bouquet"）</summary>
    public string bouquetName;
    
    /// <summary>使用的修剪后花朵名称列表</summary>
    public List<string> trimmedFlowers = new List<string>();
    
    /// <summary>使用的 Ribbon 名称</summary>
    public string ribbonName;
    
    /// <summary>创建时间戳</summary>
    public long createdAt;
    
    public BouquetData()
    {
        bouquetId = Guid.NewGuid().ToString();
        createdAt = DateTime.Now.Ticks;
    }
    
    /// <summary>
    /// 创建花束
    /// </summary>
    /// <param name="name">花束名称</param>
    /// <param name="flowers">使用的修剪后花朵列表</param>
    /// <param name="ribbon">使用的丝带</param>
    public BouquetData(string name, List<string> flowers, string ribbon)
    {
        bouquetId = Guid.NewGuid().ToString();
        bouquetName = name;
        trimmedFlowers = flowers != null ? new List<string>(flowers) : new List<string>();
        ribbonName = ribbon;
        createdAt = DateTime.Now.Ticks;
    }
    
    /// <summary>
    /// 获取花束描述
    /// </summary>
    public string GetDescription()
    {
        string flowers = trimmedFlowers != null && trimmedFlowers.Count > 0 
            ? string.Join(", ", trimmedFlowers) 
            : "(无花朵)";
        string ribbon = !string.IsNullOrEmpty(ribbonName) ? ribbonName : "(无丝带)";
        return $"{bouquetName}: {flowers} + {ribbon}";
    }
    
    /// <summary>
    /// 获取花束的简洁描述（用于库存显示）
    /// </summary>
    public string GetSimpleDescription()
    {
        if (!string.IsNullOrEmpty(bouquetName))
        {
            // 显示花束名称和丝带
            string flowers = trimmedFlowers != null && trimmedFlowers.Count > 0 
                ? string.Join(",", trimmedFlowers) 
                : "empty";
            string ribbon = !string.IsNullOrEmpty(ribbonName) ? ribbonName : "no_ribbon";
            return $"{bouquetName}[{flowers}+{ribbon}]";
        }
        return $"Bouquet_{bouquetId.Substring(0, 8)}";
    }
    
    /// <summary>
    /// 获取花束的简短显示名（仅名称，无详细信息）
    /// </summary>
    public string GetDisplayName()
    {
        return !string.IsNullOrEmpty(bouquetName) ? bouquetName : $"Bouquet_{bouquetId.Substring(0, 8)}";
    }
}
