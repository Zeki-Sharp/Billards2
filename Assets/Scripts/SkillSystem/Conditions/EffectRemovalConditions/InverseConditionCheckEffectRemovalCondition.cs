using UnityEngine;

/// <summary>
/// 反向条件检查效果移除条件
/// 当指定的条件不满足时移除效果
/// 这是旧系统 InverseConditionCheck 的新版本，用于效果移除
/// </summary>
public class InverseConditionCheckEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "InverseConditionCheckEffectRemovalCondition";
    
    private ICondition originalCondition;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="condition">原始条件</param>
    public InverseConditionCheckEffectRemovalCondition(ICondition condition)
    {
        originalCondition = condition;
    }

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 反向条件检查移除效果: {originalCondition?.ConditionName}");
        originalCondition?.Initialize();
    }

    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        if (originalCondition == null)
        {
            Debug.LogWarning($"[{ConditionName}] 原始条件未设置");
            return false;
        }
        
        // 当原始条件不满足时移除效果
        bool originalConditionMet = originalCondition.CheckCondition(eventData);
        bool shouldRemove = !originalConditionMet;
        
        if (shouldRemove)
        {
            Debug.Log($"[{ConditionName}] 原始条件不满足，移除效果: {originalCondition.ConditionName}");
        }
        
        return shouldRemove;
    }

    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
        originalCondition?.Reset();
        Debug.Log($"[{ConditionName}] 状态重置 - 反向条件检查移除条件已重置");
    }
}
