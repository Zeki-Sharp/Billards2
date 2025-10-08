using UnityEngine;

/// <summary>
/// 永不复位重置条件
/// 技能触发后永远不会重置触发条件
/// 适用于一次性技能（如永久属性提升）
/// </summary>
public class NeverResetCondition : IResetCondition
{
    public string ConditionName => "NeverResetCondition";

    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 永不复位触发条件");
    }

    /// <summary>
    /// 检查是否应该重置触发条件 - 总是返回false
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>总是返回false，表示永不复位触发条件</returns>
    public bool ShouldReset(object eventData)
    {
        Debug.Log($"[{ConditionName}] 检查重置条件: 永不复位");
        return false;
    }

    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 永不复位条件无需重置");
    }
}
