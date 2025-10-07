using UnityEngine;

/// <summary>
/// 持续时间移除条件（暂未实现）
/// </summary>
public class DurationCondition : IRemovalCondition
{
    public string ConditionName => "DurationCondition";

    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成（暂未实现）");
    }

    public bool ShouldRemove(object eventData)
    {
        // 暂未实现
        Debug.LogWarning($"[{ConditionName}] 持续时间移除条件暂未实现");
        return false;
    }

    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置");
    }
}
