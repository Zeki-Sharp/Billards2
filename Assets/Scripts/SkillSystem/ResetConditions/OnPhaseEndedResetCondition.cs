using UnityEngine;

/// <summary>
/// 回合结束重置条件
/// 当玩家回合结束时重置触发条件
/// 适用于需要在回合间重置的技能
/// </summary>
public class OnPhaseEndedResetCondition : IResetCondition
{
    public string ConditionName => "OnPhaseEndedResetCondition";

    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 回合结束时重置触发条件");
    }

    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该重置触发条件</returns>
    public bool ShouldReset(object eventData)
    {
        // 检查事件类型是否为回合结束事件
        if (eventData is GameFlowStateChangedData stateData)
        {
            bool shouldReset = stateData.NewState == GameFlowState.PlayerPhaseEnd;
            if (shouldReset)
            {
                Debug.Log($"[{ConditionName}] 检测到回合结束事件，应该重置触发条件");
            }
            return shouldReset;
        }
        
        // 检查是否为GameFlowState枚举类型
        if (eventData is GameFlowState gameFlowState)
        {
            bool shouldReset = gameFlowState == GameFlowState.PlayerPhaseEnd;
            if (shouldReset)
            {
                Debug.Log($"[{ConditionName}] 检测到回合结束状态，应该重置触发条件");
            }
            return shouldReset;
        }
        
        return false;
    }

    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 回合结束重置条件已重置");
    }
    
    /// <summary>
    /// 设置目标技能实例ID（用于事件过滤）
    /// </summary>
    /// <param name="skillInstanceId">技能实例ID</param>
    public void SetTargetSkillInstanceId(string skillInstanceId)
    {
        // 回合结束重置条件不需要过滤，因为回合结束事件是全局的
        Debug.Log($"[{ConditionName}] 设置目标技能实例ID: {skillInstanceId} (回合结束重置条件不需要过滤)");
    }
}
