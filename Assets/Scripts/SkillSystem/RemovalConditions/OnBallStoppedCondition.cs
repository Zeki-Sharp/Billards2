using UnityEngine;

/// <summary>
/// 球停止时移除条件
/// </summary>
public class OnBallStoppedCondition : IRemovalCondition
{
    public string ConditionName => "OnBallStoppedCondition";

    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成");
    }

    public bool ShouldRemove(object eventData)
    {
        if (eventData is BallStoppedData ballStoppedData)
        {
            Debug.Log($"[{ConditionName}] 检测到球停止，应该移除效果");
            return true;
        }
        
        return false;
    }

    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置");
    }
}
