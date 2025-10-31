using UnityEngine;

/// <summary>
/// 属性资源数据 - 定义单个 Attribute 的配置
/// 
/// 【设计理念】：
/// - Attribute 是有上下限的动态资源（如血量、能量）
/// - 与 Stat 不同，Attribute 有当前值/最大值的概念
/// - 支持百分比初始化（StartPercent）
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 Attribute 设计
/// - MinValue、MaxValue 可以引用 Stat（本项目简化为固定值）
/// 
/// 【典型应用】：
/// - 生命值（CurrentHealth / MaxHealth）
/// - 能量值（CurrentMana / MaxMana）
/// - 护盾值（CurrentShield / MaxShield）
/// </summary>
[System.Serializable]
public class AttributeData
{
    [Header("基本信息")]
    [Tooltip("属性ID（唯一标识符）")]
    public string attributeID = "Health";
    
    [Tooltip("属性显示名称")]
    public string displayName = "生命值";
    
    [Header("数值范围")]
    [Tooltip("最小值")]
    public float minValue = 0f;
    
    [Tooltip("最大值")]
    public float maxValue = 100f;
    
    [Header("初始化")]
    [Tooltip("初始百分比（0-1）")]
    [Range(0f, 1f)]
    public float startPercent = 1f; // 默认满值
    
    [Header("可选配置")]
    [Tooltip("是否启用")]
    public bool isEnabled = true;
    
    [Tooltip("属性描述")]
    [TextArea(2, 3)]
    public string description = "";
    
    /// <summary>
    /// 计算初始值
    /// </summary>
    public float GetStartValue()
    {
        return Mathf.Lerp(minValue, maxValue, startPercent);
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrEmpty(attributeID) && 
               isEnabled && 
               maxValue >= minValue;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{attributeID}] {displayName} = {GetStartValue():F1}/{maxValue:F1} " +
               $"({startPercent * 100:F0}%, 范围: {minValue}-{maxValue})";
    }
}

