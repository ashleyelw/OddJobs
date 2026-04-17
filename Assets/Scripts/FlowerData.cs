using UnityEngine;

/// <summary>
/// 鲜花修剪状态
/// </summary>
public enum FlowerTrimState
{
    /// <summary>未修剪状态 - 从花园采摘后</summary>
    Untrimmed,
    
    /// <summary>已修剪状态 - 修剪阶段完成后</summary>
    Trimmed
}

public class FlowerData : MonoBehaviour
{
    public GameObject prefabReference;
    
    [Header("修剪状态")]
    [SerializeField] private FlowerTrimState trimState = FlowerTrimState.Untrimmed;
    
    /// <summary>
    /// 当前修剪状态
    /// </summary>
    public FlowerTrimState TrimState
    {
        get => trimState;
        set => trimState = value;
    }
    
    /// <summary>
    /// 是否已修剪
    /// </summary>
    public bool IsTrimmed => trimState == FlowerTrimState.Trimmed;
    
    /// <summary>
    /// 标记为已修剪
    /// </summary>
    public void MarkAsTrimmed()
    {
        trimState = FlowerTrimState.Trimmed;
        Debug.Log($"[FlowerData] {gameObject.name} 已标记为已修剪状态");
    }
}
