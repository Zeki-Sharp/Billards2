using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 重置条件配置基类 - 所有多态重置条件配置的抽象基类
/// 用于在Inspector中选择不同类型的重置条件
/// </summary>
[System.Serializable]
public abstract class ResetConditionBase
{
    /// <summary>
    /// 创建重置条件实例
    /// </summary>
    public abstract IResetCondition CreateResetCondition();

    /// <summary>
    /// 获取重置条件类型名称
    /// </summary>
    public virtual string GetResetConditionTypeName()
    {
        return GetType().Name.Replace("ResetConditionConfig", "").Replace("Config", "");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public abstract string GetDebugInfo();
}

