using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 触发器配置抽象基类
/// 使用 SerializeReference 实现多态配置，消除 enum + switch 模式
/// </summary>
[System.Serializable]
public abstract class TriggerBase
{
    /// <summary>
    /// 创建触发器实例
    /// </summary>
    public abstract ITrigger CreateTrigger();
    
    /// <summary>
    /// 获取触发器类型名称（用于 Inspector 显示）
    /// </summary>
    public virtual string GetTriggerTypeName()
    {
        return GetType().Name.Replace("TriggerConfig", "").Replace("Config", "");
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public virtual string GetDebugInfo()
    {
        return GetTriggerTypeName();
    }
}

