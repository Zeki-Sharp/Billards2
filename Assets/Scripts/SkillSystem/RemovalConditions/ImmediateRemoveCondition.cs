using UnityEngine;

/// <summary>
/// 立即移除条件 - 用于ActionEffect（瞬时效果）
/// 当技能执行后立即返回true，触发条件重置，让技能可以再次触发
/// 主要用于治疗、伤害、传送等一次性效果
/// </summary>
public class ImmediateRemoveCondition : IRemovalCondition
{
    public string ConditionName => "ImmediateRemoveCondition";

    /// <summary>
    /// 初始化移除条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 立即移除效果，重置触发条件");
    }

    /// <summary>
    /// 检查是否应该移除 - 总是返回true
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回true，表示应该立即移除</returns>
    public bool ShouldRemove(object eventData)
    {
        // 立即返回true，表示应该移除（重置触发条件）
        Debug.Log($"[{ConditionName}] 检查移除条件: 立即移除");
        return true;
    }

    /// <summary>
    /// 重置移除条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 立即移除条件无需特殊重置");
    }
}
