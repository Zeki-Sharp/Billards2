using UnityEngine;

/// <summary>
/// Property 动态值获取器 - Float 类型
/// 
/// 【设计理念】：
/// - 抽象基类，定义值获取接口
/// - 支持多态配置（SerializeReference）
/// - 允许多种值提供方式
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 PropertyGetDecimal/PropertyGetInteger
/// - 支持固定值、随机值、基于属性等
/// 
/// 【典型应用】：
/// - 技能伤害值（可以是固定值或基于攻击力）
/// - 治疗量（可以是固定值或最大血量百分比）
/// - 持续时间（可以是固定值或随机范围）
/// </summary>
[System.Serializable]
public abstract class PropertyGetFloat
{
    /// <summary>
    /// 获取值（核心方法）
    /// </summary>
    /// <param name="args">事件参数（提供上下文信息）</param>
    /// <returns>计算后的值</returns>
    public abstract float Get(SkillArgs args);
    
    /// <summary>
    /// 获取值（无参数版本，使用默认 args）
    /// </summary>
    public virtual float Get()
    {
        return Get(null);
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public virtual string GetDebugInfo()
    {
        return $"{GetType().Name}";
    }
}

