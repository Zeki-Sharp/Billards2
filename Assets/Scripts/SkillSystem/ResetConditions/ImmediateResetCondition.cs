using UnityEngine;

/// <summary>
/// 立即重置条件
/// 技能执行后立即重置触发条件，让技能可以再次被触发
/// </summary>
public class ImmediateResetCondition : IResetCondition
{
    public string ConditionName => "ImmediateResetCondition";

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImmediateResetCondition()
    {
    }

    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 立即重置触发条件");
    }

    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回true，表示应该立即重置触发条件</returns>
    public bool ShouldReset(object eventData)
    {
        Debug.Log($"[{ConditionName}] 检查重置条件: 立即重置");
        return true;
    }

    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 立即重置条件无需特殊重置");
    }
}
