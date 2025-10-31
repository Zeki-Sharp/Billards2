using UnityEngine;

/// <summary>
/// 回合结束效果移除条件
/// 当玩家回合结束时移除效果
/// 适用于需要在回合间清除的效果
/// </summary>
public class OnPhaseEndedEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "OnPhaseEndedEffectRemovalCondition";

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
    }

    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(SkillArgs args)
    {
        // 检查事件类型是否为回合结束事件
        if (args.TryGetEventData<GameFlowStateChangedData>(out var stateData))
        {
            bool shouldRemove = stateData.NewState == GameFlowState.PlayerPhaseEnd;
            if (shouldRemove)
            {
                Debug.Log($"[{ConditionName}] 检测到玩家回合结束事件，应该移除效果");
            }
            return shouldRemove;
        }
        
        // 检查是否为GameFlowState枚举类型
        if (args.EventData is GameFlowState gameFlowState)
        {
            bool shouldRemove = gameFlowState == GameFlowState.PlayerPhaseEnd;
            if (shouldRemove)
            {
                Debug.Log($"[{ConditionName}] 检测到玩家回合结束状态，应该移除效果");
            }
            return shouldRemove;
        }
        
        return false;
    }

    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
    }
}
