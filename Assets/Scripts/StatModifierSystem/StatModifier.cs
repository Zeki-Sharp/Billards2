using UnityEngine;

/// <summary>
/// 属性修饰器 - 存储单个属性修改的所有信息
/// 轻量级数据结构，支持序列化和调试
/// </summary>
[System.Serializable]
public class StatModifier
{
    [Header("基本配置")]
    public string targetStat;              // 目标属性名称 (如 "Damage", "MaxHealth")
    public StatModifierType type;          // 修改类型
    public float value;                    // 修改值
    
    [Header("效果管理")]
    public object source;                  // 效果来源 (哪个技能、Buff等)
    public float duration;                 // 持续时间 (0表示永久，>0表示有时限)
    public float timeRemaining;            // 剩余时间
    public bool isTemporary => duration > 0;  // 是否是临时效果
    
    [Header("移除条件")]
    [System.NonSerialized]
    public IRemovalCondition removalCondition; // 移除条件系统
    public string customCondition;             // 自定义条件描述
    
    /// <summary>
    /// 构造函数 - 创建永久修饰器
    /// </summary>
    public StatModifier(string targetStat, StatModifierType type, float value, object source = null)
    {
        this.targetStat = targetStat;
        this.type = type;
        this.value = value;
        this.source = source;
        this.duration = 0f;  // 永久
        this.timeRemaining = 0f;
        this.removalCondition = null; // 永不移除
        this.customCondition = "";
    }
    
    /// <summary>
    /// 构造函数 - 创建临时修饰器
    /// </summary>
    public StatModifier(string targetStat, StatModifierType type, float value, float duration, object source = null)
    {
        this.targetStat = targetStat;
        this.type = type;
        this.value = value;
        this.source = source;
        this.duration = duration;
        this.timeRemaining = duration;
        this.removalCondition = null; // 时间到期自动移除
        this.customCondition = "";
    }
    
    /// <summary>
    /// 构造函数 - 创建基于条件的修饰器
    /// </summary>
    public StatModifier(string targetStat, StatModifierType type, float value, IRemovalCondition condition, object source = null)
    {
        this.targetStat = targetStat;
        this.type = type;
        this.value = value;
        this.source = source;
        this.duration = 0f;  // 永久，等待条件触发
        this.timeRemaining = 0f;
        this.removalCondition = condition;
        this.customCondition = "";
    }
    
    /// <summary>
    /// 设置移除条件
    /// </summary>
    /// <param name="condition">移除条件</param>
    public void SetRemovalCondition(IRemovalCondition condition)
    {
        removalCondition = condition;
    }
    
    /// <summary>
    /// 检查是否时间到期
    /// </summary>
    public bool IsTimeExpired()
    {
        return duration > 0 && timeRemaining <= 0f;
    }
    
    /// <summary>
    /// 检查是否应该被移除
    /// </summary>
    public bool ShouldBeRemoved(object eventData = null)
    {
        // 时间到期检查
        if (duration > 0 && timeRemaining <= 0f)
        {
            return true;
        }
        
        // 基于条件的移除检查
        if (removalCondition != null)
        {
            return removalCondition.ShouldRemove(eventData);
        }
        
        return false;
    }
    
    /// <summary>
    /// 更新剩余时间
    /// </summary>
    public void UpdateTime(float deltaTime)
    {
        if (isTemporary)
        {
            timeRemaining -= deltaTime;
            timeRemaining = Mathf.Max(0f, timeRemaining);
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{targetStat}] {type} {value} (来源: {source?.GetType().Name ?? "Unknown"}, 条件: {removalCondition})";
    }
}

/// <summary>
/// 修饰器类型枚举
/// </summary>
public enum StatModifierType
{
    Add,            // 基础值 + Value
    PercentAdd,     // 基础值 * (1 + Value)
    PercentMult     // 最终值 * Value
}

