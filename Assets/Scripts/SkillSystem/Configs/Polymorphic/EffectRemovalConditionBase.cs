using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 效果移除条件配置基类 - 所有多态效果移除条件配置的抽象基类
/// 用于在Inspector中选择不同类型的效果移除条件
/// </summary>
[System.Serializable]
public abstract class EffectRemovalConditionBase
{
    /// <summary>
    /// 创建效果移除条件实例
    /// </summary>
    public abstract IEffectRemovalCondition CreateEffectRemovalCondition();

    /// <summary>
    /// 获取效果移除条件类型名称
    /// </summary>
    public virtual string GetEffectRemovalConditionTypeName()
    {
        return GetType().Name.Replace("EffectRemovalConditionConfig", "").Replace("Config", "");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public abstract string GetDebugInfo();
}

