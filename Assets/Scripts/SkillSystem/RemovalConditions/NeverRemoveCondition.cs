using UnityEngine;

/// <summary>
/// 永不移除条件
/// </summary>
public class NeverRemoveCondition : IRemovalCondition
{
    public string ConditionName => "NeverRemoveCondition";

    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成");
    }

    public bool ShouldRemove(object eventData)
    {
        // 永不移除
        return false;
    }

    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置");
    }
}
