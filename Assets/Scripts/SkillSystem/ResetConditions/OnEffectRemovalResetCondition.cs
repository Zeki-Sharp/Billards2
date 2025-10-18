using UnityEngine;

/// <summary>
/// 效果移除时重置条件
/// 当效果移除条件满足时，重置触发条件和效果状态
/// 用于实现"条件不满足时移除效果，条件重新满足时重新应用效果"的持续监控技能
/// </summary>
public class OnEffectRemovalResetCondition : IResetCondition
{
    public string ConditionName => "OnEffectRemovalResetCondition";

    /// <summary>
    /// 构造函数
    /// </summary>
    public OnEffectRemovalResetCondition()
    {
    }

    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 效果移除时重置");
    }

    /// <summary>
    /// 检查是否应该重置触发条件
    /// 这个条件需要配合 SkillInstance 中的移除条件检查使用
    /// 当移除条件满足时，SkillInstance 会调用 Reset
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>在 ProcessEvent 中总是返回 false，因为重置由移除条件触发</returns>
    public bool ShouldReset(object eventData)
    {
        // 在正常的 ProcessEvent 流程中不主动重置
        // 重置由 SkillInstance 检测到移除条件满足时触发
        return false;
    }

    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 效果移除重置条件无需特殊重置");
    }
}

