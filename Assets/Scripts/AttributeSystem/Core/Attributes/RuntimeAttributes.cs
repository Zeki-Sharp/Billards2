using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时属性资源管理器 - 管理所有动态资源
/// 
/// 【设计理念】：
/// - 管理多个 RuntimeAttribute
/// - 提供统一的访问和修改接口
/// - 支持事件通知
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 RuntimeAttributes
/// 
/// 【典型应用】：
/// - 玩家生命值系统
/// - 敌人血量管理
/// - 能量/护盾等资源
/// </summary>
public class RuntimeAttributes
{
    #region 私有字段
    
    /// <summary>
    /// 所有运行时属性资源
    /// </summary>
    private Dictionary<string, RuntimeAttribute> attributes = new Dictionary<string, RuntimeAttribute>();
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    private bool enableDebugLog = false;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时属性资源管理器
    /// </summary>
    public RuntimeAttributes(bool enableDebugLog = false)
    {
        this.enableDebugLog = enableDebugLog;
    }
    
    #endregion
    
    #region 属性注册
    
    /// <summary>
    /// 注册属性资源
    /// </summary>
    public void RegisterAttribute(string attributeID, float minValue, float maxValue, float startValue)
    {
        if (attributes.ContainsKey(attributeID))
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 已存在，将覆盖");
        }
        
        attributes[attributeID] = new RuntimeAttribute(attributeID, minValue, maxValue, startValue);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeAttributes] 注册属性资源: {attributeID} = {startValue}/{maxValue}");
        }
    }
    
    /// <summary>
    /// 从配置注册
    /// </summary>
    public void RegisterAttribute(AttributeData data)
    {
        if (!data.IsValid())
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源配置无效: {data.attributeID}");
            return;
        }
        
        RegisterAttribute(data.attributeID, data.minValue, data.maxValue, data.GetStartValue());
    }
    
    /// <summary>
    /// 批量注册
    /// </summary>
    public void RegisterAttributes(AttributeList attributeList)
    {
        foreach (var data in attributeList.GetAllValidAttributes())
        {
            RegisterAttribute(data);
        }
    }
    
    #endregion
    
    #region 属性访问
    
    /// <summary>
    /// 获取当前值
    /// </summary>
    public float GetCurrentValue(string attributeID)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            return attribute.CurrentValue;
        }
        
        Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        return 0f;
    }
    
    /// <summary>
    /// 获取最大值
    /// </summary>
    public float GetMaxValue(string attributeID)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            return attribute.MaxValue;
        }
        
        Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        return 0f;
    }
    
    /// <summary>
    /// 获取百分比
    /// </summary>
    public float GetRatio(string attributeID)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            return attribute.Ratio;
        }
        
        Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        return 0f;
    }
    
    /// <summary>
    /// 获取 RuntimeAttribute 对象
    /// </summary>
    public RuntimeAttribute GetAttribute(string attributeID)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            return attribute;
        }
        
        return null;
    }
    
    #endregion
    
    #region 值修改
    
    /// <summary>
    /// 设置当前值
    /// </summary>
    public void SetValue(string attributeID, float value)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            attribute.CurrentValue = value;
        }
        else
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        }
    }
    
    /// <summary>
    /// 增加值
    /// </summary>
    public void Add(string attributeID, float amount)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            attribute.Add(amount);
        }
        else
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        }
    }
    
    /// <summary>
    /// 减少值
    /// </summary>
    public void Subtract(string attributeID, float amount)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            attribute.Subtract(amount);
        }
        else
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        }
    }
    
    /// <summary>
    /// 设置百分比
    /// </summary>
    public void SetPercent(string attributeID, float percent)
    {
        if (attributes.TryGetValue(attributeID, out var attribute))
        {
            attribute.SetPercent(percent);
        }
        else
        {
            Debug.LogWarning($"[RuntimeAttributes] 属性资源 {attributeID} 不存在");
        }
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 设置调试日志
    /// </summary>
    public void SetDebugLog(bool enable)
    {
        enableDebugLog = enable;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (attributes.Count == 0)
        {
            return "RuntimeAttributes: 无属性资源";
        }
        
        string info = $"RuntimeAttributes: 共 {attributes.Count} 个资源\n";
        foreach (var attr in attributes.Values)
        {
            info += $"  {attr.GetDebugInfo()}\n";
        }
        return info;
    }
    
    #endregion
    
    #region 序列化接口（跨场景持久化）
    
    /// <summary>
    /// 导出所有属性的当前值（用于跨场景保存）
    /// </summary>
    public Dictionary<string, float> ExportCurrentValues()
    {
        var snapshot = new Dictionary<string, float>();
        
        foreach (var kvp in attributes)
        {
            snapshot[kvp.Key] = kvp.Value.CurrentValue;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeAttributes] 📤 导出 {snapshot.Count} 个属性当前值");
        }
        
        return snapshot;
    }
    
    /// <summary>
    /// 恢复属性的当前值（用于跨场景恢复）
    /// </summary>
    public void RestoreCurrentValues(Dictionary<string, float> snapshot)
    {
        if (snapshot == null) return;
        
        int restoredCount = 0;
        
        foreach (var kvp in snapshot)
        {
            if (attributes.ContainsKey(kvp.Key))
            {
                attributes[kvp.Key].CurrentValue = kvp.Value;
                restoredCount++;
            }
            else if (enableDebugLog)
            {
                Debug.LogWarning($"[RuntimeAttributes] ⚠️ 属性 '{kvp.Key}' 不存在，无法恢复值 {kvp.Value}");
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeAttributes] 📥 恢复了 {restoredCount}/{snapshot.Count} 个属性的当前值");
        }
    }
    
    #endregion
}

