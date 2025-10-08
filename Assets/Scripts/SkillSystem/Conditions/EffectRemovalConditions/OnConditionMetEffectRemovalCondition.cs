using UnityEngine;

/// <summary>
/// 条件满足时效果移除条件
/// 当指定的条件满足时移除效果
/// 适用于需要特定条件才能移除的效果
/// </summary>
public class OnConditionMetEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "OnConditionMetEffectRemovalCondition";
    
    private ICondition targetCondition;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="condition">目标条件</param>
    public OnConditionMetEffectRemovalCondition(ICondition condition)
    {
        targetCondition = condition;
    }

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 条件满足时移除效果: {targetCondition?.ConditionName}");
        targetCondition?.Initialize();
    }

    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        if (targetCondition == null)
        {
            Debug.LogWarning($"[{ConditionName}] 目标条件未设置");
            return false;
        }
        
        bool conditionMet = targetCondition.CheckCondition(eventData);
        if (conditionMet)
        {
            Debug.Log($"[{ConditionName}] 目标条件满足，移除效果");
        }
        
        return conditionMet;
    }

    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
        targetCondition?.Reset();
        Debug.Log($"[{ConditionName}] 状态重置 - 条件满足移除条件已重置");
    }
}
