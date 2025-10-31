using UnityEngine;

/// <summary>
/// 属性数据 - 定义单个 Stat 的配置
/// 
/// 【设计理念】：
/// - 配置层数据（ScriptableObject 中使用）
/// - 定义属性的基本信息
/// - 可序列化，可在 Inspector 配置
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 Stat 配置
/// - 但简化为更适合本项目的结构
/// </summary>
[System.Serializable]
public class StatData
{
    [Header("基本信息")]
    [Tooltip("属性ID（唯一标识符）")]
    public string statID = "Damage";
    
    [Tooltip("属性显示名称")]
    public string displayName = "攻击力";
    
    [Header("数值配置")]
    [Tooltip("基础值")]
    public float baseValue = 10f;
    
    [Header("可选配置")]
    [Tooltip("是否启用")]
    public bool isEnabled = true;
    
    [Tooltip("属性描述")]
    [TextArea(2, 3)]
    public string description = "";
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(statID) && isEnabled;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{statID}] {displayName} = {baseValue} (启用: {isEnabled})";
    }
}

