using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 效果配置基类 - 所有多态效果配置的抽象基类
/// 用于在Inspector中选择不同类型的效果
/// </summary>
[System.Serializable]
public abstract class EffectBase
{
    /// <summary>
    /// 创建效果实例
    /// </summary>
    /// <param name="effectRemovalCondition">效果移除条件（可选）</param>
    public abstract IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null);

    /// <summary>
    /// 获取效果类型名称
    /// </summary>
    public virtual string GetEffectTypeName()
    {
        return GetType().Name.Replace("EffectConfig", "").Replace("Config", "");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public abstract string GetDebugInfo();
}

