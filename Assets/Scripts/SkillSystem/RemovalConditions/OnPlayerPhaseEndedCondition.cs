using UnityEngine;

/// <summary>
/// 玩家回合结束时移除条件
/// </summary>
public class OnPlayerPhaseEndedCondition : IRemovalCondition
{
    public string ConditionName => "OnPlayerPhaseEndedCondition";

    public void Initialize()
    {
    }

    public bool ShouldRemove(object eventData)
    {
        if (eventData is GameFlowStateChangedData stateData)
        {
            return stateData.NewState == GameFlowState.PlayerPhaseEnd;
        }
        
        return false;
    }

    public void Reset()
    {
    }
}
