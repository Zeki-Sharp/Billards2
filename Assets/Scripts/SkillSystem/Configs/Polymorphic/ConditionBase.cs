using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 条件配置基类 - 所有多态条件配置的抽象基类
/// 用于在Inspector中选择不同类型的条件
/// </summary>
[System.Serializable]
[InlineProperty]
[HideLabel]
public abstract class ConditionBase
{
    /// <summary>
    /// 创建条件实例
    /// </summary>
    public abstract ICondition CreateCondition();

    /// <summary>
    /// 获取条件类型名称
    /// </summary>
    public virtual string GetConditionTypeName()
    {
        return GetType().Name.Replace("ConditionConfig", "").Replace("Config", "");
    }

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public abstract string GetDebugInfo();
}

