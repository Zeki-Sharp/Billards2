using UnityEngine;

/// <summary>
/// 回合结束效果移除条件
/// 当玩家回合结束时移除效果
/// 适用于需要在回合间清除的效果
/// </summary>
public class OnPhaseEndedEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "OnPhaseEndedEffectRemovalCondition";
    
    private bool isPlayerPhaseEnded = false;

    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 回合结束时移除效果");
        // TODO: 订阅回合结束事件
        // GameEventBus.Subscribe<PlayerPhaseEndedEvent>(OnPlayerPhaseEnded);
    }

    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        // 检查事件类型是否为回合结束事件
        if (eventData is GameFlowStateChangedData stateData)
        {
            bool shouldRemove = stateData.NewState == GameFlowState.PlayerPhaseEnd;
            if (shouldRemove)
            {
                Debug.Log($"[{ConditionName}] 检测到玩家回合结束事件，应该移除效果");
            }
            return shouldRemove;
        }
        
        // 检查是否为GameFlowState枚举类型
        if (eventData is GameFlowState gameFlowState)
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
        isPlayerPhaseEnded = false;
        Debug.Log($"[{ConditionName}] 状态重置 - 回合结束移除条件已重置");
    }
}
