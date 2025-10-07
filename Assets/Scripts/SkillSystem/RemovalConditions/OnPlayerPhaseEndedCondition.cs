using UnityEngine;

/// <summary>
/// 玩家回合结束时移除条件
/// </summary>
public class OnPlayerPhaseEndedCondition : IRemovalCondition
{
    public string ConditionName => "OnPlayerPhaseEndedCondition";

    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成");
    }

    public bool ShouldRemove(object eventData)
    {
        if (eventData is GameFlowStateChangedData stateData)
        {
            bool shouldRemove = stateData.NewState == GameFlowState.PlayerPhaseEnd;
            if (shouldRemove)
            {
                Debug.Log($"[{ConditionName}] 检测到玩家回合结束，应该移除效果");
            }
            return shouldRemove;
        }
        
        return false;
    }

    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置");
    }
}
